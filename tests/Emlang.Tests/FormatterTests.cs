using AwesomeAssertions;
using Emlang.Linting;
using Xunit;

namespace Emlang.Tests;

/// <summary>
/// EmFormatter: alias normalization goldens (the dialect's kinds included), the RFC 0005
/// legacy-trigger rewrite, section presence round-trip, key-style default, idempotence.
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
            "    - exception: Err\n" +
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
                - err: Qux
                - st: Quux
            """);

        EmFormatter.Format(doc).Should().Be(
            "slices:\n" +
            "  s:\n" +
            "    - command: Bar\n" +
            "    - event: Baz\n" +
            "    - exception: Qux\n" +
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
                - exception: Err
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

    /// <summary>RFC emlang-0005's one-time migration: System or a gear emoji is an automation.</summary>
    [Fact]
    public void Legacy_triggers_are_rewritten_to_actor_or_automation()
    {
        var doc = EmAst.Parse(
            """
            slices:
              s:
                - t: 🧑‍🏫 host /Quiz catalog
                - trigger: ⚙️ System / Reveal lot
                - trg: system /Cron
            """);

        EmFormatter.Format(doc, "short").Should().Be(
            "slices:\n" +
            "  s:\n" +
            "    - a: 🧑‍🏫 host/Quiz catalog\n" +
            "    - auto: ⚙️ System/Reveal lot\n" +
            "    - auto: system/Cron\n");
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
