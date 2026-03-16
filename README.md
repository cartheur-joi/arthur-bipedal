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

- GTK button:
  - `Handshake (Seated)` on the main action row.
- CLI:
  - `dotnet run --project joi-gtk/joi-gtk.csproj -- --seated-handshake-test`
  - optional shake-count override:
    - `dotnet run --project joi-gtk/joi-gtk.csproj -- --seated-handshake-test 4`

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

## Solution Structure

- `joi-animations/`: WinForms UI application (`Cartheur.Animation.Joi`)
- `cartheur-animals-robot/`: Motor control and persistence library (`Cartheur.Animals.Robot`)
- `images/`: Project images

## Runtime Flow

1. App entrypoint (`Program.Main`) starts `ApplicationManager`.
2. `ApplicationManager` creates `MotorFunctions`.
3. `MotorFunctions` initializes Dynamixel port handlers, opens ports, and packet handler.
4. Motor dictionaries are collated (name, ID, location, limb groups).
5. UI subforms (`TemplaterForm`, `ControlKeypad`, `AnimationTraining`, `RobotControl`) drive motor actions.

## Core Motor Logic (`cartheur-animals-robot/`)

### `MotorFunctions`

Central low-level hardware API wrapper:

- Port setup and shutdown
  - `SetActivePorts()`
  - `InitializeDynamixelMotors()`
  - `DisposeDynamixelMotors()`
- Motor map setup
  - `CollateMotorArray()`: builds name/id/reverse/location dictionaries
- Telemetry
  - `GetPresentPosition`, `GetPresentLoad`, `GetPresentTemperature`, `GetPresentVoltage`
- Torque control
  - `SetTorqueOn(string region | string[] motors)`
  - `SetTorqueOff(string region | string[] motors)`
- Motion execution
  - `MoveMotorSequence(Dictionary<string,int>)`
  - Writes moving speed, then goal position for each motor

### `Motor`

Static helpers that resolve:

- motor name -> id
- id -> motor name
- motor name -> `"upper"`/`"lower"` section

### `Limbic`

Defines limb group arrays used across the UI:

- `LeftLeg`, `RightLeg`
- `LeftArm`, `RightArm`
- `Abdomen`, `Bust`, `Head`
- `All` (full 25-motor set)

### `MotorSequence`

High-level pose builder/replayer:

- `ReturnDictionaryOfPositions(string[] motorArray)` captures current positions
- `ReplayLimbicPosition(...)` replays a stored limb pose via `MoveMotorSequence`

### `Remember`

SQLite persistence helper:

- Stores and retrieves stable positions and training sequences
- Parses animation records into command dictionaries
- Databases used:
  - `db/memory.db`
  - `db/positions.db`
  - `db/trainings.db`

### `Extensions`

File-based sequence utilities:

- `StoreMotorSequenceAsFile` writes `motor--value` lines
- `BuildMotorSequence` reconstructs a dictionary from file

## UI Logic (`joi-animations/Subforms/`)

### `TemplaterForm`

Main pose authoring/replay tool.

Typical workflow for limb motion:

1. Capture current limb positions.
2. Manually move robot and capture desired limb positions.
3. Replay sequence:
   - current -> desired -> current
4. Release torque for selected groups.

Contains buttons for "Walk three steps forward/backward", but handlers are not implemented yet.

### `ControlKeypad`

Manual live control panel:

- Toggles torque per limb group via buttons or keyboard shortcuts
- Can capture full-body standing pose to DB
- Can recall last stored standing pose and replay it
- Includes partial/experimental gyroscope serial parsing logic

### `AnimationTraining`

Training recorder/replayer:

- Captures motor states over timer steps
- Stores step snapshots to files and optionally database
- Replays by loading dictionaries and sending via `MoveMotorSequence`

### `RobotControl`

Direct dictionary editor:

- User can type or load `motor--goal` rows
- Builds a sequence dictionary for execution elsewhere

## Motion Model In Practice

The movement abstraction in this repo is dictionary-based:

- A "pose" is `Dictionary<string,int>` of motor targets.
- A "motion" is one or more pose dictionaries executed in order with delays.

Current higher-level motions are hand-authored/replayed pose transitions, not closed-loop gait control.

## Current Gaps / Known Limitations

- `ThreeStepsForwardButtonClick` and `ThreeStepsBackwardButtonClick` are empty.
- No trajectory interpolation; writes are per target pose.
- Most motion timing uses blocking `Thread.Sleep`.
- Minimal safety constraints/validation around goal ranges in replay paths.
- Some training and serial-monitoring features are partial/experimental.

## Implementing a 3-Step Gait

This section describes a practical way to implement the two empty handlers in
`TemplaterForm` using existing primitives only.

### Goal

Implement:

- `ThreeStepsForwardButtonClick`
- `ThreeStepsBackwardButtonClick`

using `MotorControl.MoveMotorSequence(...)`, `MotorSequence.ReturnDictionaryOfPositions(...)`,
and `Limbic` groups.

### Recommended Gait State Machine

Use 6 phases for one full stride cycle:

1. `ShiftWeightToLeft`
2. `SwingRightLegForward` (or backward)
3. `SetRightLegDown`
4. `ShiftWeightToRight`
5. `SwingLeftLegForward` (or backward)
6. `SetLeftLegDown`

Repeat this cycle 3 times for "three steps".

### Pose Representation

Create one dictionary per phase:

- `Dictionary<string,int> poseShiftLeft`
- `Dictionary<string,int> poseSwingRightForward`
- `Dictionary<string,int> poseSetRightDown`
- `Dictionary<string,int> poseShiftRight`
- `Dictionary<string,int> poseSwingLeftForward`
- `Dictionary<string,int> poseSetLeftDown`

Backward walking reuses the same structure with opposite hip/knee targets.

### How To Author Initial Pose Values

1. Set robot to stable stand.
2. Use existing capture flow in `TemplaterForm` to read baseline positions.
3. Manually place robot in each gait phase and capture limb dictionaries.
4. Merge limb dictionaries into full-body phase dictionaries.
5. Save these as constants/files for replay.

Start with conservative deltas:

- Hip pitch (`*_hip_y`): small forward/back shift
- Knee (`*_knee_y`): enough lift for toe clearance
- Ankle (`*_ankle_y`): compensate to keep foot orientation
- Pelvis roll/shift (`*_hip_x`, `abs_x`): center-of-mass shift before swing

### Suggested Handler Structure

```csharp
private void ThreeStepsForwardButtonClick(object sender, EventArgs e)
{
    ExecuteThreeStepGait(forward: true);
}

private void ThreeStepsBackwardButtonClick(object sender, EventArgs e)
{
    ExecuteThreeStepGait(forward: false);
}

private void ExecuteThreeStepGait(bool forward)
{
    // 1) Safety and readiness
    MotorControl.SetTorqueOn("lower");
    MotorControl.SetTorqueOn(Limbic.Abdomen);
    MotorControl.SetTorqueOn(Limbic.Bust);

    // 2) Capture or load neutral pose
    var neutral = MotorSequences.ReturnDictionaryOfPositions(Limbic.All);

    // 3) Build cycle poses (load from file/db or prebuilt dictionaries)
    var cycle = forward ? BuildForwardCycle(neutral) : BuildBackwardCycle(neutral);

    // 4) Execute 3 stride cycles
    for (int i = 0; i < 3; i++)
    {
        foreach (var pose in cycle)
        {
            MotorControl.MoveMotorSequence(pose);
            Thread.Sleep(600); // start conservative, tune on hardware
        }
    }

    // 5) Return to neutral
    MotorControl.MoveMotorSequence(neutral);
}
```

### Integration Notes For Current Code

- Keep gait execution in `TemplaterForm` first, because UI handlers already exist there.
- If dictionaries become large, move gait construction into `MotorSequence` (for reuse).
- Prefer non-blocking timers over `Thread.Sleep` once basic gait works.

### Safety Checklist Before Running

- Confirm robot is supported or harnessed during first tests.
- Start with low speed and low angle deltas.
- Validate all target positions against known safe limits before dispatch.
- Watch motor load/temperature via existing telemetry methods.
- Add an emergency stop button that calls `SetTorqueOff("lower")`.

## Build

Open `Cartheur.Animation.Joi.sln` in Visual Studio and run the WinForms project.
Ensure required Dynamixel and serial hardware is connected on expected COM ports.
