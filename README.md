# xmlang

xmlang is a YAML-based DSL for **Experience Models**: the sibling dialect to emlang (Event Models as YAML), recording the UX judgments an Event Model deliberately omits — personas, surface composition, field salience, journeys, labels, design tokens — **as data, never as geometry**. The Experience Model depends on the Event Model strictly one-way, and every reference is lintable.

The canonical specification lives here: [xmlang-spec.md](xmlang-spec.md); the emlang dialect it depends on is specified as a delta against upstream in [emlang-dialect.md](emlang-dialect.md). This repository is also the reference implementation: a parser, an Event Model resolution surface, and a linter implementing the spec's full rule set.

The repository also hosts the .NET implementation of [emlang](https://github.com/emlang-project/emlang) itself — xmlang cannot live without it. The emlang here is an **opinionated dialect**: an Event Model DSL for **Vertical Slice Architecture × Decider × Dynamic Consistency Boundaries** (see [Stance](#stance)).

- **Emlang** — the em parser (`EmParser`, the reference surface xm resolves against) and the line-aware AST, linter and formatter (`Emlang.Linting`).
- **Emlang.Cli** — `em`, the .NET clone of the reference Go CLI (`dotnet tool install -g Emlang.Cli`). Commands so far: `em parse`, `em lint` and `em fmt` (`-w`, `--keys short|long`) with the reference toolchain's rule set and output format, stdin via `-`, plus `version`/`help`.

This repository is the two DSLs and nothing downstream of them. Code generation from a model is an application concern: each app carries its own source generators against its own conventions (kvissig.se has one for its deciders; CritterStackHelpDesk will have one for its Blazor surfaces). `Emlang.Generators` was extracted to kvissig.se on 2026-09-14 and is no longer published.

The emlang packages version and release independently (tags `emlang-v*`) from the xmlang packages (tags `xmlang-v*`).

## Stance

Event Modeling draws a system as a timeline of slices. This dialect takes three positions on what a slice *is*, and the [RFCs](rfcs/) make them lintable:

| Position | Meaning here | Reference |
|---|---|---|
| **Vertical Slice Architecture** | Every slice is a complete feature: initiator, command, events, view, tests. Nothing is layered across slices. | Jimmy Bogard, [Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/) (2018); Adam Dymitruk, [Event Modeling](https://eventmodeling.org/) |
| **Decider** | A state-change slice is a pure `decide(command, state) → events` plus `evolve(state, event) → state`. Tests give **state**, never event lists; every state has a fold test that produces it. | Jérémie Chassaing, [Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider) (2021) |
| **Dynamic Consistency Boundary** | A state is a *decision model*, defined by its fold tests: the event types they fold are its query, its identity props are its tags. No aggregates, no stream-per-entity; consistency is an append condition on that query. Swimlanes are canvas grouping only. | Sara Pellegrini, [Kill Aggregate!](https://sara.event-thinking.io/2023/04/kill-aggregate-chapter-1-I-am-here-to-kill-the-aggregate.html) (2023); Bastian Waidelich & Sara Pellegrini, [dcb.events](https://dcb.events/) |

The intended domain is B2B SaaS and line-of-business software. Background on the method: Martin Dilger, [Understanding Eventsourcing](https://leanpub.com/eventmodeling-and-eventsourcing).

The dialect forks the upstream [emlang spec v1.0.0](https://github.com/emlang-project/spec) grammar: it adds an `s:` state element and makes the decider rules the default. Upstream tools do not read dialect files. The positions above are accepted RFCs under [rfcs/](rfcs/) (2026-09-14), not yet implemented in `src/`.

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

The Xmlang package minor version tracks the specification version (`Xmlang 0.5.x` implements spec v0.5); the patch component is free for implementation fixes. The Emlang packages carry their own SemVer (`emlang-v*` tags) and implement the decider dialect in [emlang-dialect.md](emlang-dialect.md), forked from upstream emlang spec v1.0.0.

## Provenance

Extracted from the proprietary kvissig.se codebase and relicensed under MIT by its copyright holder.

## License

[MIT](LICENSE)
