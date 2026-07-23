# ARNAV — Indoor AR Navigation

Turn-by-turn AR navigation for indoor spaces, where GPS and compass don't work. Scan a QR code at the entrance, pick a destination room, and follow ground-anchored AR chevrons through the corridors to get there.

## The problem

Outdoor nav apps lean on GPS (position) and a magnetometer (heading). Both fail indoors — GPS has no signal, and magnetometer heading drifts badly near building steel. ARNAV replaces both with **ARCore visual-inertial tracking** and a **QR-anchored, graph-based routing engine**, with no GPS, compass, or magnetometer anywhere in the system.

## Engineering highlights

- **Heading without a magnetometer.** Orientation is inferred by comparing a short walked displacement (from ARCore's camera pose) against the server-computed corridor bearing for the first route step, then locking that offset for the session. AR chevrons stay corridor-locked regardless of how the phone is held — device-verified: rotating the phone in place does not move the arrows.
- **Graph-based routing engine.** The building is modeled as a coordinate graph (nodes + edges); a FastAPI backend runs Dijkstra's algorithm for shortest path, then derives turn directions and corridor bearings from node geometry — nothing is hand-typed. Pure functions, fully unit-tested.
- **QR scan-to-start.** Scanning an entrance QR code sets the user's starting node and reveals the destination picker — no manual position entry.
- **Stateless API, client-side tracking.** The server hands over the full route once; the Unity client tracks distance walked locally from ARCore's camera pose and advances through steps itself, with jump-clamping to reject tracking glitches.

## Architecture

```
  Unity (AR Foundation + ARCore)                 Python (FastAPI)
  ┌────────────────────────────────┐   HTTP     ┌─────────────────────────┐
  │ QR scan → start node            │  ───────►  │ GET  /rooms             │
  │ camera + AR chevron path        │  ◄───────  │ POST /init {start,dest} │
  │ tracks distance from ARCore pose│   JSON     │ → Dijkstra + bearings   │
  └────────────────────────────────┘             └─────────────────────────┘
            │ builds .apk
            ▼
   Android phone (ARCore-certified)
```

The two halves meet only at the HTTP API — see [`docs/api_contract.md`](docs/api_contract.md). As long as both sides honour that contract, they're developed independently.

## Tech stack

**Frontend/AR:** Unity, C#, AR Foundation, ARCore, ZXing.NET (QR decoding)
**Backend:** Python, FastAPI, Dijkstra's algorithm, pytest
**Infra:** ngrok (dev tunneling), Android (adb, Gradle builds)

## Status

Delivered to a real client and verified end to end: server tested live (real 200s from a physical device over both USB and tunneled connections), full pipeline confirmed on-device — QR scan → route computed → AR chevrons guide the walk → correct arrival. Client installed and tested it independently on her own machine.

## My role

Owned the full Unity/AR side end-to-end: scene setup, ARCore integration, QR scanning, the chevron rendering + orientation-lock system, distance tracking, and the Android build pipeline. Backend routing engine (Dijkstra + bearing computation) built by a collaborating teammate over the shared API contract above.

## Repository layout

| Path | Owner | Purpose |
|------|-------|---------|
| `python/`            | Backend  | Routing engine + FastAPI server |
| `unity/arnav-unity/` | Frontend | Unity AR project |
| `docs/`              | Shared   | Architecture + API contract |

## Getting started

- **Python:** see `python/requirements.txt`
- **Unity:** open `unity/arnav-unity/` in Unity 6.4 (6000.4.10f1) with Android Build Support
