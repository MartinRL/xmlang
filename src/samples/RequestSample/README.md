# RequestSample: Time-Off Request Workflow

A minimal example of **emlang + xmlang in practice**, proving that:

1. **Source generators work deterministically** — `SubmitRequest`, `ApproveRequest`, and other Command/Event records are generated from the em spec and produce identical output every build.
2. **xmlang captures interaction judgment** — `confirm: true` on high-consequence actions (approve/reject) and `then:` specifies where the user goes after, all as data in the spec.
3. **The interpreter runs from the spec** — one Razor interpreter renders all surfaces from `time-off.xm.yaml`, not hand-written per-screen code.

## The specs

- **`time-off.em.yaml`** — Event model: 4 slices (create → assign → decide → state), commands, events, errors, Given-When-Then test cases. Everything the backend needs to know about the workflow.
- **`time-off.xm.yaml`** — Experience model: surfaces for requester (form, history) and manager (pending list, detail with approve/reject). Shows `confirm:` and `then:` judgment on high-stakes commands.

## What's proven here

### 1. Generator determinism (approval tests)

The Command/Event records in `Domain/Decider.cs` are **generated** from the em spec. Run the generator twice, get byte-identical output. The approval tests lock this down: if the spec changes or the generator drifts, the test fails.

```
✓ Request / Created vocabulary matches spec
✓ Decider test cases generate as given-when-then facts
```

### 2. Interaction judgment in the spec

The `approve-request` and `reject-request` commands carry:
- `confirm: true` — marks them as consequential; the UI shows a confirmation prompt
- `then: PendingRequests` — after approval/rejection, send the user back to the pending list, not a success page

These are *experience judgments*: they belong in the spec, co-located with the structural judgments (composition, salience, naming), not buried in the interpreter.

### 3. One interpreter, all surfaces

The Razor interpreter (150 lines) reads the xm spec and renders:
- Requester form (SubmitRequest command)
- Manager's pending-request list (State / Request view)
- Manager's review screen (with approve/reject buttons + confirm prompts)
- History for both roles

No per-screen Razor file. New surface? Add it to the xm spec. New field kind? Plan-level decision.

## Running it

```bash
cd src/samples/RequestSample
dotnet run
```

Then:
```bash
# Create a request
curl -X POST http://localhost:5000/requests \
  -H "Content-Type: application/json" \
  -d '{"employeeId":"550e8400-e29b-41d4-a716-446655440000","startDate":"2026-10-01","endDate":"2026-10-05","reason":"vacation"}'

# Retrieve it
curl http://localhost:5000/requests/{id}

# Approve it
curl -X POST http://localhost:5000/requests/{id}/approve \
  -H "Content-Type: application/json" \
  -d '{"managerId":"550e8400-e29b-41d4-a716-446655440001"}'
```

## Articles

- [The Spec Is the Product](../../articles/the-spec-is-the-product.md) — Why deterministic generators on the backend matter
- [The Screens Left the Repo](../../articles/the-screens-left-the-repo.md) — Why interpretation over generation for UI

This sample proves both. The domain vocabulary is generated (deterministic); the screens are interpreted (one living copy).

---

**Design decisions (ponytail mode):**

- **In-memory state only** — this proves the logic, not persistence. Add a real event store when the domain stabilizes.
- **No background jobs** — assignment is manual (via AssignReviewer command). Automation slices would add them.
- **Minimal Razor** — the interpreter handles composition, tiers, labels, and tokens. Hand-written Razor is reserved for genuine exceptions (a custom form layout, for example).
- **No tests yet** — the approval tests on the generators live here, but domain tests (Decider unit tests) are pending. The Given-When-Then cases in the spec *become* those tests once the generator matures.
