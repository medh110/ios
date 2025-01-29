using System;
using System.Collections; // Required for IEnumerator and coroutines
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;

public class ARImageBehaviorManager : MonoBehaviour
{
    [Header("Dependencies")]
    public ARTrackedImageManager trackedImageManager;
    public APIClient apiClient;

    [Header("Dynamic Prefabs")]
    public GameObject overlayVideoPrefab; // Prefab for overlay videos
    public GameObject popUpVideoPrefab;   // Prefab for pop-up videos    public GameObject videoPrefab;
    public GameObject quizPrefab;
    public GameObject modelPrefab;

    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added)
        {
            Debug.Log($"Image detected: {trackedImage.referenceImage.name}");
            FetchObjectAndExecuteBehavior(trackedImage.referenceImage.name, trackedImage.transform);
        }
    }

    private void FetchObjectAndExecuteBehavior(string imageName, Transform imageTransform)
    {
        // Fetch the object tied to the marker using the `original` field
        apiClient.GetObjectProperties(
            imageName,
            response =>
            {
                Debug.Log($"Fetched object for {imageName}: Type={response.type}, Metadata={response.metadata}");

                // Based on the type, call the appropriate content
                switch (response.type)
                {
                    case "video":
                        FetchAndDisplayVideo(response, imageTransform);
                        break;

                    case "quiz":
                        FetchAndDisplayQuiz(response, imageTransform);
                        break;

                    case "model":
                        FetchAndDisplayModel(response, imageTransform);
                        break;

                    default:
                        Debug.LogWarning($"Unhandled type: {response.type}");
                        break;
                }
            },
            error =>
            {
                Debug.LogError($"Failed to fetch object for {imageName}: {error}");
            });
    }

    private void FetchAndDisplayVideo(APIClient.ShortURLResponse response, Transform imageTransform)
    {
        if (response.metadata == "overlay")
        {
            InstantiateAndConfigureOverlayVideo(response.short_url, imageTransform);
        }
        else if (response.metadata == "popup")
        {
            InstantiateAndConfigurePopupVideo(response.short_url);
        }
        else
        {
            Debug.LogWarning("Unknown video metadata. Defaulting to popup.");
            InstantiateAndConfigurePopupVideo(response.short_url);
        }
    }

    private void InstantiateAndConfigureOverlayVideo(string shortUrl, Transform imageTransform)
    {
        Debug.Log($"Preparing to play pop-up video from URL: {shortUrl}");

        // Instantiate the pop-up video prefab
        var popupVideo = Instantiate(overlayVideoPrefab);
        var videoPlayer = popupVideo.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();

        if (videoPlayer != null)
        {
            StartCoroutine(PlayVideoWithAuth(shortUrl, videoPlayer));
        }
    }


    private void InstantiateAndConfigurePopupVideo(string videoUrl)
    {
        Debug.Log($"Preparing to play pop-up video from URL: {videoUrl}");

        // Instantiate the pop-up video prefab
        var popupVideo = Instantiate(popUpVideoPrefab);
        var videoPlayer = popupVideo.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();

        if (videoPlayer != null)
        {
            StartCoroutine(PlayVideoWithAuth(videoUrl, videoPlayer));
        }
    }

    private void FetchAndDisplayQuiz(APIClient.ShortURLResponse response, Transform imageTransform)
    {
        apiClient.CallAPI(response.short_url, "GET", null,
            content =>
            {
                var quizData = JsonUtility.FromJson<APIClient.QuizResponse>(content);
                var quizObject = Instantiate(quizPrefab, imageTransform.position, imageTransform.rotation);
                var quizManager = quizObject.GetComponent<QuizManager>();
                if (quizManager != null)
                {
                    quizManager.SetQuizData(quizData);
                }
            },
            error => Debug.LogError($"Failed to fetch quiz data from {response.short_url}: {error}"));
    }


    private bool isPlaying = false; // Prevent multiple executions
    private IEnumerator PlayVideoWithAuth(string videoUrl, UnityEngine.Video.VideoPlayer videoPlayer)
    {
        if (isPlaying)
        {
            Debug.LogWarning("PlayVideoWithAuth is already running!");
            yield break; // Exit if already running
        }
        isPlaying = true;
        Debug.Log($"Fetching video with auth headers from: {videoUrl}");

        // Create a UnityWebRequest with the required headers
        using (UnityWebRequest request = UnityWebRequest.Get(videoUrl))
        {
            request.SetRequestHeader("X-API-KEY", "dfca5061-3576-47a9-872c-99d80b3c8218");

            // Begin the request
            yield return request.SendWebRequest();

            // Handle errors
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Failed to fetch video: {request.error}");
                yield break;
            }

            Debug.Log("Video fetched successfully. Streaming to VideoPlayer...");

            // Prepare the video player with the downloaded data
            var videoStream = new System.IO.MemoryStream(request.downloadHandler.data);

            videoPlayer.source = UnityEngine.Video.VideoSource.VideoClip;
            videoPlayer.url = videoUrl; // Use the URL only for metadata (not streaming)
            videoPlayer.Prepare();

            videoPlayer.prepareCompleted += (source) =>
            {
                videoPlayer.Play();
            };

            // Error handling
            videoPlayer.errorReceived += (source, message) =>
            {
                Debug.LogError($"VideoPlayer error: {message}");    
            };
        }
        isPlaying = false; // Reset the flag
    }
    private void FetchAndDisplayModel(APIClient.ShortURLResponse response, Transform imageTransform)
    {
        StartCoroutine(LoadAndAttachModel(response.short_url, imageTransform));
    }

    private System.Collections.IEnumerator LoadAndAttachModel(string modelUrl, Transform parentTransform)
    {
        // Example of loading a 3D model dynamically
        Debug.Log($"Loading model from {modelUrl}...");
        yield return new WaitForSeconds(1); // Simulate loading delay
        Debug.Log($"Model loaded from {modelUrl} and attached to {parentTransform.name}");
    }

    public void ExecuteBehaviorFromShortURL(string shortcode)
    {
        Debug.Log($"Fetching object from short URL: {shortcode}");


        apiClient.GetObjectProperties (
                shortcode,
                 response =>
                            {
                                Debug.Log($"Object fetched: Type={response.type}, Metadata={response.metadata}");
                                // Pass the object to the behavior manager for execution
                                ExecuteBehaviorFromObject(response);
                            },
            error =>
            {
                Debug.LogError($"Failed to fetch object properties for short code {shortcode}: {error}");
            });


    }
    public void ExecuteBehaviorFromObject(APIClient.ShortURLResponse response)
    {
        Debug.Log($"Executing behavior for object: Type={response.type}, Metadata={response.metadata}");

        switch (response.type)
        {
            case "video":
                HandleVideo(response);
                break;

            case "quiz":
                //HandleQuiz(response);
                break;

            case "model":
                //HandleModel(response);
                break;

            default:
                Debug.LogWarning($"Unhandled type: {response.type}");
                break;
        }
    }
    private void HandleVideo(APIClient.ShortURLResponse response)
    {
        if (response.metadata.Contains("popup"))
        {
            InstantiateAndConfigurePopupVideo(response.short_url);
        }
        else if (response.metadata.Contains("overlay"))
        {
            InstantiateAndConfigureOverlayVideo(response.short_url, null); // No marker needed for QR code
        }
        else
        {
            Debug.LogWarning($"Unknown metadata for video: {response.metadata}. Defaulting to popup.");
            InstantiateAndConfigurePopupVideo(response.short_url);
        }
    }
}
