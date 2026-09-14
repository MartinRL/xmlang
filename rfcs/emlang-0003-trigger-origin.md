---
title: "RFC emlang-0003: Trigger sets (accepted)"
status: accepted
accepted: 2026-09-14
created: 2026-09-09
targets: "the emlang dialect in this repository (prose and lint; the dialect forks upstream emlang v1.0.0 per RFC emlang-0002)"
depends: none
consumed by: "xmlang RFC 0001, debt 2; debt 1 reassigned to xmlang as `xm-origin-mismatch`"
---
# RFC emlang-0003: Trigger sets

**Status: accepted 2026-09-14, not yet implemented.** Nothing here is applied to `src/` or to the fixtures. This RFC is the artifact the maintainer decides on. It has one part: trigger sets, who may issue a command. A former Section B (trigger origin resolution to a view or a declared bare form) and a fixture fix adding host triggers to the games' Next and End slices were cut on 2026-09-11; both are recorded under Rejected alternatives with the reasoning.

## Summary

1. A slice MAY carry several `t:` elements; each is a legal initiator of the slice's command, and the set is the command's trigger set. This gives an existing legal shape a meaning and changes no validator.
2. The text after a trigger's swimlane separator stays free text. Its resolution against a surface is an experience fact and belongs to xmlang, as `xm-origin-mismatch` (`rfcs/xmlang-0001-interaction-model.md`).
3. One base naming rule survives from the cut section, because it is a gap independent of experience: the swimlane is the text before the first `/`, and a name MUST NOT contain a second `/`.

## Motivation

The emlang spec keeps a screen name in exactly one place: the trigger. `t: Customer/RegistrationForm` (SPEC L76) is the vestige of Event Modeling's wireframe row, and the spec attaches no meaning to the text after the slash. The spec also says nothing about how many `t:` a slice may hold. Zero slices among 85 in the four evidence models carry more than one (`census-gwt-state.md` §1(e)), yet xmlang's enablement default ranges over "the command's trigger roles" (RFC 0001 L136) and needs the set to be a defined thing. RFC 0001 names this as emlang debt 2 (`xmlang-0001-interaction-model.md:237`).

## What the spec says today, verbatim

- Overview example (SPEC L19): `- t: Swimlane/TriggerName`.
- Naming (L54-57): "Element names are free-form text", "Names MUST NOT be empty", "Names MUST NOT end with `/` (a swimlane prefix alone is not a valid name)", "Names MAY contain spaces, accents, and special characters".
- Swimlanes (L72-78): "Swimlanes are OPTIONAL", "A swimlane MUST be specified as a prefix separated by `/` before the element name", example `- t: Customer/RegistrationForm   # Swimlane: Customer`.
- Slices (L97): "A slice is a named sequence of elements representing a business scenario".
- Schema: `elementList` is `"type": "array", "items": { "$ref": "#/$defs/element" }, "minItems": 1` (schema.json L110-116), with no uniqueness or ordering constraint; `elementName` is `"minLength": 1, "pattern": "[^/]$"` (L216-221).

Several `t:` in one slice already pass the schema, the Go tools, `em parse`, `em lint`, `em fmt` and the codegen path (`compat.md` §1 row "Two `t:` in one slice", §4).

## Proposed normative changes (SPEC §Slices, new subsection "Triggers")

- A slice MAY contain more than one trigger element.
- Each trigger element of a slice denotes one legal initiator of the slice's first command; the set of a slice's triggers is that command's trigger set.
- The swimlane of a trigger is the initiator's role; two triggers with the same role and different origins are two initiators.
- Every trigger element MUST precede the first command element of its slice; a trigger after a command SHOULD be reported (`em-trigger-after-command`, warning).
- A trigger set says who may issue the command and nothing about which initiator issued it in a given scenario; the acting party is a prop on the event (RFC emlang-0004, `em-actor-identity`).
- A slice with more than one command element is outside this RFC; its triggers attach to the first command.
- Naming (SPEC §Swimlanes): the swimlane of an element is the text before the first `/`; the remainder is the element's name. A name MUST NOT contain `/`. This is the Go reference behaviour (`ast.go:84-92`) and `EmAst.cs:327`.

```yaml
# proposed
slices:
  ✍️ Approve invoice:
    steps:
      - s: Invoice
      - t: 🧑‍💼 Approver /Approval queue      # the approver decides from the queue
      - t: ⚙️ System /Auto-approvals          # the automation approves under the threshold
      - c: ApproveInvoice
        props:
          invoiceId: Guid
      - e: Invoice / InvoiceApproved
      - v: Approval queue
```

In the dialect the decider's state is written `s:` (RFC emlang-0002); older drafts and the fixtures write `v: State / Invoice`.

## What xmlang consumes

- Enablement (RFC 0001 L136): "rendered only to personas whose `role` matches one of the command's trigger roles" ranges over the trigger set.
- Pacing mode is derivable and needs no key: a trigger set containing only `System` is an automation; a set containing `System` and a human role is an automation the human may pre-empt; a set with no `System` is manual.
- Selection within a cell (RFC 0001 L135) reads which initiator fired from the event's actor prop, not from the slice.
- Origin: xmlang's `xm-origin-mismatch` (warning) checks that a trigger origin naming a surface matches a surface admitting that persona. emlang states nothing about it.

## Rejected alternatives

### Origin resolution in emlang (former Section B)

Drafted rule: an origin MUST either equal the name of exactly one view element in the same document or equal the slice's command name (a bare form); `em-trigger-origin-unresolved` reported the rest. Census on 2026-09-09: 43 triggers across the four models, 19 resolved, 24 unresolved (lob-ap 18/20; the games 1/23), with 12 `System` origins naming the processor rather than the Todo view it reads, 9 human origins naming a screen by a name the view does not carry, and 3 join screens with no view at all.

Cut on 2026-09-11 for the red team's reasons (`redteam.md` R15, R16): the origin is where the actor was standing, an experience fact. Making it a resolvable reference imports a fragment of Event Modeling's wireframe row into emlang while the rest stays in xmlang, dual specification with the seam in the worst place. The bare-form declaration existed so one lint could reach a clean baseline; rule and escape hatch were designed around each other, and the hatch renamed three real screens to command names. Every derivation RFC 0001 admits (entry, destination, rejected-command return) is computable from `compose` alone once a viewer stands on a surface; the origin is the modeller's statement of where the actor was, and `xm-origin-mismatch` verifies it from the xmlang side. The naming rule (no second `/`) survives because the local parser and the Go reference disagree on the split when a second `/` exists (`compat.md` §3).

### Host trigger beside System on the games' Next and End (R17)

Drafted fix: add `t: 🧑‍🏫 host /Round scores` beside `t: ⚙️ System` on the six Next and End slices (two per game), because all three shipped UIs give the host a button (`p1-derivability.md` §4a). Dropped on 2026-09-11. The only evidence is the three game models, the declared negative control; the Event Model may be right as written and the button an affordance over a System-paced loop. Where a person and an automation may both issue a command, RFC emlang-0005 (actor / automation initiators) is the construct that expresses it; this RFC's trigger-set rule is what makes that shape legal. `xm-command-trigger-mismatch` keeps firing on the games' Next and End commands (6 today, `SYNTHESIS.md:43`), which is the correct report until a model states a dual initiator.

## Changelog entry (draft, for the dialect spec)

### Triggers: trigger sets

- **Trigger sets**: a slice MAY carry several `t:`; each is a legal initiator of the slice's command; triggers precede the command (`em-trigger-after-command`). Names an existing legal shape; no schema change.
- **Naming**: swimlane split fixed at the first `/`; a second `/` in a name is not permitted.

## Migration

None required; adding a second `t:` to an existing slice is legal today, and no name in the four evidence models contains a second `/`. No origin renames; the join screens keep their names.

## Implementation notes (reference implementation)

- The swimlane split diverges locally. `EmAst.cs:327` and Go `ast.go:84-92` split at the first `/`; `EmParser.Split` (`EmParser.cs:35-41`), `SpecModel.cs:83, 89` and `TestModel.cs:93` split at the last; they agree only while no name contains a second `/` (`compat.md` §3). The naming rule fixes the split at the first `/`; the three last-slash sites should be aligned.
- `EmParser.CollectRole` (`EmParser.cs:116-121`) keeps only the role; `EmSpec` needs the trigger set per slice, which is the slice-chain projection RFC 0001 already requires (`xmlang-0001-interaction-model.md:228`), one `EmSpecShape` re-approval. The origin text should be carried through to `EmSpec` so xmlang's `xm-origin-mismatch` can read it.
- `Linter.cs` is a per-slice pass (`:27-29`); `em-trigger-after-command` fits it.
- `EmFormatter` needs no change.

## Non-goals

- Navigation semantics: what the actor sees after the command, entry surfaces, return versus advance. These are xmlang Defaults (RFC 0001 §4).
- Origin resolution; moved to xmlang as `xm-origin-mismatch`.
- A trigger construct beyond `t:`; a `role:` or `origin:` key; a screen or wireframe element (views are data and surfaces are xmlang's, `p1-derivability.md` §3). RFC emlang-0005 proposes renaming `t:` to actor / automation; that is orthogonal to the set rule.
- Ordering rules beyond "triggers precede the command".
- Meaning for a slice with several commands.
- Upstream emlang v1.0.0 compatibility; the dialect forks the grammar (RFC emlang-0002).

## Open objections (recorded, not resolved)

1. Every count rests on four models by one team; no second modeller has written triggers under these rules.
2. A model read without an Experience Model (the codegen path, a diagram) has no checked source for where a command is issued from once origin resolution lives in xmlang; the origin stays a free-text hint there. Recorded; the 2026-09-11 decision accepts it.
3. The dropped fixture fix leaves `xm-command-trigger-mismatch` firing 6 times on the games; if a future LOB model shows a dual initiator, RFC emlang-0005 carries it, not a fixture edit here.
4. Earlier objections about strict origin resolution (migration cost, the bare form reading oddly, tension with experience living in xmlang, weakening `then:`) are closed by the cut; RFC 0001's open objection 1 on `then:` is unaffected.

## Evidence

`rfcs/emlang-evidence/{PLAN,cut-lines,census-gwt-state,compat,redteam}.md` (redteam R15-R17, R22, R23); `rfcs/xmlang-0001-interaction-model.md` L126-148, L232-249; `rfcs/0001-evidence/p1-derivability.md` lead finding 4, §2 rows 2-3 and 12, §4a; `rfcs/0001-evidence/SYNTHESIS.md` L35, L43, L66-67; upstream `SPEC.md` L19, L54-57, L72-78, L97 and `schema.json` L110-116, L216-221; the four models; `src/emlang/Emlang/EmParser.cs:35-41, 104-121`, `Linting/EmAst.cs:327-332`, `Linting/Linter.cs:3, 14-18, 27-29`.
