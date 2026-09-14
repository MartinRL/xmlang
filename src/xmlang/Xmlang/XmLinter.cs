using System.Collections.Generic;
using System.Linq;
using Emlang;
using Emlang.Linting;
using EmElement = Emlang.EmElement;

namespace Xmlang;

public enum XmSeverity { Error, Warning, Info }

public record XmFinding(string Rule, XmSeverity Severity, string Message);

/// <summary>The xmlang v0.6 rule set (xmlang-spec.md "Lint rules"). Pure: XmSpec + EmSpec
/// in, findings out. Duplicate findings (a view composed on several surfaces) collapse
/// via record equality. Roles compare normalized on both sides (RFC emlang-0004).</summary>
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
        LintCells(xm, findings);
        LintOrigins(xm, em, findings);
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
            if (persona.Role is { } role && !em.TriggerRoles.Contains(EmNames.NormalizeRole(role)))
                findings.Add(new("xm-unknown-role", XmSeverity.Warning,
                    $"persona '{persona.Name}' role '{role}' matches no initiator role in the Event Model"));
    }

    // --- surfaces -------------------------------------------------------------------------

    private static void LintSurface(XmSurface surface, XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        LintUnknownPersonas(surface.For, $"surface '{surface.Name}'", xm, findings);
        LintDuring(surface, em, findings);
        if (surface.Compose.All(item => item.View is null))
            findings.Add(new("xm-surface-without-view", XmSeverity.Error,
                $"surface '{surface.Name}' composes no view"));
        foreach (var item in surface.Compose)
            LintComposeItem(surface, item, xm, em, findings);
        if (em.Elements.Any(e => e.Kind is 'v' or 's'
                                 && (e.Name == surface.Name || $"{e.Lane} / {e.Name}" == surface.Name
                                     || e.Kind == 's' && EmSpec.Split(surface.Name).Name == e.Name)))
            findings.Add(new("xm-surface-shadows-view", XmSeverity.Warning,
                $"surface '{surface.Name}' shares its name with an Event Model view"));

        var commands = surface.Compose.Where(i => i.Command is not null).Select(i => i.Command!).ToList();
        if (commands.Count > 0 && commands.Count(c => c.IsConfirmed()) * 2 > commands.Count)
            findings.Add(new("xm-confirm-habituation", XmSeverity.Warning,
                $"surface '{surface.Name}': more than half of its commands ask for confirmation"));
    }

    /// <summary>`during` resolves per decision model: a bare value must be declared by exactly one
    /// model; a map key must be a model with a phase enum; every value must be declared.</summary>
    private static void LintDuring(XmSurface surface, EmSpec em, List<XmFinding> findings)
    {
        foreach (var phase in surface.During.Bare)
        {
            var owners = em.Phases.Where(p => p.Value.Contains(phase)).Select(p => p.Key).ToList();
            if (owners.Count == 0)
                findings.Add(new("xm-unknown-phase", XmSeverity.Error,
                    $"surface '{surface.Name}' during '{phase}' is not a declared phase value"));
            else if (owners.Count > 1)
                findings.Add(new("xm-ambiguous-phase", XmSeverity.Error,
                    $"surface '{surface.Name}' during '{phase}' is declared by {owners.Count} decision models ({string.Join(", ", owners)}); use the map form"));
        }
        foreach (var (model, values) in surface.During.PerModel)
        {
            if (!em.Phases.TryGetValue(EmSpec.Split(model).Name, out var declared))
            {
                findings.Add(new("xm-unknown-phase", XmSeverity.Error,
                    $"surface '{surface.Name}' during '{model}' names no decision model with a phase enum"));
                continue;
            }
            foreach (var phase in values.Where(v => !declared.Contains(v)))
                findings.Add(new("xm-unknown-phase", XmSeverity.Error,
                    $"surface '{surface.Name}' during '{model}: {phase}' is not a declared phase value of '{model}'"));
        }
    }

    private static void LintComposeItem(XmSurface surface, XmComposeItem item, XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        if (item.View is { } view)
            LintViewItem(surface.Name, view, em, findings);
        if (item.Command is { } command)
            LintCommandItem(surface, command, xm, em, findings);
    }

    private static void LintCommandItem(XmSurface surface, XmCommandItem command, XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        if (em.FindCommand(command.Name) is null)
        {
            findings.Add(new("xm-dangling-ref", XmSeverity.Error,
                $"surface '{surface.Name}' composes unknown command '{command.Name}'"));
            return;
        }
        if (command.Confirm is not (null or "true" or "false"))
            findings.Add(new("xm-confirm-not-boolean", XmSeverity.Error,
                $"surface '{surface.Name}': command '{command.Name}' confirm '{command.Confirm}' is not true or false"));

        LintThen(surface, command, xm, em, findings);
        LintCommandTriggers(surface, command, xm, em, findings);
        LintCommandPhases(surface, command, em, findings);
    }

    private static void LintThen(XmSurface surface, XmCommandItem command, XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        if (command.Then is not { } then)
            return;
        if (xm.Surfaces.FirstOrDefault(s => s.Name == then) is not { } target)
        {
            findings.Add(new("xm-dangling-ref", XmSeverity.Error,
                $"surface '{surface.Name}': command '{command.Name}' then '{then}' names no surface"));
            return;
        }
        // The target must apply to every persona the composing surface applies to.
        var composing = surface.For.Count > 0 ? surface.For : xm.Personas.Select(p => p.Name).ToList();
        if (target.For.Count > 0 && composing.Any(p => !target.For.Contains(p)))
            findings.Add(new("xm-then-persona-mismatch", XmSeverity.Error,
                $"surface '{surface.Name}': command '{command.Name}' then '{then}' does not apply to every persona of '{surface.Name}'"));
        // A then naming the surface that composes the command's terminal view restates the default.
        var terminal = em.Chains.Where(c => c.Commands.FirstOrDefault()?.Name == command.Name)
            .Select(c => c.TerminalView).FirstOrDefault(v => v is not null);
        if (terminal is not null && ComposesView(target, terminal))
            findings.Add(new("xm-then-restates-default", XmSeverity.Info,
                $"surface '{surface.Name}': command '{command.Name}' then '{then}' is the destination default (it composes the slice's terminal view '{Reference(terminal)}')"));
    }

    /// <summary>A command on a persona-scoped surface needs an initiator whose role a listed persona carries.</summary>
    private static void LintCommandTriggers(XmSurface surface, XmCommandItem command, XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        if (surface.For.Count == 0)
            return;
        var roles = xm.Personas.Where(p => surface.For.Contains(p.Name) && p.Role is not null)
            .Select(p => EmNames.NormalizeRole(p.Role!)).ToHashSet();
        if (roles.Count == 0)
            return;
        var initiators = em.InitiatorsOf(command.Name).Select(i => i.NormalizedRole).ToList();
        if (!initiators.Any(roles.Contains))
            findings.Add(new("xm-command-trigger-mismatch", XmSeverity.Warning,
                $"surface '{surface.Name}' composes '{command.Name}' for {string.Join(", ", surface.For)}, but no initiator of that command has their role ({string.Join("|", initiators.DefaultIfEmpty("none"))})"));
    }

    /// <summary>A command offered in a phase the Event Model shows no success scenario for.</summary>
    private static void LintCommandPhases(XmSurface surface, XmCommandItem command, EmSpec em, List<XmFinding> findings)
    {
        var scenarios = em.Scenarios.Where(s => s.Command == command.Name && s.State is not null).ToList();
        if (scenarios.Count == 0)
            return;
        foreach (var phase in surface.During.AllValues().Distinct())
            if (!scenarios.Any(s => s.Success && s.Phase == phase))
                findings.Add(new("xm-command-phase-mismatch", XmSeverity.Info,
                    $"surface '{surface.Name}' offers '{command.Name}' during '{phase}', for which the Event Model has no success scenario"));
    }

    private static void LintViewItem(string surface, XmViewItem item, EmSpec em, List<XmFinding> findings)
    {
        // A composed `v:` names a read model, or a decision model by name (its phase is what a
        // surface shows of it; RFC xmlang-0002 maps `s:` to a status badge).
        if ((em.FindView(item.Name) ?? em.FindState(item.Name)) is not { } view)
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

    /// <summary>Per decision model: a declared phase value no surface claims (list or map form).</summary>
    private static void LintPhaseCoverage(XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        // A during-less surface is active in every phase — everything is covered.
        if (xm.Surfaces.Count == 0 || xm.Surfaces.Any(s => s.During.IsAll()))
            return;
        foreach (var (model, declared) in em.Phases)
        {
            var claimed = xm.Surfaces.SelectMany(s => s.During.Bare
                    .Concat(s.During.PerModel.Where(p => EmSpec.Split(p.Key).Name == model).SelectMany(p => p.Value)))
                .ToHashSet();
            foreach (var phase in declared.Where(p => !claimed.Contains(p)))
                findings.Add(new("xm-phase-uncovered", XmSeverity.Warning,
                    $"phase '{phase}' of '{model}' is claimed by no surface"));
        }
    }

    /// <summary>Two surfaces in one during × for cell composing the same view, neither with a command.</summary>
    private static void LintCells(XmSpec xm, List<XmFinding> findings)
    {
        var viewOnly = xm.Surfaces.Where(s => s.Compose.All(i => i.Command is null)).ToList();
        for (var i = 0; i < viewOnly.Count; i++)
            for (var j = i + 1; j < viewOnly.Count; j++)
            {
                var (a, b) = (viewOnly[i], viewOnly[j]);
                if (!a.For.OrderBy(x => x).SequenceEqual(b.For.OrderBy(x => x)))
                    continue;
                if (!(a.During.IsAll() || b.During.IsAll() || a.During.AllValues().Intersect(b.During.AllValues()).Any()))
                    continue;
                var shared = a.Compose.Select(x => x.View!.Name).Intersect(b.Compose.Select(x => x.View!.Name)).ToList();
                if (shared.Count > 0)
                    findings.Add(new("xm-cell-ambiguous", XmSeverity.Info,
                        $"surfaces '{a.Name}' and '{b.Name}' share a during × for cell and compose '{shared[0]}'; neither composes a command"));
            }
    }

    /// <summary>An Event Model initiator origin that names a surface must be admitted there by a persona with its role.</summary>
    private static void LintOrigins(XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        foreach (var initiator in em.Initiators.Where(i => i.Origin.Length > 0))
        {
            if (xm.Surfaces.FirstOrDefault(s => s.Name == initiator.Origin) is not { } surface || surface.For.Count == 0)
                continue;
            var admitted = xm.Personas.Where(p => surface.For.Contains(p.Name) && p.Role is not null)
                .Any(p => EmNames.NormalizeRole(p.Role!) == initiator.NormalizedRole);
            if (!admitted)
                findings.Add(new("xm-origin-mismatch", XmSeverity.Warning,
                    $"initiator '{initiator.Role} /{initiator.Origin}' names surface '{surface.Name}', which admits no persona with role '{initiator.NormalizedRole}'"));
        }
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

    // --- labels --------------------------------------------------------------------------------

    private static void LintLabels(XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        foreach (var (locale, map) in xm.Labels)
            foreach (var (element, entry) in map.Elements)
                LintLabelEntry(locale, element, entry, xm, em, findings);
    }

    private static void LintLabelEntry(
        string locale, string element, XmLabelEntry entry, XmSpec xm, EmSpec em, List<XmFinding> findings)
    {
        if (em.FindException(element) is not null)
        {
            if (entry.IsMap)
                findings.Add(new("xm-orphan-label", XmSeverity.Error,
                    $"label '{element}' ({locale}) is an exception and takes the string form only"));
            LintArguments(locale, element, entry, new Dictionary<string, string>(), findings);
            return;
        }
        if (LabelableFields(element, xm, em) is not { } fields)
        {
            findings.Add(new("xm-orphan-label", XmSeverity.Error,
                $"label '{element}' ({locale}) resolves to no element"));
            return;
        }
        foreach (var field in entry.Fields.Keys.Where(f => !fields.ContainsKey(f)))
            findings.Add(new("xm-orphan-label", XmSeverity.Error,
                $"label '{element}' field '{field}' ({locale}) resolves to no field"));
        foreach (var (field, fieldEntry) in entry.Fields.Where(f => fields.ContainsKey(f.Key)))
        {
            var declared = EnumValues(fields[field]).ToList();
            foreach (var value in fieldEntry.Values?.Keys.Where(v => !declared.Contains(v)) ?? [])
                findings.Add(new("xm-orphan-label", XmSeverity.Error,
                    $"label '{element}' field '{field}' $values '{value}' ({locale}) is not a declared enum value"));
            LintArguments(locale, $"{element}.{field}", fieldEntry, fields, findings);
        }
        LintArguments(locale, element, entry, fields, findings);
    }

    /// <summary>ICU MessageFormat, capped: simple `{name}` and `{name, plural, …}` arguments over scalar fields.</summary>
    private static void LintArguments(
        string locale, string element, XmLabelEntry entry, IReadOnlyDictionary<string, string> fields, List<XmFinding> findings)
    {
        foreach (var text in entry.AllStrings())
            foreach (var (name, type) in Arguments(text))
            {
                if (type is not (null or "plural"))
                {
                    findings.Add(new("xm-label-grammar", XmSeverity.Error,
                        $"label '{element}' ({locale}) uses ICU '{type}'; only simple and plural arguments are allowed"));
                    continue;
                }
                if (!fields.TryGetValue(name, out var annotation) || IsList(annotation))
                    findings.Add(new("xm-label-arg-missing", XmSeverity.Error,
                        $"label '{element}' ({locale}) argument '{{{name}}}' resolves to no scalar field"));
            }
    }

    /// <summary>Top-level `{…}` groups of a message: (argument name, type or null for a simple argument).</summary>
    private static IEnumerable<(string Name, string? Type)> Arguments(string text)
    {
        var depth = 0;
        var start = -1;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '{' && depth++ == 0)
                start = i + 1;
            else if (text[i] == '}' && --depth == 0 && start >= 0)
            {
                var parts = text[start..i].Split(',', 3);
                yield return (parts[0].Trim(), parts.Length > 1 ? parts[1].Trim() : null);
            }
        }
    }

    private static bool IsList(string annotation) =>
        annotation.Trim().Split(' ', 2)[0].EndsWith("[]", System.StringComparison.Ordinal);

    private static IEnumerable<string> EnumValues(string annotation)
    {
        var open = annotation.IndexOf('(');
        var close = open < 0 ? -1 : annotation.IndexOf(')', open + 1);
        if (open < 0 || close < 0)
            return [];
        var note = annotation[(open + 1)..close];
        return note.Contains('|') ? note.Split('|').Select(v => v.Trim()) : [];
    }

    /// <summary>null = unknown element; empty map = a fieldless element (journey/persona); a surface
    /// offers the scalar fields of its composed views, unambiguous names only.</summary>
    private static IReadOnlyDictionary<string, string>? LabelableFields(string element, XmSpec xm, EmSpec em)
    {
        if (em.FindCommand(element) is { } command)
            return command.Fields.ToDictionary(f => f.Name, f => f.Annotation);
        if ((em.FindView(element) ?? em.FindState(element)) is { } view)
            return view.Fields.ToDictionary(f => f.Name, f => f.Annotation);
        if (xm.Surfaces.FirstOrDefault(s => s.Name == element) is { } surface)
            return surface.Compose.Where(i => i.View is not null)
                .Select(i => em.FindView(i.View!.Name) ?? em.FindState(i.View!.Name))
                .Where(v => v is not null)
                .SelectMany(v => v!.Fields)
                .GroupBy(f => f.Name)
                .Where(g => g.Count() == 1) // an ambiguous argument resolves to nothing
                .ToDictionary(g => g.Key, g => g.First().Annotation);
        var known = xm.Journeys.Any(j => j.Name == element) || xm.Personas.Any(p => p.Name == element);
        return known ? new Dictionary<string, string>() : null;
    }

    private static bool ComposesView(XmSurface surface, EmElement view) =>
        surface.Compose.Any(i => i.View is { } v && (v.Name == view.Name || v.Name == Reference(view)
                                                     || EmSpec.Split(v.Name) == (view.Lane, view.Name)));

    private static string Reference(EmElement view) =>
        view.Lane.Length == 0 ? view.Name : $"{view.Lane} / {view.Name}";
}
