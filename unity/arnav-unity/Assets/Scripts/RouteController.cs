using UnityEngine;

// The navigation brain for the indoor pivot. Holds a route (a list of steps from the
// server's /init) and works out which step the user is on by measuring how far they've
// walked with ARCore's camera tracking — no GPS, no compass. Each step is a direction to
// take ("straight" / "left" / "right" / "arrive") and how many metres to walk on it.
public class RouteController : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        public string direction;
        public float distance;
        public float bearing;   // map-space direction of this leg (0 = map east, 90 = map north)
    }

    [System.Serializable]
    public class Route
    {
        public Step[] steps;
    }

    [Tooltip("Load a built-in test route on start, for trying it without the server or a QR.")]
    public bool useTestRoute = true;

    [Tooltip("Ignore per-frame camera jumps bigger than this (m) — AR relocalisation glitches.")]
    public float maxStepDelta = 0.5f;

    public bool HasRoute { get; private set; }
    public bool Arrived { get; private set; }
    public int StepIndex { get; private set; }
    public int StepCount => steps != null ? steps.Length : 0;
    public int RouteVersion { get; private set; }   // bumps on each LoadRoute, so the aligner re-arms

    // current instruction + how far is left on it (metres), for the chevrons and the HUD
    public string CurrentDirection =>
        (HasRoute && !Arrived && steps != null && StepIndex < steps.Length) ? steps[StepIndex].direction : "straight";
    public float CurrentBearing =>
        (HasRoute && !Arrived && steps != null && StepIndex < steps.Length) ? steps[StepIndex].bearing : 0f;
    public float RemainingDistance =>
        (HasRoute && !Arrived && steps != null && StepIndex < steps.Length)
            ? Mathf.Max(0f, steps[StepIndex].distance - walked) : 0f;

    Step[] steps;
    float walked;          // metres covered on the current step (displacement, see Update)
    Camera cam;
    MapAligner aligner;
    Vector3 stepStartPos;  // where the current leg began (horizontal anchor)
    Vector3 lastCamPos;
    bool hasAnchor;

    void Start()
    {
        cam = Camera.main;
        aligner = FindAnyObjectByType<MapAligner>();
        if (aligner == null) aligner = gameObject.AddComponent<MapAligner>();
        if (useTestRoute) LoadRoute(TestRoute());
    }

    // Hand in the steps list (from /init) to begin guiding.
    public void LoadRoute(Step[] s)
    {
        steps = s;
        StepIndex = 0;
        walked = 0f;
        Arrived = (s == null || s.Length == 0);
        HasRoute = !Arrived;
        hasAnchor = false;
        RouteVersion++;
        Debug.Log("RouteController: loaded " + (s != null ? s.Length : 0) + " steps");
    }

    public void LoadRouteJson(string json)
    {
        var r = JsonUtility.FromJson<Route>(json);
        LoadRoute(r != null ? r.steps : null);
    }

    void Update()
    {
        if (!HasRoute || Arrived) return;
        if (cam == null) { cam = Camera.main; if (cam == null) return; }

        // measure straight-line displacement from where this leg began, not path length —
        // legs are straight by design, and this way pacing/waving/spinning doesn't count
        Vector3 p = cam.transform.position;
        if (!hasAnchor)
        {
            stepStartPos = p;
            lastCamPos = p;
            hasAnchor = true;
        }

        // relocalisation jump: shift the anchor with it so displacement doesn't teleport
        Vector3 frameDelta = p - lastCamPos;
        frameDelta.y = 0f;
        if (frameDelta.magnitude > maxStepDelta) stepStartPos += frameDelta;
        lastCamPos = p;

        Vector3 fromStart = p - stepStartPos;
        fromStart.y = 0f;

        // once the map is aligned, only movement ALONG the corridor counts — walking the
        // wrong way makes no progress. Before alignment, plain displacement (as before).
        if (aligner != null && aligner.Aligned && steps != null && StepIndex < steps.Length)
            walked = Mathf.Max(0f, Vector3.Dot(fromStart, aligner.WorldDirFor(steps[StepIndex].bearing)));
        else
            walked = fromStart.magnitude;

        // advance past any steps now finished (re-anchor each time; handles a chained arrive too)
        while (!Arrived)
        {
            if (steps == null || StepIndex >= steps.Length) { Arrived = true; break; }
            Step cur = steps[StepIndex];
            if (cur.direction == "arrive") { Arrived = true; break; }
            if (walked >= cur.distance)
            {
                StepIndex++;
                stepStartPos = p;
                walked = 0f;
                continue;
            }
            break;
        }
    }

    // small home layout for testing without the server: straight 5 m, turn left, 4 m, arrive
    static Step[] TestRoute()
    {
        return new[]
        {
            new Step { direction = "straight", distance = 5f, bearing = 90f },
            new Step { direction = "left",     distance = 4f, bearing = 180f },
            new Step { direction = "arrive",   distance = 0f, bearing = 180f },
        };
    }
}
