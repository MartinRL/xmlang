using System.Collections.Generic;
using System.Linq;

namespace Xmlang;

/// <summary>A parsed xmlang v0.6 document (see xmlang-spec.md). Purely structural; all
/// Event Model resolution lives in XmLinter, all rendering judgment in the consumer.</summary>
public record XmSpec(
    string? Version,
    IReadOnlyList<string> Models,
    IReadOnlyList<XmPersona> Personas,
    IReadOnlyList<XmSurface> Surfaces,
    IReadOnlyList<XmJourney> Journeys,
    IReadOnlyDictionary<string, XmLabelMap> Labels,
    IReadOnlyList<XmToken> Tokens);

public record XmPersona(string Name, string? Role);

/// <summary>`during` in either form: a bare list of phase values (single-decider documents), or a
/// map from decision model name to its values (v0.6, RFC xmlang-0001 §5). Empty means "all phases".</summary>
public record XmDuring(IReadOnlyList<string> Bare, IReadOnlyDictionary<string, IReadOnlyList<string>> PerModel)
{
    public static readonly XmDuring All = new([], new Dictionary<string, IReadOnlyList<string>>());

    // Methods, not properties: the snapshot tests serialize data, never derivations.
    public bool IsAll() => Bare.Count == 0 && PerModel.Count == 0;

    /// <summary>Every claimed phase value, whichever form declared it.</summary>
    public IEnumerable<string> AllValues() => Bare.Concat(PerModel.Values.SelectMany(v => v));
}

/// <summary>during × for = the coarse activation lattice; empty lists mean "all".</summary>
public record XmSurface(
    string Name,
    IReadOnlyList<string> For,
    XmDuring During,
    IReadOnlyList<XmComposeItem> Compose);

/// <summary>Exactly one of View or Command is non-null (spec: a composition item MUST
/// contain exactly one of v:/c:). A plain pair keeps compose an ordered list.</summary>
public record XmComposeItem(XmViewItem? View, XmCommandItem? Command);

public record XmViewItem(
    string Name,
    IReadOnlyList<string> Primary,
    IReadOnlyList<string> Secondary,
    IReadOnlyList<string> OnDemand,
    string? Self);

/// <summary>Confirm is the raw scalar so the linter can report a non-boolean; Then names the
/// surface the viewer is returned to (a return, never an advance).</summary>
public record XmCommandItem(string Name, string Prominence, string? Confirm = null, string? Then = null)
{
    public bool IsConfirmed() => Confirm == "true";
}

public record XmJourney(string Name, IReadOnlyList<string> For, IReadOnlyList<string> Slices);

/// <summary>One locale's label map: element name → entry (nested exact-name keys, no paths).</summary>
public record XmLabelMap(string? Register, IReadOnlyDictionary<string, XmLabelEntry> Elements);

/// <summary>Self = the element's/field's own label ($self or the string form); Empty = $empty
/// empty-state copy; Confirm = $confirm assent question (commands); Values = $values enum value
/// labels (fields); Fields = field name → entry (views/commands only); IsMap records the form,
/// since an exception key takes the string form only.</summary>
public record XmLabelEntry(
    string? Self,
    string? Empty,
    IReadOnlyDictionary<string, XmLabelEntry> Fields,
    string? Confirm = null,
    IReadOnlyDictionary<string, string>? Values = null,
    bool IsMap = false)
{
    /// <summary>Every label string this entry carries, for ICU argument checks.</summary>
    public IEnumerable<string> AllStrings() =>
        new[] { Self, Empty, Confirm }.Where(s => s is not null)!.Concat(Values?.Values ?? []).Cast<string>();
}

/// <summary>A DTCG token leaf flattened to a dotted path + its $value.</summary>
public record XmToken(string Path, string Value);
