namespace Emlang.Linting;

public enum LintSeverity { Warning, Error }

public static class LintSeverities
{
    public static string Display(this LintSeverity severity) =>
        severity == LintSeverity.Error ? "error" : "warning";
}

public sealed record LintIssue(
    string Rule, string Message, int Line, int Column, LintSeverity Severity);

/// <summary>
/// The emlang lint rule set, a faithful port of the Go reference linter
/// (github.com/emlang-project/emlang internal/linter, spec v1.0.0):
/// command-without-event, orphan-exception, slice-missing-event — all warnings.
/// </summary>
public static class Linter
{
    public static IReadOnlyList<LintIssue> Lint(
        EmDocument document, IEnumerable<string>? ignoreRules = null)
    {
        var ignored = new HashSet<string>(ignoreRules ?? []);
        var issues = new List<LintIssue>();

        foreach (var subDoc in document.SubDocs)
            foreach (var slice in subDoc.Slices)
                LintSlice(slice, issues, ignored);

        return issues;
    }

    private static void LintSlice(EmSlice slice, List<LintIssue> issues, HashSet<string> ignored)
    {
        // Empty slice is a valid placeholder.
        if (slice.Elements.Count == 0)
            return;

        var hasEvent = false;
        var hasCommandInSequence = false;

        for (var i = 0; i < slice.Elements.Count; i++)
        {
            var element = slice.Elements[i];

            if (element.Type == EmElementType.Event)
                hasEvent = true;

            if (element.Type == EmElementType.Command)
            {
                hasCommandInSequence = true;
                if (!IsFollowedByEventOrException(slice.Elements, i))
                    Add(issues, ignored, "command-without-event",
                        "command should be followed by an event or exception",
                        element.Line, element.Column);
            }

            if (element.Type == EmElementType.Exception && !hasCommandInSequence)
                Add(issues, ignored, "orphan-exception",
                    "exception without preceding command",
                    element.Line, element.Column);
        }

        if (!hasEvent)
            Add(issues, ignored, "slice-missing-event",
                $"slice \"{slice.Name}\" has no events", 0, 0);
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

    private static void Add(
        List<LintIssue> issues, HashSet<string> ignored,
        string rule, string message, int line, int column)
    {
        if (!ignored.Contains(rule))
            issues.Add(new LintIssue(rule, message, line, column, LintSeverity.Warning));
    }
}
