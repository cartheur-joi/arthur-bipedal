# Next Session Agent Prompt

Use the prompt below to start the next agent from the current state.

```text
Continue from commit 2f19636 in /home/cartheur/ame/aiventure/prototype/arthur-bipedal.

Context:
- Platform is Linux (Raspberry Pi), active app is joi-gtk.
- Robot monitoring with motor map, overload indicators, safety alerts, and audible alarm is implemented.
- SafetyGate exists in RobotControlService and protects motion commands.
- Per-motor overload policy JSON exists at joi-gtk/config/motor-overload-thresholds.json.
- Persistent safety log exists at logs/safety-events.log.
- Safe Sweep panel exists in MainWindow and requires active monitoring; it returns joint to origin after stop/failure.
- MPU6050 serial integration is implemented via joi-gtk/Services/SerialMpuImuProvider.cs.
- IMU serial stream is on /dev/ttyUSB2 at 115200 and parses lines like:
  Xs=...; Ys=...; Zs=...; Xc=...; Yc=...; Zc=...;
- MainWindow has “Read IMU” and “Balance Step” actions.
- CLI probe exists: dotnet run --project joi-gtk/joi-gtk.csproj -- --imu-probe
- Relevant docs: README.md, CHANGELOG.md, TODO.md.

First tasks for this session:
1) Verify build and runtime quickly:
   - dotnet build joi-gtk/joi-gtk.csproj
   - run --imu-probe and confirm valid samples
2) Implement next TODO item:
   - Add thermal and voltage guardrails to the same policy/config pattern (per-motor + defaults), and enforce in SafetyGate checks.
3) Keep behavior conservative for standing safety:
   - Fail-safe torque-off on violations.
   - Clear operator-visible messaging in monitor logs/status.

Constraints:
- Do not remove existing safety paths.
- Preserve current GTK UI flow.
- Keep changes incremental and testable.
- Commit when done with a clear message and update README/CHANGELOG.
```
