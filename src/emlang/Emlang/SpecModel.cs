using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Emlang.CodeGen;

/// <summary>
/// One prop of a c:/e:/x:/v: element, already mapped to the C# surface it determines
/// (name PascalCased, annotation mapped to a type). Line-tracked for findings.
/// IsEnum records whether the annotation carried an enum note ("Difficulty (familj|klassisk|svår)").
/// </summary>
public record SpecProp(string Name, string Type, int Line, bool IsEnum = false);

/// <summary>
/// One c:/e:/x:/v: element lifted from an emlang spec's slice steps. Lane is the prefix
/// before the slash ("Game" for events, "State"/"Screen"/"Todo" for views, "" for bare
/// commands/exceptions). 'v' elements are inert for record emission (SurfaceEmitter filters
/// by kind); they type the `tests:` fixture values for TestsEmitter.
/// </summary>
public record SpecElement(char Kind, string Name, IReadOnlyList<SpecProp> Props, int Line, string Lane = "");

/// <summary>
/// Parses an emlang YAML spec into the record surface its c:/e:/x:/v: elements define.
/// Only slice STEPS (direct-form list or `steps:`) carry type annotations; `tests:`
/// props are fixture values and are ignored (see TestModel).
/// </summary>
public static class SpecModel
{
    public static IReadOnlyList<SpecElement> Parse(string yamlText)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yamlText));
        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        var slices = (YamlMappingNode)root.Children[new YamlScalarNode("slices")];

        var byKey = new Dictionary<(char Kind, string Lane, string Name), SpecElement>();
        foreach (var step in slices.Children.Values.SelectMany(Steps))
        {
            if (ToElement(step) is not { } element)
                continue;
            // Rule: the props-richest occurrence defines the surface — elements reappear
            // bare as slice inputs (e.g. `- e: Game / BidPlaced` feeding a processor).
            var key = (element.Kind, element.Lane, element.Name);
            if (!byKey.TryGetValue(key, out var existing) || element.Props.Count > existing.Props.Count)
                byKey[key] = element;
        }
        return [.. byKey.Values];
    }

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

    private static SpecElement? ToElement(YamlMappingNode step)
    {
        foreach (var child in step.Children)
        {
            if (child.Key is not YamlScalarNode { Value: { } key } || !Kinds.TryGetValue(key, out var kind))
                continue;
            var raw = ((YamlScalarNode)child.Value).Value ?? string.Empty;
            return new SpecElement(kind, BareName(raw), Props(step), (int)child.Key.Start.Line, Lane(raw));
        }
        return null;
    }

    /// <summary>Rule: the prefix before the slash is the lane ("Game /", "State /", …).</summary>
    private static string Lane(string value)
    {
        var slash = value.LastIndexOf('/');
        return slash < 0 ? string.Empty : value.Substring(0, slash).Trim();
    }

    /// <summary>Rule: events carry the stream prefix ("Game / LobbyOpened"); strip it.</summary>
    private static string BareName(string value) =>
        value.Substring(value.LastIndexOf('/') + 1).Trim(); // LastIndexOf -1 when no prefix ⇒ whole string

    private static IReadOnlyList<SpecProp> Props(YamlMappingNode step) =>
        step.Children.TryGetValue(new YamlScalarNode("props"), out var props) && props is YamlMappingNode map
            ? [.. map.Children.Select(p => new SpecProp(
                PascalCase(((YamlScalarNode)p.Key).Value!),
                MapType(((YamlScalarNode)p.Value).Value ?? string.Empty),
                (int)p.Key.Start.Line,
                IsEnumNote(((YamlScalarNode)p.Value).Value ?? string.Empty)))]
            : [];

    /// <summary>Rule: a parenthesized note listing cases with '|' marks an enum type.</summary>
    private static bool IsEnumNote(string annotation)
    {
        var open = annotation.IndexOf('(');
        return open >= 0 && annotation.IndexOf('|', open) > open;
    }

    /// <summary>Rule: spec props are camelCase, C# positional params are PascalCase.</summary>
    public static string PascalCase(string name) =>
        name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);

    /// <summary>
    /// The general annotation-to-C#-type mapping rules. Experiment metric: keep this list
    /// SMALL — every addition here is a counted general mapping rule (§9 experiment log).
    /// </summary>
    public static string MapType(string annotation)
    {
        var type = StripParenthesizedNote(annotation.Trim());
        return type.EndsWith("[]", StringComparison.Ordinal)
            ? $"IReadOnlyList<{type.Substring(0, type.Length - 2)}>" // rule: X[] -> IReadOnlyList<X> (constitution collections)
            : type;
    }

    /// <summary>Rule: "Direction (mer|mindre)" / "byte (0-100)" — the parenthesized note is prose.</summary>
    private static string StripParenthesizedNote(string annotation)
    {
        var open = annotation.IndexOf('(');
        return open < 0 ? annotation : annotation.Substring(0, open).Trim();
    }
}
