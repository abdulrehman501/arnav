using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Polls Sami's /nav endpoint and exposes the latest navigation result.
public class NavClient : MonoBehaviour
{
    [Tooltip("Sami's current ngrok base URL (changes every restart)")]
    public string baseUrl = "https://tastiness-silly-strife.ngrok-free.dev";

    public float pollInterval = 0.5f;

    [System.Serializable]
    public class NavData
    {
        public float heading;
        public float pitch;
        public float roll;
        public float distance;
        public float bearing;
        public string direction;
    }

    public NavData Latest { get; private set; }
    public bool HasData { get; private set; }

    IEnumerator Start()
    {
        var wait = new WaitForSeconds(pollInterval);
        while (true)
        {
            yield return GetNav();
            yield return wait;
        }
    }

    IEnumerator GetNav()
    {
        using (var req = UnityWebRequest.Get(baseUrl + "/nav"))
        {
            req.SetRequestHeader("ngrok-skip-browser-warning", "true");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Latest = JsonUtility.FromJson<NavData>(req.downloadHandler.text);
                HasData = true;
                Debug.Log("nav -> " + req.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning("nav fetch failed: " + req.error);
            }
        }
    }
}
