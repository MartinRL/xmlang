---
title: "RFC emlang-0005: Initiators, actors and automations (draft)"
status: draft
created: 2026-09-10
targets: "emlang spec 1.1.0 (base spec: two new element kinds, schema change); not the profile"
depends: "RFC emlang-0003 for the Role /Origin form and the initiator-set rules; RFC emlang-0004 for em-actor-identity"
decided: "concept fixed by the maintainer on 2026-09-10; the word is under objection, see Open objections"
---
# RFC emlang-0005: Initiators, actors and automations

**Status: draft.** Nothing here is applied to the upstream spec, to `src/`, or to the fixtures. The concept was fixed by the maintainer on 2026-09-10: what starts a slice is an initiator, of two kinds. An actor is a role that decides on a screen (Event Modeling's State Change pattern). An automation is a processor that decides from a view (the Automation pattern).

## Summary

1. Two new element kinds in the base spec, `a:`/`actor:` and `auto:`/`automation:`, together the initiators of a slice; `t:`/`trg:`/`trigger:` stay parseable as legacy and `em fmt` rewrites them once by a stated heuristic.
2. Names keep RFC emlang-0003's `Role /Origin` form; the emoji that today marks `⚙️ System` becomes decoration, because the kind carries that fact.
3. RFC emlang-0003's trigger set becomes the initiator set, rules unchanged; RFC emlang-0004's `em-actor-identity` reads the kind instead of normalizing emoji.

## Motivation

The evidence is the RFC set's own. Event Modeling's State Change pattern starts with a person on a screen; its Automation pattern starts with a processor reading a view ("Event(s) -> View -> Automated Trigger -> Command -> Event(s)", cheat sheet as quoted in `redteam.md`). emlang gives both one kind, `t:`, and the four models tell them apart by lane text and an emoji (`🧑‍🎓 Player`, `⚙️ System`), a convention the spec does not know (`p1-derivability.md` §1). RFC emlang-0004 had to define emoji stripping and lowercasing just to compare a role name (`em-actor-identity`), the cost of carrying a kind in decoration.

"Trigger" is a mechanism word for an intent fact: the model records who or what decided to issue the command, not how the request arrived.

The origin census in RFC emlang-0003 split exactly along this line: of 43 triggers, 31 are human and 19 resolve to a view; 12 are `System` and 0 resolve, because every one names the processor rather than the view it reads (`emlang-0003-trigger-origin.md`, census tables). Two kinds that fail one rule in two different ways are two kinds.

A practitioner objection to the word "initiator" from Martin Dilger is on record with the maintainer: [quote pending the maintainer's conversation].

## Proposed normative changes (SPEC §Elements, schema.json `element`)

- The Elements table gains two rows: Actor, short `a:`, long `actor:`; Automation, short `auto:`, long `automation:`.
- An actor element names a role that decides on a screen; an automation element names a processor that decides from a view.
- Actor and automation elements are initiators; every rule RFC emlang-0003 states for a slice's triggers applies to a slice's initiators unchanged, and "trigger set" reads "initiator set".
- An initiator's name keeps the `Role /Origin` form of RFC emlang-0003: the swimlane before the first `/` is the role or processor, the remainder is the origin, and a name MUST NOT contain a second `/`.
- Leading emoji in an initiator's swimlane are optional decoration and carry no meaning; tools MUST NOT read the kind from them.
- `t:`, `trg:` and `trigger:` remain valid and are marked legacy; a tool MAY report them (`em-legacy-trigger`, info).
- Initiator elements are not permitted in `given`, `when` or `then`, exactly as trigger elements are not today (`givenElement`, `commandElement`, `thenElement` are unchanged).
- The spec version becomes 1.1.0; a 1.1.0 document that contains no `a:` or `auto:` is a valid 1.0.0 document.

```yaml
# proposed
slices:
  ✍️ Approve Invoice:
    steps:
      - a: Approver /Approval queue          # a role deciding on a screen
      - c: ApproveInvoice
      - x: CannotApproveOwnInvoice
      - e: Invoice / InvoiceApproved
      - v: Invoice details
  ✍️ Execute Payment Run:
    steps:
      - e: Invoice / PaymentScheduled
      - v: Todo / Due payments
      - auto: Payment run /Due payments     # a processor deciding from the view it reads
      - c: ExecutePaymentRun
      - e: Payment / PaymentRunExecuted
      - v: Payment run log
```

The second example is `lob-ap.em.yaml:907-929` with `t: ⚙️ System / Payment run` (`:911`) rewritten: the origin names the Todo view at `:910`, as RFC emlang-0003 Section B proposes, and the processor's name moves to the swimlane where a person's role already sits.

### Schema change

Before (`schema.json` L119-135, L136-151, abridged):

```json
"element": {
  "properties": {
    "t": { "$ref": "#/$defs/elementName" },
    "trg": { "$ref": "#/$defs/elementName" },
    "trigger": { "$ref": "#/$defs/elementName" },
    "c": { "$ref": "#/$defs/elementName" },
    "props": { "oneOf": [{ "type": "null" }, { "$ref": "#/$defs/props" }] }
  },
  "oneOf": [
    { "required": ["t"] }, { "required": ["trg"] }, { "required": ["trigger"] },
    { "required": ["c"] }
  ],
  "additionalProperties": false
}
```

After (additions only; every existing entry stays):

```json
"element": {
  "properties": {
    "a": { "$ref": "#/$defs/elementName" },
    "actor": { "$ref": "#/$defs/elementName" },
    "auto": { "$ref": "#/$defs/elementName" },
    "automation": { "$ref": "#/$defs/elementName" }
  },
  "oneOf": [
    { "required": ["a"] }, { "required": ["actor"] },
    { "required": ["auto"] }, { "required": ["automation"] }
  ]
}
```

Four properties and four `oneOf` entries on one definition; the three test-item schemas are untouched.

### `em fmt` migration (one-time heuristic, not a rule)

- When `em fmt` meets a legacy trigger it writes `auto:` if the swimlane, after removing leading non-alphanumerics and lowercasing (RFC emlang-0004's normalization), is `system`, or if the swimlane begins with `⚙️`; otherwise it writes `a:`.
- This is a heuristic for the migration pass, stated so the result can be checked; it is not a rule of the language, and a person named "System" or an automation with no emoji is misclassified by it and corrected by hand.
- `em fmt` does not strip the emoji; it stays as decoration until the author removes it.

On the four models the heuristic agrees with the census on all 43 triggers: 31 actors, 12 automations.

## Effects on RFC emlang-0003 and emlang-0004

- RFC emlang-0003 Section A: "trigger set" becomes "initiator set"; the rules are unchanged in substance and `em-trigger-after-command` is renamed `em-initiator-after-command`.
- RFC emlang-0003 Section B: the recommendation that a `System` origin name the Todo view it reads becomes a statement about automations, checkable by kind; the 12 automation origins that failed resolution are now distinguishable from the 12 human failures by kind rather than by lane text.
- RFC emlang-0003's pacing derivation for xmlang (automated, pre-emptable, manual) reads the kind instead of the swimlane.
- RFC emlang-0004 `em-actor-identity`: the role normalization clause is dropped; the rule applies to events in slices whose initiator set contains an actor, decoration is removed only for the `<role>Id` prefix comparison, and the automation exemption reads the kind.

## Compatibility

A new element key breaks every v1.0.0 validator and the Go tools exactly as any new kind does: `element` is closed (`additionalProperties: false`, schema L152), so `a:` and `auto:` are rejected by the schema, by Go `parse|lint|fmt` (`unknown key`) and by `EmAst.cs:340`, and silently dropped by the local codegen path (`SpecModel.cs:68-78`). This is the cost class `compat.md` §1 records for `s:` (row "Form B `s:` in `steps`") and §2.2 itemizes: schema, `EmElementType` (`EmAst.cs:13`), `Prefixes` (`:66-82`), `EmFormatter.TypeKey`, the codegen kind tables, the Go parser, AST, formatter and diagram templates. The difference from `s:`: this distinction exists in the method's own picture, where State Change and Automation start differently, while a state box does not appear on the canvas at all (`cut-lines.md` §B). Initiators never appear in tests, so `TestModel` and the test-item schemas are untouched.

## Changelog entry (draft, for the emlang spec)

### 1.1.0: initiators

- **Added** element kinds `a:`/`actor:` and `auto:`/`automation:`; together, initiators.
- **Legacy** `t:`/`trg:`/`trigger:`: still parsed; `em-legacy-trigger` (info); `em fmt` rewrites once by the stated heuristic.
- **Naming**: initiators keep `Role /Origin`; emoji are decoration.

## Migration

`em fmt -w` rewrites each model's triggers: lob-ap 18 `a:` and 2 `auto:`; blindbudet 4 and 3; mer-eller-mindre 5 and 4; tank-till-tusen 4 and 3; authors check the 12 automations by hand. Every existing model carries `t:`, so this churns every model once; the legacy keys make the churn optional.

## Non-goals

- Navigation, pacing and enablement: xmlang derives them from the initiator set (RFC emlang-0003, "What xmlang consumes"); this RFC changes what the set is made of, not what is derived.
- A processor element beyond the automation initiator; removing `t:`; any change to `given`, `when` or `then`.

## Open objections (recorded, not resolved)

1. The rename churns every existing model, including the three upstream examples, for a distinction the four local models already encode by convention.
2. The `em fmt` heuristic can misclassify: a role literally named "System" becomes an automation; an automation without `⚙️` and not named `System` becomes an actor.
3. It is a base-spec schema change, which upstream may refuse on the closed-schema grounds `compat.md` §5 records for the profile root key.
4. The word "initiator" is under a practitioner objection from Martin Dilger: [quote pending the maintainer's conversation].
5. Every count rests on four models by one team.

## Evidence

`rfcs/emlang-0003-trigger-origin.md` (census, Section A); `rfcs/emlang-0004-lints.md` (`em-actor-identity`); `rfcs/emlang-evidence/{compat,cut-lines,redteam}.md`; `rfcs/0001-evidence/p1-derivability.md` §1; upstream `SPEC.md` L26-34, `schema.json` L117-153; `lob-ap.em.yaml:563, 907-929`; `EmAst.cs:13, 66-82, 340`, `SpecModel.cs:68-78`.
