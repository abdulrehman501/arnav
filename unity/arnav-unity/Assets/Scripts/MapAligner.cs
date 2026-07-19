using UnityEngine;

// Learns how the floor-plan map is rotated relative to ARCore's world, the same way
// car nav infers heading: from movement. The route's first leg has a known map bearing;
// after the user has walked a couple of metres, the direction they actually moved is
// that same corridor in world space. One yaw offset ties the two frames together, and
// from then on any map bearing can be turned into a real world direction.
public class MapAligner : MonoBehaviour
{
    [Tooltip("Metres of walking used to infer the map-to-world rotation.")]
    public float alignDistance = 2.0f;

    [Tooltip("Show the on-screen alignment readout (testing only).")]
    public bool debugHud = true;

    public bool Aligned { get; private set; }
    public float YawOffset { get; private set; }

    RouteController route;
    Camera cam;
    Vector3 startPos;
    bool armed;
    int seenRouteVersion = -1;

    void Start()
    {
        route = FindAnyObjectByType<RouteController>();
        cam = Camera.main;
    }

    // Map bearing (0 = map east, 90 = map north, counter-clockwise) -> Unity world yaw.
    public float WorldYawFor(float bearing) => (90f - bearing) + YawOffset;

    public Vector3 WorldDirFor(float bearing) =>
        Quaternion.Euler(0f, WorldYawFor(bearing), 0f) * Vector3.forward;

    void Update()
    {
        if (route == null || cam == null) { if (cam == null) cam = Camera.main; return; }

        // a new route re-arms the measurement from wherever the user now stands
        if (route.RouteVersion != seenRouteVersion && route.HasRoute)
        {
            seenRouteVersion = route.RouteVersion;
            startPos = cam.transform.position;
            Aligned = false;
            armed = true;
        }

        if (!armed || Aligned || !route.HasRoute || route.Arrived) return;

        Vector3 moved = cam.transform.position - startPos;
        moved.y = 0f;
        if (moved.magnitude < alignDistance) return;

        // walked direction in world = the first leg's corridor; solve for the offset
        float walkedYaw = Mathf.Atan2(moved.x, moved.z) * Mathf.Rad2Deg;
        YawOffset = Mathf.DeltaAngle(90f - route.CurrentBearing, walkedYaw);
        Aligned = true;
        Debug.Log($"MapAligner: locked, yawOffset={YawOffset:F1} (walkedYaw={walkedYaw:F1}, bearing={route.CurrentBearing:F1})");
    }

    void OnGUI()
    {
        if (!debugHud) return;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 34 };
        style.normal.textColor = Aligned ? Color.green : Color.yellow;
        string line = Aligned
            ? $"ALIGNED  offset {YawOffset:F0}°  bearing {(route != null ? route.CurrentBearing : 0f):F0}°"
            : "aligning… walk forward";
        GUI.Label(new Rect(20, 130, 900, 50), line, style);
    }
}
