# Next Session Agent Prompt

Use the prompt below to start the next agent from the current state.

```text
Continue from commit 96b3cd9 in /home/cartheur/ame/aiventure/prototype/arthur-bipedal.

Context:
- Platform is Linux (Raspberry Pi), active app is joi-gtk.
- Animation Training is the sole approved process for pose/gait programming.
- Baseline policy is mandatory: same-day `Stable Sitting Position` is required before animation training/replay.
- Animation Training window now performs baseline check/enforcement on open and keeps Start disabled until aligned.
- Start/Stop training flow has been hardened:
  - motor operations run off GTK UI thread,
  - stop/save is synchronized with in-flight capture steps,
  - logs are bounded/throttled to avoid UI freeze.
- Training persistence path is now aligned to Microsoft.Data.Sqlite in active code paths.
- Legacy System.Data.SQLite and win64 stale artifacts were removed.
- Relevant docs: README.md, CHANGELOG.md, TODO.md, docs/epics/animation-training-epic.md, docs/tasks/animation-training-task-001-seated-handshake.md.

First tasks for this session:
1) Carry-over from previous session (unfinished):
   - Execute AT-001 training iteration (seated handshake) on hardware:
   - Use Animation Training only (no direct hardcoded routine path).
   - Capture demonstration and replay with conservative timing.
   - Verify deterministic return to seated baseline on stop/failure.
2) Standing safety calibration backlog:
   - IMU prerequisite before any standing routine:
     - Verify MPU6050 serial feed on `/dev/ttyUSB2` at `115200`.
     - Confirm parser receives lines like `Xs=...; Ys=...; Zs=...; Xc=...; Yc=...; Zc=...;`.
     - Run `dotnet run --project joi-gtk/joi-gtk.csproj -- --imu-probe` and confirm valid samples.
     - In GTK, validate `Read IMU` and `Balance Step` actions are responsive.
   - Run strict body calibration + seated safety validation before standing work.
   - Tune standing overload, thermal, and voltage thresholds using monitor + safety reports.
3) Record outcomes:
   - Update docs/sessions with exact observations, failures, and safety logs.
   - Update CHANGELOG.md/README.md if behavior changed.

Constraints:
- Do not remove existing safety paths.
- Preserve current GTK UI flow and baseline enforcement gate.
- Keep changes incremental and testable.
- Keep torque behavior conservative and fail-safe torque-off on safety violations.
- Commit when done with a clear message.
```
