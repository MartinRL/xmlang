---
title: "RFC emlang-0003: Trigger sets and trigger origin (draft)"
status: draft
created: 2026-09-09
targets: emlang spec 1.0.0 (prose and lint only; no schema change)
depends: Section A none; Section B on RFC emlang-0002 for the `profile: decider` severity column (RFC emlang-0001 defines the header, emlang-0002 the profile)
consumed by: xmlang RFC 0001, debts 1 and 2
---
# RFC emlang-0003: Trigger sets and trigger origin

**Status: draft.** Nothing here is applied to the upstream spec, to `src/`, or to the fixtures. This RFC is the artifact the maintainer decides on. It has two parts. Section A (trigger sets: who may issue a command) stands alone. Section B (trigger origin: where the actor issues it from) depends on RFC emlang-0002 for its profile severity, and the red team recommends moving its resolution rule to xmlang (`redteam.md` R15, R16); Section B presents that alternative beside the drafted rule and does not decide it.

## Summary

1. Section A: a slice MAY carry several `t:` elements; each is a legal initiator of the slice's command, and the set is the command's trigger set. This gives an existing legal shape a meaning and changes no validator.
2. Section B: the text after a trigger's swimlane separator is the trigger's origin; it MUST either name a `v:` in the same document or be a bare form, declared by naming the slice's own command. A lint `em-trigger-origin-unresolved` reports the rest.
3. Both parts are prose plus lint. Every rule below is checkable against the closed v1.0.0 schema without touching it.

## Motivation

The emlang spec keeps a screen name in exactly one place: the trigger. `t: Customer/RegistrationForm` (SPEC L76) is the vestige of Event Modeling's wireframe row, and the spec attaches no meaning to the text after the slash. The local models follow a convention, "triggers carry the actor role + originating screen" (`blindbudet.em.yaml:7-8`, `lob-ap.em.yaml:6`), and the local parser discards the origin: `EmParser.CollectRole` keeps only the role (`EmParser.cs:116-121`). So the one fact that would make navigation topology a reference instead of a heuristic is written down and then thrown away (`p1-derivability.md` lead finding 4).

Two defects follow. On the games the origin text matches no view: `t: 🧑‍🎓 Player /Lot` (`blindbudet.em.yaml:343`) against `v: Lot card` (`:305`). And the games model Next and End as `⚙️ System` processors (`blindbudet.em.yaml:778`, `:815`) while all three shipped UIs give the host a button (`p1-derivability.md` §4a); xmlang's enablement default would hide those buttons until the Event Model carries a host trigger beside the System one (RFC 0001 L148). RFC 0001 names both as emlang debts 1 and 2 (`0001-interaction-model.md:236-237`).

## What the spec says today, verbatim

- Overview example (SPEC L19): `- t: Swimlane/TriggerName`.
- Naming (L54-57): "Element names are free-form text", "Names MUST NOT be empty", "Names MUST NOT end with `/` (a swimlane prefix alone is not a valid name)", "Names MAY contain spaces, accents, and special characters".
- Swimlanes (L72-78): "Swimlanes are OPTIONAL", "A swimlane MUST be specified as a prefix separated by `/` before the element name", example `- t: Customer/RegistrationForm   # Swimlane: Customer`.
- Slices (L97): "A slice is a named sequence of elements representing a business scenario".
- Schema: `elementList` is `"type": "array", "items": { "$ref": "#/$defs/element" }, "minItems": 1` (schema.json L110-116), with no uniqueness or ordering constraint; `elementName` is `"minLength": 1, "pattern": "[^/]$"` (L216-221).

The spec says nothing about how many `t:` a slice may hold, nothing about the meaning of the remainder after the swimlane, and nothing about element order beyond the word "sequence". Several `t:` in one slice already pass the schema, the Go tools, `em parse`, `em lint`, `em fmt` and the codegen path (`compat.md` §1 row "Two `t:` in one slice", §4). Zero instances exist upstream or locally (`compat.md` §4; `census-gwt-state.md` §1(e): no slice among 85 has more than one `t:`).

## Section A: trigger sets and roles

### Proposed normative changes (SPEC §Slices, new subsection "Triggers")

- A slice MAY contain more than one trigger element.
- Each trigger element of a slice denotes one legal initiator of the slice's first command; the set of a slice's triggers is that command's trigger set.
- The swimlane of a trigger is the initiator's role; two triggers with the same role and different origins are two initiators.
- Every trigger element MUST precede the first command element of its slice; a trigger after a command SHOULD be reported (`em-trigger-after-command`, warning).
- A trigger set says who may issue the command and nothing about which initiator issued it in a given scenario; the acting party is a prop on the event (RFC emlang-0004, `em-actor-identity`).
- A slice with more than one command element is outside this RFC; its triggers attach to the first command.

```yaml
# proposed
slices:
  ✍️ Next Lot:
    steps:
      - e: Game / LotRevealed
      - v: Todo / Auction progress
      - t: ⚙️ System /Auction progress     # the processor, gated by hasNextLot
      - t: 🧑‍🏫 host /Round scores          # the host's button on the results screen
      - c: AskNextLot
        props:
          gameId: Guid
      - e: Game / NextLotStarted
      - v: Lot card
```

### The host trigger on Next and End: pending

The YAML above adds a host trigger to a System slice because all three shipped UIs give the host a button (`p1-derivability.md` §4a). The red team records the other reading (`redteam.md` R17): the button is an affordance over a System-paced loop, the Event Model is right as written, and the edit serves the transformer, since it lands only because xmlang's enablement default would otherwise hide the button (RFC 0001 L148). The games are the declared negative control, so three game UIs are weak evidence that a model is wrong. The fixture fix is therefore pending: it ships when an LOB model shows a dual initiator, a command that a person and an automation may both issue, or it is recorded as an xmlang convenience and the enablement default gains a clause for System-triggered commands offered to a human. Section A's rules do not depend on the fix.

### What xmlang consumes from Section A

- Enablement (RFC 0001 L136): "rendered only to personas whose `role` matches one of the command's trigger roles" ranges over the trigger set; `xm-command-trigger-mismatch` (RFC 0001 L183, warning) stops firing on the games' Next and End commands once the host trigger exists (it fires 6 times on the games today and 0 on lob-ap, `SYNTHESIS.md:43`).
- Pacing mode is derivable and needs no key: a trigger set containing only `System` is an automation; a set containing `System` and a human role is an automation the human may pre-empt; a set with no `System` is manual. This replaces the composition of System commands on host surfaces that all three shipped Experience Models do today (`p1-derivability.md` §4a, row "Who paces the loop").
- Selection within a cell (RFC 0001 L135) reads which initiator fired from the event's actor prop, not from the slice.

## Section B: trigger origin and bare forms

### Proposed normative changes (SPEC §Elements, subsection "Swimlanes", and the lint appendix)

- The swimlane of an element is the text before the first `/`; the remainder is the element's name, and for a trigger the remainder is its origin (this is the Go reference behaviour, `ast.go:84-92`, and `EmAst.cs:327`).
- An origin MUST NOT contain `/`.
- Origins and view names are compared after trimming surrounding whitespace on both sides of the separator, so `host /Round scores`, `host/Round scores` and the formatter's output `host/Round scores` are the same origin (`EmFormatter.cs:82` and `formatter.go` re-join without spaces, `compat.md` §3).
- An origin MUST either equal the name of exactly one view element in the same document (a view origin) or equal the name of the slice's first command element (a bare form).
- A bare form states that the initiator issues the command from no modelled view: the command's own form is the whole of what the actor sees.
- A `System` trigger's origin SHOULD be the name of the `Todo`-lane view the processor reads, which in every automation slice of the four models is the view element immediately preceding the trigger (`blindbudet.em.yaml:525-526`, `lob-ap.em.yaml:909-911`).
- An origin that is neither a view origin nor a bare form MUST be reported (`em-trigger-origin-unresolved`, warning; RFC emlang-0002 MAY raise it under `profile: decider`, and that RFC owns the profile severity).
- A view name that occurs in two lanes of one document makes every origin naming it unresolved, and the lint message says so.

```yaml
# proposed
slices:
  ✍️ Join Auction:
    steps:
      - t: 🧑‍🎓 Player /JoinAuction        # bare form: the origin is the command itself
      - c: JoinAuction
      - e: Game / PlayerJoined
      - v: Roster
  ✍️ Place Bid:
    steps:
      - t: 🧑‍🎓 Player /Lot card           # view origin: resolves to v: Lot card
      - c: PlaceBid
      - e: Game / BidPlaced
      - v: Bid progress
```

### Why the bare form is the command's name

Three ways to declare a bare form were considered; none changes the schema.

Proposed: the origin equals the slice's command name. It needs no new character, `em fmt` round-trips it untouched (`compat.md` §1: nothing reads the remainder), and it lets the lint separate intent from typo: `Player /Lot` is a typo for `Lot card`, `Player /JoinAuction` is a declaration, which is what allows the lint to be an error under the profile. xmlang already calls this "the bare form of its first triggered command" (RFC 0001 L132). Cost: `Player /JoinAuction` reads as "the player's JoinAuction screen", which is what it means but is not how the fixtures have been written.

Alternative A: a bare form is any origin that matches no view, and `em-trigger-origin-unresolved` fires at warning on each. No declaration, no rename of the join screens. The cost is that the lint cannot tell `Lot` from `Join form`; it fires on 24 of 43 triggers today and on every legitimate bare form forever, so its severity can never rise and its baseline is never clean (`compat.md` §5).

Alternative B: a punctuation marker (`Player /Join form*`). Declarative, but it adds a character the naming rules do not know while "Names MAY contain ... special characters" (SPEC L57) makes `*` legal prose today.

### Alternative recommended by the red team: origin stays free text, resolution moves to xmlang

Decision pending the maintainer. The red team's position (`redteam.md` R15, R16 and "How much of RFC 0003 should move to xmlang"), in substance: the origin is where the actor was standing, an experience fact. A free-text string after a slash is not Event Modeling's wireframe row, and making it a resolvable reference imports a fragment of that row into emlang while the rest stays in xmlang, dual specification with the seam in the worst place. The bare-form declaration exists so one lint can reach a clean baseline; rule and escape hatch are designed around each other, and the hatch renames three real screens to command names, a lint dictating a model's vocabulary. Under this alternative:

- emlang keeps the origin as unlinted free text, exactly as v1.0.0 has it;
- emlang keeps one base-spec naming rule, because it is a real gap independent of experience: the swimlane is the text before the first `/`, and a name MUST NOT contain a second `/`;
- xmlang gains the resolution as its own lint, `xm-origin-mismatch` (warning): a command's emlang origin names a view that no surface composing that command composes;
- the bare-form declaration and `em-trigger-origin-unresolved` are withdrawn, and the join screens keep their names.

What this RFC has already conceded supports the alternative: surface composition is where a command is offered, and RFC 0001's Defaults use the issuing surface, not the emlang origin, for destination ("the issuing surface (stay)", L134), for rejected commands (L134) and for entry ("the bare form of its first triggered command", L132), so every derivation RFC 0001 admits is computable from `compose` alone once a viewer stands on a surface; the emlang origin adds the modeller's statement of where the actor was, which `xm-origin-mismatch` verifies. Against it: Event Modeling's wireframe row is part of the blueprint and the trigger is its only survivor in emlang (`p1-derivability.md` §3), and a model read without an Experience Model (the codegen path, a diagram) then has no source for any command. The drafted Section B and this alternative stand side by side; the cut is one deletion either way, and the naming rule survives both.

### What xmlang consumes from Section B

- Destination default (RFC 0001 L134): "the issuing surface" becomes "the surface composing the origin view"; return versus advance is then a comparison of two references (origin view equals terminal view: stay; otherwise advance), which `p1-derivability.md` §2 row 2 could only compute heuristically.
- Entry default (RFC 0001 L132): "the bare form of its first triggered command" becomes a reference to a bare-form trigger.
- `then:` (RFC 0001 §2): a `then` naming the surface that composes the origin view restates the stay default, so `xm-then-restates-default` gains a second case; PLAN.md asks that any weakening of the `then:` case be recorded in RFC 0001's open objections, not acted on, and this is that note.
- Navigation semantics stay in xmlang; emlang states a reference and its resolution, nothing about what the actor sees next.
- Under the red team's alternative the same three consumptions hold, with `xm-origin-mismatch` doing the verification that `em-trigger-origin-unresolved` does here.

### Census: every trigger in the four models

Rule applied: origin equals the name part of exactly one `v:` in the same file, both sides trimmed, case-sensitive; counted mechanically (PyYAML script in the session scratchpad; no name in the four files contains a second `/`, so first-slash and last-slash split agree). Totals agree with `compat.md` §4 (18/20, 0/7, 0/9, 1/7). RFC 0001's "18/18 on the LOB testbed; 1/12 on the games" (L236) excluded the twelve `System` triggers and counted `mer-eller-mindre`'s two identical `Player /Question` triggers once; the games have thirteen human triggers, one resolving.

`rfcs/0001-evidence/lob-ap.em.yaml`: 20 triggers, 18 resolved, 2 unresolved.

| line | trigger | nearest view (line) | proposed fix |
|---|---|---|---|
| 911 | `⚙️ System / Payment run` | `Payment run log` (955); the Todo read at 910 is `Todo / Due payments` (885) | rename origin to `Due payments` |
| 1097 | `⚙️ System / Approval reminder` | `Todo / Stale approvals` (1077), read at 1096 | rename origin to `Stale approvals` |

`tests/Emlang.Tests/fixtures/blindbudet.em.yaml`: 7 triggers, 0 resolved, 7 unresolved.

| line | trigger | nearest view (line) | proposed fix |
|---|---|---|---|
| 121 | `🧑‍🏫 host /Auction catalog` | `Pack catalog` (100) | rename |
| 190 | `🧑‍🎓 Player /Join form` | none | declare bare form `Player /JoinAuction` |
| 259 | `🧑‍🏫 host /Auction lobby` | `Roster` (162) | rename |
| 343 | `🧑‍🎓 Player /Lot` | `Lot card` (305) | rename |
| 526 | `⚙️ System / Reveal lot` | `Todo / Outstanding bids` (464), read at 525 | rename |
| 778 | `⚙️ System / Next lot` | `Todo / Auction progress` (736), read at 777 | rename; add `t: 🧑‍🏫 host /Round scores` (690), pending (Section A) |
| 815 | `⚙️ System / End auction` | `Todo / Auction progress` (736), read at 814 | rename; add `t: 🧑‍🏫 host /Round scores` (690), pending (Section A) |

`tests/Emlang.Tests/fixtures/mer-eller-mindre.em.yaml`: 9 triggers, 0 resolved, 9 unresolved.

| line | trigger | nearest view (line) | proposed fix |
|---|---|---|---|
| 166 | `🧑‍🏫 host /Quiz catalog` | `Pack catalog` (138) | rename |
| 256 | `🧑‍🎓 Player /Join form` | none | declare bare form `Player /JoinGame` |
| 333 | `🧑‍🏫 host /Game lobby` | `Roster` (223) | rename |
| 426 | `🧑‍🎓 Player /Question` | `Question card` (382) | rename |
| 525 | `🧑‍🎓 Player /Question` | `Question card` (382) | rename |
| 775 | `⚙️ System / Reveal direction` | `Todo / Outstanding directions` (649) | rename |
| 864 | `⚙️ System / Score difference` | `Todo / Outstanding differences` (706) | rename |
| 1134 | `⚙️ System / Ask next question` | `Todo / Game progress` (1085) | rename; add `t: 🧑‍🏫 host /Round scores` (1027) |
| 1173 | `⚙️ System / End game` | `Todo / Game progress` (1085) | rename; add `t: 🧑‍🏫 host /Round scores` (1027) |

`tests/Emlang.Tests/fixtures/tank-till-tusen.em.yaml`: 7 triggers, 1 resolved (`🧑‍🎓 Player /Puzzle` at 351 resolves to `Puzzle` at 310), 6 unresolved.

| line | trigger | nearest view (line) | proposed fix |
|---|---|---|---|
| 121 | `🧑‍🏫 host /Quiz catalog` | `Difficulty catalog` (102) | rename |
| 195 | `🧑‍🎓 Player /Join form` | none | declare bare form `Player /JoinGame` |
| 264 | `🧑‍🏫 host /Game lobby` | `Roster` (170) | rename |
| 584 | `⚙️ System / Score round` | `Todo / Outstanding solutions` (510) | rename |
| 855 | `⚙️ System / Ask next puzzle` | `Todo / Game progress` (810) | rename; add `t: 🧑‍🏫 host /Round scores` (763) |
| 894 | `⚙️ System / End game` | `Todo / Game progress` (810) | rename; add `t: 🧑‍🏫 host /Round scores` (763) |

Across the four files: 43 triggers, 19 resolved, 24 unresolved; of the 24, 12 are `System` origins naming the processor instead of the Todo view it reads, 9 are human origins naming a screen by a name the view does not carry, and 3 are join screens with no view (the bare-form case). No unresolved origin exists on lob-ap outside the two automations.

## Changelog entry (draft, for the emlang spec)

### Triggers: trigger sets and origins

- **Trigger sets**: a slice MAY carry several `t:`; each is a legal initiator of the slice's command; triggers precede the command (`em-trigger-after-command`). Names an existing legal shape; no schema change.
- **Trigger origin**: the remainder after the first `/` is the origin; it MUST name a view of the document or the slice's command (bare form); `em-trigger-origin-unresolved`. Swimlane split fixed at the first `/`; a second `/` in a name is not permitted.

## Migration

Section A: none required; adding a second `t:` to an existing slice is legal today. The 6 host triggers on Next and End (2 per game, listed above) are pending an LOB dual initiator and are not part of Section A's migration.

Section B as drafted: 24 origin renames, listed above (lob-ap 2; blindbudet 7; mer-eller-mindre 9; tank-till-tusen 6); the three join screens become bare-form declarations. Under the red team's alternative: no renames, and `xm-origin-mismatch` reports the 9 human mismatches on the games from the xmlang side. Both formatters rewrite `Role /Origin` to `Role/Origin`, so a formatting pass moves every line number cited here (`compat.md` §5). Under Alternative A the renames are optional and the baseline is 24 warnings. Risk: strict resolution forces a bare-form declaration on every join screen and every future entry command with no view.

## Implementation notes (reference implementation)

- Prerequisite: the swimlane split diverges locally. `EmAst.cs:327` and Go `ast.go:84-92` split at the first `/`; `EmParser.Split` (`EmParser.cs:35-41`), `SpecModel.cs:83, 89` and `TestModel.cs:93` split at the last; they agree only while no name contains a second `/` (`compat.md` §3). Section B fixes the rule at the first `/` and forbids a second; the three last-slash sites must be aligned before the lint ships, or `em lint` and `xm` read different origins.
- `EmParser.CollectRole` (`EmParser.cs:116-121`) discards the origin; `EmSpec` needs the origin per trigger and the trigger set per slice, which is the slice-chain projection RFC 0001 already requires (`0001-interaction-model.md:228`), one `EmSpecShape` re-approval.
- `Linter.cs` is a per-slice pass (`:27-29`); `em-trigger-origin-unresolved` also needs the document's view-name index. `LintSeverity` has `Warning` and `Error` only (`:3`). `Linter.cs:14-18` declares the set a faithful port of the Go reference; either rule is a divergence to negotiate, with the upstream README's ten listed rules against three implemented as precedent (`compat.md` §4).
- `EmFormatter` needs no change.

## Non-goals

- Navigation semantics: what the actor sees after the command, entry surfaces, return versus advance. These are xmlang Defaults (RFC 0001 §4) that consume the references this RFC provides.
- A trigger construct beyond `t:`; a `role:` or `origin:` key; a screen or wireframe element (the local doctrine that views are data and surfaces are xmlang's is unchanged, `p1-derivability.md` §3).
- Ordering rules beyond "triggers precede the command".
- Meaning for a slice with several commands.

## Open objections (recorded, not resolved)

1. Every count rests on four models by one team; no second modeller has written triggers under these rules.
2. Strict origin resolution is real migration work: 24 renames and 6 additions on four files, and a rename on every future join screen.
3. The bare form as the command's name reads oddly and is a convention, not a construct; Alternative A avoids the rename at the cost of a lint that can never be clean.
4. Section B makes emlang reference a screen-shaped fact, in tension with the principle that experience lives in xmlang; the red team recommends the xmlang alternative above (`redteam.md` R15, R16); undecided.
5. Resolving origins makes "stay" a reference, which weakens the case for xmlang's `then:` (RFC 0001 open objection 1); recorded there, not acted on.
6. The host trigger on Next and End rests on three game UIs, the declared negative control; the model may be right and the button an affordance over a System-paced loop (`redteam.md` R17); pending an LOB dual initiator.
7. Section B's profile severity depends on RFC emlang-0002, so Section B is not independently acceptable; only Section A stands alone (`redteam.md` R22).

## Evidence

`rfcs/emlang-evidence/{PLAN,cut-lines,census-gwt-state,compat,redteam}.md` (redteam R15-R17, R22, R23); `rfcs/0001-interaction-model.md` L126-148, L232-249; `rfcs/0001-evidence/p1-derivability.md` lead finding 4, §2 rows 2-3 and 12, §4a; `rfcs/0001-evidence/SYNTHESIS.md` L35, L43, L66-67; upstream `SPEC.md` L19, L54-57, L72-78, L97 and `schema.json` L110-116, L216-221; the four models at the lines cited; `src/emlang/Emlang/EmParser.cs:35-41, 104-121`, `Linting/EmAst.cs:327-332`, `Linting/Linter.cs:3, 14-18, 27-29`.
