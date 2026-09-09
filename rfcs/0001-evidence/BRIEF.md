# Shared brief: Interaction Model research for xmlang

You are one agent on a 10-agent research team. Read this whole brief before starting. Write your report to the output path given in your task prompt, as Markdown. Cite sources (URLs, or file:line for repo evidence). Never invent a citation: write `[citation needed]` instead. Do not edit any file outside the scratchpad.

## What xmlang is

xmlang (spec: `C:\code\GitHub\xmlang\xmlang-spec.md`, v0.5.0 draft, ~340 lines, READ IT IN FULL FIRST) is a YAML DSL for Experience Models over an emlang Event Model (Event Modeling: slices of screen/command/event/view in swimlanes). xmlang records UX judgment as data: `personas` (role = swimlane), `surfaces` (compose list of `v:` views and `c:` commands; `for:` personas; `during:` phase values; field salience tiers primary/secondary/on-demand; `self:` identity field; command `prominence` primary/secondary/overflow), `journeys` (ordered slice lists, material for walkthrough tests), `labels` (BCP 47 locale maps), `tokens` (DTCG). Rules: strict one-way dependency (xmlang references resolve against the Event Model; emlang never sees xmlang); the geometry prohibition (no coordinates, grids, containers, breakpoints, ever: "judgment as data, never as geometry"; list order = importance, never position). Consumers are "transformers": build-time codegen, runtime interpreter, or a human/agent hand-building UI. Defaults table: the Event Model alone determines a default experience; xmlang only refines it.

Considered-and-rejected (spec Non-goals): a `when:` predicate language for fine surface selection; viewer-relative expressions; `format:` hints; polling/timing/transport. A pre-registered experiment (v0.3.0-experimental `slot: header|body|footer`) was reverted in v0.4.0: every annotation restated a default (dead weight) and the first geometry key demanded a second (slope). Background article: https://rosenlidholm.se/articles/machine-readable-ux-specs-research.html (thesis: 30 years of Model-Based UI (MBUI) failed to compile concrete UIs from abstract specs; specs can determine only the abstract UI; agents now fill the concrete stratum, pinned by generated verification).

## The research question

Is there an **Interaction Model** stratum, between the Event Model (behaviour) and the concrete UI (geometry, idiom), that records genuine judgment as data and belongs in xmlang? Three senses, all in scope:

- **A. Navigation topology** (IA sense): how surfaces connect; what the user sees after a command; hub-and-spoke, wizard, master-detail, feed; entry surfaces per persona. Today `journeys` is the only cross-surface construct and it is test material, not navigation.
- **B. Dialog / state model** (MBUI sense): what the user can do at each moment; enablement; transitions. The spec rejects `when:`; but the Event Model is itself a state model (deciders, `phase` enum on a State-lane view, GWT scenarios), so re-examine the boundary rather than assume it.
- **C. Interaction idioms** per command/view: confirm, undo, optimistic vs. confirmed feedback, inline vs. modal, wizard vs. single form, read-only vs. editable posture, waiting surfaces, error display. Today concrete stratum.

Only the geometry prohibition is inviolable. The one-way dependency, the `when:` rejection, and the frozen root-key set may all be challenged. "This belongs in emlang, not xmlang" is a legitimate finding.

## The five admission tests (fixed before evidence)

A candidate vocabulary item is admitted only if it passes all five:

1. **Derivability**: NOT computable from the Event Model alone (slices, command→event→view chains, `phase`, swimlanes, GWT scenarios). If derivable, it is a Defaults-table entry or transformer heuristic, not vocabulary.
2. **Dead weight**: at least one genuine non-default annotation across the three real testbeds (BlindBudet, Mer eller mindre, Tänk till tusen). Restating a default is the geometry experiment's failure mode.
3. **Geometry leak**: names a judgment (membership, ordering, tier, closed enum), never a position, container, region, breakpoint. Includes disguised geometry: `modal`, `drawer`, `inline` used as placement.
4. **Slope**: the key does not demand a second key to do any work.
5. **Transformer neutrality**: meaningful for codegen, runtime interpreter, AND human/agent.

Plus placement per item: xmlang / emlang / concrete stratum. Map: Garrett's five planes; the Event Model is scope, xmlang is skeleton-minus-layout plus surface-as-tokens; the structure plane (interaction design + IA) is the gap being probed.

Hypotheses to confirm or falsify, not assume: H-A navigation is mostly derivable with a small residue (entry surface per persona, return-vs-advance after a command, wizard grouping); H-B the dialog model already lives in emlang, anything xmlang could add is derivable or a `when:` in disguise; H-C a few idioms are data (destructive confirmation, undo via compensating command, posture) and most are concrete residue.

## Local testbeds

- Event Models: `C:\code\GitHub\xmlang\tests\Emlang.Tests\fixtures\{blindbudet,mer-eller-mindre,tank-till-tusen}.em.yaml`
- Experience Models: `C:\code\GitHub\kvissig.se\specs\*.xm.yaml` (and `specs/*event-model*.yaml` if present)
- Live runtime interpreter (the concrete stratum): `C:\code\GitHub\kvissig.se\src\MerEllerMindre.Web` (+ `.Tests`)
- Reference implementation: `C:\code\GitHub\xmlang\src\xmlang\Xmlang\{XmParser,XmModel,XmLinter}.cs`
- Prior articles: `C:\code\GitHub\MartinRL.github.io\articles\{machine-readable-ux-specs-research,xmlang-geometry-experiment,xmlang-geometry-experiment-scores}.md`

## Report style

Lead with findings. Tables for catalogues. Every claim sourced. Under ~2500 words unless your task says otherwise. Mark uncertainty explicitly.

## AMENDMENT (2026-09-07, from the user): the target is line-of-business enterprise software

xmlang is optimized for **line-of-business (LOB) enterprise software** (the spec's own examples: billing, invoices, accountants, month-end close), NOT for online quiz games. The three kvissig games are the only shipped testbeds, so they stay useful as a **negative control** (does simple stay simple; does a key restate defaults on a small product?), but ABSENCE of a judgment in the games (no undo, no delete, no wizard, no confirm, no drafts, no bulk actions, no approvals, no master-detail) says NOTHING about LOB.

Consequences for the five tests:
- **Test 2 (dead weight) is re-scoped**: a candidate passes if it shows at least one genuine non-default annotation on a representative LOB Event Model (see `p1-lob.md` when it lands: an accounts-payable / order-to-cash style model with drafts, approvals, corrections, master data, bulk operations, long-running processes, multiple roles). The games are a secondary check: a key that is dead weight on the games AND on LOB fails; dead weight on games alone does not fail.
- **Test 1 (derivability)** is unchanged but must be argued on LOB shapes too: many commands per view, many surfaces per phase, compensating commands (void, reverse, cancel), soft-delete, approval chains, list→detail→edit round trips, search/filter surfaces, dashboards.
- Interaction judgments that recur in every enterprise design system (SAP Fiori, Salesforce Lightning, Microsoft Fluent/Dynamics, Oracle Redwood, Atlassian, Carbon) are strong candidates precisely because they recur across products: destructive-action confirmation, draft/save-and-continue, list-detail-edit navigation, wizard for long forms, bulk selection actions, approval/rejection with reason, inline vs. page edit, optimistic locking conflict display, empty/error/loading states, audit trail disclosure.
- Personas in LOB are many and role-based (clerk, approver, auditor, admin) with different entry points and dashboards; `for:`/`entry` style judgments are far more likely to be non-default than in a two-persona game.
When you write YAML examples, write them for the LOB model first and the games second.
