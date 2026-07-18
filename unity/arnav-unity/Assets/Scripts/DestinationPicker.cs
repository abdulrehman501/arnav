using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Runtime-built panel: a start-node field (filled by the QR scan later, "ENTRANCE" for now)
// and a list of room buttons fetched from /rooms. Tap a room and it asks /init for the
// route and hands the steps to the RouteController. Auto-finds MapClient + RouteController.
public class DestinationPicker : MonoBehaviour
{
    public MapClient map;
    public RouteController route;

    [Tooltip("Start node — set by the QR scan once that's in; manual for now.")]
    public string startNode = "ENTRANCE";

    TMP_InputField startInput;
    TextMeshProUGUI status;
    RectTransform list;
    GameObject uiRoot;   // the picker's own canvas — hide this, not the shared GameObject

    void Start()
    {
        if (map == null) map = FindAnyObjectByType<MapClient>();
        if (route == null) route = FindAnyObjectByType<RouteController>();
        BuildUI();
        if (map != null) StartCoroutine(map.GetRooms(OnRooms));
    }

    void BuildUI()
    {
        var canvasGO = new GameObject("DestUI_Canvas");
        uiRoot = canvasGO;
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasGO.AddComponent<GraphicRaycaster>();

        // bottom panel
        var panel = NewRect("Panel", canvasGO.transform);
        var pimg = panel.gameObject.AddComponent<Image>();
        pimg.color = new Color(0f, 0f, 0f, 0.55f);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 40f);
        panel.sizeDelta = new Vector2(1000f, 620f);

        startInput = MakeInput("StartInput", panel, new Vector2(0f, 520f), "start node");
        startInput.text = startNode;
        startInput.onValueChanged.AddListener(v => startNode = v);

        status = MakeLabel("Status", panel, 30, "loading rooms…", TextAlignmentOptions.Center);
        var srt = status.rectTransform;
        srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0f);
        srt.pivot = new Vector2(0.5f, 0f);
        srt.anchoredPosition = new Vector2(0f, 470f);
        srt.sizeDelta = new Vector2(960f, 40f);

        // container the room buttons get stacked into
        list = NewRect("RoomList", panel);
        list.anchorMin = list.anchorMax = new Vector2(0.5f, 0f);
        list.pivot = new Vector2(0.5f, 0f);
        list.anchoredPosition = new Vector2(0f, 20f);
        list.sizeDelta = new Vector2(960f, 440f);
    }

    void OnRooms(string[] rooms)
    {
        if (rooms == null || rooms.Length == 0) { SetStatus("no rooms (server up?)"); return; }
        SetStatus("pick a destination");
        float y = 440f - 84f;
        foreach (var room in rooms)
        {
            MakeRoomButton(room, y);
            y -= 96f;
        }
    }

    void MakeRoomButton(string room, float y)
    {
        var rt = NewRect("Room_" + room, list);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(900f, 84f);

        var img = rt.gameObject.AddComponent<Image>();
        img.color = new Color(0.12f, 0.6f, 1f, 1f);
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var label = MakeLabel("Label", rt, 36, room, TextAlignmentOptions.Center);
        label.color = Color.white;
        Stretch(label.rectTransform);

        btn.onClick.AddListener(() => OnPick(room));
    }

    void OnPick(string room)
    {
        if (map == null || route == null) { SetStatus("missing MapClient/RouteController"); return; }
        SetStatus("routing to " + room + "…");
        StartCoroutine(map.Init(startNode, room, steps =>
        {
            if (steps == null || steps.Length == 0) { SetStatus("no route to " + room); return; }
            route.LoadRoute(steps);
            SetStatus("");
            if (uiRoot != null) uiRoot.SetActive(false);   // hide only the picker UI, not the shared GameObject (RouteController lives here too)
        }));
    }

    // --- tiny UI helpers (same pattern as the old TargetSender) ---

    TMP_InputField MakeInput(string name, RectTransform parent, Vector2 pos, string placeholder)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900f, 80f);

        rt.gameObject.AddComponent<Image>().color = Color.white;
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

    void SetStatus(string s) { if (status != null) status.text = s; }
}
