using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

// Lays a row of chevrons on the real detected floor, in front of the user,
// pointing the way and leaning by the RouteController's current step direction.
// World-anchored to the ground via AR raycast, so it stays on the surface.
public class ChevronPath : MonoBehaviour
{
    public RouteController route;
    public GameObject chevronPrefab;
    public int count = 6;
    public float spacing = 0.5f;
    public float turnSpeed = 1.5f;
    public float scrollSpeed = 0.4f;

    Transform[] chevrons;
    ARRaycastManager raycaster;
    Camera cam;
    MapAligner aligner;
    static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    readonly List<Renderer> rends = new List<Renderer>();
    string lastColorDir = "";
    static readonly Color onCourse  = new Color(0.12f, 1f, 0.35f);  // green  = on path
    static readonly Color offCourse = new Color(1f, 0.25f, 0.12f);  // red    = drifting, correct

    void Start()
    {
        cam = Camera.main;
        if (route == null) route = FindAnyObjectByType<RouteController>();
        aligner = FindAnyObjectByType<MapAligner>();

        raycaster = FindAnyObjectByType<ARRaycastManager>();
        if (raycaster == null)
        {
            var origin = FindAnyObjectByType<XROrigin>();
            if (origin != null) raycaster = origin.gameObject.AddComponent<ARRaycastManager>();
        }

        // detach from the camera so the path lives in world space, not glued to the screen
        transform.SetParent(null, true);

        // hide the detected-plane visuals (the white dots) so they don't bury the chevrons;
        // detection still runs for the raycast, we just stop rendering the dots/feather.
        var planeManager = FindAnyObjectByType<ARPlaneManager>();
        if (planeManager != null)
        {
            planeManager.planePrefab = null;
            foreach (var plane in planeManager.trackables)
                foreach (var rend in plane.GetComponentsInChildren<Renderer>())
                    rend.enabled = false;
        }

        chevrons = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            var c = Instantiate(chevronPrefab, transform);
            c.transform.localPosition = new Vector3(0f, 0f, i * spacing);
            c.transform.localRotation = Quaternion.identity;
            chevrons[i] = c.transform;
            rends.AddRange(c.GetComponentsInChildren<Renderer>());
        }
        Debug.Log("ChevronPath: spawned " + count + ", raycaster=" + (raycaster != null));
    }

    static float TargetYaw(string dir)
    {
        switch (dir)
        {
            case "left":  return -20f;
            case "right": return  20f;
            default:      return   0f;
        }
    }

    void ApplyColor(Color col)
    {
        foreach (var r in rends)
        {
            if (r == null) continue;
            var m = r.material;
            m.SetColor("_BaseColor", col);
            m.SetColor("_Color", col);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", col * 1.5f);
        }
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;

        // colour feedback: green going straight / arrived, red at a turn (lean to that side)
        string cdir = (route != null && route.HasRoute && !route.Arrived) ? route.CurrentDirection : "straight";
        if (cdir != lastColorDir)
        {
            lastColorDir = cdir;
            bool turn = (cdir == "left" || cdir == "right");
            ApplyColor(turn ? offCourse : onCourse);
        }

        // place the whole path on the floor straight ahead of the user
        if (raycaster != null && cam != null)
        {
            var screenPt = new Vector2(Screen.width * 0.5f, Screen.height * 0.42f);
            if (raycaster.Raycast(screenPt, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hit = hits[0].pose;
                transform.position = hit.position + Vector3.up * 0.06f;

                bool navigating = route != null && route.HasRoute && !route.Arrived;
                Quaternion target;
                if (navigating && aligner == null) aligner = FindAnyObjectByType<MapAligner>();
                if (navigating && aligner != null && aligner.Aligned)
                {
                    // map is aligned: the arrows point down the REAL corridor for this leg,
                    // and stay pointing there no matter which way the phone is turned
                    target = Quaternion.Euler(0f, aligner.WorldYawFor(route.CurrentBearing), 0f);
                }
                else
                {
                    // not aligned yet: previous behaviour — ahead of the camera, leaning into turns
                    Vector3 fwd = cam.transform.forward;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
                    Quaternion baseRot = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                    float yaw = navigating ? TargetYaw(route.CurrentDirection) : 0f;
                    target = baseRot * Quaternion.Euler(0f, yaw, 0f);
                }
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
            }
        }

        // scroll the chevrons forward along the path
        if (chevrons == null) return;
        float loop = count * spacing;
        foreach (var c in chevrons)
        {
            if (c == null) continue;
            Vector3 p = c.localPosition;
            p.z -= scrollSpeed * Time.deltaTime;
            if (p.z < 0f) p.z += loop;
            c.localPosition = p;
        }
    }
}
