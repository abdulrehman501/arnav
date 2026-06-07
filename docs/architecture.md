# System Architecture

## 1. Overview

ARNAV is an augmented-reality navigation prototype. The user points their phone
at the world and sees direction arrows drawn on top of the live camera feed,
guiding them toward a destination. It is built for ARCore-certified Android
phones and is split into two independent halves: a Unity app that handles the
camera and AR rendering, and a Python server that does the sensor maths and
decides where to point the arrow.

## 2. High-level design

The system has two parts that talk over the local network:

- **Unity client** — runs on the phone. Owns the camera, the AR session, and the
  on-screen arrow. It asks the server what to draw.
- **Python server** — runs on a laptop on the same WiFi. Takes the phone's
  sensor data, fuses it into a stable orientation, works out the heading and
  distance to the next waypoint, and returns a single navigation instruction.

They communicate over HTTP. The phone polls the server; the server answers with a
small JSON object describing the current navigation state. Nothing else crosses
the boundary.

```
  Unity (AR Foundation + ARCore)        Python (FastAPI)
  ┌──────────────────────────┐  HTTP   ┌──────────────────────┐
  │ phone camera + AR arrows │ ◄─────► │ sensor fusion        │
  │ reads /nav JSON          │  JSON   │ serves navigation     │
  └──────────────────────────┘         └──────────────────────┘
```

## 3. Unity / AR client

The Unity app is built on AR Foundation with the ARCore backend. AR Foundation
runs the AR session, tracks the device against the real world, and supplies the
camera background that fills the screen. On top of that camera feed the app draws
a 3D arrow.

On a fixed timer the app calls `GET /nav` on the server. The response tells it
which way to point: `straight`, `left`, `right`, or `arrive`. The app updates the
arrow to match — for example, swinging it to point left when the instruction is
`left`, or showing a "you have arrived" state on `arrive`. The arrow is anchored
in AR space so it stays aligned with the real world as the phone moves.

The app is packaged as an `.apk` and installed on the phone. It needs an
ARCore-certified device because tracking and the camera pose come from ARCore.

## 4. Python / sensor-fusion server

The server is a FastAPI application served by uvicorn. It exposes the single
`/nav` endpoint and is bound to `0.0.0.0:8000` so the phone can reach it across
the WiFi.

Behind that endpoint sit two pieces of logic:

- **Sensor fusion** — takes the raw IMU signals (accelerometer, gyroscope,
  magnetometer) and combines them into a clean orientation: heading, pitch, and
  roll. Fusing the sensors together cancels out the drift and noise that any one
  of them has on its own.
- **Navigation** — given the device's position relative to the next waypoint,
  computes the distance remaining and the bearing to it, then reduces that to one
  discrete instruction (`straight` / `left` / `right` / `arrive`) for the arrow.

The server bundles these into the JSON response and hands it back to the phone.

## 5. The API seam

The two halves only ever meet at one place: the `GET /nav` HTTP endpoint, whose
JSON shape is fixed in [`docs/api_contract.md`](api_contract.md). That single
contract is what lets the project be built by two people in parallel. As long as
both sides honour the agreed fields, the Unity side can be developed against a
stub server and the Python side can be tested with a plain browser or `curl` —
neither has to wait on the other. Changing the contract is the one thing that
requires both owners to agree.

## 6. Data flow (end to end)

1. The phone's AR session is running and the camera feed is on screen.
2. On its polling timer, the Unity app sends `GET /nav` to the server.
3. The server reads the latest sensor data and runs sensor fusion to get a stable
   heading, pitch, and roll.
4. The navigation logic computes distance and bearing to the next waypoint and
   picks a direction.
5. The server returns the JSON object (`heading`, `pitch`, `roll`, `distance`,
   `bearing`, `direction`).
6. The Unity app reads `direction` and updates the AR arrow on screen.
7. The loop repeats several times a second, so the arrow tracks the user in
   near-real time.

## 7. Technology choices & justification

- **Unity + AR Foundation** — AR Foundation gives a single AR API that handles
  the session, tracking, and camera feed, so the app code stays focused on
  drawing the arrow rather than low-level AR plumbing.
- **ARCore** — it is Google's AR platform for Android and is what AR Foundation
  runs on for our target phones; it provides the device pose and world tracking
  the arrow is anchored to.
- **FastAPI + uvicorn** — a small, fast way to stand up the `/nav` endpoint with
  almost no boilerplate, and JSON serialization comes for free.
- **NumPy / SciPy** — the linear-algebra and rotation tools needed for sensor
  fusion (quaternions, filtering) without writing the maths from scratch.
- **HTTP / JSON over WiFi** — a simple, language-neutral boundary, which is
  exactly why the Unity and Python halves can be built independently.

## 8. Limitations & future work

- Navigation currently assumes the server and phone are on the same WiFi; there
  is no remote/cloud path.
- The waypoint route is fixed rather than computed from a real map.
- Polling adds a little latency; a push/streaming connection could make the arrow
  smoother.
- Several contract details are still open (polling rate, true-north vs magnetic
  bearing, the no-route response), noted in the API contract.
- No on-device fallback if the server is unreachable.
