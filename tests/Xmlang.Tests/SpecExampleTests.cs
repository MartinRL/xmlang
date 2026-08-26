using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Xmlang.Tests;

/// <summary>Every fenced yaml block in xmlang-spec.md, parsed and snapshotted:
/// the spec's examples cannot silently drift from what XmParser actually does.</summary>
public class SpecExampleTests
{
    // ponytail: regex fence extraction, upgrade to a markdown parser never
    private static readonly string[] Blocks =
        [.. Regex.Matches(File.ReadAllText(SpecPath()), "```yaml\r?\n(.*?)```", RegexOptions.Singleline)
            .Select(m => m.Groups[1].Value)];

    private static string SpecPath([CallerFilePath] string source = "") =>
        Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "xmlang-spec.md");

    public static TheoryData<int> Indices => [.. Enumerable.Range(0, Blocks.Length)];

    [Theory]
    [MemberData(nameof(Indices))]
    public Task YamlBlock(int index)
    {
        object result;
        try
        {
            result = XmParser.Parse(Blocks[index]);
        }
        catch (Exception e)
        {
            // A block that is not a standalone document (e.g. two alternatives of the
            // same key shown side by side) snapshots as its parse failure.
            result = $"{e.GetType().Name}: {e.Message}";
        }
        return Verifier.Verify(result).UseParameters(index);
    }
}
