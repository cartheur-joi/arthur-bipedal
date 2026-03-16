# AT-001 - Seated Handshake

## Task Metadata
- Task ID: `AT-001`
- Epic: `Animation Training Epic`
- Name: `Seated Handshake`
- Status: `Active`
- Canonical Phrase Name: `seated_handshake`
- Current Revision: `v1.0-candidate`

## Intent
Define and validate a safe, training-driven seated handshake routine that can be
captured by demonstration and replayed deterministically.

## Safety Scope
- Allowed scope: right-arm handshake chain only (shoulder/elbow/wrist as configured).
- Baseline requirement: same-day `Stable Sitting Position` must exist and be valid.
- Safety controls: all replay actions must execute under `SafetyGate`.
- Violation behavior: fail-safe protection remains active, including torque-off on violations.
- Recovery behavior: deterministic return to seated baseline on stop/failure.

## Preconditions
1. Robot is seated in stable chair posture.
2. Same-day stable baseline exists (`--capture-stable-sitting` if needed).
3. Monitoring is active and safety logging is enabled.
4. Operator confirms clear workspace around arm and hand.

## Training Method
1. Enforce seated baseline before capture/replay.
2. Demonstrate handshake motion in small safe increments.
3. Capture frame sequence as a named phrase/session.
4. Save sequence with revision metadata and operator notes.

## Replay Contract
- Start: enforce seated baseline before replay.
- During: replay only captured sequence under `SafetyGate`.
- End (success): return to seated baseline.
- End (failure/exception): attempt seated baseline return before exit.

## Acceptance Criteria
1. Routine is created by demonstration/capture (no hardcoded motion path).
2. Replay starts from enforced same-day seated baseline.
3. Replay completes without guardrail trips in normal conditions.
4. On any failure, routine exits with deterministic seated recovery attempt.
5. Logs show baseline check, enforcement pass count, replay outcome, and any safety events.

## Failure Criteria
- Missing/stale baseline at start.
- Guardrail trip or communication fault during replay.
- Replay cannot reach required seated recovery criteria.
- Motor commands executed outside allowed handshake scope.

## Telemetry Required
- Baseline validity result (date and status).
- Enforcement correction summary (motors corrected, passes, remaining drift).
- Replay attempt result (success/failure).
- Safety events from `logs/safety-events.log`.

## Versioning and Naming
- Phrase naming: `seated_handshake`
- Revision format: `seated_handshake:v<major>.<minor>` (example: `seated_handshake:v1.0`)
- Session tags: `AT-001-S<nn>` (example: `AT-001-S01`)

## Open Items
1. Finalize exact motor subset for handshake scope in code-level policy.
2. Finalize drift tolerance and pass limit for seated recovery after replay.
3. Define operator hand placement and proximity guard guidance.
