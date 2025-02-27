using System;
using UnityEngine;

public class ScanTrigger : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;
    [SerializeField]
    private ARImageBehaviorManager _imageBehavior;
    [SerializeField]
    private QRCodeDetector _qrReader;
    [SerializeField]
    private GameObject _scanIcon;

    private const string SCAN = "scan";
    private const string SCAN_IDLE = "Idle";

    private bool scanning = false;

    public void StartScan()
    {
        // Return is scan animation is currently playing
        if (scanning)
        {
            return;
        }

        // Return if overlay is active or response is pending
        if (!_imageBehavior.CanScan)
        {
            return;
        }

        scanning = true;
        _animator.SetTrigger(SCAN);
    }

    public void ScanFinished()
    {
        var isMarkerFound = false;
        if (_qrReader.HasQRResult)
        {
            isMarkerFound = _qrReader.ExecuteCachedResult();
        }
        else 
        {
            // Check if any existing marker is tracked
            isMarkerFound = _imageBehavior.Scan();
        }

        ToggleScanIcon(!isMarkerFound);
        scanning = false;
    }

    internal void ToggleScanIcon(bool isEnabled)
    {
        this.gameObject.SetActive(isEnabled);
        if (isEnabled) 
        { 
            _animator.SetTrigger(SCAN_IDLE);
        }
    }
}
