using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Grabs the AR camera's CPU image every few seconds, JPG-encodes it, and POSTs it to
// the backend's /frame endpoint (for server-side depth estimation). The server stores
// the depth and feeds it back through /nav, so Unity only needs to send the frames.
// Attach to any GameObject - it auto-finds NavClient (for baseUrl) and ARCameraManager.
public class FrameUploader : MonoBehaviour
{
    public NavClient nav;
    public ARCameraManager cameraManager;
    public float interval = 2.5f;     // seconds between frames
    public int downscale = 2;         // 1 = full res, 2 = half, etc.
    [Range(1, 100)] public int jpgQuality = 50;

    bool uploading;   // guard: skip a frame if the previous POST is still in flight

    void Start()
    {
        if (nav == null) nav = FindFirstObjectByType<NavClient>();
        if (cameraManager == null) cameraManager = FindFirstObjectByType<ARCameraManager>();
        Debug.Log("FrameUploader: started, cameraManager=" + (cameraManager != null) + " nav=" + (nav != null));
        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        var wait = new WaitForSeconds(interval);
        while (true)
        {
            yield return wait;
            UploadFrame();
        }
    }

    void UploadFrame()
    {
        if (cameraManager == null || nav == null) return;
        if (uploading) return;   // previous upload still going - don't pile up over the tunnel
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            Debug.Log("FrameUploader: no CPU image available yet");
            return;
        }

        byte[] jpg = null;
        try
        {
            var cp = new XRCpuImage.ConversionParams(image, TextureFormat.RGBA32,
                                                     XRCpuImage.Transformation.MirrorY);
            cp.outputDimensions = new Vector2Int(Mathf.Max(1, image.width / downscale),
                                                 Mathf.Max(1, image.height / downscale));

            int size = image.GetConvertedDataSize(cp);
            using (var buffer = new NativeArray<byte>(size, Allocator.Temp))
            {
                image.Convert(cp, buffer);
                var tex = new Texture2D(cp.outputDimensions.x, cp.outputDimensions.y,
                                        cp.outputFormat, false);
                tex.LoadRawTextureData(buffer);
                tex.Apply();
                jpg = tex.EncodeToJPG(jpgQuality);
                Destroy(tex);
            }
        }
        finally
        {
            image.Dispose();
        }

        if (jpg != null) { uploading = true; StartCoroutine(Post(jpg)); }
    }

    IEnumerator Post(byte[] jpg)
    {
        var form = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("file", jpg, "frame.jpg", "image/jpeg")
        };
        using (var req = UnityWebRequest.Post(nav.baseUrl + "/frame", form))
        {
            req.SetRequestHeader("ngrok-skip-browser-warning", "true");
            req.timeout = 10;   // never let a hung request leave 'uploading' stuck true
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                Debug.Log("frame -> " + req.downloadHandler.text);   // expect {"depth": ...}
            else
                Debug.LogWarning("frame post failed: " + req.error);
        }
        uploading = false;
    }
}
