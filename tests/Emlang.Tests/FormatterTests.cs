using AwesomeAssertions;
using Emlang.Linting;
using Xunit;

namespace Emlang.Tests;

/// <summary>
/// EmFormatter: alias normalization goldens (the dialect's kinds included), translator
/// key round-trip, section presence round-trip, key-style default, idempotence.
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
                - a: Foo
                - c: Bar
                - e: Baz
                - x: Err
                - v: MyView
                - s: MyState
                - auto: Bot
            """);

        EmFormatter.Format(doc).Should().Be(
            "slices:\n" +
            "  s:\n" +
            "    - actor: Foo\n" +
            "    - command: Bar\n" +
            "    - event: Baz\n" +
            "    - rejection: Err\n" +
            "    - view: MyView\n" +
            "    - state: MyState\n" +
            "    - automation: Bot\n");
    }

    [Fact]
    public void Medium_aliases_normalize_to_long_keys()
    {
        var doc = EmAst.Parse(
            """
            slices:
              s:
                - cmd: Bar
                - evt: Baz
                - rej: Qux
                - st: Quux
            """);

        EmFormatter.Format(doc).Should().Be(
            "slices:\n" +
            "  s:\n" +
            "    - command: Bar\n" +
            "    - event: Baz\n" +
            "    - rejection: Qux\n" +
            "    - state: Quux\n");
    }

    [Fact]
    public void Long_keys_normalize_to_short_style()
    {
        var doc = EmAst.Parse(
            """
            slices:
              s:
                - actor: Foo
                - command: Bar
                - event: Baz
                - rejection: Err
                - view: MyView
                - state: MyState
                - automation: Bot
            """);

        EmFormatter.Format(doc, "short").Should().Be(
            "slices:\n" +
            "  s:\n" +
            "    - a: Foo\n" +
            "    - c: Bar\n" +
            "    - e: Baz\n" +
            "    - x: Err\n" +
            "    - v: MyView\n" +
            "    - s: MyState\n" +
            "    - auto: Bot\n");
    }

    /// <summary>Translator is a first-class initiator: t: ↔ translator:, never rewritten.</summary>
    [Fact]
    public void Translator_keys_round_trip()
    {
        var doc = EmAst.Parse(
            """
            slices:
              s:
                - t: 📨 Payment gateway /Webhook
                - translator: 📨 Bank / Statement import
            """);

        EmFormatter.Format(doc).Should().Be(
            "slices:\n" +
            "  s:\n" +
            "    - translator: 📨 Payment gateway/Webhook\n" +
            "    - translator: 📨 Bank/Statement import\n");
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
                - a: Foo
            ---
            slices:
              b:
                - c: Bar
            """);

        EmFormatter.Format(doc).Should().Be(
            "slices:\n" +
            "  a:\n" +
            "    - actor: Foo\n" +
            "---\n" +
            "slices:\n" +
            "  b:\n" +
            "    - command: Bar\n");
    }
}
