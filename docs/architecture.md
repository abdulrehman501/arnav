# System Architecture

## 1. Overview

ARNAV is an indoor AR navigation app. The user scans a QR code at a building
entrance, picks a destination room, and the phone draws AR arrows on the ground
that guide them there through the corridors. It targets ARCore-certified Android
phones and is split into two independent halves: a Unity app that owns the camera,
AR tracking, and on-screen guidance, and a Python server that knows the building
layout and works out the route.

There is no GPS and no compass anywhere in the system. Both were tried in an
earlier outdoor version of this project and dropped: GPS doesn't work indoors, and
phone magnetometers drift too much near a building's steel and electronics to give
a reliable heading. Position comes from ARCore's own visual-inertial tracking
instead, and heading is inferred from the direction the user actually walks (see
§3).

## 2. High-level design

The system has two parts that talk over HTTP, once per journey:

- **Unity client** — runs on the phone. Owns the camera, AR session, QR scanning,
  and the on-screen chevrons. Asks the server for a route once, then guides the
  user through it entirely on-device.
- **Python server** — runs on a laptop, reachable over WiFi or an ngrok tunnel.
  Holds the floor-plan graph and computes the shortest route between two nodes.

```
  Unity (AR Foundation + ARCore)        Python (FastAPI)
  ┌──────────────────────────┐  HTTP   ┌──────────────────────┐
  │ QR scan, camera, chevrons│ ◄─────► │ floor-plan graph      │
  │ walks the route locally  │  JSON   │ Dijkstra shortest path│
  └──────────────────────────┘         └──────────────────────┘
```

The server is asked twice per journey — once for the room list, once for a route
— and never again. Everything after that (tracking distance walked, deciding when
a turn is reached, drawing the arrows) happens on the phone. A third endpoint,
`GET /health`, exists only so the server can be checked on its own; the app does
not call it during navigation.

## 3. Unity / AR client

Built on AR Foundation with the ARCore backend, which runs the AR session, tracks
the phone against the real world, and supplies the camera background. On top of
that the app draws a row of ground-anchored chevrons.

The client is a chain of small, single-job components:

- **QR scan** — reads the AR camera's CPU image feed (the same feed ARCore already
  uses, so there's no second camera request to fight over) and decodes QR codes
  with ZXing.Net. The scanned text becomes the start node.
- **Destination picker** — fetches `GET /rooms` and shows every room as a button.
  Tapping one sends `POST /init` and hands the returned steps to the route
  controller.
- **Route controller** — holds the current steps and measures how far the phone
  has moved using ARCore's camera pose, advancing to the next step once a leg's
  distance is covered. No further server calls.
- **Orientation (the compass problem)** — chevrons need to know which way is
  actually forward, but there's no compass to ask. Instead, once a route loads,
  the app watches the first couple of metres the user walks and compares that
  real-world direction to the corridor's known bearing (supplied by the server —
  see §4). The difference between the two is a single rotation offset, valid for
  the rest of the journey, that converts any map-space bearing into a real-world
  direction. This is the same principle a car's GPS uses to show which way you're
  facing before it has a compass fix: it watches which way you actually drove.
- **Chevrons** — once that offset is known, arrows are drawn pointing in the true
  corridor direction, independent of which way the phone is held. Before
  alignment, they fall back to pointing wherever the camera faces, so the app is
  never left with no guidance at all.
- **HUD** — plain on-screen text ("GO STRAIGHT 6 m", "TURN LEFT", "ARRIVED").

The app is packaged as an `.apk`. It needs an ARCore-certified device, since
tracking and camera pose both come from ARCore.

## 4. Python / routing server

A FastAPI application served by uvicorn, bound to `0.0.0.0:8000` so the phone can
reach it over WiFi or through a tunnel.

The building is modelled as a graph in `floor_plan.json`: every room and hallway
junction is a node with `(x, y)` coordinates, every walkable connection between
two nodes is an edge. Nothing about distances or turns is hand-typed — it's all
derived from the coordinates:

- **Distance** — the straight-line length of each edge, computed from its two
  nodes' coordinates.
- **Routing** — Dijkstra's algorithm finds the shortest path between the
  requested start and destination nodes.
- **Turns** — at each node along that path, the angle between the incoming and
  outgoing edge is computed from their vectors; a cross-product sign gives left
  vs. right, and a small angle counts as "straight". Consecutive straight legs
  merge into one step.
- **Bearing** — the compass-style direction of each leg in map space (0° = map
  east, 90° = map north), computed the same way as distance, straight from
  coordinates. This is what the Unity client's orientation step compares against
  the direction actually walked.

The server has no idea a phone or a camera exists. It only ever sees two node
names and returns a list of steps.

## 5. The API seam

The two halves meet at a small set of endpoints, all fixed in
[`docs/api_contract.md`](api_contract.md):

- `GET /health` — server status, for checking the backend on its own.
- `GET /rooms` — the destination list.
- `POST /init` — a route, called once per journey.

As long as both sides honour that contract, the Unity client can be built and
tested against a stub server, and the Python server can be tested with `curl` or
through FastAPI's own `/docs` page, without either side depending on the other.
Changing the contract means changing both sides together.

## 6. Data flow (end to end)

1. The app opens; the camera starts hunting for a QR code and a background
   request for the room list fires at the same time.
2. The QR code is scanned — its text becomes the start node, and the room list
   (already fetched) is revealed as buttons.
3. The user taps a destination. The app sends `POST /init {start, destination}`.
4. The server builds the graph, runs Dijkstra, and returns steps — each with a
   direction, a distance, and a bearing.
5. The route loads. The orientation step arms: once the user has walked a couple
   of metres, their real-world walking direction is compared to the first leg's
   bearing to lock a single rotation offset.
6. Chevrons switch to pointing in the true corridor direction; the route
   controller advances through steps as ARCore's camera pose shows each leg's
   distance covered; the HUD text updates every frame.
7. No further server calls happen until the next journey.

## 7. Technology choices & justification

- **Unity + AR Foundation** — a single AR API that handles the session, tracking,
  and camera feed, so the app code stays focused on guidance rather than
  low-level AR plumbing.
- **ARCore** — Google's AR platform for Android; supplies the visual-inertial
  device pose the chevrons and distance tracking are anchored to. Chosen over
  GPS/compass specifically because both are unreliable indoors — GPS has no
  signal, and magnetometers drift too much near a building's structure.
- **ZXing.Net** — a mature, well-tested QR decoder, used against ARCore's own CPU
  image feed rather than opening a second camera.
- **FastAPI + uvicorn** — a small, fast way to stand up a few endpoints with almost
  no boilerplate, and JSON serialization comes for free.
- **Dijkstra's algorithm** — the standard correct choice for shortest path on a
  graph with non-negative edge weights (walking distances can't be negative), and
  simple enough to implement and test directly with a priority queue.
- **HTTP / JSON** — a simple, language-neutral boundary, which is exactly why the
  Unity and Python halves can be built independently and why the server can be
  reached through an ngrok tunnel without the client caring.

## 8. Limitations & future work

- The floor-plan graph is authored by hand; there's no tooling yet to survey a
  real building and generate one automatically.
- Orientation locks once per journey from a short walked distance. If that
  initial walk is interrupted or very short, the lock can be off; there's no
  QR-wall-pose anchor yet (scanning a QR that also encodes which way the wall
  faces) — a higher-risk approach considered and deliberately deferred.
- Routing is single-building. Crossing between buildings would need a shared
  outdoor or lobby-level graph connecting each building's indoor graph, which
  doesn't exist yet.
- No offline fallback if the server is unreachable — the app needs a route from
  `/init` before it can guide anyone.
- The server assumes the phone can reach it directly (WiFi or tunnel); there's no
  authentication or multi-building routing service behind it.
