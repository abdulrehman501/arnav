using UnityEngine;
using TMPro;

// Shows the latest /nav distance as on-screen text, refreshed as new data arrives.
public class DistanceLabel : MonoBehaviour
{
    public NavClient nav;
    public TMP_Text label;

    void Update()
    {
        if (nav == null || label == null || !nav.HasData || nav.Latest == null)
            return;

        label.text = Mathf.RoundToInt(nav.Latest.distance) + " m";
    }
}
