# Compatibility analysis: proposed emlang constructs vs v1.0.0 and its tools

Evidence file for `rfcs/emlang-000{1..4}-*.md`. Every cell below is either a verified run (2026-09-09, probe files listed at the end) or a file:line citation. Nothing here is inferred from memory; the two items I could not check are marked `[unverified]`.

Sources, fetched verbatim:

- `https://raw.githubusercontent.com/emlang-project/spec/main/SPEC.md` (275 lines) and `schema.json` (227 lines), repo tag `v1.0.0` only.
- Go toolchain `github.com/emlang-project/emlang` (HEAD `ede0916`, tag `v1.0.0`, `yaml.v3`): `internal/parser/parser.go`, `internal/ast/ast.go`, `internal/formatter/formatter.go`, `internal/linter/linter.go`, `cmd/emlang/main.go`. Binary `emlang version` = `emlang version 1.0.0 (spec 1.0.0)`.
- Local: `src/emlang/Emlang/{EmParser,SpecModel,TestModel,TestsEmitter,DeciderEmitter}.cs`, `Linting/{EmAst,Linter,EmFormatter}.cs`, `Emlang.Cli/Program.cs`. Installed `em` is 0.2.0 (no `fmt`); `fmt` runs used a Release build of the working tree, `em version 0.3.0 (emlang spec 1.0.0)`. Lineage: `256f0c2 feat(emlang): canonical formatter (EmFormatter), Go reference port`, `cb4580b feat(emlang): line-aware AST and linter (Emlang.Linting)`; `EmAst.cs:6-11` and `Linter.cs:14-18` declare themselves faithful ports of `internal/parser`, `internal/ast`, `internal/linter`.
- Other org repos: `examples` (three YAML files), `emlang-intellij-plugin` (Kotlin; the file list shows an `EmlangRunner.kt` and no parser or schema, so it appears to shell out to the CLI `[unverified]`), `emlang-project.github.io`.

## 0. The closed-schema ground fact, quoted

schema.json root (L6-13): `"type": "object", "properties": { "slices": {...} }, "required": ["slices"], "additionalProperties": false`.
`extendedSlice` (L41-60): properties `steps`, `tests`; `"required": ["steps"], "additionalProperties": false`.
`test` (L68-109): properties `given`, `when`, `then`; `"additionalProperties": false`.
`element` (L117-153): fourteen type keys (`t trg trigger c cmd command e evt event x err exception v view`) plus `props`; `oneOf` requiring exactly one type key (L136-151); `"additionalProperties": false` (L152).
`givenElement` (L154-173): `e evt event v view props`; `additionalProperties: false`. `commandElement` (L174-189): `c cmd command props`; false. `thenElement` (L190-215): `e evt event x err exception v view props`; false.
`props` (L222-225): `"description": "Free-form properties", "type": "object"` — the only open object in the schema. `elementName` (L216-221): string, `minLength: 1`, `pattern: "[^/]$"`.

SPEC.md: "The root object MUST NOT contain properties other than `slices`" (L13). "An element MUST NOT contain keys other than one type key and `props`" (L40). "Element names are free-form text" (L54); "Names MUST NOT end with `/`" (L56). Swimlanes: "A swimlane MUST be specified as a prefix separated by `/` before the element name" (L73), example `- t: Customer/RegistrationForm   # Swimlane: Customer` (L76). Props: "The structure of `props` is free-form — consuming tools decide how to interpret them" (L86). Extended form "MUST NOT contain properties other than `steps` and `tests`" (L127). Tests: `given` allows `e`, `v`; `when` allows `c`; `then` allows `e`, `v`, `x` (L177-181, L185-187); "A test MUST NOT contain properties other than `given`, `when`, and `then`" (L188). Document structure: "Each document MUST independently conform to this specification" (L238). Comments: "Standard YAML comments (`#`) MAY be used anywhere" (L267). There is no section on versioning, extensions, profiles, lints, or contribution.

Both parsers implement the closure as hard errors with identical wording: unknown top-level key (`parser.go:104`, `EmAst.cs:119`), unknown slice key (`parser.go:195`, `EmAst.cs:187`), unknown test key (`parser.go:287`, `EmAst.cs:256`), unknown element key (`parser.go:380`, `EmAst.cs:340`), multiple type keys (`parser.go:362`, `EmAst.cs:317`), section type check (`parser.go:253-255, 303-306`, `EmAst.cs:224-227, 271-273`). Both CLIs exit 1 on any parse error before lint or fmt run (`main.go:178-181, 293, 396`; `Program.cs:156-164, 260, 327`).

## 1. Construct matrix

Columns: (S) upstream schema v1.0.0; (G) upstream Go `emlang parse|lint|fmt` 1.0.0; (A) local `em parse|lint` via `EmAst`; (F) local `em fmt` round-trip; (C) local codegen path `SpecModel`/`TestModel`/`TestsEmitter` (used by `EmlangRecordsGenerator.cs:59`, `EmlangTestsGenerator.cs:23`) and `EmParser` (used by `xm`, `Xmlang.Cli/Program.cs:22`).

| Construct | S schema | G Go tools | A `em parse/lint` | F `em fmt` | C codegen / `xm` |
|---|---|---|---|---|---|
| Root key `emlang: {version, profile}` | rejected: root `additionalProperties: false` (L13) | rejected: `Parse error in p1-rootkey.yaml: unknown top-level key "emlang" at line 1`, exit 1, all three commands | rejected: same string (`EmAst.cs:119`) | rejected (parse precedes format) | **silently ignored**: `SpecModel.cs:36`, `TestModel.cs:46`, `EmParser.cs:57` index `root.Children["slices"]` and never enumerate root keys |
| YAML comment header `# emlang: profile=decider` | passes (comments are not data; SPEC L267) | passes; **`fmt` silently drops it** (verified, output starts `slices:`) | passes | **silently dropped** (verified; byte-identical to Go output) | passes, ignored (YamlDotNet `YamlStream` discards comments) |
| Form A `v: State /Invoice` (+ `phase: InvoicePhase (draft\|approved)`) | passes | passes; `fmt` rewrites to `view: State/Invoice` (spaces around `/` removed) | passes | passes; same normalization (`EmAst.cs:330-331` trims, `EmFormatter.cs:82` re-joins without spaces) | passes; already the classification key (`TestsEmitter.cs:86, 114`; `EmParser.cs:147`) |
| Form B `s: Invoice` in `steps` | rejected: `element` oneOf + `additionalProperties` (L136-152) | rejected: `slice "S": steps: unknown key "s" at line 4` | rejected: same string (`EmAst.cs:340`) | rejected | **silently dropped**: `SpecModel.ToElement` returns null for a non-`c/e/x/v` key (`SpecModel.cs:68-78`) and the step is skipped (`:41-42`); `EmParser.Collect` `continue`s (`EmParser.cs:109-110`) |
| Form B `s:` in `given` | rejected: `givenElement` (L154-173) | rejected: `slice "S": tests: test "T": given: unknown key "s" at line 9` | rejected (`EmAst.cs:340`, wrapped by `:268, :217, :142`) | rejected | **hard failure**: `TestModel.ToStep` throws `InvalidDataException("test step at line 9 has no c/e/x/v key")` (`TestModel.cs:101`), i.e. the whole generator run fails, not one test |
| Form B `s:` in `then` | rejected: `thenElement` (L190-215) | rejected: `... then: unknown key "s" at line 11` | rejected | rejected | hard failure, same path (`TestModel.cs:101`) |
| Two `t:` in one slice | passes: `elementList` is an unconstrained array (L110-116) | passes: parse prints both `trigger:` lines; `lint` `OK (no issues found)`; `fmt` keeps both | passes | passes (verified) | passes: `EmParser.CollectRole` appends both roles (`EmParser.cs:104-107, 116-121`), `Distinct()` at `:68`; `SpecModel`/`TestModel` ignore `t:` (`SpecModel.cs:60-66`, `TestModel.cs:81-84`) |
| Trigger-origin resolution (text after `/` names a `v:` or a declared bare form) | passes: pure lint; the origin is just the `elementName` remainder | passes: nothing reads it; `ast.go:84-92` splits at the first `/` and keeps the rest as `Name` | passes (`EmAst.cs:327-332`) | passes | passes: `EmParser.CollectRole` discards the origin (`var (role, _) = EmSpec.Split(trigger)`, `EmParser.cs:118`) |
| `@param` inside a props value (`asOf: DateTimeOffset @param`) | passes (`props` free-form, L222-225) | passes; `fmt` preserves the string verbatim (verified) | passes | passes, preserved (verified) | **breaks generated C#**: `SpecModel.MapType` strips only a parenthesized note (`SpecModel.cs:115-128`), so the record gets type `DateTimeOffset @param`. Writing the marker inside the note, `DateTimeOffset (@param)`, is stripped by `StripParenthesizedNote` (`:124-128`) and is not an enum because `IsEnumNote` requires `\|` (`:101-105`). `EmParser` keeps the raw annotation (`EmParser.cs:138-140`) |
| `params:` sibling key on an element (deferred) | rejected (`element` L152) | rejected: `slice "S": unknown key "params" at line 6` | rejected (`EmAst.cs:340`) | rejected | silently ignored: all three read only `props` (`SpecModel.cs:92`, `TestModel.cs:105`, `EmParser.cs:137`) |
| `compensates:` on a command (deferred) | rejected (`element` L152; `commandElement` L188 in tests) | rejected: `slice "S": unknown key "compensates" at line 4` | rejected | rejected | silently ignored, same reason |

Reading of the matrix: the strict surfaces (schema, Go, `EmAst`) agree exactly, string for string. The lenient surfaces (`SpecModel`, `TestModel`, `EmParser`) were written before the port and do not validate; they tolerate the root key and unknown siblings, drop `s:` from steps without a word, and only `TestModel` fails hard. So today a document with `emlang:` at the root generates records and runs `xm` fine but cannot pass `em lint` or `em fmt`. That asymmetry is itself an argument for RFC 0001 landing first: it is the one construct the lenient path already accepts.

## 2. Breaking rows: what fails and the minimal fix

### 2.1 Root key `emlang:`

Fails at `parser.go:103-104` / `EmAst.cs:118-119` (`default: throw ... unknown top-level key`). Both formatters emit a bare `slices:` line per sub-document (`formatter.go:75-76`, `EmFormatter.cs:27-29`) and would have no place to put a header even if the parser accepted it.

Schema patch (JSON-Patch style):

```json
[
  { "op": "add", "path": "/properties/emlang", "value": { "$ref": "#/$defs/header" } },
  { "op": "add", "path": "/$defs/header", "value": {
      "type": "object",
      "properties": {
        "version": { "type": "string" },
        "profile": { "type": "string" }
      },
      "additionalProperties": false } }
]
```

Root `additionalProperties: false` and `required: ["slices"]` stay as they are. SPEC L13 must change to "other than `slices` and `emlang`".

Local: add `case "emlang":` beside `case "slices"` in `EmAst.ParseDocument` (`EmAst.cs:116-119`) and carry the header on `EmSubDoc` (`EmAst.cs:54`); emit it in `EmFormatter.WriteSubDoc` before `slices:` (`EmFormatter.cs:27-32`). Go: `parser.go:90-105` add a case; `ast.go:4-7` a field; `formatter.go:75-82` the write. `SpecModel`, `TestModel`, `EmParser` need nothing.

### 2.2 Form B `s:`

Fails at `parser.go:379-380` / `EmAst.cs:338-341`, reached from steps (`parseElementList`, `parser.go:312-327`) and from every test section (`parseTestSection`, `parser.go:295-309`). In codegen it fails at `TestModel.cs:101`.

Schema patch: add `"s": { "$ref": "#/$defs/elementName" }` (and a long form, e.g. `"state"`) to `properties` and `{ "required": ["s"] }` to `oneOf` in each of `element` (L119-151), `givenElement` (L157-171) and `thenElement` (L193-213). Three definitions, six edits.

Local touch list: `EmElementType` gets a sixth member (`EmAst.cs:13`), `Display` (`:17-25`), `Prefixes` (`:66-82`), `AllowedGiven`/`AllowedThen` (`:224-227`); `EmFormatter.TypeKey` (`EmFormatter.cs:92-103`); `SpecModel.Kinds` (`SpecModel.cs:60-66`), `TestModel.Kinds` (`TestModel.cs:81-84`), `EmParser.Kinds` (`EmParser.cs:86-92`); `TestsEmitter` classification (`TestsEmitter.cs:84-90`, `:114`) and `EmSpec.FindView`/`PhaseValues` (`EmParser.cs:24-28, 145-151`) switch from `Lane == "State"` to `Kind == 's'`. Go: `parser.go:14-29`, `ast.go:41-64`, `parser.go:253-255`, `formatter.go:17-33`, plus the diagram templates (`internal/diagram/templates/element.gohtml`, per-type colours in `main.go:117-124`) `[unverified: not fetched]`. Every downstream consumer that switches on the five kinds changes; that is the Go-parity cost the design pass named.

### 2.3 `params:` / `compensates:` (for the record)

Fail at the same `unknown key` line (`parser.go:380`, `EmAst.cs:340`). Patch: `{ "op": "add", "path": "/$defs/element/properties/params", "value": { "$ref": "#/$defs/props" } }`, and for `compensates` `{ "$ref": "#/$defs/elementName" }` on `element` and `commandElement`. Local: `EmAst.ParseElement` (`:308-312`) gets a second `continue` branch and `EmElement` (`:30-36`) a field; the formatter writes it (`EmFormatter.cs:85-89`). Deferred per PLAN.md; recorded so the RFC can say what "one key" costs.

## 3. `em fmt` round-trip (decides the comment-header fallback)

Both formatters render from the AST, never from source. `formatter.go:36-52` walks `doc.SubDocs`; `EmFormatter.cs:6-11` says so in its header ("comments are dropped, exactly like the reference"); `Program.cs:11` repeats it.

(a) Root key other than `slices`: never reaches the formatter; both CLIs stop at the parse error (verified, `p1-rootkey.yaml`). Neither `writeSubDoc` (`formatter.go:75-82`) nor `WriteSubDoc` (`EmFormatter.cs:27-32`) has a slot for one.

(b) Leading YAML comment `# emlang: profile=decider`: parses fine in both; `fmt` output begins at `slices:` in both; the two outputs are byte-identical (verified with `diff`); a second `fmt` pass is idempotent. So `emlang fmt -w` or `em fmt -w` **destroys the header on first save**. The fallback is viable only for tools that never format. It can be rescued locally without touching the AST: `CmdFmt` (`Program.cs:327-335`) already has the raw text; detect a leading `# emlang:` line and prepend it to `output`. Go's `yaml.v3` even carries `HeadComment` on the document node, but `parser.go` never reads it (`parser.go:44-70` decodes into a `yaml.Node` and passes only `Content`), so the Go side would need the same `cmdFmt` patch (`main.go:293-304`). Two forks of the same three-line hack in two CLIs versus one root key: that is the trade.

(c) Unknown element kind (`s:`): rejected at parse by both (`p3`, `p4`, `p5`); no formatter path.

Side effect worth stating in the RFCs: both formatters rewrite `Clerk /Invoice form` and `State / Invoice` to `Clerk/Invoice form` and `State/Invoice` (`ast.go:84-92` + `parser.go:374-375`; `EmAst.cs:330-331` + `EmFormatter.cs:82`). Every local fixture uses spaced lanes, so a formatting pass is a large textual diff with no semantic change; `EmSpec.Split` trims both sides (`EmParser.cs:33-41`), so `xm` is indifferent.

One more divergence to know about before RFC 0003 defines "text after `/`": Go and `EmAst` split at the **first** `/` (`ast.go:85-90`, `EmAst.cs:327`), while `EmParser.Split`, `SpecModel.Lane` and `TestModel.ToStep` split at the **last** (`EmParser.cs:37`, `SpecModel.cs:83, 89`, `TestModel.cs:93`). They agree only while names contain at most one slash. The origin rule should say "the remainder after the swimlane separator" and forbid a second `/`, or the two halves of the local toolchain will disagree on what the origin is.

## 4. What v1.0.0 already permits that the RFCs merely name

- **Multiple `t:` per slice.** `elementList` (schema L110-116) is `array` of `element` with `minItems: 1` and no uniqueness or ordering constraint; SPEC L97 calls a slice "a named sequence of elements" and says nothing about trigger count. No tool rejects it (verified `p6`, all six surfaces). No rule in `linter.go` or `Linter.cs` looks at triggers at all (the three rules: `command-without-event` L99/L54, `orphan-exception` L107/L60, `slice-missing-event` L115/L66, all `SeverityWarning`). Zero instances exist upstream (`examples/pizza.yaml` has 13 `t:` lines at 13 distinct slices; the other two examples have 1 and 0) or locally (no two adjacent `t:` lines in the three game fixtures or `lob-ap.em.yaml`). RFC 0003 gives an existing legal shape a meaning; it changes no validator.
- **Free text after `/`.** SPEC L54 ("free-form text"), L56-57 and schema `elementName` (L216-221) constrain only emptiness and a trailing slash. The swimlane is "a prefix separated by `/`" (L73); the remainder is the name and nothing in spec or code interprets it. Census against the rule "origin names a `v:` in the same file": `lob-ap.em.yaml` 18/20 resolve, the two misses being `⚙️ System / Payment run` and `⚙️ System / Approval reminder` (automation origins, the bare-form case); `blindbudet` 0/7, `mer-eller-mindre` 0/9, `tank-till-tusen` 1/7 (`Player /Puzzle`). The lint bites the games and barely touches the LOB model, which is the direction PLAN.md wants.
- **Free-form `props` values.** Schema L222-225 (`type: object`, nothing else) and SPEC L86. `phase: InvoicePhase (draft|approved)` and `asOf: DateTimeOffset @param` are plain strings to every parser; Go's `parseProps` decodes each value with `valNode.Decode(&val)` into `interface{}` (`parser.go:391-407`), `EmAst.ParseProps` into string/list/mapping (`EmAst.cs:350-372`). Only the local codegen assigns meaning (enum note `SpecModel.cs:100-105`, `PhaseValues` `EmParser.cs:143-151`), so the `@param` convention costs upstream nothing and costs the local generator one rule if written as `(@param)`.
- **Upstream lint list is already non-normative and drifted.** The Go `README.md` L68-82 documents ten rules (`empty-slice`, `test-missing-command`, `test-missing-then`, `test-invalid-*`, `trigger-in-test` as errors); `linter.go` implements three warnings, the `test-invalid-*`/`trigger-in-test` cases are parser errors (`parser.go:303-306`), and `empty-slice`, `test-missing-command`, `test-missing-then` appear nowhere in the code. A lint appendix that names rules beyond the reference implementation has precedent in the reference repo itself.

## 5. Risks, one line each

- Root key: the single schema change every profile depends on is also the only one of the ten rows all three strict surfaces reject with the same string; if upstream declines it, every profile document fails `emlang lint` upstream forever.
- Comment header: both formatters delete it on first `-w`; a fallback that dies on save is a trap unless both `cmdFmt`s are patched.
- Form B `s:`: six schema edits, eleven local touch points, four Go files plus diagram templates `[unverified]`, and a hard `InvalidDataException` in today's generator; Form A costs nothing anywhere.
- `@param` as `DateTimeOffset @param` produces uncompilable C# from today's `SpecModel.MapType`; the RFC must specify the parenthesized form `(@param)` or ship a `MapType` change with it.
- First-slash vs last-slash split between `EmAst`/Go and `EmParser`/`SpecModel`/`TestModel` means an origin containing `/` is read differently by `em lint` and `xm`.
- Formatting normalizes `State / X` to `State/X`, so the first `em fmt -w` on any fixture is a whole-file diff and every census line number in the evidence files moves.
- Origin resolution fires on 22 of 23 game triggers today; the games need bare-form declarations before the lint can be a warning with a clean baseline.
- The lenient codegen path accepts what `em lint` rejects (root key, `params:`, `compensates:`, `s:` in steps), so a document can generate and still fail CI lint; the RFCs should say which surface is normative.
- Installed `em` is 0.2.0 and lacks `fmt`; anyone reproducing section 3 needs 0.3.0 from source or NuGet.
- Upstream has one tag, no CHANGELOG, no contribution process, and a README lint list out of sync with code, so "accepted upstream" has no defined event to wait for.

## Probe files

Nine files in the session scratchpad, contents in the matrix rows: `p1-rootkey` (`emlang: {version: "1.0.0", profile: decider}` above `slices:`), `p2-comment` (`# emlang: profile=decider` first line), `p3-s-steps`, `p4-s-given`, `p5-s-then`, `p6-two-t` (two `t:`, `v: State / Invoice` with `phase` enum note), `p7-param-note`, `p8-params-key`, `p9-compensates`. Each was run through `emlang parse|lint|fmt` (Go 1.0.0), `em parse|lint` (0.2.0) and `em fmt` (0.3.0 source build); exit codes and messages are as quoted above.
