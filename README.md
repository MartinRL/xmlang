# xmlang

Two YAML-based DSLs for specification-driven development:

- **emlang** — Event Model DSL. Records what the system *does*: commands, events, views, test scenarios, state. An event-sourced domain expressed as pure data.
- **xmlang** — Experience Model DSL. Records what users *see and do*: personas, surfaces, field salience, interaction judgments (`confirm:`, `then:`), journeys, labels, design tokens. **Judgment as data, never as geometry.** The Experience Model depends on the Event Model strictly one-way; every reference is lintable.

Together, they separate domain logic from UX judgment, and make both machine-readable. The specs drive a deterministic source generator (at build time, equality contract) for domain vocabulary, and an interpreter (at request time, conformance contract) for UI rendering. See [The Screens Left the Repo](https://martinrl.github.io/chronograph/the-screens-left-the-repo) for the design rationale.

The canonical specifications live here: [xmlang-spec.md](xmlang-spec.md) and [emlang-dialect.md](emlang-dialect.md). This repository is the reference implementation: parsers, linters, and CLIs for both dialects.

### Packages

**Event Model (emlang)**
- **Emlang** — Parser (`EmParser`), line-aware AST, linter, and formatter.
- **Emlang.Cli** — `em` command-line tool. `em parse`, `em lint`, `em fmt` with support for stdin and formatting options.

**Experience Model (xmlang)**
- **Xmlang** — Parser (`XmParser`), linter, and interpreter (resolves xm specs against em specs).
- **Xmlang.Cli** — `xm` command-line tool. `xm lint` resolves and validates xm specs against their event models.

**Important:** This repository is the two DSLs, parsers, and linters only. Code generation and UI rendering are application concerns. Each app carries its own source generators and interpreters against its own conventions:
- kvissig.se has a Roslyn source generator for decider vocabulary (extracted 2026-09-14).
- RequestSample demonstrates both patterns: generated domain vocabulary + an interpreter for UI rendering.

The packages version independently: `emlang-v*` tags for Event Model releases, `xmlang-v*` for Experience Model.

## How They Work Together

```
Event Model (emlang)          Experience Model (xmlang)
├─ Commands                   ├─ Surfaces
├─ Events                     ├─ Personas
├─ Views (data, not layout)   ├─ Journeys
├─ State (decider folds)      ├─ Commands + confirm:/then: judgment
└─ Test scenarios             └─ Labels, design tokens

        ↓ Lintable references (one-way dependency)

Source Generator (app-owned)  Interpreter (request-time)
├─ Generates domain vocab     ├─ Reads xm spec
├─ Vocab lock-in: approval    ├─ Renders surfaces
│  tests at CI                ├─ Approval tests on output
└─ Deterministic: equality    └─ Conformance contract
   contract
```

The event model is the source of truth. The experience model records surface decisions, personas, and interaction judgments as data. A source generator turns event models into deterministic code; an interpreter turns experience models into rendered UX at request time. Both are validated by approval tests.

## Dialect Positions

The emlang dialect (defined in [emlang-dialect.md](emlang-dialect.md)) forks upstream emlang spec v1.0.0 with three positions on what a slice *is*. The xmlang dialect (defined in [xmlang-spec.md](xmlang-spec.md)) defines how experience decisions bind to event model slices:

| Position | Meaning in this dialect | Reference |
|---|---|---|
| **Vertical Slice Architecture** | Every slice is a complete feature: initiator, command, events, view, tests. Nothing is layered across slices. Surfaces in xm map 1:1 to decision model slices. | Jimmy Bogard, [Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/) (2018); Adam Dymitruk, [Event Modeling](https://eventmodeling.org/) |
| **Decider** | A state-change slice is a pure `decide(command, state, context) → events ∪ error`. Tests give **state**, never event lists; every state has a fold test that produces it. The xm dialect adds interaction judgment (`confirm:`, `then:`) to commands. | Jérémie Chassaing, [Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider) (2021) |
| **Dynamic Consistency Boundary** | A state is a *decision model*, defined by its fold tests: the event types they fold are its query, its identity props are its tags. Surfaces compose views from this query. No aggregates, no stream-per-entity; consistency is an append condition on that query. | Sara Pellegrini, [Kill Aggregate!](https://sara.event-thinking.io/2023/04/kill-aggregate-chapter-1-I-am-here-to-kill-the-aggregate.html) (2023); Bastian Waidelich & Sara Pellegrini, [dcb.events](https://dcb.events/) |

The intended domain is B2B SaaS and line-of-business software. Background on the method: Martin Dilger, [Understanding Eventsourcing](https://leanpub.com/eventmodeling-and-eventsourcing).

The RFCs accepting these positions live under [rfcs/](rfcs/) (accepted 2026-09-14).

## Install

The library (parser + linter, for build-time tools or runtime interpreters):

```
dotnet add package Xmlang
```

The CLI (a dotnet global tool named `xm`):

```
dotnet tool install -g Xmlang.Cli
xm lint specs/my-game.xm.yaml
```

`xm lint` resolves the document's `model:` Event Model(s) relative to the xm file, prints every finding, and exits non-zero on errors.

## Usage (library)

```csharp
using Emlang;
using Xmlang;

var xm = XmParser.Parse(File.ReadAllText("shop.xm.yaml"));
var em = EmParser.Parse(File.ReadAllText("shop.em.yaml"));
var findings = XmLinter.Lint(xm, em);
```

## Versioning

The Xmlang package minor version tracks the specification version (`Xmlang 0.6.x` implements spec v0.6); the patch component is free for implementation fixes. The Emlang packages carry their own SemVer (`emlang-v*` tags) and implement the decider dialect in [emlang-dialect.md](emlang-dialect.md), forked from upstream emlang spec v1.0.0.

## Provenance

Extracted from the proprietary kvissig.se codebase and relicensed under MIT by its copyright holder.

## License

[MIT](LICENSE)
