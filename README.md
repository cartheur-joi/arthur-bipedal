# arthur-bipedal

Bipedal platform for the supertoys line.

## Overview

This repository contains a Windows Forms control application and a Dynamixel motor
control library for a 25-DOF bipedal robot. The current codebase supports:

- Hardware initialization over two serial buses (`COM4` upper body, `COM5` lower body)
- Motor torque on/off control by region and limb
- Per-motor and per-limb position capture
- Dictionary-based pose replay (`motor_name -> goal_position`)
- Pose/training persistence using SQLite and text files

The codebase does **not** yet implement a complete autonomous walking gait routine.
The "walk three steps" UI handlers are currently empty stubs.

## Animation Training Epic

The formal execution anchor for pose/routine programming is now documented in:

- `docs/epics/animation-training-epic.md`
- `docs/process/daily-agent-prompts.md`

This epic defines the operating rule, milestones, acceptance criteria, and immediate
backlog for making Animation Training the primary safe workflow.

Mandatory policy:
- Animation Training is the sole approved process for programming new poses and gaits.
- Agent work must align to `docs/epics/animation-training-epic.md`.

## Motor Safety Threshold Policy (JSON)

`joi-gtk` supports per-motor safety guardrails from a JSON policy file:

- Source file: `joi-gtk/config/motor-overload-thresholds.json`
- Runtime copy: `joi-gtk/bin/Debug/net9.0/config/motor-overload-thresholds.json`
- Applied by: `joi-gtk/Services/RobotControlService.cs`

Policy format:

```json
{
  "defaults": {
    "overloadThreshold": 900,
    "maxTemperature": 70,
    "minVoltage": 90
  },
  "motors": {
    "l_ankle_y": {
      "overloadThreshold": 680,
      "maxTemperature": 68,
      "minVoltage": 92
    },
    "r_ankle_y": {
      "overloadThreshold": 680
    }
  }
}
```

How it works:

- Safety checks and monitor detection resolve per-motor values first.
- Guardrails covered: overload (`>=` threshold), thermal (`>=` max), voltage (`<=` min).
- If a motor is not listed for a guardrail, defaults are used.
- If the JSON is missing/invalid, code falls back to built-in defaults.

Tuning guidance for standing development:

- Start stricter on ankle and knee motors, then raise gradually as needed.
- Keep left/right thresholds symmetric unless hardware asymmetry is confirmed.
- Change only one joint group at a time and retest monitor alerts after each change.

## Startup Body Awareness Calibration

`joi-gtk` now includes a machine-readable body model and a startup calibration algorithm:

- Body model file: `joi-gtk/config/body-model.json`
- Runtime copy: `joi-gtk/bin/Debug/net9.0/config/body-model.json`
- Calibration report output: `logs/body-awareness-last.json`

Algorithm flow:

1. Load body graph (`rootJoint`, parent links, axes, location tags, hard/soft limits).
2. Read live joint telemetry (position/load/temperature/voltage) for each configured joint.
3. Validate `position` against hard and soft bounds.
4. Build a calibration report for all joints (including missing model joints).
5. In strict mode, fail startup on hard-limit or model-missing violations and apply torque-off fail-safe on upper/lower regions.

Usage:

- CLI strict (default):
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --body-calibrate`
- CLI relaxed (report only):
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --body-calibrate --non-strict`
- GTK:
  - `Body Calibrate` button in main action row (strict mode).

How to use in training:

- Run body calibration once after initialize before gait/sweep sessions.
- If strict mode fails, inspect `logs/body-awareness-last.json` and correct limits/mechanics first.
- Keep `body-model.json` updated when parent links or safe operating ranges are tuned.

## Safety Event Log

`joi-gtk` now writes persistent safety events to:

- `logs/safety-events.log`

Each line records:

- UTC timestamp (`ISO-8601`)
- event type (`SAFETY_GATE_TRIP`, `MOTION_EXCEPTION`)
- action and phase
- motor scope
- fail-safe action result
- safety detail payload

## Safety Replay Report (CLI)

Use the report command to summarize frequent safety trips from the persistent log:

- Auto-discovery (runtime then workspace log paths):
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --safety-report`
- Limit top rows:
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --safety-report 10`
- Explicit log file path:
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --safety-report-file logs/safety-events.log`

Report output includes:

- total parsed lines and malformed count
- top events/actions/phases
- most frequent motors involved in trips
- guardrail hit counts (`Comm`, `Overload`, `Thermal`, `Voltage`, `TorqueOff`)
- recent trip summaries (timestamp/action/phase/scope)

## Safe Sweep (GTK)

In `Robot Monitoring`, the **Safe Sweep** panel allows controlled back-and-forth joint tests.

Rules:

- Monitoring must be active before starting a sweep.
- Sweep uses motion safety checks (SafetyGate).
- Stop or failure returns the motor to its original position.

## Seated Handshake Safety Test

For chair-seated validation of arm-side safety gates, `joi-gtk` now includes a conservative right-arm handshake routine.

- CLI:
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --seated-handshake-test`
  - optional shake-count override:
    - `dotnet run --project joi-gtk/joi-gtk.csproj -- --seated-handshake-test 4`

Note:
- Main GTK no longer exposes a direct handshake button under the animation-training-only policy.
- Use Animation Training flows for canonical handshake behavior; keep CLI handshake for diagnostics/migration only.

Behavior:

- Uses `SafetyGate` motion protection for the right-arm motor scope.
- Applies a small shoulder raise and elbow bend/extend shake cycle around current pose.
- Returns the arm to its starting pose in a `finally` block (best effort).
- Any guardrail violation still triggers fail-safe torque-off via existing safety paths.

## Reusable Training Visualization

A reusable labeled motor-body training sheet is available at:

- `images/motors_on_robot_training_map.svg`

It overlays motor labels on `images/motors_on_robot.png` and includes a legend panel for training sessions and calibration notes.

## Robot Voice Announcements (AeonVoice)

`joi-gtk` now supports spoken intent/feedback announcements for user actions.

Packages:

- `AeonVoice`
- `AeonVoice.Native`

Runtime requirements:

- Bundled package data/config are auto-detected from app output.
- Optional overrides:
  - `AEONVOICE_DATA_PATH` and `AEONVOICE_CONFIG_PATH` for custom data/config locations
  - `ARTHUR_AEONVOICE_VOICE` (default: `Leena`)
  - `ARTHUR_SPEECH_ENABLED=0` to disable speech

Behavior:

- Before a major action, robot voice announces intent.
- After action completion/failure, robot voice announces result.
- Voice failures do not block safety/control flow.

CLI check:

- `dotnet run --project joi-gtk/joi-gtk.csproj -- --voice-test`
- custom phrase:
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --voice-test \"I will move to seated calibration pose.\"`

## Speech Recognition (Supertoys.PocketSphinx)

`joi-gtk` now includes repo-scoped speech recognition integration using
`Supertoys.PocketSphinx`.

Scope policy:
- Runtime executable and model paths are resolved from app output only.
- No global system path probing is used for recognition runtime selection.

Runtime requirements:
- Build/runtime on `linux-arm64` where bundled PocketSphinx runtime exists.
- Bundled output paths expected:
  - `runtimes/linux-arm64/native/pocketsphinx`
  - `models/en-us/en-us`
  - `models/en-us/cmudict-en-us.dict`
  - `models/en-us/en-us.lm.bin`

Optional environment variables:
- `ARTHUR_SPEECH_RECOGNITION_ENABLED=0` to disable recognition integration
- `ARTHUR_POCKETSPHINX_RID` to force runtime identifier (default: package runtime helper)

CLI checks:
- runtime status:
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --speech-recog-status`
- recognize an audio file (`.raw`/`.wav`):
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --speech-recog-file /absolute/path/to/audio.wav`
- 5-second command-window test (`START` / `STOP`):
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --speech-command-test`

Animation Training voice command syntax:
- `START` -> begin training session
- `STOP` -> stop and save training session

Tone behavior for training voice windows:
- one tone when 5-second recording window begins,
- same tone when recognition window ends,
- different completion tone when animation training is saved.

## MPU6050 Serial Integration (GTK)

`joi-gtk` now supports MPU telemetry from an external Arduino bridge over USB serial.

Default runtime configuration:

- `ARTHUR_IMU_PORT=/dev/ttyUSB2`
- `ARTHUR_IMU_BAUD=115200`

Integration points:

- `RobotControlService` wires the serial IMU provider into `WalkController` safety checks.
- Main window includes:
  - `Read IMU` button: shows current parsed pitch/roll/yaw and provider status.
  - `Balance Step` button: applies a conservative compensation step using ankles/hips.

Expected serial line format example:

`Xs=...; Ys=...; Zs=...; Xc=...; Yc=...; Zc=...;`

Current mapping:

- `pitch <- Xc` (fallback `Xs`)
- `roll <- Yc` (fallback `Ys`)
- `yaw <- Zc` (fallback `Zs`)

## Stable Sitting Position Capture (CLI)

Capture a full current-pose snapshot and tag it as `Stable Sitting Position`:

- `dotnet run --project joi-gtk/joi-gtk.csproj -- --capture-stable-sitting`

Storage format (`db/positions.db`) uses normalized tables:

- `pose_snapshot(id, pose_name, captured_at_utc, motor_count, note)`
- `pose_snapshot_value(snapshot_id, motor_name, position_value)`

This represents one pose as one snapshot row plus many per-motor rows.

Daily baseline rule (enforced):

- The robot must have a same-day `Stable Sitting Position` snapshot before training routines run.
- If baseline is missing or from a previous day, training actions fail with an operator-visible message.
- Routines currently gated:
  - `--seated-handshake-test`
  - supervised walk actions
  - any animation training begin/replay path (service-enforced)

Recommended start-of-day flow:

1. Place robot in normal seated baseline posture.
2. Run `dotnet run --project joi-gtk/joi-gtk.csproj -- --capture-stable-sitting`.
3. Start training routines.

Training replay CLI (policy-aligned):
- Replay latest captured sequence for a phrase:
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --replay-trained-phrase seated_handshake`
- Replay enforces seated baseline before motion and attempts seated recovery after replay.

## Solution Structure

- `joi-gtk/`: Linux GTK application and CLI entrypoint.
- `cartheur-animals-robot/`: motor control, safety, motion primitives, persistence helpers.
- `docs/`: process/epic/task/session documentation.
- `images/`: reference visuals for robot mapping/training.

## Runtime Flow (Current)

1. `joi-gtk/Program.cs` starts GTK app or handles CLI commands.
2. `MainWindow` provides system control, monitoring, and access to `AnimationTrainingWindow`.
3. `RobotControlService` owns bus I/O, safety gates, baseline enforcement, and fail-safe torque-off behavior.
4. `AnimationTrainingService` records/replays per-frame motor dictionaries and persists sessions.

## Animation Training Behavior (Current)

`AnimationTrainingWindow` behavior is now explicitly safety-first and non-blocking:

- On window open:
  - robot initializes (if needed) in background;
  - same-day `Stable Sitting Position` baseline is verified;
  - seated alignment enforcement runs automatically;
  - `Start Training` remains disabled until baseline reconciliation succeeds.
- Start path:
  - runs off the GTK UI thread;
  - enforces seated baseline, selects arm scope, disables torque on training arm motors, captures first frame;
  - starts timer-based capture loop.
- Capture loop:
  - runs frame capture in background to avoid UI freeze;
  - avoids overlapping capture/save operations;
  - throttles UI/log churn (bounded log buffer + periodic frame detail logging).
- Stop path:
  - always queues/executes deterministically;
  - if a capture step is in-flight, stop waits for that step then saves;
  - after save, training state resets and completion tone plays.
- Replay path:
  - enforced baseline before replay;
  - deterministic seated recovery attempt after replay (including failure paths).
- Voice path:
  - optional only (`Voice ON (Optional)`/`Voice OFF`);
  - training does not auto-start from window open.

## Persistence

- Stable seated baseline: `db/positions.db` (`pose_snapshot`, `pose_snapshot_value`).
- Training sessions: `db/trainings.db` (`TrainingSequence` rows by session/sequence/motor/position).
- SQLite provider is `Microsoft.Data.Sqlite` across active training/storage paths.

## Build

- `dotnet build joi-gtk/joi-gtk.csproj`
- `dotnet build Cartheur.Animation.Joi.sln`
