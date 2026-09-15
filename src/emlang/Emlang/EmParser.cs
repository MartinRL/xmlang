using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Emlang;

/// <summary>A declared prop. IsParam marks a projection argument, the `(@param)` note of
/// RFC emlang-0004: an input the projection takes, never a projected (tierable) datum.</summary>
public record EmField(string Name, string Annotation)
{
    public bool IsParam => Annotation.IndexOf("(@param)", StringComparison.Ordinal) >= 0;
}

/// <summary>One c:/e:/x:/v:/s: element lifted from an emlang spec's slice steps. Lane is the
/// prefix before the FIRST slash ("Game" for events, "Todo" for a todo view, "" for bare
/// commands, exceptions and states). Names stay RAW camelCase — xm references them verbatim.
/// Kind 's' is a decision model (RFC emlang-0002); the swimlane text carries no meaning.</summary>
public record EmElement(char Kind, string Lane, string Name, IReadOnlyList<EmField> Fields);

/// <summary>One initiator of a slice (RFC emlang-0003 trigger sets, RFC emlang-0005 kinds):
/// Role is the raw swimlane, Origin the free text after the slash, IsAutomation the kind
/// (legacy `t:` read through RFC 0005's System/gear heuristic).</summary>
public record EmInitiator(string Slice, string Role, string Origin, bool IsAutomation)
{
    public string NormalizedRole => Emlang.Linting.EmNames.NormalizeRole(Role);
}

/// <summary>A slice's ordered steps as referenced (kind + lane + name), the projection xmlang's
/// navigation defaults read: the command, its rejections and events, and the terminal view.</summary>
public record EmSliceChain(string Slice, IReadOnlyList<EmElement> Steps)
{
    public IEnumerable<EmElement> Commands => Steps.Where(e => e.Kind == 'c');
    public EmElement? TerminalView => Steps.LastOrDefault(e => e.Kind == 'v');
}

/// <summary>One decision test: the command, the state it gives (null when malformed), the
/// phase pinned on that state, and whether the outcome is events (success) or a rejection.</summary>
public record EmScenario(string Slice, string Command, string? State, string? Phase, bool Success);

/// <summary>The Event Model surface xm references resolve against: elements, slice keys
/// (with their emoji type prefixes), initiator roles (normalized), phase values per decision
/// model (the `phase` enum annotation on an `s:` element), initiators, slice chains and
/// decision scenarios.</summary>
public record EmSpec(
    IReadOnlyList<EmElement> Elements,
    IReadOnlyList<string> Slices,
    IReadOnlyList<string> InitiatorRoles,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Phases,
    IReadOnlyList<EmInitiator> Initiators,
    IReadOnlyList<EmSliceChain> Chains,
    IReadOnlyList<EmScenario> Scenarios)
{
    public EmElement? FindView(string reference)
    {
        var (lane, name) = Split(reference);
        return Elements.FirstOrDefault(e => e.Kind == 'v' && e.Lane == lane && e.Name == name);
    }

    public EmElement? FindCommand(string name) =>
        Elements.FirstOrDefault(e => e.Kind == 'c' && e.Name == name);

    public EmElement? FindRejection(string name) =>
        Elements.FirstOrDefault(e => e.Kind == 'x' && e.Name == name);

    /// <summary>A decision model by name; the swimlane, if any was written, is ignored.</summary>
    public EmElement? FindState(string reference) =>
        Elements.FirstOrDefault(e => e.Kind == 's' && e.Name == Split(reference).Name);

    /// <summary>Every phase value declared by any decision model, flattened.</summary>
    public IEnumerable<string> PhaseValues => Phases.Values.SelectMany(v => v).Distinct();

    /// <summary>The initiators of every slice whose first command is <paramref name="command"/>.</summary>
    public IEnumerable<EmInitiator> InitiatorsOf(string command) =>
        Chains.Where(c => c.Commands.FirstOrDefault()?.Name == command)
            .SelectMany(c => Initiators.Where(i => i.Slice == c.Slice));

    /// <summary>Rule (RFC emlang-0003): the prefix before the FIRST slash is the lane; both
    /// sides are trimmed so "host /Auction catalog" and "State / Game" normalize alike.</summary>
    public static (string Lane, string Name) Split(string reference)
    {
        var slash = reference.IndexOf('/');
        return slash < 0
            ? ("", reference.Trim())
            : (reference.Substring(0, slash).Trim(), reference.Substring(slash + 1).Trim());
    }
}

/// <summary>
/// Parses an emlang YAML spec into the reference surface xm resolution needs. The
/// conventions are lifted from Emlang.CodeGen/SpecModel.cs (props-richest occurrence wins;
/// only slice STEPS carry annotations, `tests:` props are fixture values) and extended
/// with slice keys, initiators, chains and scenarios for xmlang's lints and defaults.
/// </summary>
public static class EmParser
{
    public static EmSpec Parse(string yamlText)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yamlText));
        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        var slices = (YamlMappingNode)root.Children[new YamlScalarNode("slices")];

        var byKey = new Dictionary<(char Kind, string Lane, string Name), EmElement>();
        var initiators = new List<EmInitiator>();
        var chains = new List<EmSliceChain>();
        var scenarios = new List<EmScenario>();
        foreach (var slice in slices.Children)
        {
            var sliceName = ((YamlScalarNode)slice.Key).Value ?? "";
            var steps = new List<EmElement>();
            foreach (var step in Steps(slice.Value))
                Collect(sliceName, step, byKey, initiators, steps);
            chains.Add(new EmSliceChain(sliceName, steps));
            scenarios.AddRange(Scenarios(sliceName, slice.Value));
        }

        var elements = byKey.Values.ToList();
        return new EmSpec(
            elements,
            [.. slices.Children.Keys.Select(k => ((YamlScalarNode)k).Value ?? "")],
            [.. initiators.Select(i => i.NormalizedRole).Where(r => r.Length > 0).Distinct()],
            Phases(elements),
            initiators,
            chains,
            scenarios);
    }

    public static EmSpec Merge(IReadOnlyList<EmSpec> specs) => new(
        [.. specs.SelectMany(s => s.Elements)],
        [.. specs.SelectMany(s => s.Slices)],
        [.. specs.SelectMany(s => s.InitiatorRoles).Distinct()],
        specs.SelectMany(s => s.Phases)
            .GroupBy(p => p.Key)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)[.. g.SelectMany(p => p.Value).Distinct()]),
        [.. specs.SelectMany(s => s.Initiators)],
        [.. specs.SelectMany(s => s.Chains)],
        [.. specs.SelectMany(s => s.Scenarios)]);

    private static IEnumerable<YamlMappingNode> Steps(YamlNode slice) => slice switch
    {
        YamlSequenceNode direct => direct.Children.OfType<YamlMappingNode>(),
        YamlMappingNode extended when extended.Children.TryGetValue(new YamlScalarNode("steps"), out var steps)
            => ((YamlSequenceNode)steps).Children.OfType<YamlMappingNode>(),
        _ => [],
    };

    private static readonly IReadOnlyDictionary<string, char> Kinds = new Dictionary<string, char>
    {
        ["c"] = 'c', ["cmd"] = 'c', ["command"] = 'c',
        ["e"] = 'e', ["evt"] = 'e', ["event"] = 'e',
        ["x"] = 'x', ["rej"] = 'x', ["rejection"] = 'x',
        ["v"] = 'v', ["view"] = 'v',
        ["s"] = 's', ["st"] = 's', ["state"] = 's',
    };

    /// <summary>Initiator keys → is-automation.</summary>
    private static readonly IReadOnlyDictionary<string, bool> InitiatorKinds = new Dictionary<string, bool>
    {
        ["t"] = false, ["translator"] = false,
        ["a"] = false, ["actor"] = false,
        ["auto"] = true, ["automation"] = true,
    };

    private static void Collect(
        string slice,
        YamlMappingNode step,
        Dictionary<(char Kind, string Lane, string Name), EmElement> byKey,
        List<EmInitiator> initiators,
        List<EmElement> steps)
    {
        foreach (var child in step.Children)
        {
            if (child.Key is not YamlScalarNode { Value: { } key })
                continue;
            var raw = (child.Value as YamlScalarNode)?.Value ?? "";
            if (InitiatorKinds.TryGetValue(key, out var isAutomation))
            {
                var (role, origin) = EmSpec.Split(raw);
                initiators.Add(new EmInitiator(slice, role, origin, isAutomation));
                return;
            }
            if (!Kinds.TryGetValue(key, out var kind))
                continue;
            steps.Add(CollectElement(kind, raw, step, byKey));
            return;
        }
    }

    private static EmElement CollectElement(
        char kind, string raw, YamlMappingNode step,
        Dictionary<(char Kind, string Lane, string Name), EmElement> byKey)
    {
        var (lane, name) = EmSpec.Split(raw);
        if (kind == 's')
            lane = ""; // RFC emlang-0002: a swimlane on a state carries no meaning
        var element = new EmElement(kind, lane, name, Fields(step));
        // Rule: the props-richest occurrence defines the element — elements reappear
        // bare as slice inputs (e.g. `- e: Game / BidPlaced` feeding a processor).
        var key = (kind, lane, name);
        if (!byKey.TryGetValue(key, out var existing) || element.Fields.Count > existing.Fields.Count)
            byKey[key] = element;
        return element;
    }

    private static IReadOnlyList<EmField> Fields(YamlMappingNode step) =>
        step.Children.TryGetValue(new YamlScalarNode("props"), out var props) && props is YamlMappingNode map
            ? [.. map.Children.Select(p => new EmField(
                ((YamlScalarNode)p.Key).Value ?? "",
                (p.Value as YamlScalarNode)?.Value ?? ""))]
            : [];

    /// <summary>Decision tests (a `when:`) of an extended slice: command, given state, pinned phase, outcome.</summary>
    private static IEnumerable<EmScenario> Scenarios(string slice, YamlNode node)
    {
        if (node is not YamlMappingNode extended
            || !extended.Children.TryGetValue(new YamlScalarNode("tests"), out var testsNode)
            || testsNode is not YamlMappingNode tests)
            yield break;
        foreach (var test in tests.Children)
        {
            if (test.Value is not YamlMappingNode body)
                continue;
            var whens = Section(body, "when").Where(s => s.Kind == 'c').ToList();
            if (whens.Count == 0)
                continue;
            var states = Section(body, "given").Where(s => s.Kind == 's').ToList();
            (char Kind, string Name, YamlMappingNode Step)? state = states.Count == 1 ? states[0] : null;
            var then = Section(body, "then").ToList();
            yield return new EmScenario(
                slice,
                whens[0].Name,
                state?.Name,
                state is { } given ? Phase(given.Step) : null,
                then.Count > 0 && then.All(t => t.Kind != 'x'));
        }
    }

    private static string? Phase(YamlMappingNode step) =>
        step.Children.TryGetValue(new YamlScalarNode("props"), out var props)
        && props is YamlMappingNode map
        && map.Children.TryGetValue(new YamlScalarNode("phase"), out var phase)
            ? (phase as YamlScalarNode)?.Value : null;

    private static IEnumerable<(char Kind, string Name, YamlMappingNode Step)> Section(YamlMappingNode body, string key)
    {
        if (!body.Children.TryGetValue(new YamlScalarNode(key), out var node) || node is not YamlSequenceNode list)
            yield break;
        foreach (var step in list.Children.OfType<YamlMappingNode>())
            foreach (var child in step.Children)
                if (child.Key is YamlScalarNode { Value: { } k } && Kinds.TryGetValue(k, out var kind))
                {
                    yield return (kind, EmSpec.Split((child.Value as YamlScalarNode)?.Value ?? "").Name, step);
                    break;
                }
    }

    /// <summary>Phase values per decision model: the enum annotation of a `phase` prop on an
    /// `s:` element, emlang's parenthesized-note convention "AuctionPhase (lobby|started|ended)".</summary>
    private static IReadOnlyDictionary<string, IReadOnlyList<string>> Phases(IEnumerable<EmElement> elements) =>
        elements
            .Where(e => e.Kind == 's')
            .Select(e => (e.Name, Values: e.Fields.Where(f => f.Name == "phase").SelectMany(f => EnumValues(f.Annotation)).Distinct().ToList()))
            .Where(p => p.Values.Count > 0)
            .ToDictionary(p => p.Name, p => (IReadOnlyList<string>)p.Values);

    private static IEnumerable<string> EnumValues(string annotation)
    {
        var open = annotation.IndexOf('(');
        var close = annotation.IndexOf(')', open + 1);
        return open < 0 || close < 0
            ? []
            : annotation.Substring(open + 1, close - open - 1).Split('|').Select(v => v.Trim());
    }
}
