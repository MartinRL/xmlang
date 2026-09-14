using System.Text.RegularExpressions;

namespace Emlang.Linting;

public enum LintSeverity { Info, Warning, Error }

public static class LintSeverities
{
    public static string Display(this LintSeverity severity) => severity switch
    {
        LintSeverity.Error => "error",
        LintSeverity.Warning => "warning",
        _ => "info",
    };
}

public sealed record LintIssue(
    string Rule, string Message, int Line, int Column, LintSeverity Severity);

/// <summary>
/// The dialect's lint rule set: the three reference rules ported from the Go linter
/// (command-without-event, orphan-exception, slice-missing-event) plus the decider rules of
/// RFC emlang-0002, the initiator rules of RFC emlang-0003/0005 and the appendix rules of
/// RFC emlang-0004. Severities are the dialect column of RFC emlang-0004's index. Every
/// rule reads one YAML document (an <see cref="EmSubDoc"/>); "document order carries no
/// meaning" — folds are found by reference, never by position.
/// </summary>
public static class Linter
{
    public static readonly IReadOnlyDictionary<string, LintSeverity> Severities = new Dictionary<string, LintSeverity>
    {
        ["command-without-event"] = LintSeverity.Warning,
        ["orphan-exception"] = LintSeverity.Warning,
        ["slice-missing-event"] = LintSeverity.Warning,
        ["em-given-not-one-state"] = LintSeverity.Error,
        ["em-fold-shape"] = LintSeverity.Error,
        ["em-then-outside-query"] = LintSeverity.Error,
        ["em-fold-untagged-event"] = LintSeverity.Error,
        ["em-state-without-fold"] = LintSeverity.Error,
        ["em-state-phase-without-fold"] = LintSeverity.Error,
        ["em-given-todo"] = LintSeverity.Warning,
        ["em-initiator-after-command"] = LintSeverity.Warning,
        ["em-legacy-trigger"] = LintSeverity.Info,
        ["em-phase-transition-uncovered"] = LintSeverity.Warning,
        ["em-actor-identity"] = LintSeverity.Error,
        ["em-view-prop-untraced"] = LintSeverity.Warning,
        ["em-param-note-malformed"] = LintSeverity.Warning,
    };

    public static IReadOnlyList<LintIssue> Lint(
        EmDocument document, IEnumerable<string>? ignoreRules = null)
    {
        var ignored = new HashSet<string>(ignoreRules ?? []);
        var issues = new List<LintIssue>();
        foreach (var subDoc in document.SubDocs)
            new Pass(subDoc, issues, ignored).Run();
        return issues;
    }

    /// <summary>A fold test: the event types it folds and the phase it pins (RFC 0002 §1, §3).</summary>
    private sealed record Fold(EmTest Test, HashSet<string> EventTypes, string? Phase);

    private sealed class Pass
    {
        private readonly EmSubDoc doc;
        private readonly List<LintIssue> issues;
        private readonly HashSet<string> ignored;

        /// <summary>Props-richest steps occurrence per (kind, name); swimlane is grouping only.</summary>
        private readonly Dictionary<(EmElementType, string), EmElement> declared = new();
        private readonly List<(EmSlice Slice, EmTest Test)> decisions = [];
        private readonly Dictionary<string, List<Fold>> folds = new();

        public Pass(EmSubDoc doc, List<LintIssue> issues, HashSet<string> ignored)
        {
            this.doc = doc;
            this.issues = issues;
            this.ignored = ignored;
            foreach (var slice in doc.Slices)
            {
                foreach (var element in slice.Elements)
                {
                    var key = (element.Type, element.Name);
                    if (!declared.TryGetValue(key, out var existing) || element.Props.Count > existing.Props.Count)
                        declared[key] = element;
                }
                foreach (var test in slice.Tests)
                {
                    if (test.When.Count > 0)
                        decisions.Add((slice, test));
                    else if (IsFold(test))
                        Folds(test.Then[0].Name).Add(new Fold(
                            test,
                            new HashSet<string>(test.Given.Select(e => e.Name)),
                            Scalar(test.Then[0], "phase")));
                }
            }
        }

        public void Run()
        {
            foreach (var slice in doc.Slices)
                LintSlice(slice);
            foreach (var (slice, test) in decisions)
                LintDecision(slice, test);
            foreach (var slice in doc.Slices)
                foreach (var test in slice.Tests.Where(t => t.When.Count == 0))
                    LintFoldShape(test);
            LintPhaseTransitions();
            LintViewProps();
        }

        // --- per slice -------------------------------------------------------------

        private void LintSlice(EmSlice slice)
        {
            // Empty slice is a valid placeholder.
            if (slice.Elements.Count == 0)
                return;

            var hasEvent = false;
            var hasCommand = false;
            for (var i = 0; i < slice.Elements.Count; i++)
            {
                var element = slice.Elements[i];
                if (element.Type == EmElementType.Event)
                    hasEvent = true;

                if (element.Type == EmElementType.Command)
                {
                    hasCommand = true;
                    if (!IsFollowedByEventOrException(slice.Elements, i))
                        Add("command-without-event", "command should be followed by an event or exception", element);
                }

                if (element.Type == EmElementType.Exception && !hasCommand)
                    Add("orphan-exception", "exception without preceding command", element);

                if (element.Type.IsInitiator() && hasCommand)
                    Add("em-initiator-after-command",
                        $"initiator '{element.Swimlane}/{element.Name}' follows the slice's command; initiators precede the command they issue", element);

                if (element.Type == EmElementType.Trigger)
                    Add("em-legacy-trigger",
                        $"legacy trigger 't:'; write 'a:' (actor) or 'auto:' (automation), or run em fmt", element);

                LintParamNotes(element, inTest: false);
            }

            // RFC 0002 §7 / RFC 0004: a command-less slice (Decision Model, projection) has no event by construction.
            if (!hasEvent && hasCommand)
                Add("slice-missing-event", $"slice \"{slice.Name}\" has no events", 0, 0);

            LintActorIdentity(slice);

            foreach (var test in slice.Tests)
                foreach (var element in test.Given.Concat(test.When).Concat(test.Then))
                    LintParamNotes(element, inTest: true);
        }

        /// <summary>RFC 0004 `em-actor-identity`, scoped by kind per RFC 0005: events a slice with a
        /// human initiator produces (those after its first command) carry exactly one actor prop.</summary>
        private void LintActorIdentity(EmSlice slice)
        {
            var roles = slice.Elements
                .Where(e => e.Type.IsInitiator() && !EmNames.IsAutomation(e))
                .Select(e => EmNames.NormalizeRole(e.Swimlane))
                .Where(r => r.Length > 0)
                .ToList();
            if (roles.Count == 0)
                return;

            var afterCommand = slice.Elements
                .SkipWhile(e => e.Type != EmElementType.Command)
                .Where(e => e.Type == EmElementType.Event);
            foreach (var @event in afterCommand)
            {
                var props = Declared(@event).Props.Select(p => p.Key).ToList();
                var actorProps = props.Where(p =>
                    p.EndsWith("By", StringComparison.Ordinal)
                    || roles.Any(r => p.ToLowerInvariant().StartsWith(r, StringComparison.Ordinal)
                                      && p.EndsWith("Id", StringComparison.OrdinalIgnoreCase))).ToList();
                if (actorProps.Count == 0)
                    Add("em-actor-identity",
                        $"event '{@event.Name}' in an actor-initiated slice carries no actor prop (<verb>By or <role>Id for {string.Join("|", roles)})", @event);
                else if (actorProps.Count > 1)
                    Add("em-actor-identity",
                        $"event '{@event.Name}' carries {actorProps.Count} actor props ({string.Join(", ", actorProps)}); exactly one is expected", @event);
            }
        }

        private static readonly Regex ParamNoteLast = new(@"\(@param\)\s*$", RegexOptions.Compiled);

        /// <summary>RFC 0004 `(@param)`: the note marks a projection argument, last note, views in steps only.</summary>
        private void LintParamNotes(EmElement element, bool inTest)
        {
            foreach (var prop in element.Props)
            {
                if (prop.Value is not string value || value.IndexOf("@param", StringComparison.Ordinal) < 0)
                    continue;
                if (inTest)
                    Add("em-param-note-malformed", $"prop '{prop.Key}': (@param) is a steps note; test props are fixture values", element);
                else if (element.Type != EmElementType.View)
                    Add("em-param-note-malformed", $"prop '{prop.Key}': (@param) marks a projection argument and belongs on a view", element);
                else if (value.IndexOf("(@param)", StringComparison.Ordinal) < 0)
                    Add("em-param-note-malformed", $"prop '{prop.Key}': @param must be written as the parenthesized note (@param)", element);
                else if (!ParamNoteLast.IsMatch(value))
                    Add("em-param-note-malformed", $"prop '{prop.Key}': (@param) must be the last parenthesized note", element);
            }
        }

        // --- per decision test (RFC 0002 §2-§4) ----------------------------------------

        private void LintDecision(EmSlice slice, EmTest test)
        {
            var command = test.When[0];
            var states = test.Given.Where(e => e.Type == EmElementType.State).ToList();
            var events = test.Given.Where(e => e.Type == EmElementType.Event).ToList();
            var views = test.Given.Where(e => e.Type == EmElementType.View).ToList();

            if (states.Count != 1 || events.Count > 0)
                Add("em-given-not-one-state",
                    $"test \"{test.Name}\" gives {states.Count} state(s) and {events.Count} event(s); a decision test gives exactly one state (empty state = no props), optionally with views", command);

            if (views.Count > 0 && states.Count == 0)
                Add("em-given-todo",
                    $"test \"{test.Name}\" gives a view and no state; add the state of the decision model the command validates against", views[0]);

            if (states.Count != 1 || events.Count > 0)
                return;

            var state = states[0];
            var stateFolds = folds.TryGetValue(state.Name, out var list) ? list : [];
            var query = new HashSet<string>(stateFolds.SelectMany(f => f.EventTypes));

            foreach (var @event in test.Then.Where(e => e.Type == EmElementType.Event))
                if (!query.Contains(@event.Name))
                    Add("em-then-outside-query",
                        $"event '{@event.Name}' is emitted by a decision on '{state.Name}' but folded by no fold test of '{state.Name}'; the append condition cannot cover it", @event);

            if (state.Props.Count > 0 && stateFolds.Count == 0)
                Add("em-state-without-fold",
                    $"state '{state.Name}' is given with props but is the then: of no fold test", state);

            if (Scalar(state, "phase") is { } phase && stateFolds.Count > 0 && stateFolds.All(f => f.Phase != phase))
                Add("em-state-phase-without-fold",
                    $"state '{state.Name}' is given in phase '{phase}', which no fold test produces", state);
        }

        /// <summary>RFC 0002 §1 `em-fold-shape`: a when-less test that pins a state is a fold and folds events.</summary>
        private void LintFoldShape(EmTest test)
        {
            var pinned = test.Then.FirstOrDefault(e => e.Type == EmElementType.State);
            if (pinned is null)
                return;
            if (test.Given.Count == 0)
                Add("em-fold-shape", $"fold test \"{test.Name}\" has an empty given; a fold folds at least one event", pinned);
            else if (test.Given.Any(e => e.Type != EmElementType.Event))
                Add("em-fold-shape", $"fold test \"{test.Name}\" gives a non-event; a fold gives events only", pinned);
            if (test.Then.Count > 1)
                Add("em-fold-shape", $"fold test \"{test.Name}\" pins {test.Then.Count} elements; a fold pins exactly one state", pinned);
            else if (IsFold(test))
                LintFoldTags(test, pinned);
        }

        /// <summary>RFC 0002 §3 `em-fold-untagged-event`: every folded event type declares one of the state's identity props.</summary>
        private void LintFoldTags(EmTest test, EmElement pinned)
        {
            var identity = new HashSet<string>(Declared(pinned).Props.Select(p => p.Key)
                .Where(k => k.EndsWith("Id", StringComparison.Ordinal) || k.EndsWith("Ids", StringComparison.Ordinal)));
            foreach (var @event in test.Given)
                if (!Declared(@event).Props.Any(p => identity.Contains(p.Key)))
                    Add("em-fold-untagged-event",
                        $"event '{@event.Name}' is folded into '{pinned.Name}' but declares none of its identity props ({(identity.Count == 0 ? "none declared" : string.Join(", ", identity))})", @event);
        }

        // --- per document (RFC 0004) ---------------------------------------------------

        /// <summary>`em-phase-transition-uncovered`: for every command a state's decision tests use and every declared phase, one scenario.</summary>
        private void LintPhaseTransitions()
        {
            foreach (var state in declared.Values.Where(e => e.Type == EmElementType.State))
            {
                var values = EnumValues(Scalar(state, "phase") ?? "").ToList();
                if (values.Count == 0)
                    continue;
                var scenarios = decisions
                    .Select(d => d.Test)
                    .Where(t => t.Given.Count(e => e.Type == EmElementType.State) == 1
                                && t.Given.Single(e => e.Type == EmElementType.State).Name == state.Name)
                    .Select(t => (Command: t.When[0].Name, Phase: Scalar(t.Given.Single(e => e.Type == EmElementType.State), "phase")))
                    .ToList();
                foreach (var command in scenarios.Select(s => s.Command).Distinct())
                    foreach (var value in values.Where(v => !scenarios.Any(s => s.Command == command && s.Phase == v)))
                        Add("em-phase-transition-uncovered",
                            $"state '{state.Name}': command '{command}' has no scenario in phase '{value}'", state);
            }
        }

        /// <summary>`em-view-prop-untraced`: every declared view prop (bar projection arguments) is asserted by some projection test.</summary>
        private void LintViewProps()
        {
            var asserted = new Dictionary<string, HashSet<string>>();
            foreach (var test in doc.Slices.SelectMany(s => s.Tests))
            {
                if (test.When.Count > 0 || test.Then.Count != 1 || test.Then[0].Type != EmElementType.View
                    || test.Given.Count == 0 || test.Given.Any(e => e.Type != EmElementType.Event))
                    continue;
                if (!asserted.TryGetValue(test.Then[0].Name, out var props))
                    asserted[test.Then[0].Name] = props = [];
                foreach (var prop in test.Then[0].Props)
                    props.Add(prop.Key);
            }
            foreach (var view in declared.Values.Where(e => e.Type == EmElementType.View))
            {
                var traced = asserted.TryGetValue(view.Name, out var props) ? props : [];
                foreach (var prop in view.Props.Where(p => !(p.Value is string v && v.IndexOf("(@param)", StringComparison.Ordinal) >= 0)))
                    if (!traced.Contains(prop.Key))
                        Add("em-view-prop-untraced",
                            $"view '{view.Name}' prop '{prop.Key}' is asserted in no projection test", view);
            }
        }

        // --- helpers ----------------------------------------------------------------------

        private static bool IsFold(EmTest test) =>
            test.When.Count == 0 && test.Then.Count == 1 && test.Then[0].Type == EmElementType.State
            && test.Given.Count > 0 && test.Given.All(e => e.Type == EmElementType.Event);

        private List<Fold> Folds(string state)
        {
            if (!folds.TryGetValue(state, out var list))
                folds[state] = list = [];
            return list;
        }

        private EmElement Declared(EmElement reference) =>
            declared.TryGetValue((reference.Type, reference.Name), out var element) ? element : reference;

        private static string? Scalar(EmElement element, string key) =>
            element.Props.FirstOrDefault(p => p.Key == key).Value as string;

        /// <summary>emlang's parenthesized-note enum convention: "GamePhase (lobby|started|ended)".</summary>
        public static IEnumerable<string> EnumValues(string annotation)
        {
            var open = annotation.IndexOf('(');
            var close = open < 0 ? -1 : annotation.IndexOf(')', open + 1);
            if (open < 0 || close < 0)
                return [];
            var note = annotation.Substring(open + 1, close - open - 1);
            return note.IndexOf('|') >= 0 ? note.Split('|').Select(v => v.Trim()) : [];
        }

        private static bool IsFollowedByEventOrException(IReadOnlyList<EmElement> elements, int index)
        {
            for (var i = index + 1; i < elements.Count; i++)
            {
                switch (elements[i].Type)
                {
                    case EmElementType.Event:
                    case EmElementType.Exception:
                        return true;
                    case EmElementType.Command:
                        return false;
                }
            }
            return false;
        }

        private void Add(string rule, string message, EmElement at) => Add(rule, message, at.Line, at.Column);

        private void Add(string rule, string message, int line, int column)
        {
            if (!ignored.Contains(rule))
                issues.Add(new LintIssue(rule, message, line, column, Severities[rule]));
        }
    }
}
