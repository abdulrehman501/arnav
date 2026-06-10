using UnityEngine;

// Spawns a row of chevrons receding forward, steers the row by the /nav
// direction, and scrolls them toward the user for a flowing path effect.
public class ChevronPath : MonoBehaviour
{
    public NavClient nav;
    public GameObject chevronPrefab;
    public int count = 6;
    public float spacing = 0.5f;
    public float turnSpeed = 5f;
    public float scrollSpeed = 0.4f;

    Transform[] chevrons;

    void Start()
    {
        chevrons = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            var c = Instantiate(chevronPrefab, transform);
            c.transform.localPosition = new Vector3(0f, 0f, i * spacing);
            c.transform.localRotation = Quaternion.identity;
            chevrons[i] = c.transform;
        }
    }

    static float TargetYaw(string dir)
    {
        switch (dir)
        {
            case "left":  return -90f;
            case "right": return  90f;
            default:      return   0f;   // straight / arrive
        }
    }

    void Update()
    {
        // steer the whole path toward the current direction
        if (nav != null && nav.HasData && nav.Latest != null)
        {
            Quaternion target = Quaternion.Euler(0f, TargetYaw(nav.Latest.direction), 0f);
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation, target, turnSpeed * Time.deltaTime);
        }

        // scroll the chevrons toward the user, recycling the closest to the back
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
