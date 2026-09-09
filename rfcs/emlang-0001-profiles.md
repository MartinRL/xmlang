---
title: "emlang RFC 0001: Profiles and the emlang document header (draft for spec 1.1.0)"
status: draft
created: 2026-09-09
targets: emlang spec 1.1.0 (upstream emlang-project/spec); local Emlang 0.4.0
---
# emlang RFC 0001: Profiles and the `emlang:` document header

**Status: draft.** Nothing here is applied to the upstream spec or to `src/emlang`. This RFC is drafted in the xmlang repo as a review artifact for the maintainer to carry upstream or keep as a dialect. Dependency graph of the set: this RFC is required by RFC 0002 (the decider profile), which is required by RFC 0004 (lints that hang off the profile); RFC 0003 section A stands alone, its section B severity column depends on RFC 0002. This RFC contains no profile's rules.

## Summary

One optional root key, `emlang: { version, profile }`, so a YAML document can declare which spec version it is written against and which additional rule set (profile) it submits to. A profile is a conformance level: it never widens the set of valid documents and may narrow the meaning of a base construct. A document under a profile with the header removed MUST be a valid v1.0.0 document. `em fmt` preserves the header verbatim and first. An unknown profile is a warning, never an error. No other construct.

## Motivation

emlang v1.0.0 is closed at every level. The Overview says "The root object MUST NOT contain properties other than `slices`"; `schema.json` has `"additionalProperties": false` on the root, on `element`, on `extendedSlice`, on `test`, and on all three test-item schemas. The spec has no versioning, extension or profile section. So every new key, however additive it feels, fails every v1.0.0 validator. The root key proposed here is rejected today by the schema, by the Go `emlang` CLI and by `em lint`/`em fmt`, all with the same string, `unknown top-level key "emlang" at line 1` (`parser.go:104`, `EmAst.cs:119`), and silently accepted by `SpecModel`, `TestModel` and `EmParser`, which index the root for `slices` and never enumerate it (`SpecModel.cs:36`, `TestModel.cs:46`, `EmParser.cs:57`). So codegen and `xm` already run on a document that carries the header; only the strict surfaces refuse it (`rfcs/emlang-evidence/compat.md`, section 1, row 1). That asymmetry is why this RFC comes first in the set: it is the one construct the lenient path already accepts.

The maintainer's planned rules for decider-true tests (RFC 0002) narrow what `given` means; they are stricter than v1.0.0 and would reject the upstream spec's own `EmailMustBeUnique` example. Shipping them unmarked loses the property that a reader of the document knows which rules it was written under. The alternatives for marking a document are weighed below; the root key is the proposal, the per-project config file is the recorded alternative the maintainer decides between.

xmlang RFC 0001 (open objection 3) records that xmlang's derived Defaults rest on conventions the emlang spec does not state. Profiles are where such conventions become stated text without becoming v1.0.0 requirements.

## Proposed normative changes

Throughout, "document" means one YAML document (the unit between `---` separators), matching "Document Structure": "Each document MUST independently conform to this specification". RFC 0002 uses the same scope.

### 1. The `emlang:` header

Amend "Overview": replace "The root object MUST NOT contain properties other than `slices`" with the following.

- The root object MUST NOT contain properties other than `slices` and `emlang`
- `emlang` is OPTIONAL; a document without it is a v1.0.0 document
- If `emlang` is present it MUST be a mapping with a REQUIRED `version` key and an OPTIONAL `profile` key, and MUST NOT contain other keys
- `version` MUST be a string naming the spec version the document is written against, in `MAJOR.MINOR.PATCH` form
- `profile` MUST be a string naming one profile defined by this specification or by a later RFC
- A tool that does not implement the named profile MUST report a warning (`em-header-unknown-profile`) and MUST process the document as a document of the declared `version`
- A tool that does not implement the declared `version` MUST report a warning (`em-header-version-unsupported`) and MUST process the document under the version it implements
- The header SHOULD be the first key of the document; formatters MUST write it first
- In a file with several YAML documents the header applies only to the document that carries it

`version` is required when the header is present because a profile is defined relative to a base version; `emlang: { profile: decider }` with no base would leave the reader to guess which `given` rules the profile tightens. Unknown profile and unsupported version are warnings so that a newer tool never fails on a document an older tool accepts: the header must not convert v1.0.0's tolerable "unknown root key" into a hard failure on the tools that know the key.

```yaml
# proposed
emlang: { version: "1.1.0", profile: decider }
slices:
  ✍️ Submit Invoice:
    steps:
      - t: 🧾 Clerk /Invoice details
      - c: SubmitInvoice
      - e: Invoice / InvoiceSubmitted
```

### 2. Profiles

Add a section "Profiles" after "Document Structure".

- A profile is a named set of rules that a document opts into through the header
- A profile MUST NOT widen the set of valid documents of the `version` it is declared against; a profile MAY narrow the meaning of a base construct
- A document that declares a profile MUST, with its `emlang` key removed, be a valid document of the declared `version`
- A profile MAY reserve names (swimlanes, prop names) and MAY attach meaning to constructs the base leaves free-form; it MUST say so explicitly
- A profile's rules are reported with the severity the profile states; outside the profile the same checks MAY run at a lower severity or not at all
- This specification defines no profile; profiles are defined by separate RFCs (the first is RFC 0002, `decider`)

"Narrow the meaning" is the license RFC 0002 uses: in v1.0.0 a `given` is "Pre-conditions (events, views)"; under the decider profile a decision test's `given` is the decider's state. Every decider-profile document is still a valid v1.0.0 document; the profile only rejects some v1.0.0 documents and says more about the rest.

### 3. Formatting

- A formatter MUST preserve the `emlang` header verbatim, including the key order inside it, and MUST emit it as the first key of the document

### 4. Schema

Before (`schema.json`, root):

```json
"properties": {
  "slices": { "$ref": "#/$defs/slices" }
},
"required": ["slices"],
"additionalProperties": false,
```

After:

```json
"properties": {
  "emlang": { "$ref": "#/$defs/header" },
  "slices": { "$ref": "#/$defs/slices" }
},
"required": ["slices"],
"additionalProperties": false,
"$defs": {
  "header": {
    "description": "Spec version and optional profile the document is written against",
    "type": "object",
    "properties": {
      "version": { "type": "string", "pattern": "^[0-9]+\\.[0-9]+\\.[0-9]+$" },
      "profile": { "type": "string", "minLength": 1 }
    },
    "required": ["version"],
    "additionalProperties": false
  },
```

### 5. Lint rules

| Rule | Severity | Meaning |
|---|---|---|
| `em-header-unknown-profile` | warning | `profile` names no profile the tool implements; the document is processed as its base version |
| `em-header-version-unsupported` | warning | `version` is not a version the tool implements; the document is processed under the tool's version |
| `em-header-not-first` | warning | The header is present but not the first root key; `em fmt` fixes it |

## Changelog entry (draft)

### v1.1.0 - the `emlang:` header and profiles

- **Added `emlang:`** (root, optional): `version` (required) and `profile` (optional). Every other root key remains forbidden
- **Added "Profiles"**: a profile never widens validity and may narrow meaning; a profile document with the header removed is a valid base document; unknown profiles are warnings
- **Formatting**: the header is preserved verbatim and written first
- Lint rules: `em-header-unknown-profile`, `em-header-version-unsupported`, `em-header-not-first`, all warnings

## Migration

None. Every v1.0.0 document is a v1.1.0 document. A v1.0.0 validator rejects a document that carries the header with one finding, the unknown root key; removing the key restores conformance by construction.

## Implementation notes (reference implementation)

- Touch list, from `compat.md` section 2.1. Local: `EmAst.ParseDocument` (`EmAst.cs:116-119`) gains a `case "emlang":` beside `case "slices"` that parses `version` and `profile` into a header record on `EmSubDoc` (`EmAst.cs:54`); `EmFormatter.WriteSubDoc` (`EmFormatter.cs:27-32`) writes it before `slices:`. `SpecModel`, `TestModel` and `EmParser` need nothing. Go: `parser.go:90-105` a case, `ast.go:4-7` a field, `formatter.go:75-82` the write.
- Both formatters render from the AST and drop comments by design (`EmFormatter.cs:8`, `formatter.go:36-52`), which is why the header is a key and not a comment (see the rejected alternative below).
- `Linter` (`Linter.cs:19-32`) has no notion of document-level context; pass the header down so profile rules (RFC 0002) can pick their severity. The existing three rules are a port of the Go reference set and carry no prefix (`Linter.cs:14-18`); the `em-` prefix on new rules marks them as local until upstream adopts them.
- Adding a field to `EmSubDoc` re-approves `tests/Xmlang.Tests/Snapshots/ApprovalTests.EmSpecShape.verified.txt` only if `EmSpec` changes; the header can stay on the linting AST.
- Which surface is normative: `compat.md` section 5 notes that the lenient codegen path accepts what `em lint` rejects, so a document can generate and still fail CI lint. This RFC makes `em lint` (the `EmAst` surface) normative; the lenient parsers are consumers.

## Non-goals

- **Profile composition** (`profile: [a, b]`): one profile per document; a combined rule set is a new profile.
- **Per-slice or per-test opt-in**: the unit of conformance is the document.
- **Semantic content in the header** (decider names, phase lists, tool options): the header says which rules apply, never what the model contains.

## Recorded alternative: `.emlang.yaml` `lint.profile`

emlang already has a marked per-project configuration file. The Go reference reads `.emlang.yaml` with `lint.ignore` and `fmt.keys` (`internal/config/config.go:10-25`: `Config { Lint LintConfig; Diagram DiagramConfig; Fmt FmtConfig }`, `LintConfig { Ignore []string }`; README "Configuration"), resolved as `-c` flag, then `EMLANG_CONFIG`, then `.emlang.yaml` in the current directory. The maintainer's `em` ports the same resolution and reads the same two keys (`Emlang.Cli/Program.cs:63-71`, shipped in `0ecdb11`). A third key, `lint.profile: decider`, would deliver every rule of RFC 0002 with no schema edit, no formatter work, no validator failing on any document, and no upstream acceptance to wait for. That is exactly the cost this RFC's open objection 1 names.

What the document-carried header buys that the config file cannot: the document says what its own `given` means, wherever it is read. A config file is per directory and per CLI; it is not seen by the schema, by a consumer in another repo, or by a tool that is not `em` or `emlang`. xmlang reads `.em.yaml` files through `EmParser`, not through `em`, and RFC 0002 section 5 changes how xmlang resolves `during` on a profile document; a copied fixture, a model published on its own, or a second consumer has no `.emlang.yaml` to consult. Beyond that portability and self-description the header buys nothing: every rule is enforceable from the config, and the config route is the honest name for "my dialect" if the maintainer does not intend the documents to travel.

Decision: maintainer, on reading. If the config route is taken, RFC 0002's rules stand unchanged and its "under the profile" reads "when `lint.profile` is `decider`".

## Rejected alternative: a comment header

A first-line YAML comment `# emlang: version=1.1.0 profile=decider` would fork nothing, since comments are legal anywhere ("Comments" section), and every parser accepts it. It is rejected because it does not survive formatting: both the Go and the local formatter render from the AST and drop every comment, so `emlang fmt -w` and `em fmt -w` delete the header on first save; verified on a probe file, the two outputs are byte-identical and begin at `slices:` (`compat.md`, section 3(b)). Rescuing it needs a raw-text patch outside the AST in both CLIs (`Program.cs:327-335`, `main.go:293-304`): two forks of the same hack against one root key.

## Open objections (recorded, not resolved)

1. A profile whose only enforcement surface is a linter may be a linter configuration, not a spec construct, and emlang already ships that configuration file (see the recorded alternative). The counter-argument, made in RFC 0002, is that the decider profile narrows the meaning of `given`, which is spec text a generator and a reader rely on whether or not a linter runs; the objection stands for any profile that only tunes severities, and it stands against the header whenever the documents do not travel.
2. A `version` field invites documents to pin old versions and tools to carry compatibility tables; the spec so far has one version and no deprecation policy.
3. The Go reference tools reject the header today (verified, `compat.md` section 1), so until upstream ships 1.1.0 every profile document fails upstream `emlang lint`; upstream has one tag, no changelog and no contribution process, so "accepted upstream" has no defined event to wait for.

## Evidence

- Upstream text quoted above: `SPEC.md` sections "Overview", "Document Structure", "Comments"; `schema.json` root (lines 6-13); Go `internal/config/config.go:10-25` and `README.md` "Configuration" (fetched 2026-09-09 from `emlang-project/emlang` main).
- Compatibility runs against schema, Go `emlang` 1.0.0, `em` 0.2.0/0.3.0 and the codegen path: `rfcs/emlang-evidence/compat.md` (sections 1, 2.1, 3, 5; probe files `p1-rootkey`, `p2-comment`).
- Red team: `rfcs/emlang-evidence/redteam.md` R1, R2, R3, R7, R22 (applied), and "The single strongest objection" (recorded above).
- Local parser, formatter and CLI: `src/emlang/Emlang/Linting/EmAst.cs`, `src/emlang/Emlang/Linting/EmFormatter.cs`, `src/emlang/Emlang/EmParser.cs`, `src/emlang/Emlang/Linting/Linter.cs`, `src/emlang/Emlang.Cli/Program.cs`.
- Design pass and plan: `rfcs/emlang-evidence/cut-lines.md` (sections A, C, D), `rfcs/emlang-evidence/PLAN.md` (Decisions, Final cut).
- The debt this pays: xmlang `rfcs/0001-interaction-model.md`, "Open objections" item 3.
