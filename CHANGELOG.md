# Changelog

All notable changes to this project are documented in this file.

## [Unreleased] - 2026-03-14

### Added (Safety)
- Added a centralized motor `SafetyGate` in `joi-gtk/Services/RobotControlService.cs` for motion commands:
  - Runs motor-state checks before and after motion execution.
  - Validates communication status, overload status, and torque-on state.
  - Applies fail-safe torque-off when a safety violation is detected during a protected motion action.
- Added `SafetyOverloadThreshold` configuration property on `RobotControlService` (default: `MotorFunctions.PresentLoadAlarm`).

### Changed (Motion Guarding)
- `MoveToPositions(...)` now executes through the new motion safety gate.
- `ExecuteWalkCycleSupervised(...)` now executes through the new motion safety gate over lower-body motor scope.

### Added (Operator Safety UX)
- Main monitor now reacts to `SafetyGate` trips with:
  - a red alert banner showing the safety-trip detail,
  - audible alarm signaling,
  - `Acknowledge Alert` action to clear the visual alert state.

### Added (Per-Motor Threshold Policy)
- Added motor-specific overload threshold policy loading in `RobotControlService`.
- Added `joi-gtk/config/motor-overload-thresholds.json` with standing-focused defaults for ankles, knees, and hips.
- Monitoring and SafetyGate overload checks now resolve threshold by motor first, then fall back to the global/default threshold.

### Added (Persistent Safety Events)
- Added durable safety event logging to `logs/safety-events.log` from `RobotControlService`.
- Logged entries now include timestamp, event type, action, phase, motor scope, fail-safe action, and detail payload.

### Added (Safe Sweep Tool)
- Added GTK "Safe Sweep" panel in robot monitoring view:
  - motor picker,
  - low/high target fields,
  - step duration field,
  - start/stop sweep controls.
- Safe Sweep requires active monitoring and automatically returns the joint to the original position when stopping or on failure.

### Changed (Integration)
- Updated solution/project wiring to reference `cartheur-animals-robot` instead of the removed `dynamixel` project:
  - `Cartheur.Animation.Joi.sln` now points to `cartheur-animals-robot/Cartheur.Animals.Robot.csproj`.
  - `joi-animations/Cartheur.Animation.Joi.csproj` now points to `../cartheur-animals-robot/Cartheur.Animals.Robot.csproj`.
  - `joi-animations/Cartheur.Animation.Joi.csproj` SQLite hint path now points to `../cartheur-animals-robot/lib/System.Data.SQLite.dll`.

### Verified
- `cartheur-animals-robot/Cartheur.Animals.Robot.csproj` builds successfully on .NET 9.

### Known Limitation
- Full solution build in non-Windows environments still requires Windows-targeting configuration (`NETSDK1100`).
- UI walk handlers (`ThreeStepsForwardButtonClick` / `ThreeStepsBackwardButtonClick`) are still not wired to `WalkController`.

### Fixed
- `MotorFunctions.InitializeDynamixelMotors` now checks return values from both `openPort` calls and reports failure if either port does not open.
- `MotorFunctions.IsTorqueOn(string[] motors)` now returns actual torque state instead of always returning `false`.
- `MotorSequence` now initializes `TrainingMotorSequence` and clears sequence dictionaries before reuse to prevent null references and stale entries.
- `Extensions.BuildMotorSequence` now uses method-local storage and de-duplicates motor keys to avoid cross-call state leakage and key collisions.
- `Remember` constructors now initialize instance/static dictionaries more safely to avoid re-initialization side effects.
- `Remember` SQL operations now use parameterized commands for values instead of string concatenation.
- `Remember.QueryLimbicValue` now uses a parameterized query with null handling and returns `0` when no value exists.
- `Remember.RetrieveAnimation` now checks for empty result sets before parsing.
- `Remember.ParseAnimation` now guards against duplicate keys and updates existing command entries safely.
- `Remember.ClearTable` and `Remember.RetrieveData` now validate table names against an allowlist.

### Added (Phase 2)
- Added timed trajectory primitives in `MotionTrajectory.cs`:
  - `MotionTrajectoryStep` for timed pose targets.
  - `MotionTrajectoryPlayer` for executing trajectory steps.
  - `BipedGaitFactory` for generating a simple two-phase biped walking cycle from a neutral pose.
- Added `MotorFunctions.MoveMotorSequenceSmooth(...)` to interpolate target positions over time for smoother movement transitions.
- Added `WalkController` for high-level walking-cycle execution with basic safety checks (torque, load, temperature, voltage).
- Added sensor abstraction layer in `BalanceSensors.cs`:
  - IMU and foot-contact interfaces (`IImuProvider`, `IFootContactProvider`).
  - Default null providers for environments without hardware sensors.
- Added `Mpu6050ImuProvider` and `IMpu6050Source` for chest-mounted MPU6050 orientation input (X/Y tilt), with axis/sign calibration and low-pass smoothing.
- Extended `WalkController` with supervised execution (`ExecuteWalkCycleSupervised`) using timeout, IMU tilt checks, and stance foot-contact validation.
- Added neutral-pose calibration helpers in `WalkController` (`CaptureNeutralPose`, `BuildSoftLimits`, `ClampPose`) and integrated soft-limit clamping into walk-step execution.
