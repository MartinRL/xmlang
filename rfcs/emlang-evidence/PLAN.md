# Plan: emlang RFC set (drafted in the xmlang repo as `rfcs/emlang-*`)

## Context

xmlang RFC 0001 (`rfcs/xmlang-0001-interaction-model.md`) names eight debts that belong in emlang and records three open objections, the third of which is that xmlang's derived Defaults rest on conventions the emlang spec does not state. The user is separately planning an emlang change of their own: a 1:1 map between the Event Model and the decider, so that the linter prohibits `given: [e…]` for state-change GWT, allows only state (possibly a new `s:` element instead of `v: State /`), and requires every such state to be preceded by its fold test. These are the same concern from two sides. Since xmlang depends one-way on emlang, now is the time to draft the emlang RFCs, one per concern, as review artifacts. Nothing is pushed or applied.

Established facts (from this session's exploration; see `rfcs/0001-evidence/` for the inquiry):

- emlang spec v1.0.0 has five element kinds (t/c/e/x/v), closed schema (`additionalProperties: false` on element, slice, test, root), `given ∈ {e,v}`, `when ∈ {c}`, `then ∈ {e,v,x}`. It says nothing about state, phase, trigger origin text, multiple triggers, params, compensation, or lints, and has no contribution process. The repo has SPEC.md, schema.json, README, MIT license.
- Local practice across four models (three games, lob-ap) and 185 tests: state-change slices give `v: State / X` (106) or nothing (22); projection slices give events (54); zero mixed givens; every State view has a fold test; two lob-ap automation slices give `v: Todo / X`. No slice has two triggers. State views sit in a trailing "Decision Model" slice. The user's rule already describes existing practice.
- Local tooling (`src/emlang/Emlang`): closed kind tables in `EmAst.cs`, `EmParser.cs`, `SpecModel.cs`, `TestModel.cs`; unknown keys are hard parse errors, so `s:`/`params:`/`compensates:` break parse and `em fmt` today. Generator `TestsEmitter.EmitGiven` uses state only when given is exactly one State view; multi-State or Todo givens emit uncompilable C# (a latent bug the decider RFC fixes by making the shape a parse-time rule). Linter has three warning rules, a port of the Go reference set; any new rule is a divergence to negotiate.

## Deliverables

Files under `C:\code\GitHub\xmlang\rfcs\`, drafts only, no commits:

1. `emlang-0001-decider-alignment.md` — the user's change plus debts 4, 6, 7. Scope: a first-class state element or a normative promotion of the `State /` lane (decision recorded in the RFC with the objection to a sixth kind stated); given-shape rule (state-change tests give state or nothing; projection tests give events); every state has a fold test in the same document and the fold test precedes its use (document order); phase per decider (the enum note on the state's `phase` prop is the decider's lifecycle; multi-decider models are legal; how xmlang `during` map form consumes it); actor identity on events as a stated convention (how `self:` and the selection default consume it); scenario coverage lint (`em-phase-transition-uncovered`, `em-given-shape`, `em-state-without-fold`); the Todo-lane given for automation slices ruled (state of a processor, or a smell). Includes the generator bug as motivation and the 185-test census as evidence.
2. `emlang-0002-trigger-origin-and-multiplicity.md` — debts 1 and 2. Trigger text after the slash resolves to a view or a declared bare form; multiple triggers per slice are legal and mean "any of these actors can issue this command"; lint for unresolved origins; fixture fixes for the three games (`Lot` → `Lot card`, host trigger beside System on Next/End). How xmlang consumes it (destination default "stay", `xm-command-trigger-mismatch`).
3. `emlang-0003-view-parameters.md` — debt 3. Mark view props that are inputs to a projection (`params:` sibling of `props`, or a marker); how xmlang treats them (never tiered, labelable).
4. `emlang-0004-compensation.md` — debt 5. `compensates: <Command>` on a command or `reverses: <Event>` on an event; decide the shape against lob-ap, which models compensation as a second event; explicitly states xmlang derives nothing from it.
5. `emlang-README.md` (or a header in each) — the compatibility posture: additive vs breaking against the closed v1.0.0 schema (any new key fails today's validators, so every construct RFC is a schema bump), the profile/conformance-level idea if the design pass recommends it, `em fmt` round-trip and Go-reference parity as the cost of each construct, and cross-references to xmlang RFC 0001.

The design pass (Plan agent, running) decides the exact cut, the `s:` vs promoted-lane ruling, and the profile mechanism; its recommendation is folded into this plan before drafting.

## Team (small, 3 agents, after plan approval)

1. **Spec reader / compatibility** (general-purpose): reads SPEC.md and schema.json verbatim, the Go reference parser behaviour where discoverable (emlang-project org repos; `[citation needed]` where not), and the local four parsers; produces the exact additive/breaking analysis per proposed key and the `em fmt` round-trip impact. Output `rfcs/emlang-evidence/compat.md`.
2. **Drafter** (general-purpose): writes the four RFCs in the register of RFC 0001 (RFC 2119, proposed YAML marked `# proposed`, evidence with file:line, migration, open objections), LOB examples first, games second. Inputs: this plan, the design recommendation, `compat.md`, `rfcs/0001-evidence/SYNTHESIS.md`, `p1-derivability.md`, `lob-ap.em.yaml`, the fixtures.
3. **Red team against Event Modeling** (general-purpose): attacks each RFC on Event Modeling's own method (eventmodeling.org: the picture has no state element; State is a read model; "no screens above one another"; four slice patterns), on the MBUI dual-specification mechanism (does `s:` make emlang a second decider?), on the five-kind virtue, and on Go-parity cost. Rules KEEP/AMEND/KILL per proposal; the drafter revises.

I write the compatibility posture README myself and review every RFC against the plan before handing over.

## Files to read during execution

- Spec: `https://raw.githubusercontent.com/emlang-project/spec/main/SPEC.md`, `schema.json`
- Local: `src/emlang/Emlang/{EmParser,SpecModel,TestModel}.cs`, `Linting/{EmAst,Linter}.cs`, `EmFormatter.cs`; `src/emlang/Emlang.Generators/{TestsEmitter,DeciderEmitter,SurfaceEmitter,EmitTarget}.cs`
- Fixtures: `tests/Emlang.Tests/fixtures/*.em.yaml`; `rfcs/0001-evidence/lob-ap.em.yaml`
- Inquiry: `rfcs/xmlang-0001-interaction-model.md`, `rfcs/0001-evidence/{SYNTHESIS,p1-derivability,p3-redteam,p3-impact}.md`

## Verification

- Every proposed construct has a YAML example that is checked by hand against schema.json (it will fail today's validator; the RFC says so and shows the schema diff).
- Every lint rule has a fixture line where it would fire today (the census gives them) or an explicit "fires on none of the four models".
- The decider RFC's given-shape rule is checked against all 185 tests: it must accept the 106 + 22 + 54 and rule on the 3 Todo givens explicitly.
- `dotnet build` is not run (no code changes); `em parse`/`em lint` may be run read-only on fixtures to confirm current behaviour.
- No commits, no pushes, no edits to `xmlang-spec.md` or `src/`; RFC 0001's debt list gets a one-line pointer to each emlang RFC only after the user approves.

## Not doing

- No pull request to emlang-project/spec; the RFCs are drafts for the user to carry upstream or keep as a dialect.
- No implementation in `EmParser`/`EmFormatter`/generators; costs are estimated, not paid.
- No change to xmlang RFC 0001's admissions; if an emlang RFC weakens the case for `then:` (once trigger origins resolve, "stay" and "return" are both references), that is recorded as a note in RFC 0001's open objections, not acted on.

## Decisions (2026-09-09, Martin)

- State element: **draft both forms** in the decider RFC as alternatives A (`v: State /` promoted to normative text) and B (`s:` sixth kind), same rules under both; Martin picks on reading.
- Opt-in: **profile RFC first** (one root key `emlang: {version, profile}`); later RFCs' stricter rules hang off `profile: decider`.
- View parameters and compensation: **deferred** per the design pass (params as a `props` note convention + lint inside the lint RFC; compensation as a Non-goal with re-open condition). Default taken after Martin asked for the question to be explained; revisit if he wants standalone RFCs.

## Final cut

| file | scope | depends on |
|---|---|---|
| `rfcs/emlang-0001-profiles.md` | root key `emlang: {version, profile}`; profile = strict superset of v1.0.0; stripping header yields v1.0.0 doc; `em fmt` preserves header | — |
| `rfcs/emlang-0002-decider-profile.md` | `profile: decider`: state element A/B, given-shape rule, fold-by-reference rule, Todo-given smell, phase per decider, how xmlang `during`/`self` consume it | 0001 |
| `rfcs/emlang-0003-trigger-origin.md` | origin after `/` resolves to a view or declared bare form; multiple `t:` per slice legal = alternative initiators; fixture fixes | — (lints only) |
| `rfcs/emlang-0004-lints.md` | non-normative lint appendix: coverage, actor identity, information completeness, `@param` props note, given-shape/fold/todo (from 0002), origin resolution (from 0003) | 0002, 0003 |

Evidence: `rfcs/emlang-evidence/{census-gwt-state,cut-lines,compat,redteam}.md`.

## Red team outcome (2026-09-09, `redteam.md`, 24 findings)

Uncontested fixes (R2-R9, R11-R14, R18-R23) are being folded into the drafts. Real dependency graph: 0001 -> 0002 -> 0004; 0003 section A standalone; 0003 section B depends on 0002 for the profile column.

### Decisions pending the maintainer

1. **Root key vs config file (R1, strongest objection).** emlang already ships `.emlang.yaml` with `lint.ignore` and `fmt.keys` (Go `internal/config/config.go`, local `Emlang.Cli/Program.cs:63-71`). `lint.profile: decider` there delivers every rule in 0002-0004 with zero schema change and no document failing any tool. The root key only buys document portability. Keep RFC 0001, or fold the profile into config and withdraw it?
2. **Form B (R10).** Red team verdict: withdraw. `s:` cannot live in a profile (a profile cannot add a kind), so B is a base-spec change; EM's canvas has no state box. Both forms remain drafted per the 2026-09-09 decision.
3. **RFC 0003 section B (R15, R16).** Red team agrees with the coordinator: origin is an experience fact; keep free text in emlang, add `xm-origin-mismatch` in xmlang, withdraw bare forms. Section A (trigger sets) stays. Cut or keep?
4. **Host-trigger fixture fix (R17).** Evidence is three game UIs (the negative control). Apply, or wait for an LOB dual initiator?

## Decisions (2026-09-10, Martin): all-in on decider + DCB

- **Consistency model: DCB** (Dynamic Consistency Boundary). A state element is a decision model; its fold tests define its query (event types = union of fold givens; tags = identity-typed props). Append condition replaces per-stream version. The aggregate rule (`em-given-decider-mismatch`, then-events share the given state's swimlane) is the rejected alternative; swimlanes are canvas grouping only.
- **Given is always exactly one explicit state.** No `given: []` anywhere. Empty state = props absent, needs no fold. "Initial state" is not a concept; a rich starting state (tenant after onboarding) is a fold like any other. Three shape lints collapse into `em-given-not-one-state`.
- **Closed decision model:** then-events must be in the given state's query (`em-then-outside-query`). Tags: fold events carry a state identity prop (`em-fold-untagged-event`). `em-phase-ambiguous` deleted.
- **Domain focus: B2B SaaS.** Examples are B2B SaaS only (DraftInvoice against supplier status, seat limits, workspace slugs, approval thresholds, tenant onboarding). Games appear only as census rows.
- **RFC 0005 initiators:** rename `t:` trigger to actor / automation (base spec 1.1.0). Practitioner objection from Martin Dilger on record, quote pending. Martin to ask Dilger for his reasoning and for an outside model (answers redteam R24).
- **Second testbed:** a ChronosHub flow is intended; this repo is public, so either anonymise into a generic publishing-SaaS model or cite counts only. Martin's call.
- Still open from 2026-09-09: (1) root key vs `.emlang.yaml`; (2) form B; (3) RFC 0003 section B; (4) host-trigger fixture fix.
