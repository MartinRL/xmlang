using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Emlang.Linting;

/// <summary>
/// Line/column-aware AST over emlang YAML, ported from the Go reference parser
/// (github.com/emlang-project/emlang internal/parser + internal/ast) and extended with the
/// dialect's kinds: `s:` state (RFC emlang-0002) and `a:`/`auto:` initiators (RFC emlang-0005).
/// Slices in document order, per-YAML-document sub-docs (--- separated), swimlane split at
/// the first '/', GWT test sections with type validation. This is the surface `em lint`/`em parse` operate on; EmParser
/// and SpecModel stay the resolution/codegen surfaces.
/// </summary>
public enum EmElementType { Trigger, Command, Event, Rejection, View, State, Actor, Automation }

public static class EmElementTypes
{
    public static string Display(this EmElementType type) => type switch
    {
        EmElementType.Trigger => "trigger",
        EmElementType.Command => "command",
        EmElementType.Event => "event",
        EmElementType.Rejection => "rejection",
        EmElementType.View => "view",
        EmElementType.State => "state",
        EmElementType.Actor => "actor",
        EmElementType.Automation => "automation",
        _ => "unknown",
    };

    /// <summary>Initiators (RFC emlang-0005): actors and automations, plus the legacy trigger.</summary>
    public static bool IsInitiator(this EmElementType type) =>
        type is EmElementType.Trigger or EmElementType.Actor or EmElementType.Automation;
}

/// <summary>Name conventions shared by the linter, the formatter and xmlang (RFC emlang-0004
/// role normalization; RFC emlang-0005 legacy-trigger classification).</summary>
public static class EmNames
{
    /// <summary>Strip leading non-letter/digit characters (emoji, joiners, whitespace), trim, lowercase.</summary>
    public static string NormalizeRole(string swimlane)
    {
        var i = 0;
        while (i < swimlane.Length && !char.IsLetterOrDigit(swimlane[i]))
            i++;
        return swimlane.Substring(i).Trim().ToLowerInvariant();
    }

    /// <summary>The `em fmt` migration heuristic for a legacy `t:`: System, or a gear emoji, is an automation.</summary>
    public static bool IsAutomationHeuristic(string swimlane) =>
        NormalizeRole(swimlane) == "system" || swimlane.TrimStart().StartsWith("⚙", StringComparison.Ordinal);

    /// <summary>An initiator's kind, reading legacy triggers through the heuristic.</summary>
    public static bool IsAutomation(EmElement initiator) => initiator.Type switch
    {
        EmElementType.Automation => true,
        EmElementType.Actor => false,
        _ => IsAutomationHeuristic(initiator.Swimlane),
    };
}

/// <summary>Prop values are string (scalar), IReadOnlyList&lt;object?&gt; (sequence) or
/// IReadOnlyList&lt;KeyValuePair&lt;string, object?&gt;&gt; (mapping), in document order.</summary>
public sealed record EmElement(
    EmElementType Type,
    string Name,
    string Swimlane,
    IReadOnlyList<KeyValuePair<string, object?>> Props,
    int Line,
    int Column);

/// <summary>The Has* flags record which section keys were present in the source
/// (the Go AST's HasGiven/HasWhen/HasThen), so fmt can round-trip empty sections.</summary>
public sealed record EmTest(
    string Name,
    IReadOnlyList<EmElement> Given,
    IReadOnlyList<EmElement> When,
    IReadOnlyList<EmElement> Then,
    bool HasGiven = false,
    bool HasWhen = false,
    bool HasThen = false);

public sealed record EmSlice(
    string Name,
    IReadOnlyList<EmElement> Elements,
    IReadOnlyList<EmTest> Tests);

public sealed record EmSubDoc(IReadOnlyList<EmSlice> Slices);

public sealed record EmDocument(IReadOnlyList<EmSubDoc> SubDocs)
{
    /// <summary>Distinct slice names across all sub-documents (later docs shadow earlier
    /// ones by name, mirroring the Go parser's merged map).</summary>
    public int SliceCount =>
        SubDocs.SelectMany(d => d.Slices).Select(s => s.Name).Distinct().Count();
}

public static class EmAst
{
    private static readonly Dictionary<string, EmElementType> Prefixes = new()
    {
        ["t"] = EmElementType.Trigger,
        ["trg"] = EmElementType.Trigger,
        ["trigger"] = EmElementType.Trigger,
        ["c"] = EmElementType.Command,
        ["cmd"] = EmElementType.Command,
        ["command"] = EmElementType.Command,
        ["e"] = EmElementType.Event,
        ["evt"] = EmElementType.Event,
        ["event"] = EmElementType.Event,
        ["x"] = EmElementType.Rejection,
        ["rej"] = EmElementType.Rejection,
        ["rejection"] = EmElementType.Rejection,
        ["v"] = EmElementType.View,
        ["view"] = EmElementType.View,
        ["s"] = EmElementType.State,
        ["st"] = EmElementType.State,
        ["state"] = EmElementType.State,
        ["a"] = EmElementType.Actor,
        ["actor"] = EmElementType.Actor,
        ["auto"] = EmElementType.Automation,
        ["automation"] = EmElementType.Automation,
    };

    /// <summary>Parses emlang YAML; throws <see cref="FormatException"/> with the Go
    /// parser's error wording on structural violations.</summary>
    public static EmDocument Parse(string yaml)
    {
        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(yaml));
        }
        catch (YamlException ex)
        {
            throw new FormatException("yaml parse error: " + ex.Message, ex);
        }

        var subDocs = new List<EmSubDoc>();
        foreach (var document in stream.Documents)
            subDocs.Add(new EmSubDoc(ParseDocument(document.RootNode)));

        return new EmDocument(subDocs);
    }

    private static IReadOnlyList<EmSlice> ParseDocument(YamlNode root)
    {
        if (IsNull(root))
            return [];
        if (root is not YamlMappingNode mapping)
            throw new FormatException($"expected mapping at root, got {root.NodeType}");

        IReadOnlyList<EmSlice> slices = [];
        foreach (var entry in mapping.Children)
        {
            var key = KeyOf(entry.Key);
            if (key == "slices")
                slices = ParseSlices(entry.Value);
            else
                throw new FormatException($"unknown top-level key \"{key}\" at line {LineOf(entry.Key)}");
        }

        return slices;
    }

    private static IReadOnlyList<EmSlice> ParseSlices(YamlNode node)
    {
        if (IsNull(node))
            return [];
        if (node is not YamlMappingNode mapping)
            throw new FormatException($"slices must be a mapping at line {LineOf(node)}");

        var slices = new List<EmSlice>();
        foreach (var entry in mapping.Children)
        {
            var name = KeyOf(entry.Key);
            try
            {
                slices.Add(ParseSlice(name, entry.Value));
            }
            catch (FormatException ex)
            {
                throw new FormatException($"slice \"{name}\": {ex.Message}", ex);
            }
        }

        return slices;
    }

    private static EmSlice ParseSlice(string name, YamlNode node)
    {
        if (IsNull(node))
            return new EmSlice(name, [], []);

        switch (node)
        {
            case YamlSequenceNode sequence:
                var elements = ParseElementList(sequence);
                if (elements.Count == 0)
                    throw new FormatException($"slice must have at least one element at line {LineOf(node)}");
                return new EmSlice(name, elements, []);

            case YamlMappingNode mapping:
                IReadOnlyList<EmElement>? steps = null;
                IReadOnlyList<EmTest> tests = [];
                foreach (var entry in mapping.Children)
                {
                    var key = KeyOf(entry.Key);
                    switch (key)
                    {
                        case "steps":
                            if (IsNull(entry.Value))
                            {
                                steps = [];
                            }
                            else
                            {
                                steps = Wrap("steps", () => ParseElementList(entry.Value));
                                if (steps.Count == 0)
                                    throw new FormatException(
                                        $"steps must have at least one element at line {LineOf(entry.Value)}");
                            }
                            break;
                        case "tests":
                            tests = Wrap("tests", () => ParseTests(entry.Value));
                            break;
                        default:
                            throw new FormatException($"unknown slice key \"{key}\" at line {LineOf(entry.Key)}");
                    }
                }

                if (steps is null)
                    throw new FormatException($"extended slice must have 'steps' at line {LineOf(node)}");
                return new EmSlice(name, steps, tests);

            default:
                throw new FormatException($"slice must be a sequence or mapping at line {LineOf(node)}");
        }
    }

    private static IReadOnlyList<EmTest> ParseTests(YamlNode node)
    {
        if (IsNull(node))
            return [];
        if (node is not YamlMappingNode mapping)
            throw new FormatException($"tests must be a mapping at line {LineOf(node)}");

        var tests = new List<EmTest>();
        foreach (var entry in mapping.Children)
        {
            var name = KeyOf(entry.Key);
            try
            {
                tests.Add(ParseTest(name, entry.Value));
            }
            catch (FormatException ex)
            {
                throw new FormatException($"test \"{name}\": {ex.Message}", ex);
            }
        }

        return tests;
    }

    private static readonly EmElementType[] AllowedGiven =
        [EmElementType.Event, EmElementType.View, EmElementType.State];
    private static readonly EmElementType[] AllowedWhen = [EmElementType.Command];
    private static readonly EmElementType[] AllowedThen =
        [EmElementType.Event, EmElementType.View, EmElementType.Rejection, EmElementType.State];

    private static EmTest ParseTest(string name, YamlNode node)
    {
        if (IsNull(node))
            return new EmTest(name, [], [], []);
        if (node is not YamlMappingNode mapping)
            throw new FormatException($"test must be a mapping at line {LineOf(node)}");

        IReadOnlyList<EmElement> given = [], when = [], then = [];
        bool hasGiven = false, hasWhen = false, hasThen = false;
        foreach (var entry in mapping.Children)
        {
            var key = KeyOf(entry.Key);
            switch (key)
            {
                case "given":
                    given = ParseTestSection("given", entry.Value, AllowedGiven);
                    hasGiven = true;
                    break;
                case "when":
                    when = ParseTestSection("when", entry.Value, AllowedWhen);
                    hasWhen = true;
                    break;
                case "then":
                    then = ParseTestSection("then", entry.Value, AllowedThen);
                    hasThen = true;
                    break;
                default:
                    throw new FormatException($"unknown test key \"{key}\" at line {LineOf(entry.Key)}");
            }
        }

        return new EmTest(name, given, when, then, hasGiven, hasWhen, hasThen);
    }

    private static IReadOnlyList<EmElement> ParseTestSection(
        string section, YamlNode node, EmElementType[] allowed)
    {
        if (IsNull(node))
            return [];
        var elements = Wrap(section, () => ParseElementList(node));
        foreach (var element in elements)
        {
            if (Array.IndexOf(allowed, element.Type) < 0)
                throw new FormatException(
                    $"{section}: {element.Type.Display()} not allowed at line {element.Line}");
        }

        return elements;
    }

    private static IReadOnlyList<EmElement> ParseElementList(YamlNode node)
    {
        if (node is not YamlSequenceNode sequence)
            throw new FormatException($"expected sequence at line {LineOf(node)}");

        var elements = new List<EmElement>();
        foreach (var item in sequence.Children)
            elements.Add(ParseElement(item));

        return elements;
    }

    private static EmElement ParseElement(YamlNode node)
    {
        if (node is not YamlMappingNode mapping)
            throw new FormatException($"element must be a mapping at line {LineOf(node)}");

        var line = LineOf(node);
        var column = ColumnOf(node);
        EmElementType? type = null;
        var name = "";
        var swimlane = "";
        IReadOnlyList<KeyValuePair<string, object?>> props = [];

        foreach (var entry in mapping.Children)
        {
            var key = KeyOf(entry.Key);
            var keyLine = LineOf(entry.Key);

            if (key == "props")
            {
                props = Wrap($"props at line {LineOf(entry.Value)}", () => ParseProps(entry.Value));
                continue;
            }

            if (Prefixes.TryGetValue(key, out var elementType))
            {
                if (type is not null)
                    throw new FormatException($"element has multiple type keys at line {line}");
                type = elementType;

                name = (entry.Value as YamlScalarNode)?.Value?.Trim() ?? "";
                if (name.Length == 0)
                    throw new FormatException(
                        $"element {elementType.Display()} has no name at line {keyLine}");
                if (name.EndsWith("/", StringComparison.Ordinal))
                    throw new FormatException($"element name must not end with '/' at line {keyLine}");

                var slash = name.IndexOf('/');
                if (slash >= 0)
                {
                    swimlane = name.Substring(0, slash).Trim();
                    name = name.Substring(slash + 1).Trim();
                }

                if (swimlane.Length > 0 && name.Length == 0)
                    throw new FormatException(
                        $"element {elementType.Display()} has empty name after swimlane at line {keyLine}");
            }
            else
            {
                throw new FormatException($"unknown key \"{key}\" at line {keyLine}");
            }
        }

        if (type is null)
            throw new FormatException($"element missing type at line {line}");

        return new EmElement(type.Value, name, swimlane, props, line, column);
    }

    private static IReadOnlyList<KeyValuePair<string, object?>> ParseProps(YamlNode node)
    {
        if (IsNull(node))
            return [];
        if (node is not YamlMappingNode mapping)
            throw new FormatException("props must be a mapping");

        var props = new List<KeyValuePair<string, object?>>();
        foreach (var entry in mapping.Children)
            props.Add(new KeyValuePair<string, object?>(KeyOf(entry.Key), Convert(entry.Value)));

        return props;
    }

    private static object? Convert(YamlNode node) => node switch
    {
        YamlScalarNode scalar => IsNull(scalar) ? null : scalar.Value,
        YamlSequenceNode sequence => sequence.Children.Select(Convert).ToList(),
        YamlMappingNode mapping => mapping.Children
            .Select(entry => new KeyValuePair<string, object?>(KeyOf(entry.Key), Convert(entry.Value)))
            .ToList(),
        _ => null,
    };

    private static T Wrap<T>(string context, Func<T> parse)
    {
        try
        {
            return parse();
        }
        catch (FormatException ex)
        {
            throw new FormatException($"{context}: {ex.Message}", ex);
        }
    }

    private static bool IsNull(YamlNode node) =>
        node is YamlScalarNode { Style: ScalarStyle.Plain } scalar
        && (string.IsNullOrEmpty(scalar.Value) || scalar.Value == "~" || scalar.Value == "null");

    private static string KeyOf(YamlNode key) =>
        (key as YamlScalarNode)?.Value ?? key.ToString() ?? "";

    private static int LineOf(YamlNode node) => (int)node.Start.Line;

    private static int ColumnOf(YamlNode node) => (int)node.Start.Column;
}
