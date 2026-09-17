"""Indoor route finding over the floor-plan graph.

Pure functions, no web framework here on purpose, so the routing can be tested
on its own. The graph is loaded from the coordinate map; distances come from the
node coordinates and turn directions are worked out from the geometry rather than
being written into the map by hand.
"""
import json
import math
import heapq


def load_floor_plan(path):
    with open(path) as f:
        return build_graph(json.load(f))


def build_graph(data):
    nodes = data["nodes"]
    coords = {name: (n["x"], n["y"]) for name, n in nodes.items()}
    types = {name: n.get("type", "room") for name, n in nodes.items()}

    adj = {name: [] for name in nodes}
    for e in data["edges"]:
        a, b = e["a"], e["b"]
        d = e.get("distance", _dist(coords[a], coords[b]))
        adj[a].append((b, d))
        adj[b].append((a, d))          # corridors are walkable both ways
    return coords, adj, types


def _dist(p, q):
    return math.hypot(p[0] - q[0], p[1] - q[1])


def _unit(p, q):
    dx, dy = q[0] - p[0], q[1] - p[1]
    m = math.hypot(dx, dy)
    return (dx / m, dy / m) if m else (0.0, 0.0)


def _bearing(p, q):
    """Map-space direction from p to q, degrees: 0 = +x (east), 90 = +y (north)."""
    return math.degrees(math.atan2(q[1] - p[1], q[0] - p[0])) % 360.0


def dijkstra(adj, start, goal):
    """Shortest path by total distance. Returns (path, distance), or (None, inf)."""
    best = {start: 0.0}
    prev = {}
    pq = [(0.0, start)]
    settled = set()

    while pq:
        d, u = heapq.heappop(pq)
        if u in settled:
            continue
        settled.add(u)
        if u == goal:
            break
        for v, w in adj.get(u, []):
            nd = d + w
            if nd < best.get(v, math.inf):
                best[v] = nd
                prev[v] = u
                heapq.heappush(pq, (nd, v))

    if goal not in best:
        return None, math.inf

    path = [goal]
    while path[-1] != start:
        path.append(prev[path[-1]])
    path.reverse()
    return path, best[goal]


def turn_at(coords, prev_node, node, next_node, straight_deg=30.0):
    """Which way you turn passing through `node`: straight, left, or right.

    Compares the direction you arrived on with the direction you leave on. A
    positive signed angle is a left turn (counter-clockwise), negative a right.
    Anything gentler than `straight_deg` counts as carrying straight on.
    """
    a = _unit(coords[prev_node], coords[node])
    b = _unit(coords[node], coords[next_node])
    cross = a[0] * b[1] - a[1] * b[0]
    dot = a[0] * b[0] + a[1] * b[1]
    angle = math.degrees(math.atan2(cross, dot))
    if abs(angle) <= straight_deg:
        return "straight"
    return "left" if angle > 0 else "right"


def build_route(coords, adj, start, destination):
    """Turn the shortest path into the app's ordered step list, or None."""
    path, _ = dijkstra(adj, start, destination)
    if not path or len(path) < 2:
        return None

    # One raw step per corridor. A step's direction is the turn made at the
    # node it leaves from; the first move has no prior heading, so it's straight.
    # Each step also carries the map-space bearing its corridor runs along, so
    # the client can point the AR arrows down the real corridor once aligned.
    raw = []
    for i in range(1, len(path)):
        leg = _dist(coords[path[i - 1]], coords[path[i]])
        if i == 1:
            direction = "straight"
        else:
            direction = turn_at(coords, path[i - 2], path[i - 1], path[i])
        raw.append({
            "direction": direction,
            "distance": leg,
            "bearing": _bearing(coords[path[i - 1]], coords[path[i]]),
        })

    # Fold consecutive straight legs into one ("go straight 12 m", not 6 + 6);
    # the merged step keeps the bearing it set off on.
    steps = []
    for s in raw:
        if steps and s["direction"] == "straight" and steps[-1]["direction"] == "straight":
            steps[-1]["distance"] += s["distance"]
        else:
            steps.append(dict(s))

    for s in steps:
        s["distance"] = round(s["distance"], 2)
        s["bearing"] = round(s["bearing"], 1)
    steps.append({"direction": "arrive", "distance": 0.0, "bearing": steps[-1]["bearing"] if steps else 0.0})
    return steps
