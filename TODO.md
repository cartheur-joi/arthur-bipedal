# TODO

## Next Steps

Formal planning anchor:
- `docs/epics/animation-training-epic.md`
- `docs/process/daily-agent-prompts.md`
- `docs/tasks/README.md`

Mandatory policy for all agent tasks:
- Animation Training is the sole approved method for pose/gait programming.
- Every TODO item must map to epic milestones, acceptance criteria, or immediate backlog.
- Any temporary hardcoded motion test must include a follow-up item to replace it with training-driven replay.

1. Animation Training Epic Backlog (Immediate)
- Execute items in `docs/epics/animation-training-epic.md` under `Immediate Backlog (Now)`.
- Keep this list aligned with epic milestones and acceptance criteria.
- First canonical task package:
  - Task spec: `docs/tasks/animation-training-task-001-seated-handshake.md`
  - Latest session log: `docs/sessions/2026-03-16-at-001-seated-handshake-session-02.md`

2. Standing Threshold Calibration Session
- Run strict startup body calibration first (`Body Calibrate` or `--body-calibrate`).
- Run seated handshake safety test first (`--seated-handshake-test`) to confirm arm-scope safety gating before standing work.
- Start monitor, run safe sweeps for ankles/knees/hips, and tune `motor-overload-thresholds.json`.
- Goal: no false trips at stable stance, immediate trips near hard-stop/load risk.

3. Tune Thermal and Voltage Guardrails
- Thermal/voltage guardrails are now enforced in policy + SafetyGate.
- Run standing sessions and tune per-motor `maxTemperature` and `minVoltage` values to reduce false positives.

4. Use Safety Replay Report for Daily Tuning
- Run `--safety-report` after standing sessions and track top actions/motors over time.
- Use repeated guardrail hits to prioritize mechanical checks and threshold adjustments.

5. Add Sweep Presets for Standing Development
- Add preset controls for ankles, knees, and hips with conservative ranges/durations.
- Reduce manual entry errors and speed up daily validation runs.
