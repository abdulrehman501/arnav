import json
import sys
from pathlib import Path

from fastapi import FastAPI
from fastapi.responses import JSONResponse
from pydantic import BaseModel

import routing

app = FastAPI()

DATA = Path(__file__).parent / "data" / "floor_plan.json"


def load_map(path):
    """Load the floor plan, failing with a readable message instead of a traceback."""
    if not path.exists():
        sys.exit(f"Floor plan not found: {path}\nThe server cannot start without it.")
    try:
        coords, adj, node_types = routing.load_floor_plan(path)
    except json.JSONDecodeError as e:
        sys.exit(f"Floor plan is not valid JSON: {path}\n  line {e.lineno}, column {e.colno}: {e.msg}")
    except KeyError as e:
        sys.exit(f"Floor plan is missing a required field: {path}\n  missing key: {e}")

    if not coords:
        sys.exit(f"Floor plan has no nodes: {path}")
    if not any(t == "room" for t in node_types.values()):
        sys.exit(f"Floor plan has no nodes of type 'room', so there are no destinations: {path}")
    return coords, adj, node_types


coords, adj, node_types = load_map(DATA)


class InitData(BaseModel):
    start: str
    destination: str


@app.get("/health")
def get_health():
    """Server-status check, so the server can be verified without the phone app."""
    return {"status": "ok", "nodes": len(coords)}


@app.get("/rooms")
def get_rooms():
    """Pickable destinations — the room nodes, in map order."""
    return [name for name, t in node_types.items() if t == "room"]


@app.post("/init")
def post_init(data: InitData):
    if data.start not in coords or data.destination not in coords:
        return JSONResponse(
            status_code=400,
            content={"error": f"unknown node: {data.start} -> {data.destination}"},
        )
    steps = routing.build_route(coords, adj, data.start, data.destination)
    if steps is None:
        return JSONResponse(
            status_code=400,
            content={"error": f"no route: {data.start} -> {data.destination}"},
        )
    return {"steps": steps}
