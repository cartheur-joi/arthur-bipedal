## Next Session Plan: Real Lubos Personality Runtime

### Summary
Implement a real Lubos personality in `joi-gtk` by integrating Henry Aeon as the dialogue core, while preserving strict motor safety separation. Keep voice recognition echo routed through personality rephrasing. Maintain full-self-check policy where motor unavailability is a hard FAIL.

### Key Changes
- Add `IPersonalityEngine` and `IPersonalitySessionStore` as stable runtime contracts.
- Implement `HenryAeonPersonalityEngine` to load Henry `.aeon` assets and generate replies with timeout + deterministic fallback.
- Add session memory persistence for recent turns, tone state, last intent, and optional user identity.
- Replace heuristic Lubos response generation in:
  - `--personality-test`
  - `--interactive-toys-once`
  - `--interactive-toys`
  - `--interactive-toys-voice`
  - GTK voice recognition echo callback
- Keep personality non-authoritative for actuation: it may shape language/intent only; all motor actions remain gated by existing control/safety services.
- Extend `--full-self-check` and GTK Full Self Check output to report personality engine mode/readiness and asset status, while keeping motor unavailable = FAIL.

### Test Plan
- Unit tests:
  - personality asset load success/failure
  - generation timeout handling
  - fallback output determinism
  - session store save/load and corrupt-state recovery
- Integration tests:
  - CLI commands return Henry-backed responses when assets present
  - voice interactive mode reiterates via personality rephrase
  - full self-check reports personality readiness and preserves motor FAIL semantics
- Manual GTK validation:
  - voice recognition echo toggle ON/OFF lifecycle
  - recognized phrase appears in logs and is spoken through personality output
  - full self-check button reports all subsystems clearly

### Assumptions
- Personality core: Henry Aeon.
- Voice echo behavior: personality rephrase.
- Motor-unavailable in self-check: FAIL.
- Runtime remains local/offline with safe fallback when personality assets are missing.
