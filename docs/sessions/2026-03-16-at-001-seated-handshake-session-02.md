# Session Log - AT-001 Seated Handshake - Session 02

## Session Metadata
- Session ID: `AT-001-S02`
- Date: `2026-03-16`
- Task: `AT-001 Seated Handshake`
- Operator: `cartheur`
- Agent: `Codex`
- Mode: `Training-driven replay validation`

## Goal for This Session
Execute the first real training-driven handshake replay cycle (`seated_handshake`)
with baseline enforcement and deterministic seated recovery validation.

## Preconditions Checklist
- [ ] Robot seated and mechanically stable
- [ ] Same-day baseline captured (`--capture-stable-sitting`)
- [ ] Monitoring active
- [ ] Clear right-arm workspace confirmed

## Commands For This Session
1. Baseline capture:
   - `dotnet run --project joi-gtk/joi-gtk.csproj -- --capture-stable-sitting`
2. GTK capture flow:
   - Open `Animation Training`
   - Phrase: `seated_handshake`
   - Arm: `Right Arm`
   - `Begin Animation Training` -> demonstrate -> `Stop && Save`
3. Replay (CLI deterministic path):
   - `dotnet run --project joi-gtk/joi-gtk.csproj -- --replay-trained-phrase seated_handshake`

## New Policy/Code Behaviors Validated In This Session
1. Replay path enforces seated baseline before replay.
2. Replay path always attempts seated baseline recovery after replay.
3. Recovery attempt is enforced both after success and after replay failure.

## Results
- Capture frames: `[fill after run]`
- Replay result: `[fill after run]`
- Recovery result: `[fill after run]`
- Safety events: `[fill after run]`

## Acceptance Criteria Check
- [ ] Demonstration-created sequence exists for `seated_handshake`
- [ ] Replay starts from enforced same-day baseline
- [ ] Replay completes under SafetyGate without unexpected trips
- [ ] On failure test, seated recovery attempt is visible in logs/output
- [ ] Logs clearly show baseline/replay/recovery outcomes

## Next Action
If all checks pass, promote `seated_handshake:v1.0` to canonical routine and retire
hardcoded diagnostic handshake from standard operator flow.
