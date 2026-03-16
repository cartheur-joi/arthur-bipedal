# Changelog

All notable changes to this project are documented in this file.

## [Unreleased] - 2026-03-14

### Changed (Animation Training Reliability + UX)
- `AnimationTrainingWindow` training start/stop/capture flows now execute motor I/O on background tasks to avoid GTK UI-thread blocking.
- Added stop/save synchronization and operation guards so `Stop Training` is honored even when a capture step is in-flight.
- Added stop-queue behavior and clearer status/log messages for pending stop/save transitions.
- Reduced training-window runtime churn:
  - bounded console buffer,
  - reduced per-frame logging density,
  - safety polling skips overlapping cycles during active capture operations.

### Changed (SQLite Provider Alignment)
- Migrated `cartheur-animals-robot/Remember.cs` to `Microsoft.Data.Sqlite` APIs.
- Updated `Cartheur.Animals.Robot.csproj` to remove direct `System.Data.SQLite` reference and use package-based `Microsoft.Data.Sqlite`.
- Removed stale solution dependency on deleted `joi-animations/Cartheur.Animation.Joi.csproj`.

### Removed (Stale Legacy Files)
- Removed legacy Windows shortcut and unused SQLite binary artifacts from the repo:
  - `cartheur-animals-robot/db/SQLiteBrowser.lnk`
  - `cartheur-animals-robot/db/c-programs-sqlite`
  - `cartheur-animals-robot/lib/System.Data.SQLite.dll`
  - `cartheur-animals-robot/lib/win64/*`

### Added (Animation Training Epic Kickoff)
- Added formal epic document: `docs/epics/animation-training-epic.md`.
- Defined Animation Training purpose, scope, operating rule, milestones, and acceptance criteria.
- Added immediate backlog focused on replacing hardcoded handshake with training-driven replay and deterministic seated recovery.
- Linked epic as planning anchor from `README.md` and `TODO.md`.
- Added reusable strict daily agent prompt at `docs/process/daily-agent-prompts.md` and linked it from `README.md` and `TODO.md`.
- Documented mandatory development policy that Animation Training is the sole approved process for new pose/gait programming and that agent tasks must map to epic policy.
- Added Animation Training task/session documentation scaffold:
  - `docs/tasks/README.md` (task index),
  - `docs/tasks/animation-training-task-001-seated-handshake.md`,
  - `docs/sessions/2026-03-16-at-001-seated-handshake-session-01.md`.

### Changed (Animation Training Replay Reliability)
- `AnimationTrainingService.ReplayLatest(...)` now attempts seated baseline recovery after replay execution, including failure paths.
- Added CLI command: `--replay-trained-phrase <phrase>` for deterministic training-driven replay runs.
- Animation training UI now defaults to canonical AT-001 phrase `seated_handshake` with `Right Arm` selected and exposes an explicit `Replay Phrase` action.
- Added Session 02 execution doc: `docs/sessions/2026-03-16-at-001-seated-handshake-session-02.md`.

### Changed (Stable Sitting Capture Storage)
- Added CLI capture command in `joi-gtk`: `--capture-stable-sitting`.
- Stable sitting capture now persists via `Microsoft.Data.Sqlite` in `db/positions.db` (no `Remember` dependency in this flow).
- Replaced legacy `StablePosition` storage path for this feature with normalized pose tables:
  - `pose_snapshot`
  - `pose_snapshot_value`
- Capture migration now removes stale `StablePosition` table in runtime DB and writes one snapshot row plus many motor-value rows.
- Enforced daily baseline gate for training routines:
  - same-day `Stable Sitting Position` is now required before seated handshake, supervised walk, and animation training begin/replay.

### Changed (Docs Cleanup)
- Updated README path references from `dynamixel/` to `cartheur-animals-robot/`.
- Updated README voice runtime notes to reflect bundled AeonVoice package auto-detection with optional env var overrides.
- Documented stable sitting pose capture command and normalized DB schema in README.

### Added (Startup Body Awareness)
- Added machine-readable body model policy at `joi-gtk/config/body-model.json` with parent-link, axis, region, and hard/soft limit metadata.
- Added startup body-awareness calibration algorithm in `RobotControlService`:
  - reads live joint telemetry against model limits,
  - writes `logs/body-awareness-last.json` report,
  - supports strict-mode fail behavior with upper/lower torque-off fail-safe.
- Added CLI command: `--body-calibrate` (strict) with optional `--non-strict`.
- Added GTK main action button: `Body Calibrate`.

### Added (Robot Voice)
- Added `AeonVoice` + `AeonVoice.Native` package integration in `joi-gtk`.
- Added `RobotNarrationService` for spoken action intent/result announcements.
- Main GTK action flow now announces what the robot intends to do before executing user commands.
- Added CLI diagnostic command: `--voice-test [text...]`.

### Added (Speech Recognition)
- Updated `Supertoys.PocketSphinx` package in `joi-gtk` to `1.2.1`.
- Added `RobotSpeechRecognitionService` with repo-scoped runtime/model path resolution from app output.
- Added CLI diagnostics:
  - `--speech-recog-status`
  - `--speech-recog-file <audio-path>`
- Added README documentation for scope policy, runtime requirements, and usage.

### Changed (Animation Training Voice Commands)
- `PocketSphinxVoiceCommandSource` now uses repo-scoped speech recognition windows (5-second capture/recognize loop) instead of direct global `pocketsphinx_continuous` process usage.
- Added voice command syntax support in `AnimationTrainingWindow`:
  - `START` begins animation training.
  - `STOP` stops and saves animation training.
- Added tone signaling behavior:
  - same tone at recording-window start and recognition-window end,
  - distinct tone when training save completes.
- Added CLI test command: `--speech-command-test`.

### Added (Thermal + Voltage Guardrails)
- Extended `joi-gtk/config/motor-overload-thresholds.json` to support safety defaults and per-motor guardrails for:
  - overload threshold,
  - max temperature,
  - min voltage.
- `RobotControlService` now loads and applies thermal/voltage policy values with per-motor override then default fallback, while remaining backward compatible with legacy numeric motor threshold entries.
- `SafetyGate` checks now trip on thermal and voltage violations in addition to communication, overload, and torque-off conditions.
- Safety trip and monitor detail payloads now include guardrail-specific values (`load/threshold`, `temperature/max`, `voltage/min`) for operator diagnosis.

### Changed (Operator Monitoring)
- Main GTK monitor summary/log output now reports overload/thermal/voltage alert counts and fingerprints.
- Animation training safety polling now surfaces thermal/voltage alert counts and beeps on any guardrail alert.
- CLI `--self-test` output now includes per-motor threshold context and thermal/voltage violation counts in summary.

### Added (Safety Replay CLI)
- Added `--safety-report` and `--safety-report-file <path>` commands in `joi-gtk/Program.cs`.
- Reports now summarize parsed safety log lines, top events/actions/phases, top motors, guardrail hit counts, and recent safety trips.

### Added (Seated Safety Test)
- Added `ExecuteSeatedHandshakeSafetyTest(...)` to `RobotControlService` for chair-seated right-arm validation using existing `SafetyGate` protections.
- Added CLI command: `--seated-handshake-test [shakes]`.
- Routine returns the tested arm to origin pose after the handshake sequence (best effort), while retaining fail-safe torque-off behavior on safety trips.

### Changed (GTK Policy Alignment)
- Main GTK form now reflects animation-training-only policy for pose/gait programming.
- Removed direct main-form seated handshake trigger to avoid bypassing training-driven flows.
- Removed stale walk-parameter row from main form and retained `Emergency Stop` as a primary control.

### Changed (GTK Compatibility)
- Replaced deprecated GTK calls in `MainWindow`:
  - `ScrolledWindow.AddWithViewport(...)` -> `Add(...)`
  - `OverrideBackgroundColor(...)` -> CSS-provider based event-box background styling.

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

### Added (IMU Serial + Balance Step)
- Added serial MPU IMU provider in `joi-gtk/Services/SerialMpuImuProvider.cs` using USB serial input (default `/dev/ttyUSB2` at `115200`).
- Wired IMU provider into `WalkController` initialization via `RobotControlService`.
- Added `Read IMU` action in GTK main window for live parsed pitch/roll/yaw diagnostics.
- Added `Balance Step` action in GTK main window to apply conservative ankle/hip compensation from IMU lean readings.

### Changed (Integration)
- Updated solution/project wiring to reference `cartheur-animals-robot` instead of the removed `dynamixel` project:
  - `Cartheur.Animation.Joi.sln` now points to `cartheur-animals-robot/Cartheur.Animals.Robot.csproj`.

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
