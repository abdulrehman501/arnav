using UnityEngine;

// Points the arrow based on the direction field coming from NavClient.
public class ArrowController : MonoBehaviour
{
    public NavClient nav;
    public Transform arrow;        // the object to rotate (the arrow / placeholder cube)
    public float turnSpeed = 5f;

    // yaw (in degrees) for each instruction, relative to forward
    static float TargetYaw(string direction)
    {
        switch (direction)
        {
            case "left":  return -90f;
            case "right": return  90f;
            default:      return   0f;   // straight / arrive
        }
    }

    string lastDir;

    void Start()
    {
        Debug.Log("ArrowController: nav set=" + (nav != null) + ", arrow set=" + (arrow != null));
    }

    void Update()
    {
        if (nav == null || arrow == null || !nav.HasData || nav.Latest == null)
            return;

        string dir = nav.Latest.direction;

        if (dir != lastDir)
        {
            Debug.Log("arrow dir -> '" + dir + "' yaw " + TargetYaw(dir));
            lastDir = dir;
        }

        // hide the arrow once we've arrived
        arrow.gameObject.SetActive(dir != "arrive");
        if (dir == "arrive")
            return;

        Quaternion target = Quaternion.Euler(0f, TargetYaw(dir), 0f);
        arrow.localRotation = Quaternion.Slerp(
            arrow.localRotation, target, turnSpeed * Time.deltaTime);
    }
}
