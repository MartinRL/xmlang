using System.Globalization;
using System.Text;

namespace Emlang.Linting;

/// <summary>
/// Canonical emlang YAML formatter, ported from the Go reference internal/formatter:
/// renders from the AST (comments are dropped, exactly like the reference), 2-space indent,
/// slices in document order, props and test names sorted, element type keys normalized to
/// "long" (command/event/...) or "short" (c/e/...). Dialect: legacy `t:` triggers are
/// rewritten to actor/automation by RFC emlang-0005's stated heuristic.
/// </summary>
public static class EmFormatter
{
    public static string Format(EmDocument document, string keyStyle = "long")
    {
        var buf = new StringBuilder();
        for (var i = 0; i < document.SubDocs.Count; i++)
        {
            if (i > 0)
                buf.Append("---\n");
            WriteSubDoc(buf, document.SubDocs[i], keyStyle);
        }

        return buf.ToString();
    }

    private static void WriteSubDoc(StringBuilder buf, EmSubDoc subDoc, string style)
    {
        buf.Append("slices:\n");
        foreach (var slice in subDoc.Slices)
            WriteSlice(buf, slice, style);
    }

    private static void WriteSlice(StringBuilder buf, EmSlice slice, string style)
    {
        Line(buf, 1, $"{slice.Name}:");

        if (slice.Tests.Count > 0)
        {
            if (slice.Elements.Count > 0)
            {
                Line(buf, 2, "steps:");
                WriteElements(buf, 3, slice.Elements, style);
            }

            Line(buf, 2, "tests:");
            foreach (var test in slice.Tests.OrderBy(t => t.Name, StringComparer.Ordinal))
                WriteTest(buf, test, style);
        }
        else
        {
            WriteElements(buf, 2, slice.Elements, style);
        }
    }

    private static void WriteTest(StringBuilder buf, EmTest test, string style)
    {
        Line(buf, 3, $"{test.Name}:");
        WriteTestSection(buf, "given", test.HasGiven, test.Given, style);
        WriteTestSection(buf, "when", test.HasWhen, test.When, style);
        WriteTestSection(buf, "then", test.HasThen, test.Then, style);
    }

    private static void WriteTestSection(
        StringBuilder buf, string label, bool present, IReadOnlyList<EmElement> elements, string style)
    {
        if (!present)
            return;
        Line(buf, 4, $"{label}:");
        WriteElements(buf, 5, elements, style);
    }

    private static void WriteElements(
        StringBuilder buf, int level, IReadOnlyList<EmElement> elements, string style)
    {
        foreach (var element in elements)
            WriteElement(buf, level, element, style);
    }

    private static void WriteElement(StringBuilder buf, int level, EmElement element, string style)
    {
        var name = element.Swimlane.Length == 0 ? element.Name : $"{element.Swimlane}/{element.Name}";
        Line(buf, level, $"- {TypeKey(element.Type, style)}: {name}");

        if (element.Props.Count == 0)
            return;
        Line(buf, level + 1, "props:");
        foreach (var prop in element.Props.OrderBy(p => p.Key, StringComparer.Ordinal))
            Line(buf, level + 2, $"{prop.Key}: {FormatValue(prop.Value)}");
    }

    private static string TypeKey(EmElementType type, string style) =>
        style == "short"
            ? type switch
            {
                EmElementType.Translator => "t",
                EmElementType.Command => "c",
                EmElementType.Event => "e",
                EmElementType.Rejection => "x",
                EmElementType.View => "v",
                EmElementType.State => "s",
                EmElementType.Actor => "a",
                EmElementType.Automation => "auto",
                _ => "unknown",
            }
            : type.Display();

    // Go fmt %v semantics: scalars verbatim, sequences [a b c], mappings map[k:v] with
    // sorted keys. ponytail: scalars stay as-written (YamlDotNet gives us strings) — the
    // Go CLI normalizes typed floats (5.0 → 5); diverges only on non-canonical numerics.
    private static string FormatValue(object? value) => value switch
    {
        IReadOnlyList<KeyValuePair<string, object?>> mapping => "map[" + string.Join(" ",
            mapping.OrderBy(e => e.Key, StringComparer.Ordinal)
                .Select(e => $"{e.Key}:{FormatValue(e.Value)}")) + "]",
        IReadOnlyList<object?> sequence =>
            "[" + string.Join(" ", sequence.Select(FormatValue)) + "]",
        null => "<nil>",
        _ => System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };

    private static void Line(StringBuilder buf, int level, string text) =>
        buf.Append(' ', level * 2).Append(text).Append('\n');
}
