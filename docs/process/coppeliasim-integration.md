# CoppeliaSim Integration

## Goal

Use CoppeliaSim as an offline pose sketching tool, then convert those poses into
repo-native artifacts that this project can already consume safely.

The current recommended outputs are:

- `db/trainings.db` rows in `TrainingSequence` for phrase replay
- `.jvf` joint-vector logs for the TCP joint-vector bridge

## Setup

1. Install CoppeliaSim on the workstation used for pose authoring.
2. Model only joints that exist in this repo's body model:
   - source: `joi-gtk/config/body-model.json`
3. Name simulation joints to match robot joint names exactly, for example:
   - `head_z`
   - `r_shoulder_y`
   - `l_knee_y`
4. Keep simulated values in raw Dynamixel position space where possible:
   - default project range in this repo is roughly `0..4095`
5. Export the authored session to JSON using this template:
   - `docs/templates/coppeliasim-session.template.json`
6. For a first motion sanity check, use the sample Lua child script:
   - `docs/templates/coppeliasim-joint-cycle.lua`

## JSON Contract

Required top-level fields:

- `sessionName`
- `frames`

Optional top-level fields:

- `defaultDurationMs`
- `defaultInterpolationSteps`

Each frame supports:

- `name`
- `durationMs`
- `interpolationSteps`
- `jointTargets`

`jointTargets` must be a dictionary of:

- `joint name -> integer position`

## Generate Repo Files

Create training rows for phrase replay:

```bash
dotnet run --project joi-gtk/joi-gtk.csproj -- --import-sim-training docs/templates/coppeliasim-session.template.json seated_handshake
```

This writes to:

- `joi-gtk/bin/Debug/net9.0/db/trainings.db`

Create a joint-vector log for the live TCP bridge:

```bash
dotnet run --project joi-gtk/joi-gtk.csproj -- --export-sim-jvf docs/templates/coppeliasim-session.template.json sim/seated-handshake.jvf
```

This writes:

- a `.jvf` file using the repo's `JointVectorFrame` binary format

## Live Use

For phrase replay path:

1. Capture same-day baseline:
   - `dotnet run --project joi-gtk/joi-gtk.csproj -- --capture-stable-sitting`
2. Import the simulation JSON into training storage.
3. Replay the phrase:
   - `dotnet run --project joi-gtk/joi-gtk.csproj -- --replay-trained-phrase seated_handshake`

For live joint-vector bridge path:

1. Start the GTK app.
2. Start the `Open-R Joint Vector Bridge` on `127.0.0.1:9384`.
3. Produce a `.jvf` log from the simulation session.
4. Use a replay sender or future publisher to feed those frames to the bridge.

## Rules To Keep It Safe

- Treat simulation output as draft motion, not approved final motion.
- Keep joint names aligned with `body-model.json`.
- Start with upper-body or seated routines first.
- Always capture same-day stable sitting before replay on hardware.
- If the importer reports unknown joints, fix the simulation naming before using the output.

## Recommended First Session

Start with a seated right-arm gesture with 3 frames:

1. start
2. extend
3. return

That is the fastest way to validate:

- joint naming
- file generation
- baseline enforcement
- safe replay on the live robot

## First Sim Cycle

1. Create or open your robot model in CoppeliaSim.
2. Ensure the test joint name matches this repo naming, for example `r_elbow_y`.
3. Attach a non-threaded child script to the robot model.
4. Paste in:
   - `docs/templates/coppeliasim-joint-cycle.lua`
5. Start simulation.
6. Confirm the joint cycles through:
   - start
   - positive offset
   - negative offset
   - return
7. Once the cycle works, replace the scripted offsets with your real authored poses.
8. Record those pose values into `docs/templates/coppeliasim-session.template.json`.
9. Import that JSON into this repo for replay on the real robot.
