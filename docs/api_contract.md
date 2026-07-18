# API Contract — app ↔ server

Both sides must honour these shapes. The server is stateless: it hands over the full
route once and the app walks it locally.

> **Base URL** lives in one editable field (`baseUrl` on `MapClient`). The server is
> reached through an ngrok tunnel, so every request sends the header
> `ngrok-skip-browser-warning: true` — without it ngrok returns an HTML warning page
> instead of JSON.

---

## 1. `GET /rooms` — list destinations

Returns a bare JSON array of room/node names for the destination picker.

```json
["ROOM_201", "ROOM_205", "LAB_1"]
```

---

## 2. `POST /init` — request a route

### Request body

```json
{ "start": "ENTRANCE", "destination": "ROOM_201" }
```

`start` and `destination` are node names from the floor-plan waypoint graph.

### Response

```json
{
  "steps": [
    { "direction": "straight", "distance": 12.0 },
    { "direction": "left",     "distance": 4.5 },
    { "direction": "arrive",   "distance": 0.0 }
  ]
}
```

| Field       | Type   | Description                                        |
|-------------|--------|----------------------------------------------------|
| `direction` | string | `straight` / `left` / `right` / `arrive`           |
| `distance`  | float  | Metres to walk for this step                       |

The app tracks distance walked via ARCore's camera pose and advances to the next
step itself — there are no per-step server calls after `/init`.

---

## Dropped in the indoor pivot (2026-06-28)

The old outdoor endpoints — `GET /nav` (heading/bearing/pitch/roll), `POST /sensors`,
`POST /frame`, `POST /target` — are gone. No GPS, magnetometer, or compass anywhere.
The pre-pivot contract is in git history (`git show 5e88d2a:docs/api_contract.md`).
