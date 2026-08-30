using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Emlang.CodeGen;

/// <summary>A fixture value in a `tests:` prop. Scalars carry the raw YAML text; the
/// consumer types them via the element's declared props. Maps are parsed only so the
/// emitter can yield a clear diagnostic — tests reference fixtures, never build maps inline.</summary>
public abstract record TestValue;

public sealed record ScalarValue(string Raw) : TestValue;

public sealed record ListValue(IReadOnlyList<TestValue> Items) : TestValue;

public sealed record MapValue : TestValue;

/// <summary>One prop of a test step, name PascalCased to match the C# surface.</summary>
public record TestProp(string Name, TestValue Value, int Line);

/// <summary>One given/when/then step: kind c|e|x|v, lane ("" for bare c:/x:), bare name.</summary>
public record TestStep(char Kind, string Lane, string Name, IReadOnlyList<TestProp> Props, int Line);

/// <summary>One named GWT/GT case under a slice's `tests:` section.</summary>
public record SpecTest(
    string Slice,
    string Name,
    IReadOnlyList<TestStep> Given,
    TestStep? When,
    IReadOnlyList<TestStep> Then,
    int Line);

/// <summary>
/// Parses the `tests:` sections of an emlang YAML spec (the GWT/GT cases), the sibling of
/// SpecModel which parses the slice STEPS. Values stay raw here; TestsEmitter types them
/// against the SpecModel elements.
/// </summary>
public static class TestModel
{
    public static IReadOnlyList<SpecTest> Parse(string yamlText)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(yamlText));
        var root = (YamlMappingNode)stream.Documents[0].RootNode;
        var slices = (YamlMappingNode)root.Children[new YamlScalarNode("slices")];

        var tests = new List<SpecTest>();
        foreach (var slice in slices.Children)
        {
            if (slice.Value is not YamlMappingNode extended
                || !extended.Children.TryGetValue(new YamlScalarNode("tests"), out var node)
                || node is not YamlMappingNode cases)
                continue;
            var sliceName = ((YamlScalarNode)slice.Key).Value ?? string.Empty;
            foreach (var testCase in cases.Children)
                tests.Add(ToTest(sliceName, testCase));
        }
        return tests;
    }

    private static SpecTest ToTest(string slice, KeyValuePair<YamlNode, YamlNode> testCase)
    {
        var name = ((YamlScalarNode)testCase.Key).Value ?? string.Empty;
        var body = (YamlMappingNode)testCase.Value;
        var when = Steps(body, "when");
        return new SpecTest(
            slice,
            name,
            Steps(body, "given"),
            when.Count == 0 ? null : when[0],
            Steps(body, "then"),
            (int)testCase.Key.Start.Line);
    }

    private static IReadOnlyList<TestStep> Steps(YamlMappingNode body, string key) =>
        body.Children.TryGetValue(new YamlScalarNode(key), out var node) && node is YamlSequenceNode steps
            ? [.. steps.Children.OfType<YamlMappingNode>().Select(ToStep)]
            : [];

    private static readonly IReadOnlyDictionary<string, char> Kinds = new Dictionary<string, char>
    {
        ["c"] = 'c', ["e"] = 'e', ["x"] = 'x', ["v"] = 'v',
    };

    private static TestStep ToStep(YamlMappingNode step)
    {
        foreach (var child in step.Children)
        {
            if (child.Key is not YamlScalarNode { Value: { } key } || !Kinds.TryGetValue(key, out var kind))
                continue;
            var raw = ((YamlScalarNode)child.Value).Value ?? string.Empty;
            var slash = raw.LastIndexOf('/');
            return new TestStep(
                kind,
                slash < 0 ? string.Empty : raw.Substring(0, slash).Trim(),
                raw.Substring(slash + 1).Trim(),
                Props(step),
                (int)child.Key.Start.Line);
        }
        throw new InvalidDataException($"test step at line {step.Start.Line} has no c/e/x/v key");
    }

    private static IReadOnlyList<TestProp> Props(YamlMappingNode step) =>
        step.Children.TryGetValue(new YamlScalarNode("props"), out var props) && props is YamlMappingNode map
            ? [.. map.Children.Select(p => new TestProp(
                SpecModel.PascalCase(((YamlScalarNode)p.Key).Value!),
                ToValue(p.Value),
                (int)p.Key.Start.Line))]
            : [];

    private static TestValue ToValue(YamlNode node) => node switch
    {
        YamlScalarNode scalar => new ScalarValue(scalar.Value ?? "null"),
        YamlSequenceNode list => new ListValue([.. list.Children.Select(ToValue)]),
        _ => new MapValue(),
    };
}
