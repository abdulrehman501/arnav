# API Contract — `GET /nav`

This is the single integration point between the Python backend and the Unity app.
Both sides MUST honour this shape. Any change here must be agreed by Sami **and** Rehman.

## Endpoint

```
GET http://<server-ip>:8000/nav
```

- Server and phone must be on the **same WiFi network**.
- `<server-ip>` is the LAN IP of the machine running the Python server (e.g. `192.168.1.42`).

## Response — `200 OK`, `application/json`

```json
{
  "heading":   0.0,
  "pitch":     0.0,
  "roll":      0.0,
  "distance":  0.0,
  "bearing":   0.0,
  "direction": "straight"
}
```

## Field definitions

| Field       | Type   | Unit / Range            | Meaning |
|-------------|--------|-------------------------|---------|
| `heading`   | float  | degrees, 0–360          | Device compass heading (yaw), from sensor fusion |
| `pitch`     | float  | degrees, -90 to +90     | Device tilt up/down |
| `roll`      | float  | degrees, -180 to +180   | Device tilt left/right |
| `distance`  | float  | metres, ≥ 0             | Distance remaining to the next waypoint |
| `bearing`   | float  | degrees, 0–360          | Compass direction to the next waypoint |
| `direction` | string | enum (see below)        | Discrete turn instruction for the AR arrow |

### `direction` enum

| Value      | Arrow shows |
|------------|-------------|
| `straight` | forward |
| `left`     | turn left |
| `right`    | turn right |
| `arrive`   | destination reached |

## Notes / open questions (resolve with Sami)

- Polling rate: how often should Unity call `/nav`? (proposed: ~5–10 Hz)
- Coordinate frame: is `bearing` true-north or magnetic?
- Error / no-route response shape (e.g. `direction: "none"`?)

> Status: **DRAFT** — confirm with Sami before relying on it.
