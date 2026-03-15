# TODO

## Next Steps

1. Standing Threshold Calibration Session
- Start monitor, run safe sweeps for ankles/knees/hips, and tune `motor-overload-thresholds.json`.
- Goal: no false trips at stable stance, immediate trips near hard-stop/load risk.

2. Add Thermal and Voltage Guardrails
- Extend the safety policy format with per-motor temperature and voltage bounds.
- Block motion preemptively when thermal/voltage limits are violated.

3. Add Safety Replay Report Command
- Parse `logs/safety-events.log` and summarize most frequent tripping motors/actions.
- Use the report to prioritize tuning and mechanical checks.

4. Add Sweep Presets for Standing Development
- Add preset controls for ankles, knees, and hips with conservative ranges/durations.
- Reduce manual entry errors and speed up daily validation runs.
