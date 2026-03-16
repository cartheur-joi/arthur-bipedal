# Animation Training Epic

## Purpose
Make Animation Training the sole, safe, repeatable way to program robot poses and routines.

## Vision
Operators demonstrate motion. The robot records structured frames, validates safety, and replays reliable routines with seated-baseline recovery on failures.

## Scope
- In scope:
  - Capture, save, version, replay, and enforce trained motion sequences.
  - Daily seated baseline capture/enforcement before training routines.
  - Safety-gated execution and deterministic fallback to seated baseline.
  - Operator-visible status, logs, and clear failure reasons.
- Out of scope (for this epic):
  - Autonomous policy learning.
  - Full-body dynamic gait generation from scratch.

## Operating Rule
Every training day starts from `Stable Sitting Position` baseline capture.  
No training routine runs unless baseline is valid for the current day.

## Development Policy (Mandatory)
- Animation Training is the only approved path for teaching poses and gait routines.
- Any new feature or task must map to an Animation Training milestone or acceptance criterion.
- Ad-hoc hardcoded motion routines are temporary diagnostics only and must be replaced by training-driven equivalents.
- Agent work that bypasses training capture/replay policy is out of scope unless explicitly approved.

## Milestones
1. Baseline & Safety Foundation
- Daily baseline gate enforced.
- Multi-pass seated enforcement with strict fail if convergence is not achieved.
- Fail-safe seated return path on training failures.

2. Training Data Contract
- Formal frame schema:
  - `pose_snapshot` / `pose_snapshot_value` for baseline.
  - Stable sequence contract for training frames in `trainings.db`.
- Versioning conventions for phrases/sessions.
- Minimal export format for review and rollback.

3. Authoring UX
- Clean GTK training flow for:
  - begin capture,
  - pause/resume,
  - stop/save.
- Remove ambiguous controls that bypass policy.

4. Replay Reliability
- Trained-sequence replay under SafetyGate.
- Deterministic start pose enforcement before replay.
- Deterministic seated return after replay/failure.

5. Validation & Ops
- Session checklist for operators.
- Log audit hooks for baseline validity, replay results, and safety trips.
- Repeatable acceptance test procedure on seated robot.

## Immediate Backlog (Now)
1. Replace hardcoded handshake with animation-trained replay sequence.
2. Add guaranteed seated return in `finally` for trained replay failures.
3. Add report line items for baseline date, corrected motors, and remaining drift.
4. Define naming convention for canonical training phrases (e.g., `seated handshake`).
5. Add operator runbook for start-of-day and end-of-session.

## Acceptance Criteria
- Operator can create/update a routine by demonstration only.
- Routine replay starts from enforced seated baseline.
- On any exception/safety trip, robot attempts seated return before exit.
- Logs clearly show baseline check, enforcement passes, and final routine result.
- Daily workflow is documented and reproducible.

## Definition of Done
- Code, docs, and CLI/GTK paths align with this policy.
- No hidden bypass path for training replay without baseline enforcement.
- At least one canonical routine (`seated handshake`) is fully training-driven.
