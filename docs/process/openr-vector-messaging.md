# OPEN-R Vector Messaging In C#

This repo’s motion code already has the right high-level abstraction for OPEN-R style
joint delivery: a timed pose step. The missing piece is the transport behavior.

Q: Do we need a discrete "time-frame", say on the order of 8 ms?

In `openr-debian`, the relevant rule is not "send motor values whenever you want".
It is:

1. observer sends `ASSERT-READY`
2. subject sends one `NotifyEvent` payload
3. observer processes it
4. observer asserts ready again

That means the host-side C# implementation should model:

- a `subject` that publishes joint vectors
- one or more `observers` that consume them
- per-observer ready state
- a pending buffer when readiness is unknown
- discard behavior when readiness is explicitly deasserted

## Mapping To This Repo

- `JointVectorFrame` corresponds to one outbound vector payload
- `OpenRJointVectorSubject` models OPEN-R-style ready/notify behavior
- `OpenRJointVectorPlayer` converts received vectors into `MotionTrajectoryStep`
- `JointVectorFrameSerializer` gives the frame a compact binary wire format
- `JointVectorFrameRecorder` appends frames to a replayable log
- `JointVectorReplayPlayer` feeds logged frames back into `OpenRJointVectorSubject`
- `JointVectorTcpClient` / `JointVectorTcpServer` provide a live TCP bridge
- `joi-gtk/Services/JointVectorBridgeService` wires the TCP bridge into `RobotControlService`
- `MotionTrajectoryPlayer` remains the actuator-facing execution layer

## Suggested Usage

```csharp
var motorFunctions = new MotorFunctions();
var trajectoryPlayer = new MotionTrajectoryPlayer(motorFunctions);
var subject = new OpenRJointVectorSubject();
var observer = new OpenRJointVectorPlayer("LowerBodyMotion", subject, trajectoryPlayer);

observer.Start();

subject.Publish(new JointVectorFrame(
    new Dictionary<string, int>
    {
        ["l_hip_y"] = 520,
        ["r_hip_y"] = 480,
        ["l_knee_y"] = 560,
        ["r_knee_y"] = 500
    },
    durationMilliseconds: 400,
    interpolationSteps: 8,
    sequenceNumber: 1));
```

Round-trip through a transport payload:

```csharp
byte[] payload = JointVectorFrameSerializer.Serialize(frame);
JointVectorFrame restored = JointVectorFrameSerializer.Deserialize(payload);
subject.Publish(restored);
```

Capture and replay a session:

```csharp
using var recorder = new JointVectorFrameRecorder("walk-session.jvf");
recorder.Record(frame);

var replay = new JointVectorReplayPlayer(subject);
await replay.ReplayFileAsync(
    "walk-session.jvf",
    JointVectorReplayTimingMode.RecordedGap,
    cancellationToken);
```

Send live vectors over TCP:

```csharp
var subject = new OpenRJointVectorSubject();
var server = new JointVectorTcpServer(subject, port: 9384);
server.Start();
_ = server.RunSingleClientAsync(cancellationToken);

using var client = new JointVectorTcpClient("127.0.0.1", 9384);
await client.SendAsync(frame, cancellationToken);
```

In the GTK app, use the `Open-R Joint Vector Bridge` panel to:

- bind to `0.0.0.0` for remote publishers or `127.0.0.1` for local-only tests
- choose the listening port
- start the bridge and watch applied frames in the console log

Validated live test path on current hardware:

- initialize robot control
- capture daily stable sitting baseline
- pass `--seated-head-test`
- start GTK joint-vector bridge on `127.0.0.1:9384`
- send `head-nod`, `head-left`, `head-right`, `head-center` presets from the CLI publisher

The canned CLI sequence currently stays head-only because arm IDs `41-44` and
`51-54` were not present in the confirmed Wizard scan for this robot.

## Hardware Map

The following USB-to-bus mapping was confirmed on the robot with Dynamixel Wizard
at `1000000` bps using protocol `1.0`.

Environment mapping for this robot:

- `ARTHUR_UPPER_PORT=/dev/ttyUSB0`
- `ARTHUR_LOWER_PORT=/dev/ttyUSB1`

Observed motors on `/dev/ttyUSB0` (upper bus):

- `036` `AX-18A`
- `031` `MX-64`
- `032` `MX-64`
- `033` `MX-28`
- `034` `MX-28`
- `035` `MX-28`

Observed motors on `/dev/ttyUSB1` (lower bus):

- `011` `MX-28`
- `012` `MX-28`
- `013` `MX-28`
- `014` `MX-28`
- `015` `MX-28`
- `021` `MX-28`
- `022` `MX-28`
- `023` `MX-64`
- `024` `MX-28`
- `025` `MX-28`

Practical implications:

- the lower-body chain is on `/dev/ttyUSB1`, not `/dev/ttyUSB0`
- the upper-body chain is on `/dev/ttyUSB0`
- IDs `41-44` and `51-54` were not present in this scan, so left/right arm logic
  should not assume those joints exist on this robot without another confirming scan
- head/neck logic should be based on observed IDs, especially `036`, rather than
  assuming the full nominal map is present

## Binary Layout

The serializer uses a small self-describing binary envelope:

- `uint32` magic: `JVF1`
- `uint16` version
- `int64` sequence number
- `int64` `CreatedUtc` via `DateTime.ToBinary()`
- `int32` duration milliseconds
- `int32` interpolation steps
- `int32` joint count
- repeated joint entries:
- `uint16` UTF-8 name length
- `byte[]` UTF-8 joint name
- `int32` position value

The capture log wraps each serialized frame as:

- `int32` payload length
- `byte[]` serialized `JointVectorFrame`

The TCP transport uses the same framing:

- `int32` payload length
- `byte[]` serialized `JointVectorFrame`

## Why This Is Closer To OPEN-R

This keeps the important semantics described in the sibling repo:

- delivery only occurs after readiness is asserted
- delivery is one frame at a time
- an observer must re-arm itself after each frame
- deasserted links do not accumulate stale motion commands
- replay can exercise the same readiness path as live publishing

## Practical Next Step

If you later want socket transport, keep `JointVectorFrame` as the in-memory
contract and reuse the same serializer and length-prefix framing rather than
coupling network logic directly into `MotionTrajectoryPlayer`.

For safety, prefer `IPAddress.Loopback` unless you intentionally want a remote
publisher on the network.
