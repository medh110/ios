using UnityEngine;

public class ScanTrigger : MonoBehaviour
{
    [SerializeField]
    private Animator _animator;
    [SerializeField]
    private ARImageBehaviorManager _imageBehavior;
    [SerializeField]
    private QRCodeDetector _qrReader;

    private const string SCAN = "scan";

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
        if (_qrReader.HasQRResult)
        {
            _qrReader.ExecuteCachedResult();
        }
        else 
        {
            // Check if any existing marker is tracked
            _imageBehavior.Scan();
        }
        scanning = false;
    }
}
