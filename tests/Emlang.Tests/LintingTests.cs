using AwesomeAssertions;
using Emlang.Linting;
using Xunit;

namespace Emlang.Tests;

/// <summary>
/// EmAst + Linter port fidelity against the Go reference toolchain (emlang v1.0.0):
/// the three frozen kvissig specs lint clean (verified against `emlang lint` output),
/// and each of the three rules fires with the reference message and position.
/// </summary>
public class LintingTests
{
    [Theory]
    [InlineData("mer-eller-mindre.em.yaml", 11)]
    [InlineData("blindbudet.em.yaml", 9)]
    [InlineData("tank-till-tusen.em.yaml", 9)]
    public void Frozen_kvissig_specs_match_the_Go_reference_lint(string specFile, int viewOnlySlices)
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", specFile));
        var document = EmAst.Parse(text);

        // Golden against `emlang lint` v1.0.0 (2026-08-30): N slice-missing-event warnings raw...
        var raw = Linter.Lint(document);
        raw.Should().HaveCount(viewOnlySlices);
        raw.Should().OnlyContain(i => i.Rule == "slice-missing-event"
                                      && i.Severity == LintSeverity.Warning);

        // ...and clean under kvissig's .emlang.yaml (ignore: slice-missing-event).
        Linter.Lint(document, ["slice-missing-event"]).Should().BeEmpty();
    }

    [Fact]
    public void Command_without_event_warns_at_the_command_position()
    {
        var doc = EmAst.Parse(
            """
            slices:
              Broken:
                - e: Game/Opened
                - c: DoThing
            """);

        var issue = Linter.Lint(doc).Should().ContainSingle().Subject;

        issue.Should().Be(new LintIssue(
            "command-without-event",
            "command should be followed by an event or exception",
            4, 7, LintSeverity.Warning));
    }

    [Fact]
    public void Orphan_exception_warns()
    {
        var doc = EmAst.Parse(
            """
            slices:
              Broken:
                - x: NotAllowed
                - e: Game/Opened
            """);

        Linter.Lint(doc).Should().ContainSingle().Which.Should().Be(new LintIssue(
            "orphan-exception", "exception without preceding command", 3, 7, LintSeverity.Warning));
    }

    [Fact]
    public void Slice_without_event_warns_at_position_zero()
    {
        var doc = EmAst.Parse(
            """
            slices:
              Catalog:
                - v: Pack catalog
            """);

        Linter.Lint(doc).Should().ContainSingle().Which.Should().Be(new LintIssue(
            "slice-missing-event", "slice \"Catalog\" has no events", 0, 0, LintSeverity.Warning));
    }

    [Fact]
    public void Empty_slice_is_a_valid_placeholder()
    {
        var doc = EmAst.Parse("slices:\n  Placeholder:\n");

        Linter.Lint(doc).Should().BeEmpty();
    }

    [Fact]
    public void Ignored_rules_are_suppressed()
    {
        var doc = EmAst.Parse(
            """
            slices:
              Catalog:
                - v: Pack catalog
            """);

        Linter.Lint(doc, ["slice-missing-event"]).Should().BeEmpty();
    }

    [Fact]
    public void Swimlane_splits_at_the_first_slash_and_trims()
    {
        var doc = EmAst.Parse(
            """
            slices:
              S:
                - e: Game / LobbyOpened
            """);

        var element = doc.SubDocs.Single().Slices.Single().Elements.Single();
        element.Swimlane.Should().Be("Game");
        element.Name.Should().Be("LobbyOpened");
    }

    [Fact]
    public void Multi_document_specs_merge_slice_counts_by_name()
    {
        var doc = EmAst.Parse(
            """
            slices:
              A:
                - e: E1
            ---
            slices:
              A:
                - e: E2
              B:
                - e: E3
            """);

        doc.SubDocs.Should().HaveCount(2);
        doc.SliceCount.Should().Be(2, "the Go parser merges slices by name across documents");
    }

    [Fact]
    public void Extended_slice_form_carries_steps_and_tests()
    {
        var doc = EmAst.Parse(
            """
            slices:
              Open:
                steps:
                  - c: Open
                  - e: Game/Opened
                tests:
                  can open:
                    when:
                      - c: Open
                        props:
                          name: Martin
                    then:
                      - e: Game/Opened
            """);

        var slice = doc.SubDocs.Single().Slices.Single();
        slice.Elements.Should().HaveCount(2);
        var test = slice.Tests.Single();
        test.Name.Should().Be("can open");
        test.When.Single().Props.Single().Should().Be(
            new KeyValuePair<string, object?>("name", "Martin"));
        test.Then.Single().Type.Should().Be(EmElementType.Event);
    }

    [Theory]
    [InlineData("slices: [x]\n", "slices must be a mapping at line 1")]
    [InlineData("nope:\n  A:\n", "unknown top-level key \"nope\" at line 1")]
    [InlineData("slices:\n  A: []\n", "slice \"A\": slice must have at least one element at line 2")]
    [InlineData("slices:\n  A:\n    - c: X\n      e: Y\n", "slice \"A\": element has multiple type keys at line 3")]
    [InlineData("slices:\n  A:\n    - c: Bad/\n", "slice \"A\": element name must not end with '/' at line 3")]
    [InlineData("slices:\n  A:\n    - k: X\n", "slice \"A\": unknown key \"k\" at line 3")]
    [InlineData("slices:\n  A:\n    tests:\n", "slice \"A\": extended slice must have 'steps' at line 3")]
    [InlineData(
        "slices:\n  A:\n    steps:\n      - e: E\n    tests:\n      t:\n        when:\n          - e: E\n",
        "slice \"A\": tests: test \"t\": when: event not allowed at line 8")]
    public void Structural_violations_use_the_reference_parsers_wording(string yaml, string expected)
    {
        var act = () => EmAst.Parse(yaml);

        act.Should().Throw<FormatException>().WithMessage(expected);
    }
}
