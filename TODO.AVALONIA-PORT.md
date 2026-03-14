# Avalonia Port TODO (WinForms -> Linux GUI)

This checklist tracks migration of `joi-animations` WinForms screens into `joi-avalonia`,
with explicit functional tests per step.

## 0) Migration Rules (Do First)

- [ ] Keep `cartheur-animals-robot` as the only robot-control backend.
- [ ] Move UI logic only; avoid duplicating motor-control logic in viewmodels.
- [ ] Use service wrappers in `joi-avalonia/Services` for all hardware calls.
- [ ] Add log output for every hardware action (timestamp + action + result).
- [ ] Keep emergency stop available on every motion screen.

Functional test:
- [ ] Build `joi-avalonia` cleanly on Linux.
- [ ] Start app and confirm no robot call happens before explicit user command.

## 1) Application Shell (from `ApplicationManager`)

- [x] Create Avalonia app entry and main window.
- [ ] Add navigation shell (left menu / tabs) for each migrated screen.
- [ ] Add shared status bar (ports, torque state, last error).

Functional test:
- [ ] Navigate between all views without crashes.
- [ ] Confirm shared status/log updates from any view.

## 2) Templater Form Port (highest priority)

Source: `joi-animations/Subforms/TemplaterForm.cs`

- [x] Add core controls: init, torque lower on/off, telemetry read.
- [x] Add supervised walk command wiring (`WalkController`).
- [x] Add parameter inputs (cycles, step duration, interpolation, timeout).
- [x] Add emergency stop command.
- [ ] Add torque controls for upper/all regions.
- [ ] Port limb capture workflow:
  - [ ] Capture current left/right arm
  - [ ] Capture desired left/right arm
  - [ ] Capture current left/right leg
  - [ ] Capture desired left/right leg
- [ ] Port replay workflow (`current -> desired -> current`) for arm/leg.
- [ ] Port dictionary engagement action (`MoveMotorSequence()` from selected set).
- [ ] Port serial monitor panel (if still needed).

Functional test:
- [ ] Init opens expected motor ports.
- [ ] Torque ON/OFF lower works repeatedly.
- [ ] Supervised 1-cycle walk runs and either completes or aborts safely.
- [ ] 3-cycle walk logs every cycle and returns control to UI.
- [ ] Emergency stop always disables lower torque during motion.

## 3) Control Keypad Port

Source: `joi-animations/Subforms/ControlKeypad.cs`

- [ ] Add per-limb toggle buttons for torque:
  - [ ] Left/right leg (with/without pelvis)
  - [ ] Left/right arm
  - [ ] Abdomen/bust/head
  - [ ] Left/right ankle
- [ ] Add keyboard shortcut layer in Avalonia.
- [ ] Port standing-pose capture to DB.
- [ ] Port recall-position action from DB.
- [ ] Port clear-DB action with confirmation.
- [ ] Decide whether gyroscope live panel stays in this screen or Monitoring screen.

Functional test:
- [ ] Every toggle changes torque state and UI indicator.
- [ ] Stored standing pose can be recalled and replayed correctly.
- [ ] Keyboard shortcuts trigger the same actions as buttons.

## 4) RobotControl Port (Dictionary Editor)

Source: `joi-animations/Subforms/RobotControl.cs`

- [ ] Add editable dictionary grid (`motor`, `goal`).
- [ ] Add add/remove/clear row actions.
- [ ] Add load/save from `logs/*.txt`.
- [ ] Add validate-before-run (motor exists, value range, no duplicates).
- [ ] Add run sequence action via `MoveMotorSequence`.

Functional test:
- [ ] Save -> load roundtrip keeps full dictionary fidelity.
- [ ] Invalid entries blocked with explicit error message.
- [ ] Valid dictionary executes against robot without app freeze.

## 5) AnimationTraining Port

Source: `joi-animations/Subforms/AnimationTraining.cs`

- [ ] Port training selection UI (limbic areas, pose hardening selection).
- [ ] Port timer-driven recording.
- [ ] Port replay from recorded files.
- [ ] Port DB storage/retrieval interactions.
- [ ] Replace blocking timing with async timers in Avalonia.

Functional test:
- [ ] Record N steps and confirm files created for each step.
- [ ] Replay runs same number of steps and drives expected motors.
- [ ] Training DB write/read returns consistent data.

## 6) Monitoring Form Port

Source: `joi-animations/Subforms/MonitoringForm.cs`

- [ ] Build telemetry dashboard:
  - [ ] motor load
  - [ ] motor temperature
  - [ ] voltage
  - [ ] optional IMU pitch/roll
- [ ] Add poll interval control and start/stop polling.
- [ ] Add threshold highlighting (warning/critical).

Functional test:
- [ ] Polling updates at configured interval.
- [ ] Alerts fire when thresholds exceeded.
- [ ] Stop polling fully stops hardware reads.

## 7) Reference/Utility Forms

Sources:
- `MotorsRobot`
- `RelationalTable`
- `MxControlTable`
- `Ax12ControlTable`
- `Ax18ControlTable`
- `DemosForm`
- `PopoutCamera`

- [ ] Decide keep vs drop for each utility form.
- [ ] Port essential data/reference screens first.
- [ ] For camera, decide Avalonia-compatible preview approach (or remove).

Functional test:
- [ ] Each kept screen opens/closes cleanly.
- [ ] Data displayed matches backend/source files.

## 8) Cross-Cutting Hardening

- [ ] Add centralized exception handling for command execution.
- [ ] Add cancellation support for long-running motions.
- [ ] Add robot preflight check command:
  - [ ] ports ready
  - [ ] torque status
  - [ ] min voltage
  - [ ] temperature sanity
- [ ] Add persistent app settings (COM ports, walk defaults).
- [ ] Add structured run log file export.

Functional test:
- [ ] Inject failures (bad port, timeout, sensor unavailable) and verify safe handling.
- [ ] Confirm no UI hangs during motion execution.

## 9) Test Sequence for Real Robot Bring-Up

Run this every time after a migration milestone:

- [ ] Build + launch Avalonia app on Linux.
- [ ] Preflight check passes.
- [ ] Torque on lower, read telemetry snapshot.
- [ ] Execute supervised walk: 1 cycle, slow settings.
- [ ] Execute supervised walk: 3 cycles, slow settings.
- [ ] Trigger emergency stop during movement.
- [ ] Save run log with pass/fail notes.

## 10) Exit Criteria (WinForms Decommission)

- [ ] All required WinForms features marked ported and tested.
- [ ] Linux hardware test pass rate acceptable for 1-cycle and 3-cycle walks.
- [ ] No blocking regressions for torque control, pose replay, or telemetry.
- [ ] Team agrees WinForms can be frozen/deprecated.

## Resume Prompt

Use this prompt to resume work:

`Continue the Avalonia migration in TODO.AVALONIA-PORT.md. Start at the first unchecked item in priority order, implement it directly in code, run build/tests, then update the TODO checkboxes and summarize what changed plus what remains.`

Optional stricter variant:

`Continue TODO.AVALONIA-PORT.md from the first unchecked task. Do not ask for planning; execute changes immediately. After each completed task, mark it checked in the TODO and run a build of joi-avalonia. If blocked, state the exact blocker and take the next unblocked task.`
