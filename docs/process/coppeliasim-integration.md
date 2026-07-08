# CoppeliaSim Integration

## Goal

Use CoppeliaSim as an offline pose sketching tool, then convert those poses into
repo-native artifacts that this project can already consume safely.

The current recommended outputs are:

- `db/trainings.db` rows in `TrainingSequence` for phrase replay
- `.jvf` joint-vector logs for the TCP joint-vector bridge

## Setup

1. Install CoppeliaSim on the workstation used for pose authoring.
2. Open the local scene asset in this repo when working on Arthur:
   - `scene/arthur.ttt`
3. Keep the XML export alongside the binary scene when you need inspectable structure:
   - `scene/sim_scene.simscene.xml`
4. Model only joints that exist in this repo's body model:
   - source: `joi-gtk/config/body-model.json`
5. Name simulation joints to match robot joint names exactly, for example:
   - `head_z`
   - `r_shoulder_y`
   - `l_knee_y`
6. Keep simulated values in raw Dynamixel position space where possible:
   - default project range in this repo is roughly `0..4095`
7. Export the authored session to JSON using this template:
   - `docs/templates/coppeliasim-session.template.json`
8. For a first motion sanity check, use the sample Lua child script:
   - `docs/templates/coppeliasim-joint-cycle.lua`

## Current Arthur Scene

The current local authoring setup is:

- `scene/arthur.ttt`
- `scene/sim_scene.simscene.xml`
- `scene/random_force.lua`
- `scene/timer.lua`

Use `arthur.ttt` as the scene you open in CoppeliaSim.
Use `sim_scene.simscene.xml` when we need to inspect object names, joint names, or
scene structure in plain text.

The present scene organization comes from a Poppy Humanoid base, but the exported
XML confirms that the key Arthur-facing joints are already named in the format this
repo expects.

## Verified Joint Coverage

The XML export currently shows these joint families with repo-aligned names:

- torso: `abs_x`, `abs_y`, `abs_z`, `bust_x`, `bust_y`
- head: `head_y`, `head_z`
- left arm: `l_shoulder_y`, `l_shoulder_x`, `l_arm_z`, `l_elbow_y`
- right arm: `r_shoulder_y`, `r_shoulder_x`, `r_arm_z`, `r_elbow_y`
- left leg: `l_hip_x`, `l_hip_y`, `l_hip_z`, `l_knee_y`, `l_ankle_y`
- right leg: `r_hip_x`, `r_hip_y`, `r_hip_z`, `r_knee_y`, `r_ankle_y`

That means the current imported scene is good enough for a Phase 1 fitness pass on
upper-body pose authoring, and it also appears to include the previously missing
`l_arm_z` and `r_arm_z` joints needed for a fuller Phase 2 mapping.

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

## Recommended Arthur Workflow

1. Open `scene/arthur.ttt` in CoppeliaSim.
2. Verify the joint names you plan to touch against `scene/sim_scene.simscene.xml`.
3. Start with seated upper-body poses first:
   - `head_y`
   - `head_z`
   - `r_shoulder_y`
   - `r_shoulder_x`
   - `r_arm_z`
   - `r_elbow_y`
4. Record authored targets into `docs/templates/coppeliasim-session.template.json`.
5. Import that JSON into training storage or export it to `.jvf`.
6. Replay only after same-day seated baseline capture on hardware.

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
