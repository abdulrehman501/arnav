# ARNAV — AR Navigation

An augmented-reality navigation prototype. The phone camera shows the real world with
AR direction arrows overlaid on it. A Python backend fuses phone sensor data and serves
navigation instructions; the Unity app renders them in AR.

## Architecture (at a glance)

```
  Unity (AR Foundation + ARCore)        Python (FastAPI)
  ┌──────────────────────────┐  HTTP   ┌──────────────────────┐
  │ phone camera + AR arrows │ ◄─────► │ sensor fusion        │
  │ reads /nav JSON          │  JSON   │ serves navigation     │
  └──────────────────────────┘         └──────────────────────┘
            │ builds .apk
            ▼
   Android phone (ARCore-certified)
```

## Repository layout

| Path | Owner | Purpose |
|------|-------|---------|
| `python/`            | Sami   | Sensor fusion + FastAPI server |
| `unity/arnav-unity/` | Rehman | Unity AR project |
| `docs/`              | Shared | Architecture + API contract |

## The seam between the two halves

The Unity app and the Python server only meet at one place: the HTTP endpoint
`GET /nav`. Its JSON shape is defined in [`docs/api_contract.md`](docs/api_contract.md).
As long as both sides honour that contract, they can be developed independently.

## Branches

- `main` — integration branch
- `rehman-unity` — Rehman's Unity work
- (Sami's Python work branch as agreed)

## Getting started

- **Python:** see `python/requirements.txt`
- **Unity:** open `unity/arnav-unity/` in Unity 6.4 (6000.4.10f1)
