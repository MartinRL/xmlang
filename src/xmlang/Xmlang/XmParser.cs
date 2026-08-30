using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Xmlang;

/// <summary>
/// Parses an xmlang v0.2 document into XmSpec. RepresentationModel, same style as
/// Emlang.CodeGen/SpecModel.cs — quoted "Todo / Outstanding bids" keys survive verbatim.
/// Structural only: unknown keys are ignored here; resolution errors are XmLinter's job.
/// </summary>
public static class XmParser
{
    public static XmSpec Parse(string yamlText)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yamlText));
        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        return new XmSpec(
            Scalar(root, "xmlang"),
            Models(root),
            Personas(root),
            Surfaces(root),
            Journeys(root),
            Labels(root),
            Tokens(root));
    }

    private static IReadOnlyList<string> Models(YamlMappingNode root) => Child(root, "model") switch
    {
        YamlScalarNode { Value: { } single } => [single],
        YamlSequenceNode list => Strings(list),
        _ => [],
    };

    private static IReadOnlyList<XmPersona> Personas(YamlMappingNode root) =>
        Child(root, "personas") is not YamlMappingNode personas ? []
            : [.. personas.Children.Select(p => new XmPersona(
                Name(p.Key),
                p.Value is YamlMappingNode def ? Scalar(def, "role") : null))];

    private static IReadOnlyList<XmSurface> Surfaces(YamlMappingNode root) =>
        Child(root, "surfaces") is not YamlMappingNode surfaces ? []
            : [.. surfaces.Children.Select(s => Surface(Name(s.Key), (YamlMappingNode)s.Value))];

    private static XmSurface Surface(string name, YamlMappingNode def) => new(
        name,
        StringList(def, "for"),
        StringList(def, "during"),
        Child(def, "compose") is YamlSequenceNode compose
            ? [.. compose.Children.OfType<YamlMappingNode>().Select(ComposeItem)]
            : []);

    private static XmComposeItem ComposeItem(YamlMappingNode item) =>
        Scalar(item, "v") is { } view
            ? new XmComposeItem(ViewItem(view, item), null)
            : new XmComposeItem(null, new XmCommandItem(Scalar(item, "c") ?? "", Scalar(item, "prominence") ?? "primary"));

    private static XmViewItem ViewItem(string view, YamlMappingNode item)
    {
        var tiers = Child(item, "fields") as YamlMappingNode ?? new YamlMappingNode();
        return new XmViewItem(
            view,
            StringList(tiers, "primary"),
            StringList(tiers, "secondary"),
            StringList(tiers, "on-demand"),
            Scalar(item, "self"));
    }

    private static IReadOnlyList<XmJourney> Journeys(YamlMappingNode root) =>
        Child(root, "journeys") is not YamlMappingNode journeys ? []
            : [.. journeys.Children.Select(j => new XmJourney(
                Name(j.Key),
                StringList((YamlMappingNode)j.Value, "for"),
                StringList((YamlMappingNode)j.Value, "slices")))];

    private static IReadOnlyDictionary<string, XmLabelMap> Labels(YamlMappingNode root) =>
        Child(root, "labels") is not YamlMappingNode locales
            ? new Dictionary<string, XmLabelMap>()
            : locales.Children.ToDictionary(l => Name(l.Key), l => LabelMap((YamlMappingNode)l.Value));

    private static XmLabelMap LabelMap(YamlMappingNode map) => new(
        Scalar(map, "register"),
        map.Children.Where(e => Name(e.Key) != "register")
            .ToDictionary(e => Name(e.Key), e => LabelEntry(e.Value)));

    private static XmLabelEntry LabelEntry(YamlNode value) => value switch
    {
        YamlScalarNode scalar => new(scalar.Value, null, NoFields),
        YamlMappingNode map => new(
            Scalar(map, "$self"),
            Scalar(map, "$empty"),
            map.Children.Where(f => !Name(f.Key).StartsWith('$'))
                .ToDictionary(f => Name(f.Key), f => LabelEntry(f.Value))),
        _ => new(null, null, NoFields),
    };

    private static readonly IReadOnlyDictionary<string, XmLabelEntry> NoFields =
        new Dictionary<string, XmLabelEntry>();

    private static IReadOnlyList<XmToken> Tokens(YamlMappingNode root)
    {
        var tokens = new List<XmToken>();
        if (Child(root, "tokens") is YamlMappingNode top)
            CollectTokens(top, "", tokens);
        return tokens;
    }

    private static void CollectTokens(YamlMappingNode node, string path, List<XmToken> tokens)
    {
        if (Child(node, "$value") is { } value)
        {
            tokens.Add(new XmToken(path, TokenValue(value)));
            return;
        }
        foreach (var child in node.Children)
            if (child.Value is YamlMappingNode group)
                CollectTokens(group, path.Length == 0 ? Name(child.Key) : $"{path}.{Name(child.Key)}", tokens);
    }

    private static string TokenValue(YamlNode value) => value switch
    {
        YamlScalarNode scalar => scalar.Value ?? "",
        YamlSequenceNode list => string.Join(", ", Strings(list)),
        _ => "",
    };

    private static string Name(YamlNode key) => ((YamlScalarNode)key).Value ?? "";

    private static YamlNode? Child(YamlMappingNode map, string key) =>
        map.Children.TryGetValue(new YamlScalarNode(key), out var value) ? value : null;

    private static string? Scalar(YamlMappingNode map, string key) =>
        Child(map, key) is YamlScalarNode { Value: { } value } ? value : null;

    private static IReadOnlyList<string> StringList(YamlMappingNode map, string key) =>
        Child(map, key) is YamlSequenceNode list ? Strings(list) : [];

    private static IReadOnlyList<string> Strings(YamlSequenceNode list) =>
        [.. list.Children.OfType<YamlScalarNode>().Select(n => n.Value ?? "")];
}
