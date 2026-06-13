using System.Collections;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Runtime-built panel: two numeric input fields (lat, lon) + a confirm button.
// On press, POSTs {"lat":..,"lon":..} as JSON to the backend's /target endpoint,
// letting the user change destination without touching code. A status line reports
// the result. Attach to any GameObject - it auto-finds NavClient for the base URL.
public class TargetSender : MonoBehaviour
{
    public NavClient nav;

    TMP_InputField latInput;
    TMP_InputField lonInput;
    TextMeshProUGUI status;

    void Start()
    {
        if (nav == null) nav = FindFirstObjectByType<NavClient>();
        BuildUI();
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("TargetUI_Canvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGO.AddComponent<GraphicRaycaster>();

        // translucent panel along the bottom
        var panel = NewRect("Panel", canvasGO.transform);
        var pimg = panel.gameObject.AddComponent<Image>();
        pimg.color = new Color(0f, 0f, 0f, 0.55f);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 40f);
        panel.sizeDelta = new Vector2(1000f, 340f);

        latInput = MakeInput("LatInput", panel, new Vector2(0f, 230f), "latitude");
        lonInput = MakeInput("LonInput", panel, new Vector2(0f, 140f), "longitude");

        // confirm button
        var btnRect = NewRect("SetBtn", panel);
        var bimg = btnRect.gameObject.AddComponent<Image>();
        bimg.color = new Color(0.12f, 0.6f, 1f, 1f);
        var btn = btnRect.gameObject.AddComponent<Button>();
        btn.targetGraphic = bimg;
        btnRect.anchorMin = btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 40f);
        btnRect.sizeDelta = new Vector2(460f, 84f);
        var btnLabel = MakeLabel("BtnText", btnRect, 38, "SET TARGET", TextAlignmentOptions.Center);
        btnLabel.color = Color.white;
        Stretch(btnLabel.rectTransform);
        btn.onClick.AddListener(OnSet);

        // status line above the panel
        status = MakeLabel("Status", panel, 30, "", TextAlignmentOptions.Center);
        status.color = Color.yellow;
        var srt = status.rectTransform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -6f);
        srt.sizeDelta = new Vector2(960f, 40f);
    }

    TMP_InputField MakeInput(string name, RectTransform parent, Vector2 pos, string placeholder)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900f, 80f);

        var bg = rt.gameObject.AddComponent<Image>();
        bg.color = Color.white;

        var input = rt.gameObject.AddComponent<TMP_InputField>();

        var area = NewRect("Text Area", rt);
        area.gameObject.AddComponent<RectMask2D>();
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(20f, 8f); area.offsetMax = new Vector2(-20f, -8f);

        var ph = MakeLabel("Placeholder", area, 34, placeholder, TextAlignmentOptions.MidlineLeft);
        ph.color = new Color(0.45f, 0.45f, 0.45f);
        Stretch(ph.rectTransform);

        var txt = MakeLabel("Text", area, 34, "", TextAlignmentOptions.MidlineLeft);
        txt.color = Color.black;
        Stretch(txt.rectTransform);

        input.textViewport = area;
        input.textComponent = txt;
        input.placeholder = ph;
        input.contentType = TMP_InputField.ContentType.DecimalNumber;
        input.text = "";
        return input;
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static TextMeshProUGUI MakeLabel(string name, Transform parent, float size, string text, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.text = text;
        t.alignment = align;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void OnSet()
    {
        if (nav == null) { SetStatus("no NavClient found"); return; }
        if (!float.TryParse(latInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float lat) ||
            !float.TryParse(lonInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float lon))
        {
            SetStatus("enter valid lat & lon");
            return;
        }
        StartCoroutine(PostTarget(lat, lon));
    }

    IEnumerator PostTarget(float lat, float lon)
    {
        SetStatus("sending...");
        string json = "{\"lat\":" + lat.ToString(CultureInfo.InvariantCulture) +
                      ",\"lon\":" + lon.ToString(CultureInfo.InvariantCulture) + "}";
        using (var req = new UnityWebRequest(nav.baseUrl + "/target", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("ngrok-skip-browser-warning", "true");
            req.timeout = 10;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                SetStatus("target set ✓");
                Debug.Log("target -> " + json + " | resp: " + req.downloadHandler.text);
            }
            else
            {
                SetStatus("failed: " + req.error);
                Debug.LogWarning("target post failed: " + req.error);
            }
        }
    }

    void SetStatus(string s) { if (status != null) status.text = s; }
}
