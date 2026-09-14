using AwesomeAssertions;
using Emlang.Linting;
using Xunit;

namespace Emlang.Tests;

/// <summary>
/// The dialect rule set, one smallest-failing document per rule: the three reference rules
/// (Go linter parity, with the dialect's command-less slice exemption), RFC emlang-0002's
/// decider rules, RFC emlang-0003/0005's initiator rules and RFC emlang-0004's appendix.
/// </summary>
public class LintingTests
{
    private static IReadOnlyList<LintIssue> Lint(string yaml) => Linter.Lint(EmAst.Parse(yaml));

    private static IReadOnlyList<string> Rules(string yaml) => [.. Lint(yaml).Select(i => i.Rule)];

    /// <summary>A complete dialect document: one decider, folds by reference, actor identity, traced view.</summary>
    private const string Clean = """
        slices:
          ✍️ Open lobby:
            steps:
              - a: 🧑‍🏫 host /Quiz catalog
              - c: OpenLobby
                props: { hostPlayerId: Guid }
              - x: LobbyAlreadyOpen
              - e: Game / LobbyOpened
                props: { gameId: Guid, hostPlayerId: Guid }
              - v: Roster
                props: { names: "string[]" }
            tests:
              lobby opens:
                given:
                  - s: Game
                when:
                  - c: OpenLobby
                    props: { hostPlayerId: martinId }
                then:
                  - e: Game / LobbyOpened
                    props: { hostPlayerId: martinId }
              roster lists the host:
                given:
                  - e: Game / LobbyOpened
                    props: { hostPlayerId: martinId }
                then:
                  - v: Roster
                    props: { names: [Martin] }
              cannot open twice:
                given:
                  - s: Game
                    props: { phase: lobby }
                when:
                  - c: OpenLobby
                then:
                  - x: LobbyAlreadyOpen
              cannot open a started game:
                given:
                  - s: Game
                    props: { phase: started }
                when:
                  - c: OpenLobby
                then:
                  - x: LobbyAlreadyOpen
          ✍️ Start game:
            steps:
              - a: 🧑‍🏫 host /Game lobby
              - c: StartGame
              - x: GameNotFound
              - e: Game / GameStarted
                props: { gameId: Guid, hostPlayerId: Guid }
            tests:
              game starts from the lobby:
                given:
                  - s: Game
                    props: { phase: lobby }
                when:
                  - c: StartGame
                then:
                  - e: Game / GameStarted
              cannot start a game that is not open:
                given:
                  - s: Game
                when:
                  - c: StartGame
                then:
                  - x: GameNotFound
              cannot start twice:
                given:
                  - s: Game
                    props: { phase: started }
                when:
                  - c: StartGame
                then:
                  - x: GameNotFound
          👀 Decision Model:
            steps:
              - s: Game
                props:
                  gameId: Guid
                  phase: GamePhase (lobby|started)
            tests:
              an opened lobby:
                given:
                  - e: Game / LobbyOpened
                then:
                  - s: Game
                    props: { phase: lobby }
              a started game:
                given:
                  - e: Game / LobbyOpened
                  - e: Game / GameStarted
                then:
                  - s: Game
                    props: { phase: started }
        """;

    /// <summary>The GameStarted declaration in <see cref="Clean"/>, anchored by what follows it.</summary>
    private const string GameStartedProps =
        "        props: { gameId: Guid, hostPlayerId: Guid }\n    tests:\n      game starts";

    [Fact]
    public void A_conforming_dialect_document_lints_clean() =>
        Lint(Clean).Should().BeEmpty();

    // --- reference rules -----------------------------------------------------------

    [Fact]
    public void Command_without_event_warns_at_the_command_position()
    {
        var issue = Lint("""
            slices:
              Broken:
                - e: Game/Opened
                - c: DoThing
            """).Should().ContainSingle().Subject;

        issue.Should().Be(new LintIssue(
            "command-without-event", "command should be followed by an event or exception",
            4, 7, LintSeverity.Warning));
    }

    [Fact]
    public void Orphan_exception_warns() =>
        Rules("""
            slices:
              Broken:
                - x: NotAllowed
                - e: Game/Opened
            """).Should().Equal("orphan-exception");

    [Fact]
    public void Slice_with_a_command_and_no_event_warns_at_position_zero()
    {
        var issue = Lint("""
            slices:
              Broken:
                - c: DoThing
                - x: Failed
            """).Should().ContainSingle().Subject;

        issue.Should().Be(new LintIssue(
            "slice-missing-event", "slice \"Broken\" has no events", 0, 0, LintSeverity.Warning));
    }

    [Fact]
    public void Command_less_slices_are_exempt_from_slice_missing_event() =>
        Lint("""
            slices:
              Catalog:
                - v: Pack catalog
              Decision Model:
                - s: Game
            """).Should().BeEmpty();

    [Fact]
    public void Empty_slice_is_a_valid_placeholder() =>
        Lint("slices:\n  Placeholder:\n").Should().BeEmpty();

    // --- RFC emlang-0002: decider rules ------------------------------------------------

    [Fact]
    public void A_decision_test_giving_events_is_an_error()
    {
        var issues = Lint(Clean.Replace(
            "        given:\n          - s: Game\n            props: { phase: lobby }\n        when:\n          - c: StartGame\n",
            "        given:\n          - e: Game / LobbyOpened\n        when:\n          - c: StartGame\n"));

        // The mutation also removes StartGame's lobby scenario, so the coverage warning fires too.
        issues.Should().Contain(i => i.Rule == "em-given-not-one-state" && i.Severity == LintSeverity.Error);
    }

    [Fact]
    public void A_fold_with_an_empty_given_is_an_error() =>
        Rules("""
            slices:
              Decision Model:
                steps:
                  - s: Game
                tests:
                  nothing folds:
                    then:
                      - s: Game
            """).Should().Equal("em-fold-shape");

    [Fact]
    public void An_emitted_event_no_fold_reads_is_outside_the_query() =>
        Rules(Clean.Replace(
            "          - e: Game / LobbyOpened\n          - e: Game / GameStarted\n        then:\n",
            "          - e: Game / LobbyOpened\n        then:\n")).Should().Equal("em-then-outside-query");

    [Fact]
    public void A_folded_event_must_declare_an_identity_prop_of_the_state() =>
        Rules(Clean.Replace(GameStartedProps, "        props: { hostPlayerId: Guid }\n    tests:\n      game starts"))
            .Should().Equal("em-fold-untagged-event");

    [Fact]
    public void A_given_state_with_props_needs_a_fold() =>
        Rules("""
            slices:
              Decide:
                steps:
                  - c: Go
                  - e: Game / Went
                tests:
                  goes:
                    given:
                      - s: Game
                        props: { ready: true }
                    when:
                      - c: Go
                    then:
                      - e: Game / Went
            """).Should().Equal("em-then-outside-query", "em-state-without-fold");

    [Fact]
    public void A_given_phase_no_fold_produces_is_an_error() =>
        Rules(Clean.Replace("            props: { phase: started }\n        when:\n          - c: StartGame",
                            "            props: { phase: ended }\n        when:\n          - c: StartGame"))
            .Should().Contain("em-state-phase-without-fold");

    [Fact]
    public void A_view_given_without_its_state_is_a_todo_warning() =>
        Rules("""
            slices:
              Run:
                steps:
                  - auto: ⚙️ System /Due payments
                  - c: Run
                  - e: Run / Ran
                tests:
                  runs:
                    given:
                      - v: Todo / Due payments
                    when:
                      - c: Run
                    then:
                      - e: Run / Ran
            """).Should().Equal("em-given-not-one-state", "em-given-todo");

    // --- RFC emlang-0003 / 0005: initiators ---------------------------------------------

    [Fact]
    public void An_initiator_after_the_command_warns_and_a_legacy_trigger_is_info()
    {
        var issues = Lint("""
            slices:
              S:
                - c: Go
                - t: ⚙️ System /Somewhere
                - e: Game / Went
            """);

        issues.Select(i => (i.Rule, i.Severity)).Should().Equal(
            ("em-initiator-after-command", LintSeverity.Warning),
            ("em-legacy-trigger", LintSeverity.Info));
    }

    // --- RFC emlang-0004: appendix ---------------------------------------------------------

    [Fact]
    public void Every_command_needs_a_scenario_in_every_phase()
    {
        var issue = Lint(Clean.Replace(
            "      cannot start twice:\n        given:\n          - s: Game\n            props: { phase: started }\n"
            + "        when:\n          - c: StartGame\n        then:\n          - x: GameNotFound\n", ""))
            .Should().ContainSingle().Subject;

        issue.Rule.Should().Be("em-phase-transition-uncovered");
        issue.Message.Should().Be("state 'Game': command 'StartGame' has no scenario in phase 'started'");
    }

    [Fact]
    public void An_event_in_an_actor_slice_carries_exactly_one_actor_prop()
    {
        Rules(Clean.Replace(GameStartedProps, "        props: { gameId: Guid }\n    tests:\n      game starts"))
            .Should().Equal("em-actor-identity");
        Rules(Clean.Replace(GameStartedProps, "        props: { gameId: Guid, hostPlayerId: Guid, startedBy: Guid }\n    tests:\n      game starts"))
            .Should().Equal("em-actor-identity");
    }

    [Fact]
    public void Automation_slices_are_exempt_from_actor_identity() =>
        Lint("""
            slices:
              Score:
                steps:
                  - auto: ⚙️ System /Scoreboard
                  - c: Score
                  - e: Game / Scored
                    props: { gameId: Guid, playerId: Guid }
            """).Should().BeEmpty();

    [Fact]
    public void A_view_prop_no_projection_test_asserts_is_untraced() =>
        Rules(Clean.Replace("props: { names: [Martin] }", "props: {}")).Should().Equal("em-view-prop-untraced");

    [Theory]
    [InlineData("- c: Go\n        props: { asOf: DateOnly (@param) }", 1)]
    [InlineData("- v: Due\n        props: { asOf: DateOnly (@param) }", 0)]
    [InlineData("- v: Due\n        props: { asOf: DateOnly @param }", 1)]
    [InlineData("- v: Due\n        props: { mode: Mode (@param) (dry|live) }", 1)]
    [InlineData("- v: Due\n        props: { mode: Mode (dry|live) (@param) }", 0)]
    public void The_param_note_is_a_last_parenthesized_note_on_a_view(string element, int malformed) =>
        Rules($"slices:\n  S:\n    - e: Run / Ran\n    {element.Replace("\n        ", "\n      ")}\n")
            .Count(r => r == "em-param-note-malformed").Should().Be(malformed);

    [Fact]
    public void Ignored_rules_are_dropped() =>
        Linter.Lint(EmAst.Parse("slices:\n  S:\n    - t: Foo\n    - e: Game / Went\n"), ["em-legacy-trigger"])
            .Should().BeEmpty();
}
