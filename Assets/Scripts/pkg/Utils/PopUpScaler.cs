using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class PopUpScaler : MonoBehaviour
{
    [Header("UI Components")]
    public RectTransform videoFrame;  // The parent frame containing the video surface
    public RawImage videoSurface;    // The RawImage displaying the video
    public VideoPlayer videoPlayer;  // The VideoPlayer component

    [Header("Scaling Settings")]
    [Range(0.1f, 1.0f)] public float screenScaleFactor = 0.9f; // Scale to 90% of the screen size

    private bool isPrepared = false;

    void Start()
    {
        // Register the prepareCompleted event
        videoPlayer.prepareCompleted += OnVideoPrepared;

        // Prepare the video
        Debug.Log("Preparing VideoPlayer...");
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        Debug.Log("VideoPlayer prepared. Scaling video surface.");

        // Set flag to indicate preparation is complete
        isPrepared = true;

        // Adjust the video frame and surface after preparation
        ScaleVideoFrame();
        FitVideoToSurface();
    }

    void ScaleVideoFrame()
    {
        // Get screen dimensions
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Scale the frame to 90% of the screen size
        float targetWidth = screenWidth * screenScaleFactor;
        float targetHeight = screenHeight * screenScaleFactor;

        // Update the video frame's size while maintaining the aspect ratio
        videoFrame.sizeDelta = new Vector2(targetWidth, targetHeight);
    }

    void FitVideoToSurface()
    {
        if (!isPrepared)
        {
            Debug.LogWarning("VideoPlayer is not prepared. Cannot fit video to surface.");
            return;
        }

        // Get the video's aspect ratio
        float videoAspectRatio = (float)videoPlayer.width / videoPlayer.height;

        // Get the frame's aspect ratio
        float frameAspectRatio = videoFrame.rect.width / videoFrame.rect.height;

        // Scale the RawImage to fit within the frame while maintaining the video's aspect ratio
        if (videoAspectRatio > frameAspectRatio)
        {
            // Video is wider than the frame
            videoSurface.rectTransform.sizeDelta = new Vector2(videoFrame.rect.width, videoFrame.rect.width / videoAspectRatio);
        }
        else
        {
            // Video is taller than the frame
            videoSurface.rectTransform.sizeDelta = new Vector2(videoFrame.rect.height * videoAspectRatio, videoFrame.rect.height);
        }
    }

    void Update()
    {
        // Recheck dimensions on orientation change
        if (isPrepared && (Screen.orientation == ScreenOrientation.LandscapeLeft ||
                           Screen.orientation == ScreenOrientation.LandscapeRight ||
                           Screen.orientation == ScreenOrientation.Portrait))
        {
            ScaleVideoFrame();
            FitVideoToSurface();
        }
    }
}
