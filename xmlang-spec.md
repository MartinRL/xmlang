---
title: "xmlang Specification v0.4.1 (draft)"
description: "A YAML-based DSL for Experience Models: the sibling dialect to emlang, recording UX judgment as data, never as geometry"
created: 2026-07-11
last published: 2026-08-25
release: v0.4.1
tags: [spec, xmlang, emlang, experience-modeling, event-modeling, ux]
---
# xmlang Specification v0.4.1 (draft)

xmlang is a YAML-based DSL for describing the Experience Model of an event-modeled system. An Experience Model records the UX judgments an Event Model deliberately omits (personas, surface composition, salience, journeys, labels, tokens) and depends on the Event Model strictly one-way. It is the sibling dialect to [emlang](https://github.com/emlang-project/spec); the rationale is in [How Far Does a Machine-Readable UX Spec Go?](https://martinrl.github.io/articles/machine-readable-ux-specs-research.html).

The key words "MUST", "MUST NOT", "REQUIRED", "SHALL", "SHALL NOT", "SHOULD", "SHOULD NOT", "RECOMMENDED", "MAY", and "OPTIONAL" in this document are to be interpreted as described in [RFC 2119](https://www.rfc-editor.org/rfc/rfc2119).

## Transformers

This specification uses **transformer** for anything that turns the model pair (Event Model + Experience Model) into a concrete experience: a build-time code generator, a runtime interpreter rendering over a component vocabulary, or a human or agent hand-building UI against the models. Every normative statement addressed to "conforming transformers" binds all three equally; where earlier drafts said "generator", read "transformer".

## Overview

- An xmlang file MUST be a valid YAML file
- The RECOMMENDED file naming convention is `*.xm.yaml`, alongside the Event Model's `*.emlang.yaml`
- The root object MAY contain the keys `xmlang`, `model`, `personas`, `surfaces`, `journeys`, `labels`, and `tokens`
- The root object MUST NOT contain other keys
- `xmlang` is RECOMMENDED and MUST be a string naming the specification version this document targets (e.g. `"0.2"`); its absence SHOULD be reported as a warning (`xm-version-missing`)
- Every root key is OPTIONAL: an Experience Model is adopted judgment by judgment, and an empty document is a valid (vacuous) Experience Model
- A file MAY contain multiple YAML documents separated by `---`; each MUST independently conform to this specification

```yaml
---
xmlang: "0.2"
model: billing.emlang.yaml

personas:
  Accountant:
    role: Accountant

surfaces:
  Dashboard:
    compose:
      - v: OutstandingInvoices
      - v: RecentPayments
      - c: PayInvoice
```

## The one-way dependency rule

- Every reference from an Experience Model to an Event Model element (view, command, slice, field, swimlane, phase value) MUST resolve against the Event Model
- A dangling reference MUST be reported as an error by conforming tools
- The Event Model MUST NOT reference the Experience Model; nothing in this specification is visible from emlang
- The `model` key names the Event Model document(s) references resolve against
- `model` MUST be a string or a list of strings (paths or URIs of emlang documents)
- If `model` is absent, tools MAY resolve references against a model supplied out of band (e.g. a CLI argument), and MUST report an error if no model is available while references exist

```yaml
# ✅ Valid
model: billing.emlang.yaml

# ✅ Valid
model:
  - billing.emlang.yaml
  - onboarding.emlang.yaml
```

## The geometry prohibition

The single rule that separates xmlang from every failed Model-Based UI dialect: **an Experience Model records judgment as data, never as geometry.**

- Memberships, orderings, tiers, names, and tokens are expressible
- Coordinates, dimensions, grids, containers, and breakpoints are NOT expressible; this specification defines no geometric vocabulary and future versions MUST NOT add one
- List order in this specification expresses precedence of importance, never spatial position
- This rule is no longer only a discipline: it was deliberately challenged on its weakest ground by a [pre-registered experiment](https://martinrl.github.io/articles/xmlang-geometry-experiment.html) (v0.3.0-experimental `slot: header|body|footer`) and re-affirmed by its own decision rule — every candidate annotation on the testbed restated a default, and the only genuine placement judgments sat below the key's granularity, demanding exactly the per-field region axis the slope predicts (see the v0.4.0 changelog)
- Conforming tools MUST NOT interpret any `props` content as geometry
- Arrangement of composed elements on a rendered surface is out of scope by design: it belongs to the concrete-UI stratum (agents and humans), pinned by generated verification, not to the model

## Personas

- `personas` MUST be an object mapping persona names to persona definitions
- A persona definition MAY be empty (null value in YAML)
- A persona MAY contain a `role` key attaching it to an Event Model swimlane
- `role` MUST be a string matching a swimlane used in the Event Model; a non-matching `role` SHOULD be reported as a warning (swimlanes are optional in emlang)
- A persona MAY contain a `props` key; `props` is free-form (goals, device, context, accessibility needs)
- A persona MUST NOT contain keys other than `role` and `props`

```yaml
personas:
  Accountant:
    role: Accountant
    props:
      goal: close the books by the 5th
      device: desktop, dual monitor
      accessibility: keyboard-first

  Auditor:
    role: Accountant   # two personas MAY share one swimlane role
```

## Surfaces

A surface is what the user has in front of her (a screen, modal, drawer, card, or chat pane); the noun is deliberately modality-neutral.

- `surfaces` MUST be an object mapping surface names to surface definitions
- A surface definition MUST contain a `compose` key and MAY contain `for`, `during`, and `props` keys, and MUST NOT contain other keys
- `for` MUST be a list of persona names declared under `personas`; if absent, the surface applies to all personas
- `compose` MUST be a list of composition items
- A composition item MUST contain exactly one of `v:` (a view) or `c:` (a command), referencing an Event Model element
- `compose` MUST contain at least one view item and MAY contain command items; a surface composing no view MUST be reported as an error (a bare command form is not a composable surface; it remains reachable through the defaults, see Defaults)
- Item order within `compose` expresses descending importance, not position
- A surface's on-screen heading is its label (see Labels); a surface without a label defaults to its name

### Phase activation with `during`

- A surface MAY contain a `during` key stating when in the system's lifecycle the surface is active
- `during` MUST be a list of phase values
- Phase values resolve against the enum annotation of a prop named `phase` on an Event Model view in the `State` lane, using emlang's parenthesized-note convention (e.g. `phase: AuctionPhase (lobby|started|ended)` declares the values `lobby`, `started`, `ended`)
- A `during` value not among the declared phase values MUST be reported as an error (`xm-unknown-phase`)
- A declared phase value that no surface claims SHOULD be reported as a warning (`xm-phase-uncovered`)
- If `during` is absent, the surface is active in all phases
- `during` × `for` forms the coarse activation lattice: which surfaces exist for which persona in which phase. **Fine selection within one cell of that lattice is deliberately NOT expressible** (e.g. "the viewer has already bid" vs "the viewer has not", or stage 1 vs stage 2 of a multi-step round). That judgment is a hand-written selector in the concrete stratum, typically a small pure function per product. This is a considered rejection of a general `when:` predicate language: every Model-Based UI dialect that grew one drowned in its own dialog model

```yaml
surfaces:
  Bidding:
    during: [started]
    compose:
      - v: CurrentLot
      - c: PlaceBid

  Lobby:
    during: [lobby]
    for: [Host]
    compose:
      - v: LobbyRoster
      - c: StartAuction
```

### View items and field salience

- A view item MAY contain a `fields` key classifying the view's fields into salience tiers
- `fields` MUST be an object whose keys are drawn from `primary`, `secondary`, and `on-demand`, each holding a list of field names from the view's `props` in the Event Model
- A field not listed in any tier defaults to `primary`
- Within a tier, list order expresses descending importance; fields defaulted into `primary` (not listed anywhere) follow the listed primary fields, in Event Model prop order
- A field name not present on the referenced view MUST be reported as an error

### View items and viewer identity with `self`

- A view item MAY contain a `self` key naming the field that carries player/user identity, so transformers can distinguish the viewer's own row or entry (highlighting "you" in a roster or scoreboard)
- `self` MUST be a string: a field name on the referenced view, optionally followed by `.` and a member name when the field's item type is complex (e.g. `players.playerId` where `players: Player[]` and `Player` carries a `playerId`)
- The first segment MUST resolve to a field on the referenced view; an unresolvable first segment MUST be reported as an error (`xm-self-field-missing`)
- Segments beyond the first name a member of the field's item type; tools MAY verify them when the item type is machine-readable, and transformers MUST resolve them
- `self` declares **where identity lives**, nothing more. This is a considered rejection of viewer-relative expressions (`viewer.hasBid`, `row.playerId == viewer.id`) and of a `viewer-is-host:` flag: viewer-conditional composition is already covered by `for:` persona pairs, and everything finer is concrete-stratum selection (see `during`)

### Command items and action prominence

- A command item MAY contain a `prominence` key
- `prominence` MUST be one of `primary`, `secondary`, or `overflow`
- If absent, `prominence` defaults to `primary`

```yaml
surfaces:
  Dashboard:
    for: [Accountant]
    compose:
      - v: OutstandingInvoices
        fields:
          primary: [amount, dueDate]
          secondary: [invoiceNumber]
          on-demand: [auditTrail]
      - v: CashPosition
      - v: RecentPayments
        self: payer.accountId
        fields:
          secondary: [payer, method]
      - c: PayInvoice
        prominence: primary
      - c: ExportCsv
        prominence: overflow
```

- A view or command not composed onto any surface remains reachable through the defaults (see Defaults); composition restricts nothing, it only aggregates

## Journeys

- `journeys` MUST be an object mapping journey names to journey definitions
- A journey definition MUST contain a `slices` key and MAY contain `for` and `props` keys, and MUST NOT contain other keys
- `slices` MUST be a non-empty list of slice names defined in the Event Model, in traversal order
- `for` MUST be a list of persona names declared under `personas`; if absent, the journey applies to all personas
- A journey is the material for generated cross-slice walkthrough tests: conforming transformers SHOULD chain the per-slice generated scenarios in journey order

```yaml
journeys:
  MonthEndClose:
    for: [Accountant]
    slices:
      - ReviewOutstandingInvoices
      - PayInvoice
      - ExportMonthlyReport
```

## Labels

Labels are nested maps keyed by **exact element names**. There is no path grammar: a label key is compared verbatim against Event Model and Experience Model element names, which makes the label section rename-indifferent by construction (renaming an element breaks its label key loudly at lint, never silently at a separator). This is a considered rejection of the v0.1 dotted-path form (`View.field`), which collided with element names containing `.`, `/`, or spaces, and of any slug-normalization scheme.

- `labels` MUST be an object mapping locale tags ([BCP 47](https://www.rfc-editor.org/rfc/rfc5646)) to label maps
- A label map MAY contain a `register` key with a free-form value describing the copy register (e.g. formal, informal)
- Every other key of a label map MUST be the exact name of a command, view, surface, journey, or persona
- The value of an element key MUST be either:
  - a string: the element's own label, or
  - a map whose keys are field names on that element (for views and commands) plus the reserved keys below
- The value of a field key MUST be either a string (the field's label) or a map of reserved keys
- Reserved keys (recognized at both element and field level):
  - `$self` — the element's or field's own label (used when sibling field keys are present)
  - `$empty` — empty-state copy shown when the element or field has no content (an empty list, no winner)
- A label key that resolves to no element, and a field key that resolves to no field on its element, MUST be reported as an error (`xm-orphan-label`)
- Labels are the source of accessible names in generated verification (aria snapshot baselines); an element without a label defaults to its Event Model name

```yaml
labels:
  sv:
    register: lekfull
    PlaceBid: Lägg bud!                # element label, string form
    "Screen / Round results":          # map form: field labels under the element
      trueWorth: Sant värde
      pricePaid: Bud
      winnerIds:
        $self: Vinnare
        $empty: Alla bjöd över!        # empty-state copy for this field
  en:
    PlaceBid: Place bid
```

## Tokens

- `tokens` MUST be an object conforming to the [Design Tokens Format Module](https://www.designtokens.org/tr/drafts/format/) (DTCG), using `$type` and `$value`
- Tokens are the sanctioned visual vocabulary: conforming transformers SHOULD treat them as the design-token source, and token lint in the verification harness SHOULD treat any value outside this vocabulary as a violation
- This specification imposes no structure beyond DTCG conformance

```yaml
tokens:
  color:
    brand:
      $type: color
      $value: "#0B5FFF"
  spacing:
    md:
      $type: dimension
      $value: 16px
```

## Defaults

An Experience Model refines a default experience; it never creates the experience from nothing. In the absence of a section (or of the whole file), conforming transformers MUST provide:

| Absent | Default |
|---|---|
| `personas` | One implicit persona per Event Model swimlane |
| `surfaces` | One surface per slice; every field `primary`; every action `primary`; active in all phases |
| `journeys` | No cross-slice walks; per-slice scenarios only |
| `labels` | Event Model names as labels and accessible names |
| `tokens` | No sanctioned vocabulary; token lint inert |

Composed commands SHOULD render after all of a surface's view content, including on-demand disclosures (the geometry experiment's C1 baseline showed transformers genuinely vary on this ordering; stating the default buys the determinism at zero vocabulary cost). A view's fields MAY be split across such rendered regions by transformer judgment — that split is deliberately not expressible in the model.

Bare command forms (commands composed on no surface, including commands that legitimately have no backing view, like a create or join form) MUST remain reachable; the concrete form is transformer-defined (a generated page, an interpreter's default form, a hand-written route).

This is exactly the wireframe: the Event Model alone determines the default experience, and each Experience Model entry replaces one informal artifact (a persona doc, a sitemap, a journey map) with machine-leveraged data.

## Non-goals (considered and rejected)

Recorded so they are not re-litigated one deadline at a time:

- **Geometry** — see The geometry prohibition
- **A `when:` predicate language** for fine surface selection — `during` × `for` is the ceiling; finer selection is a hand-written pure function in the concrete stratum (see `during`)
- **Viewer-relative expressions** and `viewer-is-host:` — `self` names where identity lives, `for:` covers persona-split surfaces; the rest is concrete-stratum selection
- **`format:` hints** (`format: money`, date patterns, precision) — every formatting judgment is either derivable (an Event Model `decimal` plus the locale of the label map) or concrete-stratum residue; a format vocabulary is the inner platform's first brick
- **Polling, timing, and transport** — when a surface refreshes and how state moves are concrete-stratum decisions (non-normative note: a useful heuristic is that a surface with no typed-input command and a non-terminal phase is a waiting surface, but that inference belongs to the transformer, not the model)

## Lint rules

Conforming tools SHOULD implement at least:

| Rule | Severity | Meaning |
|---|---|---|
| `xm-dangling-ref` | error | A referenced view, command, slice, or field does not exist in the Event Model |
| `xm-unknown-persona` | error | A `for` list names an undeclared persona |
| `xm-orphan-label` | error | A label key resolves to no Event Model or Experience Model element, or a field key to no field on its element |
| `xm-surface-without-view` | error | A surface composes no view (bare command forms fall back to defaults) |
| `xm-self-field-missing` | error | A `self` path's first segment resolves to no field on the referenced view |
| `xm-unknown-phase` | error | A `during` value is not among the phase values declared on the Event Model's `State` lane view |
| `xm-unknown-role` | warning | A persona `role` matches no swimlane in the Event Model |
| `xm-surface-shadows-view` | warning | A surface name equals an Event Model view name; the Event Model likely modeled a surface instead of data |
| `xm-phase-uncovered` | warning | A declared phase value that no surface claims |
| `xm-version-missing` | warning | The document carries no `xmlang` version key |
| `xm-screen-lane-view` | info | An Event Model view sits in a screen-shaped lane (e.g. `Screen /`), i.e. the Event Model names surfaces rather than data; suppressible — xmlang observes Event Model naming, it never legislates it |

## Document structure

- A file MAY contain one or more YAML documents separated by `---`
- Each document MUST independently conform to this specification
- Tools MAY merge documents; a name collision within `personas`, `surfaces`, or `journeys` across merged documents MUST be reported as an error

## Changelog

**Canonical home (2026-08):** this specification moved from martinrl.github.io to [github.com/MartinRL/xmlang](https://github.com/MartinRL/xmlang), where the reference implementation is co-located (NuGet packages `Xmlang` and `Xmlang.Cli`).

### v0.4.0 (2026-08-23) — the geometry prohibition re-affirmed

The v0.3.0-experimental `slot:` key is REMOVED by the pre-registered experiment's own decision rule ([pre-registration](https://martinrl.github.io/articles/xmlang-geometry-experiment.html), [raw scores + tripwire log](https://martinrl.github.io/articles/xmlang-geometry-experiment-scores.html)). The experiment falsified H1 twice over at the annotation phase, before the treatment probe:

- **Dead weight (H0-dead-weight)**: the annotation hunt across all 8 BlindBudet surfaces found ZERO genuine non-default slots — provably, since the live design is rendered by the default transformer; every candidate `slot: footer` restated the command default (`xm-slot-restates-default` 5/5)
- **Slope (H0-slope, tripwire b)**: the only placements where live judgment does real work are per-FIELD region splits (secondary scalars as header chrome; a join QR in the header while its view's roster stays body) — below the key's per-item granularity. The first geometry key demanded the second to do any work; logged, never solved
- **Removed**: `slot:`, `xm-unknown-slot`, `xm-slot-restates-default`; the prohibition section regains its unconditional form, now citing the experiment as evidence rather than discipline
- **Kept (the experiment's residue, zero vocabulary)**: a Defaults clarification — composed commands SHOULD render after all view content including on-demand disclosures. The C1 baseline showed all three clean-room transformers genuinely vary here (rubric item 1, 0/3); a stated default closes it without geometric syntax
- The reference implementation is reverted (kvissig.se `3cc6913` → its revert); probe outputs and scoring sheets remain published as the experiment's data

### v0.3.0-experimental (2026-08-23)

The geometry prohibition, deliberately challenged on pre-registered terms ([experiment](https://martinrl.github.io/articles/xmlang-geometry-experiment.html)). Baseline probes (C1) showed the v0.2 pair leaves two region-placement decisions transformer-variable (commands-vs-on-demand order; secondary-scalar placement); this version tests whether one closed region key closes them.

- **Added `slot:`** (composition-item key, experimental): closed enum `header | body | footer`, optional, absent = transformer judgment. Names a region, never a position; per-field regions explicitly rejected
- **Lint**: `xm-unknown-slot` (error), `xm-slot-restates-default` (info — the dead-weight metric)
- **Defaults**: command items default to the footer region; a view's fields MAY be split across regions by transformer judgment
- **Two exits, both pre-registered**: promote to v0.3.0 stable (C1-variable rubric items converge 3/3 in C2, ≥1 genuine non-default annotation, zero slope tripwires) or revert in v0.4.0 with a "prohibition re-affirmed" entry (dead weight, delta ≈ 0, or any tripwire fires)

### v0.2.0 (2026-08-23)

Driven by the first load-bearing use of an Experience Model: a runtime interpreter rendering game screens directly from the parsed xm (field notes in the research article).

- **Added `during:`** (surface key): phase activation resolved against the enum annotation of a `phase` prop on an Event Model `State` lane view. `during` × `for` is the coarse activation lattice; fine selection is normatively left to the concrete stratum. Rejects a general `when:` predicate language
- **Added `self:`** (view-item key): names the field (or `field.member`) carrying viewer identity. Rejects viewer-relative expressions and `viewer-is-host:`
- **BREAKING: labels are nested two-level maps** keyed by exact element names, with reserved `$self` and `$empty` keys. Replaces v0.1 dotted paths, which broke on element names containing `.`, `/`, or spaces
- **Added `xmlang:`** root version key (RECOMMENDED; `xm-version-missing` warning)
- **Defaults rewritten transformer-neutral**: "transformer" (generator | runtime interpreter | human/agent) replaces "generator" throughout; bare command forms "must remain reachable; the form is transformer-defined"
- **Clarified**: tier order is list order and unlisted primary fields follow in Event Model prop order; surface headings are the surface's label; new Non-goals section records rejected extensions (`format:` hints, polling/timing) so they stay rejected
- **Lint rules v0.2**: added `xm-self-field-missing` (error), `xm-unknown-phase` (error), `xm-phase-uncovered` (warning), `xm-version-missing` (warning), `xm-screen-lane-view` (info, suppressible)

### v0.1.0 (2026-07-11)

Initial draft: personas, surfaces (compose, salience tiers, prominence), journeys, dotted-path labels, DTCG tokens, defaults table, six lint rules, the one-way dependency rule and the geometry prohibition.
