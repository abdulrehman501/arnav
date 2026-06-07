using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

// Reads phone sensors and POSTs them to Sami's /sensors endpoint once a second.
// Uses the new Input System (the project's active input handler).
public class SensorUploader : MonoBehaviour
{
    [Tooltip("Sami's current ngrok base URL (changes every restart)")]
    public string baseUrl = "https://tastiness-silly-strife.ngrok-free.dev";

    public float sendInterval = 1f;

    [System.Serializable]
    class Payload
    {
        public float[] accel;
        public float[] mag;
        public float[] gps;
    }

    IEnumerator Start()
    {
        // New Input System sensors are off by default — enable the ones we have.
        if (Accelerometer.current != null)
            InputSystem.EnableDevice(Accelerometer.current);
        if (MagneticFieldSensor.current != null)
            InputSystem.EnableDevice(MagneticFieldSensor.current);

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            Permission.RequestUserPermission(Permission.FineLocation);
#endif

        Input.location.Start();
        int tries = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && tries-- > 0)
            yield return new WaitForSeconds(1f);

        StartCoroutine(SendLoop());
    }

    IEnumerator SendLoop()
    {
        var wait = new WaitForSeconds(sendInterval);
        while (true)
        {
            yield return StartCoroutine(PostSensors());
            yield return wait;
        }
    }

    IEnumerator PostSensors()
    {
        Vector3 a = Accelerometer.current != null
            ? Accelerometer.current.acceleration.ReadValue() : Vector3.zero;
        Vector3 m = MagneticFieldSensor.current != null
            ? MagneticFieldSensor.current.magneticField.ReadValue() : Vector3.zero;

        float lat = 0f, lon = 0f;
        if (Input.location.status == LocationServiceStatus.Running)
        {
            lat = Input.location.lastData.latitude;
            lon = Input.location.lastData.longitude;
        }

        var payload = new Payload
        {
            accel = new[] { a.x, a.y, a.z },
            mag   = new[] { m.x, m.y, m.z },
            gps   = new[] { lat, lon }
        };

        string json = JsonUtility.ToJson(payload);

        using (var req = new UnityWebRequest(baseUrl + "/sensors", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("ngrok-skip-browser-warning", "true");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                Debug.Log("sensors sent " + json + " -> " + req.downloadHandler.text);
            else
                Debug.LogWarning("sensor post failed: " + req.error);
        }
    }
}
