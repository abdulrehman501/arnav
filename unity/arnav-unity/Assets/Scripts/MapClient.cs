using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Talks to the indoor-nav server. Two calls: fetch the room list, and ask for a route
// between two nodes. Stateless on purpose — we pull the steps once from /init and let
// RouteController walk them locally, so a network blip mid-walk doesn't stop navigation.
public class MapClient : MonoBehaviour
{
    [Tooltip("Server base URL (ngrok tunnel) — set per deployment")]
    public string baseUrl = "https://your-ngrok-domain.ngrok-free.dev";

    // Some networks (carrier NAT, Wi-Fi with TLS inspection) break Unity's TLS
    // handshake even though browsers cope. The tunnel serves both schemes, so try
    // https first and drop to plain http if it fails — the route data is
    // non-sensitive. Whichever scheme answers is reused for every later call.
    string workingBaseUrl;

    // Accept the tunnel's certificate ourselves: some Android system cert stores
    // reject chains the phone's own browser accepts.
    class AcceptServerCert : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }

    string[] CandidateUrls()
    {
        if (workingBaseUrl != null) return new[] { workingBaseUrl };
        if (baseUrl.StartsWith("https://"))
            return new[] { baseUrl, "http://" + baseUrl.Substring("https://".Length) };
        return new[] { baseUrl };
    }

    [System.Serializable] class RoomList { public string[] items; }

    // GET /rooms -> ["ROOM_201", "ROOM_205", ...]
    public IEnumerator GetRooms(Action<string[]> onResult)
    {
        foreach (string root in CandidateUrls())
        {
            using (var req = UnityWebRequest.Get(root + "/rooms"))
            {
                req.certificateHandler = new AcceptServerCert();
                req.SetRequestHeader("ngrok-skip-browser-warning", "true");
                req.timeout = 10;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    workingBaseUrl = root;
                    // /rooms is a bare JSON array; wrap it so JsonUtility can parse it
                    string wrapped = "{\"items\":" + req.downloadHandler.text + "}";
                    var list = JsonUtility.FromJson<RoomList>(wrapped);
                    onResult?.Invoke(list != null && list.items != null ? list.items : new string[0]);
                    Debug.Log("rooms (" + root + ") -> " + req.downloadHandler.text);
                    yield break;
                }
                Debug.LogWarning("rooms fetch failed (" + root + "): " + req.error);
            }
        }
        onResult?.Invoke(new string[0]);
    }

    // POST /init {start, destination} -> { "steps": [ {direction, distance}, ... ] }
    public IEnumerator Init(string start, string destination, Action<RouteController.Step[]> onResult)
    {
        string json = "{\"start\":\"" + start + "\",\"destination\":\"" + destination + "\"}";
        foreach (string root in CandidateUrls())
        {
            using (var req = new UnityWebRequest(root + "/init", "POST"))
            {
                req.certificateHandler = new AcceptServerCert();
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("ngrok-skip-browser-warning", "true");
                req.timeout = 10;
                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    workingBaseUrl = root;
                    var route = JsonUtility.FromJson<RouteController.Route>(req.downloadHandler.text);
                    onResult?.Invoke(route != null ? route.steps : null);
                    Debug.Log("init -> " + req.downloadHandler.text);
                    yield break;
                }
                Debug.LogWarning("init failed (" + root + "): " + req.error);
            }
        }
        onResult?.Invoke(null);
    }
}
