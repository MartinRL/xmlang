using AwesomeAssertions;
using Emlang.Linting;
using Xunit;

namespace Emlang.Tests;

/// <summary>
/// EmFormatter port fidelity against the Go reference formatter (emlang v1.0.0):
/// alias normalization goldens, section presence round-trip, key-style default,
/// and idempotent round-trips over the frozen kvissig specs.
/// </summary>
public class FormatterTests
{
    [Fact]
    public void Short_aliases_normalize_to_long_keys()
    {
        var doc = EmAst.Parse(
            """
            slices:
              s:
                - t: Foo
                - c: Bar
                - e: Baz
                - x: Err
                - v: MyView
            """);

        EmFormatter.Format(doc).Should().Be(
            "slices:\n" +
            "  s:\n" +
            "    - trigger: Foo\n" +
            "    - command: Bar\n" +
            "    - event: Baz\n" +
            "    - exception: Err\n" +
            "    - view: MyView\n");
    }

    [Fact]
    public void Medium_aliases_normalize_to_long_keys()
    {
        var doc = EmAst.Parse(
            """
            slices:
              s:
                - trg: Foo
                - cmd: Bar
                - evt: Baz
                - err: Qux
            """);

        EmFormatter.Format(doc).Should().Be(
            "slices:\n" +
            "  s:\n" +
            "    - trigger: Foo\n" +
            "    - command: Bar\n" +
            "    - event: Baz\n" +
            "    - exception: Qux\n");
    }

    [Fact]
    public void Long_keys_normalize_to_short_style()
    {
        var doc = EmAst.Parse(
            """
            slices:
              s:
                - trigger: Foo
                - command: Bar
                - event: Baz
                - exception: Err
                - view: MyView
            """);

        EmFormatter.Format(doc, "short").Should().Be(
            "slices:\n" +
            "  s:\n" +
            "    - t: Foo\n" +
            "    - c: Bar\n" +
            "    - e: Baz\n" +
            "    - x: Err\n" +
            "    - v: MyView\n");
    }

    [Fact]
    public void Extended_form_keeps_only_present_test_sections_and_sorts_props()
    {
        var doc = EmAst.Parse(
            """
            slices:
              Payment:
                steps:
                  - c: ProcessPayment
                    props:
                      b: two
                      a: one
                tests:
                  happy-path:
                    when:
                      - c: ProcessPayment
            """);

        EmFormatter.Format(doc).Should().Be(
            "slices:\n" +
            "  Payment:\n" +
            "    steps:\n" +
            "      - command: ProcessPayment\n" +
            "        props:\n" +
            "          a: one\n" +
            "          b: two\n" +
            "    tests:\n" +
            "      happy-path:\n" +
            "        when:\n" +
            "          - command: ProcessPayment\n");
    }

    [Fact]
    public void Swimlane_is_rejoined_without_spaces()
    {
        var doc = EmAst.Parse(
            """
            slices:
              s:
                - e: Game / LobbyOpened
            """);

        EmFormatter.Format(doc).Should().Contain("- event: Game/LobbyOpened\n");
    }

    [Fact]
    public void Multi_document_specs_keep_the_separator()
    {
        var doc = EmAst.Parse(
            """
            slices:
              a:
                - t: Foo
            ---
            slices:
              b:
                - c: Bar
            """);

        EmFormatter.Format(doc).Should().Be(
            "slices:\n" +
            "  a:\n" +
            "    - trigger: Foo\n" +
            "---\n" +
            "slices:\n" +
            "  b:\n" +
            "    - command: Bar\n");
    }

    [Theory]
    [InlineData("mer-eller-mindre.em.yaml")]
    [InlineData("blindbudet.em.yaml")]
    [InlineData("tank-till-tusen.em.yaml")]
    public void Frozen_kvissig_specs_round_trip_idempotently(string specFile)
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", specFile));

        var formatted = EmFormatter.Format(EmAst.Parse(text));
        var reformatted = EmFormatter.Format(EmAst.Parse(formatted));

        reformatted.Should().Be(formatted);
    }
}
