# API Contract — app ↔ server

Both sides must honour these shapes. The server is stateless: it hands over the full
route once and the app walks it locally.

The base URL lives in one editable field (`baseUrl` on the `MapClient` component,
on the `Nav` GameObject in the Unity scene). When the server is reached through an
ngrok tunnel, every request also sends the header `ngrok-skip-browser-warning: true`
— without it, ngrok returns an HTML warning page instead of JSON.

All examples below use the floor plan shipped in `python/data/floor_plan.json`
(`ENTRANCE`, `ROOM_101`, `ROOM_102`). A different map produces different node names.

---

## 1. `GET /health` — server status

Confirms the server is running and the map loaded. Intended for checking the backend
independently of the phone app — open it in a browser or call it with `curl`.

```json
{ "status": "ok", "nodes": 3 }
```

| Field | Type | Description |
|---|---|---|
| `status` | string | `ok` when the server is running |
| `nodes` | int | Number of nodes loaded from the floor plan |

---

## 2. `GET /rooms` — list destinations

Returns a bare JSON array of destination names, for the destination picker. Only
nodes with `"type": "room"` are returned, so entrances and corridor junctions are
excluded.

```json
["ROOM_101", "ROOM_102"]
```

---

## 3. `POST /init` — request a route

### Request body

```json
{ "start": "ENTRANCE", "destination": "ROOM_102" }
```

`start` and `destination` are node names from the floor-plan graph. Both fields are
required.

### Success response — `200 OK`

```json
{
  "steps": [
    { "direction": "straight", "distance": 2.5, "bearing": 90.0 },
    { "direction": "right",    "distance": 2.5, "bearing": 0.0 },
    { "direction": "arrive",   "distance": 0.0, "bearing": 0.0 }
  ]
}
```

| Field | Type | Description |
|---|---|---|
| `direction` | string | `straight` / `left` / `right` / `arrive` |
| `distance` | float | Metres to walk on this step |
| `bearing` | float | Map-space direction of this leg, in degrees (0 = map east, 90 = map north), computed from node coordinates |

The final step is always `arrive`, with `distance` `0.0`.

The app tracks distance walked via ARCore's camera pose and advances to the next step
itself — there are no per-step server calls after `/init`. `bearing` is not used for
distance tracking; the app compares it against the direction the user actually walks
to work out which way the phone is really facing, since there is no compass.

### Error — unknown node (`400 Bad Request`)

Returned when `start` or `destination` is not present in the floor plan.

```json
{ "error": "unknown node: ENTRANCE -> ROOM_999" }
```

### Error — no route (`400 Bad Request`)

Returned when both nodes exist but no path connects them.

```json
{ "error": "no route: ENTRANCE -> ROOM_102" }
```

### Error — malformed request (`422 Unprocessable Entity`)

Returned by FastAPI's request validation when a required field is missing or the
wrong type. The response is FastAPI's standard validation format:

```json
{
  "detail": [
    {
      "type": "missing",
      "loc": ["body", "destination"],
      "msg": "Field required",
      "input": { "start": "ENTRANCE" }
    }
  ]
}
```

---

## Testing the API without the phone

With the server running on port 8000:

```bat
curl http://127.0.0.1:8000/health
curl http://127.0.0.1:8000/rooms
curl -X POST http://127.0.0.1:8000/init -H "Content-Type: application/json" -d "{\"start\":\"ENTRANCE\",\"destination\":\"ROOM_102\"}"
```

FastAPI also serves interactive documentation at `http://127.0.0.1:8000/docs`, where
each endpoint can be called directly from the browser.
