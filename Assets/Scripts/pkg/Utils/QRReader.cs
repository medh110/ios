using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;

public class QRCodeDetector : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] 
    private ARCameraManager arCameraManager; // AR Camera Manager for acquiring frames
    [SerializeField] 
    private ARImageBehaviorManager behaviorManager; // Reference to ARImageBehaviorManager for executing behavior
    [SerializeField] 
    private TMPro.TextMeshProUGUI qrResultText; // Optional: Text display for QR result

    private IBarcodeReader qrReader; // ZXing QR code reader
    private Result CachedResult;

    public bool HasQRResult { get; private set; }


    void Awake()
    {
        qrReader = new BarcodeReader();
    }

    void OnEnable()
    {
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
        {
            HasQRResult = false;
            return;
        }

        using (cpuImage)
        {
            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.MirrorX
            };

            var buffer = new NativeArray<byte>(cpuImage.GetConvertedDataSize(conversionParams), Allocator.Temp);
            cpuImage.Convert(conversionParams, buffer);

            CachedResult = qrReader.Decode(buffer.ToArray(), conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, RGBLuminanceSource.BitmapFormat.RGBA32);
            if (CachedResult != null)
            {
                HasQRResult = true;
                if (!behaviorManager.IsTouchToScan)
                {
                    ExecuteQRResult(CachedResult);
                }
            }
            else 
            {
                HasQRResult = false;
            }

            buffer.Dispose();
        }
    }

    private void ExecuteQRResult(Result cachedResult)
    {
        if (qrResultText != null)
        {
            qrResultText.text = $"QR Code: {CachedResult.Text}";
        }

        // Extract short_code from the short_url (e.g., "M8MdiW" from "https://epy.digital/M8MdiW")
        string shortCode = ExtractShortCode(CachedResult.Text);
        Debug.Log($"Extracted Short Code: {shortCode}");

        // Fetch object properties from backend
        if (!string.IsNullOrEmpty(shortCode))
        {
            if (behaviorManager != null)
            {
                behaviorManager.ExecuteBehaviorFromShortURL(shortCode);
            }
        }
    }

    public void ExecuteCachedResult()
    {
        if (CachedResult != null)
        {
            ExecuteQRResult(CachedResult);
        }
    }

    private string ExtractShortCode(string shortUrl)
    {
        try
        {
            // Assuming shortUrl is of the form "https://epy.digital/<short_code>"
            Uri uri = new Uri(shortUrl);
            return uri.Segments[^1].TrimEnd('/');
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to extract short_code from URL: {shortUrl}, Error: {ex.Message}");
            return null;
        }
    }
}
