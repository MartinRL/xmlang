---
title: "xmlang Specification v0.6.0 (draft)"
description: "A YAML-based DSL for Experience Models: the sibling dialect to emlang, recording UX judgment as data, never as geometry"
created: 2026-07-11
last published: 2026-09-14
release: xmlang-v0.6.0
tags: [spec, xmlang, emlang, experience-modeling, event-modeling, ux]
---
# xmlang Specification v0.6.0 (draft)

xmlang is a YAML-based DSL for describing the Experience Model of an event-modeled system. An Experience Model records the UX judgments an Event Model deliberately omits (personas, surface composition, salience, journeys, labels, tokens) and depends on the Event Model strictly one-way. It is the sibling dialect to emlang, in the [decider dialect](emlang-dialect.md) this repository implements; the rationale is in [How Far Does a Machine-Readable UX Spec Go?](https://martinrl.github.io/articles/machine-readable-ux-specs-research.html).

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

- A surface MAY contain a `during` key stating when in the lifecycle of **the decision model the surface is about** the surface is active
- `during` MUST be either a list of phase values (valid when every listed value is declared by exactly one decision model) or a map from a decision model name to a list of its phase values
- Phase values resolve against the enum annotation of a prop named `phase` on an Event Model state element (`s:`, the decider dialect's decision model), using emlang's parenthesized-note convention (e.g. `phase: AuctionPhase (lobby|started|ended)` declares the values `lobby`, `started`, `ended`); phase values are namespaced by decision model
- A `during` value not among the phase values its decision model declares MUST be reported as an error (`xm-unknown-phase`)
- A bare `during` value declared by more than one decision model MUST be reported as an error (`xm-ambiguous-phase`); the map form disambiguates
- A declared phase value that no surface claims SHOULD be reported as a warning (`xm-phase-uncovered`); the check resolves per decision model
- If `during` is absent, the surface is active in all phases
- `during` × `for` forms the coarse activation lattice: which surfaces exist for which persona in which phase. Fine selection within one cell is derived from the Event Model; see Defaults. This is a considered rejection of a general `when:` predicate language, which would be redundant

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

  Standings:
    during: { Game: [ended] }        # map form: the decision model named, for multi-decider models
    compose:
      - v: Scoreboard
```

> Non-normative. Fine selection within a cell was declared inexpressible in v0.2 and implemented as a hand-written selector in every product. It is derivable. Read the phase's slices in order; each state-change slice ends in the view the actor sees next. A viewer's position is the terminal view of the last slice whose event fired for her: her own command's event moves her (PlaceBid → the waiting surface), a System processor's event moves everyone (LotRevealed → the results surface), another persona's event moves her (StartAuction → the bidding surface), and a peer's event does not. This reproduces the selectors of all three testbed games and all three clean-room probes. On a line-of-business model with one record surface per persona per phase the rule is trivially true. Where a surface is entered by no event at all, the transition is navigation or a missing command, never a predicate.

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
- A command item MAY contain a `confirm` key
- `confirm` MUST be a boolean; if absent it defaults to `false`; a non-boolean value MUST be reported as an error (`xm-confirm-not-boolean`)
- `confirm: true` states that the transformer MUST obtain the viewer's explicit assent before issuing the command; the form of assent (dialog, inline second step, typed re-entry) is transformer-defined and MUST NOT be expressed in the model
- The assent copy is the command's `$confirm` label (see Labels); absent, transformers MUST use the command's own label in a generic question
- `confirm` records a judgment about consequence and friction, never about geometry or degree; it is the whole of xmlang's confirmation vocabulary, and future versions MUST NOT add a tier, placement or style axis to it. Conforming tools MUST NOT derive `confirm` from an Event Model compensation fact
- More than half of the commands composed on one surface carrying `confirm: true` SHOULD be reported as a warning (`xm-confirm-habituation`)
- A command item MAY contain a `then` key naming the surface the viewer is returned to after the command is accepted
- `then` MUST be a single surface name declared under `surfaces`; an unknown name MUST be reported as an error (`xm-dangling-ref`)
- The named surface MUST apply to every persona the composing surface applies to; a mismatch MUST be reported as an error (`xm-then-persona-mismatch`)
- `then` expresses a return, never an advance. The advance is the Event Model's: the terminal view of the command's slice names what the actor sees next (see Defaults). A `then` naming the surface that composes that terminal view restates the default and SHOULD be reported as an info finding (`xm-then-restates-default`)
- `then` is never conditional and never selects an instance. A command whose destination depends on the outcome is two commands in the Event Model; a rejected command always re-presents the issuing surface; "the next item" is a worklist view or a command, not a destination

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
        then: Dashboard          # return to the worklist; the default would show the paid record
      - c: ExportCsv
        prominence: overflow
      - c: VoidInvoice
        prominence: overflow
        confirm: true            # consequence to others: posted amounts are reversed
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
- Every other key of a label map MUST be the exact name of a command, view, surface, journey, persona, or exception (an `x:` element); an exception key takes the string form only
- The value of an element key MUST be either:
  - a string: the element's own label, or
  - a map whose keys are field names on that element (for views and commands) plus the reserved keys below
- The value of a field key MUST be either a string (the field's label) or a map of reserved keys
- Reserved keys (recognized at both element and field level unless stated):
  - `$self` — the element's or field's own label (used when sibling field keys are present)
  - `$empty` — empty-state copy shown when the element or field has no content (an empty list, no winner)
  - `$confirm` (command level) — the assent question shown when the command is composed with `confirm: true`
  - `$values` (field level) — a map from the field's declared enum values (emlang's parenthesized-note convention) to labels; a key that is not a declared value MUST be reported as an error (`xm-orphan-label`)
- A label key that resolves to no element, and a field key that resolves to no field on its element, MUST be reported as an error (`xm-orphan-label`)
- A label string MAY be an [ICU MessageFormat](https://unicode-org.github.io/icu/userguide/format_parse/messages/) message restricted to simple arguments `{name}` and plural arguments `{name, plural, …}`; `select` and nested conditionals MUST NOT be used and MUST be reported as an error (`xm-label-grammar`). An argument MUST name a scalar field of the labelled element or, for a surface label, of one of the surface's composed views; an unresolvable or ambiguous argument MUST be reported as an error (`xm-label-arg-missing`)
- A surface's on-screen heading remains its label. A heading that carries data is a surface label with an argument; this specification defines no `heading` or `subheading` key
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
    NotEnoughPlayers: At least two players are needed.   # an exception, string form only
    EndAuction:
      $self: End auction
      $confirm: "End the auction now? No more bids can be placed."
    "Screen / Round results":
      trueWorth: "True worth: {trueWorth}"                  # an ICU argument over a scalar field
      phase:
        $self: Status
        $values: { lobby: Waiting, started: Bidding, ended: Closed }
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

### Navigation and enablement defaults

These rows state what every shipped interpreter and every clean-room probe computed identically; stating them buys cross-transformer determinism at zero vocabulary, as the v0.4.0 command-order default did.

| Absent | Default |
|---|---|
| Entry surface | A persona's entry surface is the first surface in document order that applies to the persona and carries no `during`; a persona with no such surface enters through the bare form of its first initiated command |
| Global navigation | The surfaces the entry rule ranges over, in document order; `during`-bound surfaces are reached through destinations and links; a terminal-phase surface offers a return to the entry surface |
| Destination after a command | The surface applicable to the viewer's persona that composes the terminal view of the command's slice, in the resulting phase; if there is none, or it is the issuing surface, the issuing surface (stay); a rejected command re-presents the issuing surface. `then` overrides |
| Selection within a `during` × `for` cell | If the cell holds one `during`-bound surface for the persona, that surface. If several, the surface composing the terminal view of the slice whose event fired most recently for the viewer: the viewer's own command's event, an automation's event, or another persona's event advance the viewer; an event from a peer of the viewer's own persona does not. Ties are broken by offerable commands, then declaration order |
| Enablement | A composed command is rendered only to personas whose `role` matches one of the command's initiator roles. A command the Event Model shows would be rejected for this viewer in the current state independent of typed input (a rejection scenario whose `given` differs from a success scenario's while its `when` props match) is rendered disabled with the blocking exception's label, never hidden by state alone. Input-validation and concurrency rejections are reported after the attempt |
| Required input | A command prop is required iff some rejection scenario rejects its absence |
| Bulk actions | A command whose prop is a list of a composed view's item identity is offered over the viewer's selection of that view |
| Links | A view field typed as another view's identity links to the surface composing that view for the persona |
| Unidentified viewer | Sees only surfaces with no `for:`, with commands withheld, and is routed to the bare entry form where one exists |

This is exactly the wireframe: the Event Model alone determines the default experience, and each Experience Model entry replaces one informal artifact (a persona doc, a sitemap, a journey map) with machine-leveraged data.

## Non-goals (considered and rejected)

Recorded so they are not re-litigated one deadline at a time:

- **Geometry** — see The geometry prohibition
- **A `when:` predicate language** for fine surface selection — rejected because redundant, not only dangerous: fine selection is derived (see Defaults); anything else `when:` would express is a missing phase value, a missing command (an intent with no event), or a decider guard
- **Viewer-relative expressions** and `viewer-is-host:` — `self` names where identity lives, `for:` covers persona-split surfaces; the rest is concrete-stratum selection
- **`format:` hints** (`format: money`, date patterns, precision) — every formatting judgment is either derivable (an Event Model `decimal` plus the locale of the label map) or concrete-stratum residue; a format vocabulary is the inner platform's first brick
- **Polling, timing, and transport** — when a surface refreshes and how state moves are concrete-stratum decisions (non-normative note: a useful heuristic is that a surface with no typed-input command and a non-terminal phase is a waiting surface, but that inference belongs to the transformer, not the model)
- **Entry surface keys** — document order expresses the entry surface (see Defaults); the judgment is positional, which is this specification's "order = importance" doctrine
- **Wizard steps and command-prop grouping** — a multi-step interaction with intermediate state is several slices (a draft and its edits); chunking one command's fields is concrete; groups need labels, so the first instance demands a new element kind
- **Conditional and instance destinations** (`then-on-reject`, `then: next`) — a destination that depends on outcome is two commands; "the next item" is a worklist view or a command
- **A `heading` or `subheading` key** — a heading is the surface's label, with an argument when it carries data; a field promoted out of its tier into a region is `slot:` again
- **Posture** (read-only/editable, sovereign/transient) — editable follows from composing a command; the rest is a container property
- **Presentation modality** (modal, drawer, inline, page edit) — placement
- **Undo pairing** — which command compensates which is an Event Model fact; xmlang decides friction and prominence only
- **Per-command hide/disable** — hide-by-role is `for:` plus the enablement default; disable-by-state is the enablement default; a switch restates a design-system-wide rule
- **Confirmation tiers** (`impact:`, type-to-confirm) — degree of friction is the transformer's rendering of consequence; `prominence: overflow` with `confirm: true` is the strong form
- **Message channel, loading thresholds** — fixed once per design system in every system surveyed, never per action; timing is already a Non-goal
- **Related-surface edges, topology archetypes, master-detail arrangement, site IA across Event Models** — identity lineage derives the relation; arrangement is geometry; whole-product shape is a graph property of the Event Model

### Deferred, with a re-open condition

- **Status criticality per enum value** — a closed semantic tier (`negative | critical | positive | neutral | information`) per declared enum value, which tokens then bind. Not derivable, not geometry, recorded per value by four design systems. One instance across four testbeds; a token-naming convention (`color.status.<value>`) carries it today. Re-open when a second line-of-business model needs a semantic state the token path cannot express

## Lint rules

Conforming tools SHOULD implement at least:

| Rule | Severity | Meaning |
|---|---|---|
| `xm-dangling-ref` | error | A referenced view, command, slice, or field does not exist in the Event Model |
| `xm-unknown-persona` | error | A `for` list names an undeclared persona |
| `xm-orphan-label` | error | A label key resolves to no Event Model or Experience Model element, or a field key to no field on its element |
| `xm-surface-without-view` | error | A surface composes no view (bare command forms fall back to defaults) |
| `xm-self-field-missing` | error | A `self` path's first segment resolves to no field on the referenced view |
| `xm-unknown-phase` | error | A `during` value is not among the phase values its decision model declares (an `s:` element's `phase` enum) |
| `xm-ambiguous-phase` | error | A bare `during` value is declared by more than one decision model |
| `xm-confirm-not-boolean` | error | `confirm` is not `true` or `false` |
| `xm-then-persona-mismatch` | error | A `then` surface does not apply to every persona of the composing surface |
| `xm-label-arg-missing` | error | A label argument resolves to no scalar field of the labelled element or, for a surface, of its composed views, or resolves ambiguously |
| `xm-label-grammar` | error | A label uses ICU constructs beyond simple and plural arguments |
| `xm-unknown-role` | warning | A persona `role` matches no initiator role in the Event Model (roles compare normalized: leading emoji stripped, lowercased) |
| `xm-confirm-habituation` | warning | More than half of the commands composed on one surface carry `confirm: true` |
| `xm-command-trigger-mismatch` | warning | A command composed on a `for:`-scoped surface has no Event Model initiator whose role matches any listed persona's `role` |
| `xm-origin-mismatch` | warning | An Event Model initiator origin (`a: Role /origin`) names a surface, and no surface of that name admits a persona with that `role`; origin text matching no surface at all is free text |
| `xm-surface-shadows-view` | warning | A surface name equals an Event Model view name; the Event Model likely modeled a surface instead of data |
| `xm-phase-uncovered` | warning | A declared phase value that no surface claims |
| `xm-version-missing` | warning | The document carries no `xmlang` version key |
| `xm-then-restates-default` | info | A `then` names the surface the destination default would pick (the dead-weight metric) |
| `xm-command-phase-mismatch` | info | A command is composed on a surface active in a phase for which the Event Model has no success scenario for that command |
| `xm-cell-ambiguous` | info | Two surfaces in one `during` × `for` cell compose the same view and neither composes a command; suppressible |
| `xm-screen-lane-view` | info | An Event Model view sits in a screen-shaped lane (e.g. `Screen /`), i.e. the Event Model names surfaces rather than data; suppressible — xmlang observes Event Model naming, it never legislates it |

## Document structure

- A file MAY contain one or more YAML documents separated by `---`
- Each document MUST independently conform to this specification
- Tools MAY merge documents; a name collision within `personas`, `surfaces`, or `journeys` across merged documents MUST be reported as an error

## Changelog

**Canonical home (2026-08):** this specification moved from martinrl.github.io to [github.com/MartinRL/xmlang](https://github.com/MartinRL/xmlang), where the reference implementation is co-located (NuGet packages `Xmlang` and `Xmlang.Cli`).

### v0.6.0 (2026-09-14) — interaction judgments, and the derived interaction stratum

Driven by the Interaction Model inquiry ([RFC xmlang-0001](rfcs/xmlang-0001-interaction-model.md), accepted 2026-09-14) and its line-of-business testbed, and by the emlang decider dialect ([RFC emlang-0002](rfcs/emlang-0002-decider-profile.md)).

- **Added `confirm:`** (command item, boolean): the viewer's explicit assent before a command. Form of assent is transformer-defined. Rejects tiers, placement and derivation from compensation
- **Added `then:`** (command item, surface name): return to a surface after an accepted command. Return, never advance; never conditional; never an instance. Provisionally admitted; `xm-then-restates-default` is its dead-weight metric
- **Labels**: exception names are labelable; reserved `$confirm` (command) and `$values` (field); ICU MessageFormat arguments restricted to simple and plural; surface headings carry data through arguments. Replaces a proposed `heading:` key
- **Defaults**: new sub-table "Navigation and enablement defaults" (entry, global navigation, destination, selection within a cell, enablement, required input, bulk, links, unidentified viewer). **Corrects v0.2**: fine selection within a cell is derived, not inexpressible; the `when:` Non-goal is rewritten as redundant
- **`during` resolves per decision model**: phases come from the decider dialect's `s:` state element (the `State` lane on a view carries no meaning); map form added; `xm-ambiguous-phase`. Fixes a namespace collision in multi-decider models
- **Lint rules v0.6**: eleven rules added (`xm-ambiguous-phase`, `xm-confirm-not-boolean`, `xm-then-persona-mismatch`, `xm-label-arg-missing`, `xm-label-grammar`, `xm-confirm-habituation`, `xm-command-trigger-mismatch`, `xm-origin-mismatch`, `xm-then-restates-default`, `xm-command-phase-mismatch`, `xm-cell-ambiguous`); `xm-unknown-role` compares normalized roles
- **Non-goals**: twelve entries recording what the inquiry rejected; **Deferred**: status criticality per enum value
- Migration: none required; all v0.5 documents remain valid v0.6 documents. Single-decider documents keep the list form of `during`

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
