---
title: "RFC xmlang-0002: The frontend architecture xmlang maps onto (accepted)"
status: accepted
accepted: 2026-09-14
created: 2026-09-11
targets: "README Stance (xmlang half), a conformance check, the design-first protocol; no spec key changes"
depends: "ChronosHub ADR 002 VSA, 004 CQRS, 006 Task-Based UI, 007 Async UX (proposed), 008 SignalR, 009 tenancy (provisional); RFC xmlang-0001 for confirm/then"
decided: "2026-09-11, maintainer: (1) xm succeeds only if it maps 1:1 onto a frontend architecture, as em maps 1:1 onto VSA × Decider × DCB; (2) the Task-based UI position is confirmed as worded; (3) xm stays stack-neutral, the transformer does the heavy lifting"
---
# RFC xmlang-0002: The frontend architecture xmlang maps onto

**Status: accepted 2026-09-14, not yet implemented.** Nothing here is applied to `xmlang-spec.md`, `src/`, or the fixtures. Fixed by the maintainer on 2026-09-11: the principle, the Task-based UI position, and that xm stays stack-neutral with the transformer doing the heavy lifting. The other two positions, the map and the ChronosHub binding are the artifact he decides on.

## Summary

1. emlang earns its keep because every element lands on one code shape: slice → folder, `c:` → command record and `decide`, `s:` → state and `evolve`, `v:` → read model, GWT → test. The dialect states those positions in the README and proves them with emitters that round-trip (`src/emlang/Emlang/SurfaceEmitter.cs`). xmlang has no landing shape: the spec addresses "transformers" as anything from a generator to a human (`xmlang-spec.md:15-17`), and the geometry experiment showed transformers vary exactly where the model does not pin them.
2. This RFC gives xmlang the same three-position stance for the frontend: **Task-based UI**, **frontend as observer**, **slice-local and surface-composed**, on the **design system as substrate**. All four are already ChronosHub decisions or the maintainer's own notes; none is new.
3. It states the 1:1 map: every xm and em construct → one frontend artifact → one design-system component family. The map is the transformer contract read forward and the design-extraction rule read backward.
4. It names what the em side has and the xm side lacks: a conformance check (the frontend `SurfaceComparer`) and, later, a skeleton emitter. The check comes first.
5. It binds the map to ChronosHub: Vue 3 as the instrument, the Claude Design design system as the component inventory, scholarly-publishing personas, tenancy, long-running money and licence workflows.

No spec key changes. The design-first run (`~/.claude/plans/i-d-like-ur-feedback-glittery-eich.md`) shrinks to reading this map backward over a Claude Design artifact.

## Motivation

The backend pairs an architecture with a spec: VSA / FC-IS / ES with the Event Model. The maintainer's 2026-06-30 note already demands the same pair for the frontend: "the design system is the architecture, the Experience Model is the spec" (`ChronosHubVault/strategy/experience-modeling/experience-model.md` §1). xmlang v0.5 kept the spec half and dropped the architecture half; `Transformers` says any renderer will do.

That is the gap. em's map is what makes an Event Model a build artifact instead of a diagram: the slice is a folder, the command is a record, the test is a test. Without an equivalent map, an Experience Model is a well-linted design brief. With one, `xm lint` can check the code against the model the way ADR 006 asks for by hand: "per shipped user-facing action, is there a 1:1 correspondence to a single named command on the event model?" (`architectural-decision-records/006 - Adopt Task-Based UI_SHORT.md`, Confirmation).

ADR 006 also names a "frontend blueprint" to codify task-based component patterns, task composition, and "integration with event-modelled specs — translating model tasks into UI components". It was never written; the frontend engineer it waits for is still TBD. This RFC is the blueprint's architectural half, written so that the integration section is a table rather than prose.

## The stance: three positions on what a surface is

Mirror of the emlang README's three positions on what a slice is.

| Position | Meaning here | Reference |
|---|---|---|
| **Task-based UI** | A command is a **Task**: one component, one submission, one lifecycle. The UI never edits an entity and lets the backend infer intent. A surface offers Tasks; it does not offer forms over read models. | ChronosHub ADR 006 (decided); Greg Young, task-based UIs as a CQRS prerequisite |
| **Frontend as observer** | A view is a **Query** the client subscribes to. The client derives nothing from events or from other views; freshness arrives by push. The gap between a Task's acceptance and its reflection in a Query is a first-class UI state, not a bug. | ChronosHub ADR 004 (async projections default), ADR 008 (SignalR via Wolverine, decided), ADR 007 (proposed) |
| **Slice-local, surface-composed** | Task, Query and View live in their em slice's folder; nothing is layered across slices. A **surface** is the only cross-slice artifact: it composes Views and Tasks from several slices and is the unit of routing and persona gating. | ChronosHub ADR 002 (decided); `xmlang-spec.md` Surfaces |

**Substrate: the design system.** Components and DTCG tokens are the only render vocabulary. A transformer that emits a component outside the design system, or a value outside `tokens`, is non-conforming. This is the "architecture" half of the maintainer's pair, and the reason a design made in the design system can be read back into the model (see Design-first below).

**xm is stack-neutral; the transformer does the heavy lifting** (maintainer, 2026-09-11). The positions name architectural roles (Task, Query, View, surface), never a framework. Vue 3 is the current stack (ADR 006: "Frontend stack is Vue") and the experiments' instrument; a Blazor Web App direction is under analysis (`blazor_vs_vue_chronoshub.md`, gate at migration stage 4). Each stack gets its own transformer; a stack change replaces a transformer, never the model or the map.

## The 1:1 map

Read forward, this is the transformer contract. Read backward, it is the extraction rule for a design. The first two columns are what xm and em pin. The **design-system binding column is the transformer's**: it is non-normative here, named generically, and each transformer (Vue, Blazor, human) owns its own copy filled from the real design-system inventory (see Decisions).

| Model construct | Frontend artifact (one each) | Design-system binding | Anchor |
|---|---|---|---|
| em slice | `features/<Slice>/` folder holding either one Task or one Query + View, its labels, its tests | — | ADR 002 |
| `c:` command | **Task** component + one mutation; command `props` → form fields; the command's `x:` exceptions → the Task's rejected states | form inputs, ConfirmDialog when `confirm: true` | ADR 006; RFC xmlang-0001 §1 |
| `prominence` on a command item | the Task's trigger on the surface | Button `primary` / `secondary`; `overflow` → overflow menu item | `xmlang-spec.md` Command items |
| Task lifecycle (not in xm) | `idle → submitting → accepted → reflected \| rejected`; `accepted` and `reflected` differ because projections are async | **TaskList** as default feedback (task-list-primary); inline processing state as the per-task escape hatch; optimistic update only with written justification | ADR 007 tentative position |
| `then:` on a command item (provisional) | post-accept navigation target | router push | RFC xmlang-0001 §2 |
| `v:` view | **Query** (subscription, refreshed by push) + **View** component; view `props` → fields | DataTable for list views, DescriptionList for detail views | ADR 004, ADR 008 |
| `fields.primary` / `secondary` / `on-demand` | column set / secondary text / disclosure | DataTable columns, muted text, Disclosure or drawer | `xmlang-spec.md` Field salience |
| `self` | the viewer's own row or entry marked | row highlight, "you" chip | `xmlang-spec.md` Viewer identity |
| `s:` state's `phase` enum | phase read from the state Query, shown wherever the state is | StatusBadge | RFC emlang-0002; `xmlang-spec.md` `during` |
| surface | **route** (page) or **shell** (modal, drawer, card) composing Views first, then Tasks, in `compose` order; heading from its label | Page / Modal / Drawer shells | `xmlang-spec.md` Surfaces, Defaults |
| `for` + persona `role` | route guard on persona; `role` = the auth role claim = em swimlane | — | `xmlang-spec.md` Personas |
| `during` | phase guard on the surface, read from the state Query | — | `xmlang-spec.md` `during` |
| journey | ordered route sequence + one end-to-end walkthrough test chaining the slices' scenarios | — | `xmlang-spec.md` Journeys |
| labels | i18n resources keyed by exact element names; accessible names; `$empty` → empty state | EmptyState; aria snapshot baselines | `xmlang-spec.md` Labels |
| tokens | DTCG → CSS custom properties; token lint fails any value outside the set | the design system's own tokens | `xmlang-spec.md` Tokens |
| Defaults | one generated default surface per slice, every field primary, every action primary, all phases | — | `xmlang-spec.md` Defaults |

Two rules the table implies, stated so they can be checked:

- **One Task per command, one command per Task.** A Task that issues two commands is two Tasks. A command with no Task is dead model or dead code (ADR 006's second failure mode).
- **No client-side fold.** A View shows a Query's fields. It never computes state from events or joins two Queries; if the UI needs a datum no view carries, that is an em debt (a missing view or view prop), not a frontend helper.

## What the map needs that does not exist

em has `SurfaceEmitter` plus `SurfaceComparer`: the emitted records must round-trip against the spec with zero divergences. xm has nothing between the model and the code. Two artifacts, in this order:

1. **Conformance check.** The frontend build emits a small manifest: component → model element (`Task: PayInvoice`, `View: OutstandingInvoices`, `Surface: ClerkHome → /clerk`). A check diffs it against the model pair: every Task ↔ exactly one command, every View ↔ one view, every surface ↔ one route, no command reachable from the UI without a Task, no route without a surface. This is ADR 006's fidelity metric made mechanical. It lands first as a script in the frontend repo, and as `xm check <manifest>` only if the script survives a second slice.
2. **Skeleton emitter.** Task, View, Surface stubs, route table, i18n keys, one end-to-end journey skeleton, from the model pair. Not before the check has run on one real slice. `Emlang.Generators` is the precedent: records and tests first, decider skeleton later.

## ChronosHub binding

B2B SaaS for scholarly publishing. The domain constraint from `xmlang-target-lob` holds: LOB examples only.

**Stack.** Vue 3 for the instrument and the first application (ADR 006; Post Acceptance 2.0). Blazor Web App + Telerik is a live hypothesis with a gate; the positions do not move if it wins.

**Design system.** The Claude Design artifact the maintainer already has, plus prototype-suite `packages/design-system` for vendored tokens. The map needs these component families to exist there: Button (three variants), DataTable, DescriptionList, Disclosure, StatusBadge, EmptyState, Page / Modal / Drawer shells, TaskList, form inputs, ConfirmDialog. Anything missing is a design-system debt the run will surface, not an xm key.

**Personas.** Author (corresponding author submits and pays), Journal editor or manager, Publisher admin, Institution or funder admin (agreements, waivers), ChronosHub operations. Each is an em swimlane and an auth role; `for` does the gating.

**Tenancy.** ADR 009 (provisional) chooses conjoined application-layer tenancy. A persona acts within a publisher, institution or funder tenant. On the em side that is a DCB tag on the state, not an xm fact; xm inherits it through the Query. Candidate Non-goal: no `tenant:` key. Test on the second testbed before ruling.

**Workflows.** Licence selection, APC payment, invoicing, waivers, post-publication corrections: long-running, money-bearing, externally dependent. This is why ADR 007's task-list-primary default fits: the author's mental model already is "I submitted this and something is happening". Payment confirmation is the named processing-state escape hatch.

**External translations.** Crossref, ORCID, payment providers, institutional agreements arrive as em translation slices. Their views carry lag by nature; the Observer position makes that lag a StatusBadge, not a spinner.

**Documents.** Manuscripts, invoices, licence PDFs are view fields of a document type, usually `on-demand`. No document vocabulary in xm.

**Locales and accessibility.** Labels per locale carry the copy; accessible names come from labels; aria snapshots are the verification. WCAG 2.2 AA is a hard requirement under IEEE scrutiny (`blazor_vs_vue_chronoshub.md`, risk 4), so the EmptyState, StatusBadge and TaskList bindings must carry accessible text by construction.

## Design-first, simplified

The design is a Claude Design artifact built from the design system. Because the substrate is closed, extraction is the map read backward: a Button `primary` is a `c:` with `prominence: primary`; a DataTable is a `v:` and its columns the `fields.primary`; a StatusBadge is a `phase` value; an EmptyState is a `$empty` label; a Page is a surface; a prototype link is a journey step; a ConfirmDialog is `confirm: true`. What the map cannot place goes to the gap bin, and the gap bin is the result.

The plan's §3 signal-to-candidate rules are replaced by the table. Its provenance tags (observed / inferred-confirmed / inferred-rejected / supplied), frozen vocabulary, four-bin census and pre-registered prediction stand unchanged. The em interior (events, states, folds, decision tests) is still the agent-plus-human reverse-engineering step with provenance; the map only tells it which components are commands and which are views.

## Decisions only the maintainer can make

1. ~~Task-based UI position~~ confirmed 2026-09-11. ~~Observer and Slice/Surface positions, substrate framing~~ confirmed as worded 2026-09-14 with the RFC's acceptance.
2. ~~Vue binding now or after the Blazor gate~~ resolved 2026-09-11: neither belongs in xm. The binding column is a transformer artifact; the Vue transformer for the experiments carries the first one.
3. Adopt ADR 007's tentative task-list-primary as xm's ChronosHub transformer default, knowing ADR 007 is proposed and expects the frontend engineer to overrule it.
4. Link to the Claude Design design-system artifact, so the binding column is filled from the real component inventory and the missing-families list becomes evidence. Direction fixed 2026-09-14: the first Blazor transformer (below) reuses as much of the ChronosHub design system as possible; how is the next question.
5. Home of the conformance check: frontend repo script first, `xm check` later.
6. Tenancy: Non-goal now, or admission test on the second testbed.

## First application (decided 2026-09-14)

The first transformer is not the Vue instrument. It is a **Blazor standalone (WebAssembly) SPA for CritterStackHelpDesk** (`C:/code/GitHub/CritterStackHelpDesk`, `specs/helpdesk.em.yaml`), emitted by a Roslyn source generator that lives in the CritterStackHelpDesk repository (decision 2026-09-14: generators are per application, never in this repository; kvissig.se carries its own emlang generator) in the shape kvissig.se already proved: the generator emits code-behind partial classes (parameters, Task submission, Query subscription, label keys, route table) from `.em.yaml` + `.xm.yaml` as AdditionalFiles, and the human writes the `.razor` markup. Order per "What the map needs": one slice hand-written first, the binding column read off it, the generator emits what that slice showed. The Observer position costs one SignalR hub (Wolverine transport, ADR 008). Consequence: the experiments program stays on Vue, so the two transformers run side by side and the helpdesk is evidence for the ChronosHub Blazor gate and for the claim that the positions do not move when the transformer changes.

## Open objections

- **Two stacks.** Every binding example is Vue-shaped. If Blazor wins, Telerik's component families may not partition the way the table assumes (DataTable vs DescriptionList). Answer (2026-09-11): the binding column is the transformer's, so a Blazor transformer brings its own; the model columns and the positions do not move. Made real 2026-09-14: the first application is Blazor (above); this objection is now the thing the helpdesk tests.
- **ADR 007 is a draft.** Building xm's lifecycle default on a tentative position risks re-work. Answer: the lifecycle row is the only row that depends on it, and the escape hatches are the same in every option.
- **"Design system as architecture" hides the router and state layer.** The substrate framing names components and tokens; routing, guards and subscriptions are the Observer and Surface positions. If a reviewer finds an artifact in the frontend that none of the three positions owns, that is a fourth position, not a footnote.

## Evidence

- `architectural-decision-records/002 - Adopt Vertical Slice Architecture_SHORT.md`, `006 - Adopt Task-Based UI_SHORT.md` (main); `007 - Handle Asynchronous Read Consistency in the Frontend.md`, `008 - Select the Client-Server Communication Mechanism for Asynchronous Updates.md`, `009 - Provisionally Adopt Conjoined (Application-Layer) Tenancy as the Default Posture.md` (branch `bootstrap/process-docs`)
- `ChronosHubVault/strategy/experience-modeling/experience-model.md` §1 (the symmetric pair), `experience-modeling.md` §4.4 (DTCG, DESIGN.md, Astryx as substrate precedents)
- `ChronosHubVault/strategy/ChronosHub-Engineering-Strategy/raw/blazor_vs_vue_chronoshub.md` (stack hypothesis and gate)
- `xmlang-spec.md` Transformers, Surfaces, Defaults, Non-goals; `src/emlang/Emlang/SurfaceEmitter.cs` and `EmitTarget.cs` (the em landing shape)
- `rfcs/xmlang-0001-interaction-model.md` §1-2 (`confirm`, `then`); `rfcs/0001-evidence/lob-ap.xm.yaml` (the known-answer testbed the check should run on first)
