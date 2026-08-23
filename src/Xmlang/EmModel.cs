using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Xmlang;

public record EmField(string Name, string Annotation);

/// <summary>One c:/e:/x:/v: element lifted from an emlang spec's slice steps. Lane is the
/// prefix before the last slash ("Game" for events, "State"/"Screen"/"Todo" for views,
/// "" for bare commands/exceptions). Names stay RAW camelCase — xm references them verbatim.</summary>
public record EmElement(char Kind, string Lane, string Name, IReadOnlyList<EmField> Fields);

/// <summary>The Event Model surface xm references resolve against: elements, slice keys
/// (with their emoji type prefixes), trigger roles (initiator-lane prefixes), and the phase
/// values declared on a State-lane view's `phase` enum annotation.</summary>
public record EmSpec(
    IReadOnlyList<EmElement> Elements,
    IReadOnlyList<string> Slices,
    IReadOnlyList<string> TriggerRoles,
    IReadOnlyList<string> PhaseValues)
{
    public EmElement? FindView(string reference)
    {
        var (lane, name) = Split(reference);
        return Elements.FirstOrDefault(e => e.Kind == 'v' && e.Lane == lane && e.Name == name);
    }

    public EmElement? FindCommand(string name) =>
        Elements.FirstOrDefault(e => e.Kind == 'c' && e.Name == name);

    /// <summary>Rule (SpecModel.cs): the prefix before the LAST slash is the lane; both
    /// sides are trimmed so "host /Auction catalog" and "State / Game" normalize alike.</summary>
    public static (string Lane, string Name) Split(string reference)
    {
        var slash = reference.LastIndexOf('/');
        return slash < 0
            ? ("", reference.Trim())
            : (reference[..slash].Trim(), reference[(slash + 1)..].Trim());
    }
}

/// <summary>
/// Parses an emlang YAML spec into the reference surface xm resolution needs. The
/// conventions are lifted from Emlang.CodeGen/SpecModel.cs (props-richest occurrence wins;
/// only slice STEPS carry annotations, `tests:` props are fixture values) and extended
/// with slice keys and trigger roles for journey/persona lint.
/// </summary>
public static class EmModel
{
    public static EmSpec Parse(string yamlText)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yamlText));
        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        var slices = (YamlMappingNode)root.Children[new YamlScalarNode("slices")];

        var byKey = new Dictionary<(char Kind, string Lane, string Name), EmElement>();
        var roles = new List<string>();
        foreach (var step in slices.Children.Values.SelectMany(Steps))
            Collect(step, byKey, roles);

        var elements = byKey.Values.ToList();
        return new EmSpec(
            elements,
            [.. slices.Children.Keys.Select(k => ((YamlScalarNode)k).Value ?? "")],
            [.. roles.Distinct()],
            PhaseValues(elements));
    }

    public static EmSpec Merge(IReadOnlyList<EmSpec> specs) => new(
        [.. specs.SelectMany(s => s.Elements)],
        [.. specs.SelectMany(s => s.Slices)],
        [.. specs.SelectMany(s => s.TriggerRoles).Distinct()],
        [.. specs.SelectMany(s => s.PhaseValues).Distinct()]);

    private static IEnumerable<YamlMappingNode> Steps(YamlNode slice) => slice switch
    {
        YamlSequenceNode direct => direct.Children.OfType<YamlMappingNode>(),
        YamlMappingNode extended when extended.Children.TryGetValue(new YamlScalarNode("steps"), out var steps)
            => ((YamlSequenceNode)steps).Children.OfType<YamlMappingNode>(),
        _ => [],
    };

    private static readonly IReadOnlyDictionary<string, char> Kinds = new Dictionary<string, char>
    {
        ["c"] = 'c', ["command"] = 'c',
        ["e"] = 'e', ["event"] = 'e',
        ["x"] = 'x', ["exception"] = 'x',
        ["v"] = 'v', ["view"] = 'v',
    };

    private static void Collect(
        YamlMappingNode step,
        Dictionary<(char Kind, string Lane, string Name), EmElement> byKey,
        List<string> roles)
    {
        foreach (var child in step.Children)
        {
            if (child.Key is not YamlScalarNode { Value: { } key })
                continue;
            var raw = (child.Value as YamlScalarNode)?.Value ?? "";
            if (key is "t" or "trigger")
            {
                CollectRole(raw, roles);
                return;
            }
            if (!Kinds.TryGetValue(key, out var kind))
                continue;
            CollectElement(kind, raw, step, byKey);
            return;
        }
    }

    private static void CollectRole(string trigger, List<string> roles)
    {
        var (role, _) = EmSpec.Split(trigger);
        if (role.Length > 0)
            roles.Add(role);
    }

    private static void CollectElement(
        char kind, string raw, YamlMappingNode step,
        Dictionary<(char Kind, string Lane, string Name), EmElement> byKey)
    {
        var (lane, name) = EmSpec.Split(raw);
        var element = new EmElement(kind, lane, name, Fields(step));
        // Rule: the props-richest occurrence defines the element — elements reappear
        // bare as slice inputs (e.g. `- e: Game / BidPlaced` feeding a processor).
        var key = (kind, lane, name);
        if (!byKey.TryGetValue(key, out var existing) || element.Fields.Count > existing.Fields.Count)
            byKey[key] = element;
    }

    private static IReadOnlyList<EmField> Fields(YamlMappingNode step) =>
        step.Children.TryGetValue(new YamlScalarNode("props"), out var props) && props is YamlMappingNode map
            ? [.. map.Children.Select(p => new EmField(
                ((YamlScalarNode)p.Key).Value ?? "",
                (p.Value as YamlScalarNode)?.Value ?? ""))]
            : [];

    /// <summary>during: values resolve against the enum annotation of a `phase` prop on a
    /// State-lane view — emlang's parenthesized-note convention, "AuctionPhase (lobby|started|ended)".</summary>
    private static IReadOnlyList<string> PhaseValues(IEnumerable<EmElement> elements) =>
        [.. elements
            .Where(e => e.Kind == 'v' && e.Lane == "State")
            .SelectMany(e => e.Fields)
            .Where(f => f.Name == "phase")
            .SelectMany(f => EnumValues(f.Annotation))
            .Distinct()];

    private static IEnumerable<string> EnumValues(string annotation)
    {
        var open = annotation.IndexOf('(');
        var close = annotation.IndexOf(')', open + 1);
        return open < 0 || close < 0
            ? []
            : annotation[(open + 1)..close].Split('|').Select(v => v.Trim());
    }
}
