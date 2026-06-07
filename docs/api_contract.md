# API Contract — Integration point between backend and Unity app

Both sides MUST honour these shapes. Any change here must be agreed by **Sami** and **Rehman**.

> **Base URL:** Sami's current ngrok tunnel, e.g. `https://<sub>.ngrok-free.dev`.
> On the free tier this **changes every time the tunnel restarts** — do not hardcode it; keep it in one editable field. Get the current URL from Sami.
> Every request must send the header `ngrok-skip-browser-warning: true`, or ngrok returns an HTML warning page instead of JSON.

---

## 1. `GET /nav` — get navigation result

```
GET  [ngrok-url]/nav
header:  ngrok-skip-browser-warning: true
```

### Schema

| Field       | Type   | Units   | Range               | Description                            |
| ----------- | ------ | ------- | ------------------- | -------------------------------------- |
| `heading`   | float  | degrees | 0–360               | Compass direction the device is facing |
| `pitch`     | float  | degrees | -90–90              | Forward/backward tilt of the device    |
| `roll`      | float  | degrees | -180–180            | Left/right tilt of the device          |
| `distance`  | float  | metres  | 0–∞                 | Straight-line distance to target       |
| `bearing`   | float  | degrees | 0–360               | Angle from North to target             |
| `direction` | string | —       | left / right / straight / arrive | Turn instruction for the user |

### Sample response

```json
{
  "heading": 9.46,
  "pitch": -0.58,
  "roll": 1.17,
  "distance": 1903.93,
  "bearing": 56.7,
  "direction": "right"
}
```

---

## 2. `POST /sensors` — upload phone sensor data

Unity reads the phone's raw sensors and sends them to the server (~1 Hz). The
server uses these for fusion and navigation.

```
POST [ngrok-url]/sensors
header:  ngrok-skip-browser-warning: true
header:  Content-Type: application/json
```

### Request body

```json
{
  "accel": [x, y, z],
  "mag":   [x, y, z],
  "gps":   [lat, lon]
}
```

| Field   | Type        | Description                          |
| ------- | ----------- | ------------------------------------ |
| `accel` | float[3]    | Accelerometer x, y, z                |
| `mag`   | float[3]    | Magnetometer x, y, z                 |
| `gps`   | float[2]    | GPS latitude, longitude              |

### Response

```json
{ "status": "ok" }
```

---

## Open questions (resolve with Sami)

- Units for `accel` (m/s²?) and `mag` (µT?) — confirm.
- POST rate: confirmed ~1 Hz (once per second)?
- `bearing` / `heading`: true-north or magnetic?
- Error / no-route response shape for `/nav`?
