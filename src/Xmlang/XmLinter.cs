using System.Collections.Generic;
using System.Linq;

namespace Xmlang;

public enum XmSeverity { Error, Warning, Info }

public record XmFinding(string Rule, XmSeverity Severity, string Message);

/// <summary>The xmlang v0.2 rule set (xmlang-spec.md "Lint rules"). Pure: XmSpec + EmSpec
/// in, findings out. Duplicate findings (a view composed on several surfaces) collapse
/// via record equality.</summary>
public static class XmLinter
{
    public static IReadOnlyList<XmFinding> Lint(XmSpec xm, EmSpec em)
    {
        var findings = new List<XmFinding>();
        LintVersion(xm, findings);
        LintPersonas(xm, em, findings);
        foreach (var surface in xm.Surfaces)
            LintSurface(surface, xm, em, findings);
        LintPhaseCoverage(xm, em, findings);
        LintJourneys(xm, em, findings);
        LintLabels(xm, em, findings);
        return [.. findings.Distinct()];
    }

    private static void LintVersion(XmSpec xm, List<XmFinding> findings)
    {
        if (xm.Version is null)
            findings.Add(new("xm-version-missing", XmSeverity.Warning, "document carries no 'xmlang' version key"));
    }

    private static void LintPersonas(XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        foreach (var persona in xm.Personas)
            if (persona.Role is { } role && !em.TriggerRoles.Contains(role))
                findings.Add(new("xm-unknown-role", XmSeverity.Warning,
                    $"persona '{persona.Name}' role '{role}' matches no swimlane in the Event Model"));
    }

    private static void LintSurface(XmSurface surface, XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        LintUnknownPersonas(surface.For, $"surface '{surface.Name}'", xm, findings);
        foreach (var phase in surface.During.Where(p => !em.PhaseValues.Contains(p)))
            findings.Add(new("xm-unknown-phase", XmSeverity.Error,
                $"surface '{surface.Name}' during '{phase}' is not a declared phase value"));
        if (surface.Compose.All(item => item.View is null))
            findings.Add(new("xm-surface-without-view", XmSeverity.Error,
                $"surface '{surface.Name}' composes no view"));
        foreach (var item in surface.Compose)
            LintComposeItem(surface.Name, item, em, findings);
        if (em.Elements.Any(e => e.Kind == 'v' && (e.Name == surface.Name || $"{e.Lane} / {e.Name}" == surface.Name)))
            findings.Add(new("xm-surface-shadows-view", XmSeverity.Warning,
                $"surface '{surface.Name}' shares its name with an Event Model view"));
    }

    private static void LintComposeItem(string surface, XmComposeItem item, EmSpec em, List<XmFinding> findings)
    {
        if (item.View is { } view)
            LintViewItem(surface, view, em, findings);
        if (item.Command is { } command && em.FindCommand(command.Name) is null)
            findings.Add(new("xm-dangling-ref", XmSeverity.Error,
                $"surface '{surface}' composes unknown command '{command.Name}'"));
    }

    private static void LintViewItem(string surface, XmViewItem item, EmSpec em, List<XmFinding> findings)
    {
        if (em.FindView(item.Name) is not { } view)
        {
            findings.Add(new("xm-dangling-ref", XmSeverity.Error,
                $"surface '{surface}' composes unknown view '{item.Name}'"));
            return;
        }
        foreach (var field in item.Primary.Concat(item.Secondary).Concat(item.OnDemand))
            if (view.Fields.All(f => f.Name != field))
                findings.Add(new("xm-dangling-ref", XmSeverity.Error,
                    $"surface '{surface}': view '{item.Name}' has no field '{field}'"));
        if (item.Self is { } self && view.Fields.All(f => f.Name != FirstSegment(self)))
            findings.Add(new("xm-self-field-missing", XmSeverity.Error,
                $"surface '{surface}': self '{self}' resolves to no field on '{item.Name}'"));
        if (view.Lane == "Screen")
            findings.Add(new("xm-screen-lane-view", XmSeverity.Info,
                $"view '{item.Name}' sits in a screen-shaped lane; the Event Model names a surface, not data"));
    }

    private static string FirstSegment(string self)
    {
        var dot = self.IndexOf('.');
        return dot < 0 ? self : self[..dot];
    }

    private static void LintPhaseCoverage(XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        // A during-less surface is active in every phase — everything is covered.
        if (xm.Surfaces.Count == 0 || xm.Surfaces.Any(s => s.During.Count == 0))
            return;
        var claimed = xm.Surfaces.SelectMany(s => s.During).ToHashSet();
        foreach (var phase in em.PhaseValues.Where(p => !claimed.Contains(p)))
            findings.Add(new("xm-phase-uncovered", XmSeverity.Warning,
                $"phase '{phase}' is claimed by no surface"));
    }

    private static void LintJourneys(XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        foreach (var journey in xm.Journeys)
        {
            LintUnknownPersonas(journey.For, $"journey '{journey.Name}'", xm, findings);
            foreach (var slice in journey.Slices.Where(s => !em.Slices.Contains(s)))
                findings.Add(new("xm-dangling-ref", XmSeverity.Error,
                    $"journey '{journey.Name}' walks unknown slice '{slice}'"));
        }
    }

    private static void LintUnknownPersonas(IReadOnlyList<string> personas, string owner, XmSpec xm, List<XmFinding> findings)
    {
        foreach (var persona in personas.Where(p => xm.Personas.All(d => d.Name != p)))
            findings.Add(new("xm-unknown-persona", XmSeverity.Error,
                $"{owner} is for undeclared persona '{persona}'"));
    }

    private static void LintLabels(XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        foreach (var (locale, map) in xm.Labels)
            foreach (var (element, entry) in map.Elements)
                LintLabelEntry(locale, element, entry, xm, em, findings);
    }

    private static void LintLabelEntry(
        string locale, string element, XmLabelEntry entry, XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        if (LabelableFields(element, xm, em) is not { } fields)
        {
            findings.Add(new("xm-orphan-label", XmSeverity.Error,
                $"label '{element}' ({locale}) resolves to no element"));
            return;
        }
        foreach (var field in entry.Fields.Keys.Where(f => !fields.Contains(f)))
            findings.Add(new("xm-orphan-label", XmSeverity.Error,
                $"label '{element}' field '{field}' ({locale}) resolves to no field"));
    }

    /// <summary>null = unknown element; empty set = a fieldless element (surface/journey/persona).</summary>
    private static IReadOnlySet<string>? LabelableFields(string element, XmSpec xm, EmSpec em)
    {
        if (em.FindCommand(element) is { } command)
            return command.Fields.Select(f => f.Name).ToHashSet();
        if (em.FindView(element) is { } view)
            return view.Fields.Select(f => f.Name).ToHashSet();
        var known = xm.Surfaces.Any(s => s.Name == element)
            || xm.Journeys.Any(j => j.Name == element)
            || xm.Personas.Any(p => p.Name == element);
        return known ? new HashSet<string>() : null;
    }
}
