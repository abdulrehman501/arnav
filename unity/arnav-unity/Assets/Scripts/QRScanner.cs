using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using ZXing.Common;

// Reads QR codes off the AR camera feed. It pulls the frame from the same CPU image
// ARCore already produces (ARCameraManager), so it shares the camera with AR tracking
// instead of fighting it. Fires OnScanned once with the decoded text, then stops.
public class QRScanner : MonoBehaviour
{
    public Action<string> OnScanned;

    ARCameraManager cameraManager;
    readonly BarcodeReaderGeneric reader = new BarcodeReaderGeneric();
    Texture2D scanTex;
    float nextScan;
    bool active = true;

    void Awake()
    {
        cameraManager = FindAnyObjectByType<ARCameraManager>();
    }

    public void Resume() { active = true; }
    public void Stop()   { active = false; }

    void Update()
    {
        if (!active || cameraManager == null || Time.time < nextScan) return;
        nextScan = Time.time + 0.5f;                 // decode twice a second, not every frame
        Scan();
    }

    void Scan()
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return;

        int w = image.width / 2;                     // half-size is plenty to read a QR and cheaper
        int h = image.height / 2;
        if (scanTex == null || scanTex.width != w || scanTex.height != h)
            scanTex = new Texture2D(w, h, TextureFormat.RGB24, false);

        var param = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(w, h),
            outputFormat = TextureFormat.RGB24
        };
        image.Convert(param, scanTex.GetRawTextureData<byte>());
        image.Dispose();
        scanTex.Apply();

        var source = new RGBLuminanceSource(
            scanTex.GetRawTextureData(), w, h, RGBLuminanceSource.BitmapFormat.RGB24);
        var result = reader.Decode(source);
        if (result == null) return;

        active = false;
        Debug.Log("QR decoded: " + result.Text);
        OnScanned?.Invoke(result.Text);
    }
}
