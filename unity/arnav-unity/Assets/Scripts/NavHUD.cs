using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Runtime-built 2D overlay HUD. Builds its own Canvas (no manual wiring), reads NavClient and shows:
//  1. a persistent turn instruction ("TURN LEFT" / "GO STRAIGHT" / ...) so guidance is visible
//     even when the chevrons are off-screen,
//  2. a red border vignette + warning when the backend reports obstacle == true,
//  3. an amber "stale" state / "Reconnecting" banner when /nav stops responding (ngrok drop).
// Attach this to the same GameObject as NavClient (it auto-finds it).
public class NavHUD : MonoBehaviour
{
    public NavClient nav;

    TextMeshProUGUI dirText;
    TextMeshProUGUI obstacleText;
    Image vignette;

    static readonly Color white = Color.white;
    static readonly Color amber = new Color(1f, 0.65f, 0f);

    void Start()
    {
        if (nav == null) nav = FindFirstObjectByType<NavClient>();
        BuildUI();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("NavHUD_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGO.AddComponent<GraphicRaycaster>();

        // red border vignette (transparent centre, red edges), hidden until an obstacle appears
        var vigGO = new GameObject("Vignette");
        vigGO.transform.SetParent(canvasGO.transform, false);
        vignette = vigGO.AddComponent<Image>();
        vignette.sprite = MakeVignetteSprite();
        vignette.type = Image.Type.Simple;
        vignette.raycastTarget = false;
        vignette.color = new Color(1f, 0f, 0f, 0f);
        Stretch(vignette.rectTransform);

        // top: turn instruction
        dirText = MakeText("DirText", canvasGO.transform, 72);
        var d = dirText.rectTransform;
        d.anchorMin = d.anchorMax = new Vector2(0.5f, 1f);
        d.pivot = new Vector2(0.5f, 1f);
        d.anchoredPosition = new Vector2(0f, -140f);
        d.sizeDelta = new Vector2(1000f, 180f);

        // bottom: obstacle warning
        obstacleText = MakeText("ObstacleText", canvasGO.transform, 60);
        obstacleText.color = new Color(1f, 0.35f, 0.2f);
        var o = obstacleText.rectTransform;
        o.anchorMin = o.anchorMax = new Vector2(0.5f, 0f);
        o.pivot = new Vector2(0.5f, 0f);
        o.anchoredPosition = new Vector2(0f, 240f);
        o.sizeDelta = new Vector2(1000f, 120f);
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

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Sprite MakeVignetteSprite()
    {
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var c = new Vector2(s * 0.5f, s * 0.5f);
        float maxD = c.magnitude;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxD; // 0 centre .. 1 corner
                float a = Mathf.Clamp01((d - 0.55f) / 0.45f);           // clear centre, red edges
                tex.SetPixel(x, y, new Color(1f, 0f, 0f, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
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
        if (nav == null || !nav.HasData || nav.Latest == null)
        {
            if (dirText != null) dirText.text = "";
            return;
        }

        // turn instruction + stale / reconnect state
        if (nav.IsStale && nav.StaleSeconds > 4f)        // ~8 missed polls
        {
            dirText.text = "Reconnecting to backend…";
            dirText.color = amber;
        }
        else
        {
            dirText.text = DirLabel(nav.Latest.direction);
            dirText.color = nav.IsStale ? amber : white;
        }

        // obstacle vignette
        if (nav.Latest.obstacle)
        {
            float a = Mathf.Lerp(0.18f, 0.55f, Mathf.PingPong(Time.time * 2f, 1f));
            vignette.color = new Color(1f, 0f, 0f, a);
            obstacleText.text = "OBSTACLE AHEAD";
        }
        else
        {
            vignette.color = new Color(1f, 0f, 0f, 0f);
            obstacleText.text = "";
        }
    }
}
