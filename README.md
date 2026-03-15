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

## Motor Safety Threshold Policy (JSON)

`joi-gtk` now supports per-motor overload thresholds from a JSON policy file:

- Source file: `joi-gtk/config/motor-overload-thresholds.json`
- Runtime copy: `joi-gtk/bin/Debug/net9.0/config/motor-overload-thresholds.json`
- Applied by: `joi-gtk/Services/RobotControlService.cs`

Policy format:

```json
{
  "defaultThreshold": 900,
  "motors": {
    "l_ankle_y": 680,
    "r_ankle_y": 680
  }
}
```

How it works:

- Safety checks and monitor overload detection resolve threshold per motor first.
- If a motor is not listed, `defaultThreshold` is used.
- If the JSON is missing/invalid, code falls back to built-in defaults.

Tuning guidance for standing development:

- Start stricter on ankle and knee motors, then raise gradually as needed.
- Keep left/right thresholds symmetric unless hardware asymmetry is confirmed.
- Change only one joint group at a time and retest monitor alerts after each change.

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

## Safe Sweep (GTK)

In `Robot Monitoring`, the **Safe Sweep** panel allows controlled back-and-forth joint tests.

Rules:

- Monitoring must be active before starting a sweep.
- Sweep uses motion safety checks (SafetyGate).
- Stop or failure returns the motor to its original position.

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

## Solution Structure

- `joi-animations/`: WinForms UI application (`Cartheur.Animation.Joi`)
- `dynamixel/`: Motor control and persistence library (`Cartheur.Animals.Robot`)
- `images/`: Project images

## Runtime Flow

1. App entrypoint (`Program.Main`) starts `ApplicationManager`.
2. `ApplicationManager` creates `MotorFunctions`.
3. `MotorFunctions` initializes Dynamixel port handlers, opens ports, and packet handler.
4. Motor dictionaries are collated (name, ID, location, limb groups).
5. UI subforms (`TemplaterForm`, `ControlKeypad`, `AnimationTraining`, `RobotControl`) drive motor actions.

## Core Motor Logic (`dynamixel/`)

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
