---
title: "emlang decider dialect 1.1.0 (forked from upstream emlang spec v1.0.0)"
description: "The delta between this repository's emlang and the upstream spec: the s: state element, actor/automation initiators, the decider rules and the lint appendix"
created: 2026-09-14
release: emlang-v0.4.0
tags: [spec, emlang, event-modeling, decider, dcb]
---
# emlang decider dialect

This repository implements an **opinionated dialect** of [emlang v1.0.0](https://github.com/emlang-project/spec): an Event Model DSL for **Vertical Slice Architecture × Decider × Dynamic Consistency Boundaries**. Everything upstream says holds unless a section below says otherwise. The dialect forks the grammar; upstream tools do not read dialect files and no export is offered. The rationale, evidence and rejected alternatives are in the accepted RFCs under [rfcs/](rfcs/): [emlang-0002](rfcs/emlang-0002-decider-profile.md) (state element, decider rules), [emlang-0003](rfcs/emlang-0003-trigger-origin.md) (trigger sets, naming), [emlang-0004](rfcs/emlang-0004-lints.md) (lint appendix), [emlang-0005](rfcs/emlang-0005-initiators.md) (initiators).

The reference implementation is `src/emlang`: `Emlang.Linting.EmAst` (parser), `Linter`, `EmFormatter`, the `em` CLI, and `EmParser` (the surface [xmlang](xmlang-spec.md) resolves against). Code generation from a model is not part of the dialect or this repository; each application carries its own generator against its own conventions.

## Elements

Upstream defines five element kinds. The dialect defines **eight**:

| Kind | short | acronym | long | Where |
|---|---|---|---|---|
| Actor (initiator) | `a:` | | `actor:` | `steps` |
| Automation (initiator) | `auto:` | | `automation:` | `steps` |
| Trigger (legacy initiator) | `t:` | `trg:` | `trigger:` | `steps` |
| Command | `c:` | `cmd:` | `command:` | `steps`, `when` |
| Event | `e:` | `evt:` | `event:` | `steps`, `given`, `then` |
| Exception | `x:` | `err:` | `exception:` | `steps`, `then` |
| View | `v:` | | `view:` | `steps`, `given`, `then` |
| **State** | `s:` | `st:` | `state:` | `steps`, `given`, `then` |

- An **actor** names a role that decides on a screen; an **automation** names a processor that decides from a view. Together they are a slice's **initiators**. `t:`/`trg:`/`trigger:` remain valid as legacy (`em-legacy-trigger`, info); `em fmt` rewrites a legacy trigger once: `auto:` when its swimlane normalizes to `system` or begins with `⚙️`, otherwise `a:`. Leading emoji in a swimlane are decoration and carry no meaning; tools MUST NOT read the kind from them.
- A **state** names one decision model (see below). A **view** is a read model and never a decision model; the swimlane text `State` carries no meaning on a view.

## Swimlanes and names

- The swimlane of an element is the text before the **first** `/`; the remainder is the element's name. A name MUST NOT contain `/`.
- A swimlane is canvas grouping and nothing else: it names no stream, no consistency boundary and no owner.
- An initiator keeps the `Role /Origin` form: the swimlane is the role or processor, the remainder is the origin (free text; its resolution against a surface is xmlang's `xm-origin-mismatch`).

## Initiators (trigger sets)

- A slice MAY contain more than one initiator. Each denotes one legal initiator of the slice's first command; the set is that command's **initiator set**. Two initiators with the same role and different origins are two initiators.
- Every initiator MUST precede the first command of its slice (`em-initiator-after-command`, warning).
- An initiator set says who may issue the command and nothing about who issued it in a scenario; the acting party is a prop on the event (`em-actor-identity`).
- A slice with more than one command is outside the dialect's rules; its initiators attach to the first command.

## Decision models

1. **The state element is a decision model.** Its name is the model's name; a swimlane MAY be written and carries no meaning. Its `props` document the folded state; in a test it carries only the props that test reads or pins. Its **identity props** are the props whose declared name ends in `Id` or `Ids`; they are the model's **tags**. Two state elements with the same name in one document denote the same model.
2. **Fold tests define the query.** A fold test is a test without `when` whose `given` holds at least one event and whose `then` is exactly one state (`em-fold-shape`, error, on an empty `given`, a non-event in `given`, or more than one element in `then`). A model's query is the union of the event types in the `given` of every fold test whose `then` is that state, tagged by the state's identity props. Nothing about the query is declared outside the fold tests.
3. **Append condition.** A decision's events MUST NOT be appended if any event matching the model's query was appended after the position at which the decision's state was read.
4. **Given: exactly one explicit state.** A test with a non-empty `when` is a decision test; its `given` MUST hold exactly one state, MAY hold non-state views the automation reads, and MUST NOT hold events or a second state (`em-given-not-one-state`, error). A state with no `props` is the **empty state**: the query matched no events; it needs no fold test. `given: []` appears nowhere in the dialect; "initial state" is not a concept.
5. **Fold by reference, closed model.** Every event type in a decision test's `then` MUST appear in the `given` of a fold test whose `then` is the given state (`em-then-outside-query`, error). Every folded event type MUST declare, in `steps`, at least one identity prop of the state (`em-fold-untagged-event`, error). Every non-empty given state MUST be the `then` of a fold test (`em-state-without-fold`, error). Every `phase` value pinned on a given state MUST be pinned by a fold test of that state (`em-state-phase-without-fold`, error). Document order carries no meaning.
6. **Todo givens.** A decision test giving a view SHOULD also give the state of the model the command validates against (`em-given-todo`, warning).
7. **Phase per decision model.** A state MAY carry a prop `phase` typed as an enum note `<Enum> (a|b|c)`. Phase values are namespaced by model; two models MAY declare the same bare value. xmlang's `during` consumes them per model.
8. **Actor identity.** Every event a slice with an actor produces carries exactly one actor prop (`em-actor-identity`, error): `<verb>By` (the participle of the event's name: `InvoiceApproved` → `approvedBy`) or `<role>Id` (lowercased, starts with a normalized role of the slice's initiator set, ends with `id`: `BidPlaced` → `playerId`, `AuctionOpened` → `hostPlayerId`). Role normalization: strip leading non-letter/digit characters, trim, lowercase. Events of automation-only slices are exempt; a `<role>Id` there names a subject, not an actor.
9. **Inherited rules.** A slice whose `steps` contain no command (a Decision Model or projection slice) is exempt from `slice-missing-event`. Upstream's documented `test-missing-command` is not implemented: fold and projection tests have no `when` by construction.

## Projection arguments: the `(@param)` note

A view prop whose value ends with the parenthesized note `(@param)` is a **projection argument**: an input the projection takes (`asOf: DateOnly (@param)`, `viewerId: Guid (@param)`), never projected data. The note MUST be the last parenthesized note (`mode: RunMode (dry|live) (@param)`), MUST appear only on views in `steps`, and `@param` MUST NOT appear outside a note (`em-param-note-malformed`, warning). UI filters (what the user asked to see) are surface state and do not belong on an emlang view.

## Lint rules

Rule ids are lowercase hyphenated; dialect rules carry `em-`. Tools MAY let a document suppress a rule by id (`.emlang.yaml` `lint.ignore`). Severities are the dialect column of RFC emlang-0004's index; `info` never fails a lint run.

| Rule | Severity | Meaning |
|---|---|---|
| `command-without-event` | warning | A command not followed by an event or exception (upstream) |
| `orphan-exception` | warning | An exception without a preceding command (upstream) |
| `slice-missing-event` | warning | A slice with a command and no event (upstream, command-less slices exempt) |
| `em-given-not-one-state` | error | A decision test gives anything but exactly one state, optionally with views |
| `em-fold-shape` | error | A fold with an empty or non-event `given`, or more than one element in `then` |
| `em-then-outside-query` | error | A decision emits an event type no fold of its state reads |
| `em-fold-untagged-event` | error | A folded event type declares none of the state's identity props |
| `em-state-without-fold` | error | A given state with props is the `then` of no fold |
| `em-state-phase-without-fold` | error | A given phase value no fold of that state pins |
| `em-given-todo` | warning | A decision test gives a view and no state |
| `em-initiator-after-command` | warning | An initiator follows the slice's command |
| `em-legacy-trigger` | info | A `t:`/`trg:`/`trigger:` element |
| `em-phase-transition-uncovered` | warning | For a command with a scenario on a state, a declared phase with no scenario (success or rejection) |
| `em-actor-identity` | error | An event in an actor-initiated slice with zero or several actor props |
| `em-view-prop-untraced` | warning | A declared view prop (bar `(@param)`) asserted by no projection test |
| `em-param-note-malformed` | warning | `@param` outside a note, a `(@param)` note that is not last, or one outside a view in `steps` |

## Migration from an upstream v1.0.0 document

- `- v: State / X` → `- s: X` in `steps`, `given` and `then`; `given: []` → the named empty state `- s: X`.
- One fold test per pinned phase value and per emitted event type, in the Decision Model slice.
- `t:` → `a:`/`auto:` (`em fmt -w` does it once); decoration emoji stay.
- Events of actor slices gain their actor prop where missing.
- `em-phase-transition-uncovered` and `em-view-prop-untraced` fire on every existing model; suppress by id until the matrix is filled, or accept the count.

## Changelog

### emlang-v0.4.0 (2026-09-14) — the decider dialect, 1.1.0

- **Added** `s:`/`st:`/`state:`; `a:`/`actor:` and `auto:`/`automation:`; `t:` marked legacy with the `em fmt` rewrite.
- **Added** the decider rules and thirteen `em-` lint rules; `slice-missing-event` exempts command-less slices; `info` severity.
- **Changed** the swimlane split to the first `/` in every parser (`EmParser`, upstream-parity with `EmAst`).
- **Changed** `EmParser`: phases per decision model, initiators with origins and kind, slice chains, decision scenarios, `(@param)` on fields.
- **Removed** `Emlang.CodeGen` and the `Emlang.Generators` package: code generation is an application concern (extracted to kvissig.se).
