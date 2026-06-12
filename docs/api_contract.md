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

| Field       | Type   | Units   | Range                            | Description                              |
| ----------- | ------ | ------- | -------------------------------- | ---------------------------------------- |
| `heading`   | float  | degrees | 0–360                            | Compass direction the device is facing   |
| `pitch`     | float  | degrees | -90–90                           | Forward/backward tilt of the device      |
| `roll`      | float  | degrees | -180–180                         | Left/right tilt of the device            |
| `distance`  | float  | metres  | 0–∞                              | Straight-line distance to target         |
| `bearing`   | float  | degrees | 0–360                            | Angle from North to target               |
| `direction` | string | —       | left / right / straight / arrive | Turn instruction for the user            |
| `depth`     | float  | (TBD)   | 0–∞                              | Depth ahead, from the latest `/frame`    |
| `obstacle`  | bool   | —       | true / false                     | True when an obstacle is detected ahead  |

Unity currently *uses* `direction` (steer the chevron path), `distance` (label),
and `obstacle` (red warning vignette). `depth` flows through but isn't displayed yet.

### Sample response

```json
{
  "heading": 189.61,
  "pitch": 15.95,
  "roll": -91.09,
  "distance": 295.81,
  "bearing": 358.93,
  "direction": "right",
  "depth": 314.37,
  "obstacle": false
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

| Field   | Type     | Description             |
| ------- | -------- | ----------------------- |
| `accel` | float[3] | Accelerometer x, y, z   |
| `mag`   | float[3] | Magnetometer x, y, z    |
| `gps`   | float[2] | GPS latitude, longitude |

### Response

```json
{ "status": "ok" }
```

---

## 3. `POST /frame` — upload a camera frame for depth

Unity grabs the AR camera image (~every 2.5 s), JPG-encodes it (downscaled), and
uploads it. The server estimates depth, stores it, and feeds the result back
through `/nav` (`depth` + the `obstacle` flag). Unity does **not** consume the
`/frame` response beyond logging it.

```
POST [ngrok-url]/frame
header:        ngrok-skip-browser-warning: true
Content-Type:  multipart/form-data
form field:    file = <jpeg bytes>   (filename: frame.jpg)
```

### Response

```json
{ "depth": 314.37 }
```

> **Backend requirement:** the `/frame` handler must return `200 OK` **immediately**
> (store the frame / update `latest_depth`, run inference async). If it blocks on
> model inference, uploads stall and the tunnel chokes.

---

## 4. `POST /target` — set the navigation destination

Lets the user change the destination from the Unity UI without touching code.

```
POST [ngrok-url]/target
header:        ngrok-skip-browser-warning: true
Content-Type:  application/json
```

### Request body

```json
{ "lat": 33.7184, "lon": 73.0720 }
```

### Response

```json
{ "status": "ok" }
```

---

## Rates (current, agreed)

| Call            | Rate                   | Notes                                                                                                                  |
| --------------- | ---------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `GET /nav`      | **2 Hz** (every 0.5 s) | Faster was rejected — extra load worsens ngrok drops, and Unity already smooths rendering per-frame, so 2 Hz is enough. |
| `POST /sensors` | ~1 Hz                  | Once per second.                                                                                                       |
| `POST /frame`   | every ~2.5 s           | Skips a frame if a previous upload is still in flight (no pile-up over the tunnel).                                     |

---

## Open questions (resolve with Sami)

- Units for `accel` (m/s²?), `mag` (µT?), and `depth` (what unit/scale? observed ~300–500).
- `bearing` / `heading`: true-north or magnetic?
- `/nav` error / no-route response shape (e.g. `direction: "none"`?).
- Exact response shapes for `/target` and `/frame` (confirm beyond the samples above).
- Confirm camera-frame orientation matches the depth model (portrait phone vs landscape sensor).

### Resolved
- POST `/sensors` rate: ~1 Hz. ✔
- `GET /nav` poll rate: 2 Hz (decided; faster is worse over ngrok). ✔
- `/frame` upload format: multipart/form-data, field `file`. ✔
- `/target` body: JSON `{ "lat":.., "lon":.. }`. ✔
