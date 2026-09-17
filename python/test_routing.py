"""Tests for the routing logic. Small hand-checked graphs, plus the real map.

Run from the python/ folder:  python -m pytest
"""
from pathlib import Path

import routing


def graph(nodes, edges):
    data = {
        "nodes": {n: {"x": p[0], "y": p[1], "type": "room"} for n, p in nodes.items()},
        "edges": [{"a": a, "b": b} for a, b in edges],
    }
    return routing.build_graph(data)


def test_straight_line_merges_into_one_step():
    coords, adj, _ = graph(
        {"A": (0, 0), "B": (0, 3), "C": (0, 8)},
        [("A", "B"), ("B", "C")],
    )
    path, dist = routing.dijkstra(adj, "A", "C")
    assert path == ["A", "B", "C"]
    assert dist == 8.0
    assert routing.build_route(coords, adj, "A", "C") == [
        {"direction": "straight", "distance": 8.0, "bearing": 90.0},
        {"direction": "arrive", "distance": 0.0, "bearing": 90.0},
    ]


def test_right_turn():
    # north, then east = a right turn
    coords, adj, _ = graph(
        {"A": (0, 0), "B": (0, 5), "C": (4, 5)},
        [("A", "B"), ("B", "C")],
    )
    steps = routing.build_route(coords, adj, "A", "C")
    assert steps[0] == {"direction": "straight", "distance": 5.0, "bearing": 90.0}
    assert steps[1] == {"direction": "right", "distance": 4.0, "bearing": 0.0}
    assert steps[-1]["direction"] == "arrive"


def test_left_turn():
    # north, then west = a left turn
    coords, adj, _ = graph(
        {"A": (0, 0), "B": (0, 5), "C": (-4, 5)},
        [("A", "B"), ("B", "C")],
    )
    assert routing.build_route(coords, adj, "A", "C")[1]["direction"] == "left"


def test_diamond_two_equal_paths():
    # both ways round a unit square are length 2 — either is a valid answer
    coords, adj, _ = graph(
        {"A": (0, 0), "B": (0, 1), "C": (1, 0), "D": (1, 1)},
        [("A", "B"), ("B", "D"), ("A", "C"), ("C", "D")],
    )
    path, dist = routing.dijkstra(adj, "A", "D")
    assert dist == 2.0
    assert path in (["A", "B", "D"], ["A", "C", "D"])


def test_picks_the_shorter_of_two_routes():
    # the real correctness check: a short way (13) and a long way (22) to G
    coords, adj, _ = graph(
        {"S": (0, 0), "M": (0, 5), "NEAR": (0, 10),
         "FAR1": (10, 5), "FAR2": (10, 10), "G": (3, 10)},
        [("S", "M"), ("M", "NEAR"), ("NEAR", "G"),
         ("M", "FAR1"), ("FAR1", "FAR2"), ("FAR2", "G")],
    )
    path, dist = routing.dijkstra(adj, "S", "G")
    assert path == ["S", "M", "NEAR", "G"]
    assert round(dist, 2) == 13.0


def test_unreachable_returns_none():
    coords, adj, _ = graph(
        {"A": (0, 0), "B": (0, 1), "X": (99, 99)},
        [("A", "B")],
    )
    assert routing.build_route(coords, adj, "A", "X") is None


def test_real_floor_plan_takes_the_direct_route():
    coords, adj, _ = routing.load_floor_plan(
        Path(__file__).parent / "data" / "floor_plan.json"
    )
    path, dist = routing.dijkstra(adj, "ENTRANCE", "ROOM_102")
    assert path == ["ENTRANCE", "ROOM_101", "ROOM_102"]
    assert round(dist, 2) == 5.0
    steps = routing.build_route(coords, adj, "ENTRANCE", "ROOM_102")
    assert steps[0] == {"direction": "straight", "distance": 2.5, "bearing": 90.0}
    assert steps[1] == {"direction": "right", "distance": 2.5, "bearing": 0.0}
    assert steps[-1] == {"direction": "arrive", "distance": 0.0, "bearing": 0.0}
