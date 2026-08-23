using System.Collections.Generic;

namespace Xmlang;

/// <summary>A parsed xmlang v0.2 document (see xmlang-spec.md). Purely structural; all
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

/// <summary>during × for = the coarse activation lattice; empty lists mean "all".</summary>
public record XmSurface(
    string Name,
    IReadOnlyList<string> For,
    IReadOnlyList<string> During,
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

public record XmCommandItem(string Name, string Prominence);

public record XmJourney(string Name, IReadOnlyList<string> For, IReadOnlyList<string> Slices);

/// <summary>One locale's label map: element name → entry (nested exact-name keys, no paths).</summary>
public record XmLabelMap(string? Register, IReadOnlyDictionary<string, XmLabelEntry> Elements);

/// <summary>Self = the element's/field's own label ($self or the string form);
/// Empty = $empty empty-state copy; Fields = field name → entry (views/commands only).</summary>
public record XmLabelEntry(string? Self, string? Empty, IReadOnlyDictionary<string, XmLabelEntry> Fields);

/// <summary>A DTCG token leaf flattened to a dotted path + its $value.</summary>
public record XmToken(string Path, string Value);
