# ARNAV — AR Navigation

An augmented-reality navigation prototype. The phone camera shows the real world with
an AR chevron path and navigation cues overlaid on it. A Python backend fuses phone sensor
data and serves navigation instructions; the Unity app renders them in AR.

## Architecture (at a glance)

```
  Unity (AR Foundation + ARCore)        Python (FastAPI)
  ┌──────────────────────────┐  HTTP   ┌──────────────────────┐
  │ phone camera + AR path   │ ◄─────► │ sensor fusion         │
  │ reads /nav JSON          │  JSON   │ serves navigation     │
  └──────────────────────────┘         └──────────────────────┘
            │ builds .apk
            ▼
   Android phone (ARCore-certified)
```

## Repository layout

| Path | Owner | Purpose |
|------|-------|---------|
| `python/`            | Backend  | Sensor fusion + FastAPI server |
| `unity/arnav-unity/` | Frontend | Unity AR project |
| `docs/`              | Shared   | Architecture + API contract |

## The seam between the two halves

The Unity app and the Python server only meet at one place: the HTTP endpoint
`GET /nav`. Its JSON shape is defined in [`docs/api_contract.md`](docs/api_contract.md).
As long as both sides honour that contract, they can be developed independently.

## Branches

- `main` — integration branch
- `rehman-unity` — frontend (Unity) work
- `backend` — backend (Python server) work

## Getting started

- **Python:** see `python/requirements.txt`
- **Unity:** open `unity/arnav-unity/` in Unity 6.4 (6000.4.10f1)
