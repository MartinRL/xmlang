using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Emlang.Tests;

public record CodeParam(string Name, string Type);

public record CodeRecord(string Name, IReadOnlyList<CodeParam> Parameters);

public record CodeUnion(string Name, IReadOnlyList<string> Members);

/// <summary>
/// Test-only harness: text-level Roslyn parse of a Commands.cs/Events.cs/Errors.cs surface —
/// record declarations (name + positional params) and `union` members. No compilation.
/// </summary>
public static class CodeSurface
{
    private static readonly CSharpParseOptions Preview = new(LanguageVersion.Preview);

    public static IReadOnlyList<CodeRecord> Records(string source) =>
        [.. CSharpSyntaxTree.ParseText(source, Preview)
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<RecordDeclarationSyntax>()
            .Select(r => new CodeRecord(r.Identifier.Text, Parameters(r)))];

    public static string? Namespace(string source) =>
        CSharpSyntaxTree.ParseText(source, Preview)
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Select(n => n.Name.ToString())
            .FirstOrDefault();

    private static IReadOnlyList<CodeParam> Parameters(RecordDeclarationSyntax record) =>
        record.ParameterList is { } parameters
            ? [.. parameters.Parameters.Select(p =>
                new CodeParam(p.Identifier.Text, p.Type?.ToString() ?? string.Empty))]
            : [];

    // ponytail: `union` is C# 15 preview syntax released Roslyn grammars may not model —
    // a targeted regex over the union declaration only; records go through the real parser.
    private static readonly Regex UnionPattern =
        new(@"union\s+(?<name>\w+)\s*\((?<body>[^)]*)\)", RegexOptions.Singleline);

    public static CodeUnion? Union(string source, string name) =>
        UnionPattern.Matches(source)
            .Where(m => m.Groups["name"].Value == name)
            .Select(m => new CodeUnion(name,
                [.. m.Groups["body"].Value.Split(',')
                    .Select(member => member.Trim())
                    .Where(member => member.Length > 0)]))
            .FirstOrDefault();
}
