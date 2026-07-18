using UnityEngine;
using TMPro;

// Runtime-built 2D overlay. Reads the RouteController and shows the current instruction
// ("GO STRAIGHT 12 m" / "TURN LEFT 4 m" / "ARRIVED") plus a small step counter, so the
// guidance is readable even when the chevrons are off-screen. Builds its own Canvas.
public class NavHUD : MonoBehaviour
{
    public RouteController route;

    TextMeshProUGUI dirText;
    TextMeshProUGUI stepText;

    static readonly Color white = Color.white;
    static readonly Color green = new Color(0.2f, 1f, 0.4f);

    void Start()
    {
        if (route == null) route = FindAnyObjectByType<RouteController>();
        BuildUI();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("NavHUD_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // top: the turn instruction + remaining distance
        dirText = MakeText("DirText", canvasGO.transform, 80);
        var d = dirText.rectTransform;
        d.anchorMin = d.anchorMax = new Vector2(0.5f, 1f);
        d.pivot = new Vector2(0.5f, 1f);
        d.anchoredPosition = new Vector2(0f, -140f);
        d.sizeDelta = new Vector2(1000f, 200f);

        // just under it: step counter
        stepText = MakeText("StepText", canvasGO.transform, 40);
        stepText.color = new Color(0.8f, 0.9f, 1f);
        var s = stepText.rectTransform;
        s.anchorMin = s.anchorMax = new Vector2(0.5f, 1f);
        s.pivot = new Vector2(0.5f, 1f);
        s.anchoredPosition = new Vector2(0f, -340f);
        s.sizeDelta = new Vector2(1000f, 80f);
    }

    TextMeshProUGUI MakeText(string n, Transform parent, float size)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        t.text = "";
        return t;
    }

    static string DirLabel(string d)
    {
        switch (d)
        {
            case "left":     return "TURN LEFT";
            case "right":    return "TURN RIGHT";
            case "straight": return "GO STRAIGHT";
            case "arrive":   return "ARRIVED";
            default:         return "";
        }
    }

    void Update()
    {
        if (dirText == null) return;

        if (route == null || !route.HasRoute)
        {
            dirText.text = "";
            stepText.text = route == null ? "" : "pick a destination";
            return;
        }

        if (route.Arrived)
        {
            dirText.text = "ARRIVED";
            dirText.color = green;
            stepText.text = "";
            return;
        }

        dirText.color = white;
        dirText.text = DirLabel(route.CurrentDirection) + "   " + Mathf.CeilToInt(route.RemainingDistance) + " m";
        stepText.text = "step " + (route.StepIndex + 1) + " / " + route.StepCount;
    }
}
