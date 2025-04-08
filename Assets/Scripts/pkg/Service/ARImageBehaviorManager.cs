using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARImageBehaviorManager : MonoBehaviour
{
    [Header("Dependencies")]
    public ARTrackedImageManager trackedImageManager;
    public APIClient apiClient;

    // Dictionary to keep track of active images
    private Dictionary<TrackableId, ARTrackedImage> activeTrackedImages = new Dictionary<TrackableId, ARTrackedImage>();

    // Optionally, track which images have already triggered behavior so you don’t do it repeatedly
    private Dictionary<TrackableId, bool> processedImages = new Dictionary<TrackableId, bool>();

    [Header("Dynamic Prefabs")]
    public OverlayVideoPage overlayVideoPrefab; // Prefab for overlay videos
    public GameObject popUpVideoPrefab;         // Prefab for pop-up videos    
    public GameObject videoPrefab;
    public QuizManager quizPrefab;

    [SerializeField]
    private GameObject _loadingScreen;
    [SerializeField]
    private MainHud _hudCanvas;
    [SerializeField]
    private bool _touchToScan;
    [SerializeField]
    private bool _localTesting;

    // For legacy support: current tracked image and list (used in some methods)
    private ARTrackedImage currentTrackable;
    private TrackableId currentTrackableId;
    private List<ARTrackedImage> trackedList = new List<ARTrackedImage>();

    private bool isOverlayActive = false;
    private bool isPendingResponse = false;

    private APIClient.QuizResponse sampleQuiz;
    private string sampleClip;
    private bool isLocalTesting;

    public GameObject CurrentMovableObject { get; private set; }
    public ARType CurrentType { get; private set; }
    public MarkerType CurrentMarkerType { get; private set; }
    public bool CanScan => !isOverlayActive && !isPendingResponse;
    public bool IsTouchToScan => _touchToScan;

    void OnEnable()
    {
#if UNITY_EDITOR
        // Give option to do local testing in editor
        isLocalTesting = _localTesting;
#else
        // Always set local testing to false, cached asset won't work anyway outside editor
        isLocalTesting = false;
#endif
        // This is just sample data
        sampleQuiz = new APIClient.QuizResponse();
        sampleQuiz.questions = "what is the national flower of singapore";
        sampleQuiz.answer_a = "Sunflower";
        sampleQuiz.answer_b = "Epidendrum Orchid";
        sampleQuiz.answer_c = "Vanda Miss Joaquim Orchid";
        sampleQuiz.answer_d = "Brassavola Orchid";
        sampleQuiz.explanation = "Papilionanthe Miss Joaquim, Also known as the Singapore orchid, this hybrid orchid is the national flower of Singapore. It was chosen for its resilience and vibrant colors.";
        sampleQuiz.correct_answer = sampleQuiz.answer_c;
        sampleQuiz.image = $"file:///{Application.dataPath}/../SampleAssets/singapore-orchids1.jpg";

        // This is just a sample clip for popup video and overlay video
        sampleClip = $"file:///{Application.dataPath}/../SampleAssets/SampleVideo.mp4";

        trackedImageManager.trackablesChanged.AddListener(OnTrackedImagesChanged);
    }

    void OnDisable()
    {
        trackedImageManager.trackablesChanged.RemoveListener(OnTrackedImagesChanged);
    }

    /// <summary>
    /// Processes changes to AR tracked images.
    /// Maintains a dictionary of active tracked images so that multiple markers
    /// can be processed concurrently.
    /// </summary>
    /// <param name="eventArgs">The AR trackable events</param>
    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // If any overlay UI or pending response is active, skip processing.
        if (isOverlayActive || isPendingResponse)
        {
            return;
        }

        foreach (var removedPair in eventArgs.removed)
        {
            // Use removedPair.Key to get the TrackableId.
            if (activeTrackedImages.ContainsKey(removedPair.Key))
            {
                activeTrackedImages.Remove(removedPair.Key);
                processedImages.Remove(removedPair.Key); // Allow reprocessing if re-detected
            }
        }


        // Process added images
        foreach (var addedImage in eventArgs.added)
        {
            if (!string.IsNullOrEmpty(addedImage.referenceImage.name))
            {
                activeTrackedImages[addedImage.trackableId] = addedImage;
                Debug.Log($"[Added] {addedImage.name} == {addedImage.referenceImage.name}, state: {addedImage.trackingState}");
            }
        }

        // Process updated images
        foreach (var updatedImage in eventArgs.updated)
        {
            if (!string.IsNullOrEmpty(updatedImage.referenceImage.name))
            {
                activeTrackedImages[updatedImage.trackableId] = updatedImage;
                Debug.Log($"[Updated] {updatedImage.name} == {updatedImage.referenceImage.name}, state: {updatedImage.trackingState}");
            }
        }

        // Update legacy tracked list from active tracked images (used in some helper methods)
        trackedList = activeTrackedImages.Values.ToList();

        // Process each active tracked image.
        foreach (var kv in activeTrackedImages)
        {
            ARTrackedImage trackedImage = kv.Value;
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                Debug.Log($"Image {trackedImage.referenceImage.name} is Tracking.");
                // If not already processed, trigger behavior (if auto scanning is enabled)
                if (!processedImages.ContainsKey(trackedImage.trackableId))
                {
                    processedImages[trackedImage.trackableId] = true;
                    // Update current trackable (for legacy support)
                    currentTrackable = trackedImage;
                    currentTrackableId = trackedImage.trackableId;
                    if (!_touchToScan)
                    {
                        FetchObjectAndExecuteBehavior(trackedImage.referenceImage.name, trackedImage.transform);
                    }
                }
            }
            else if (trackedImage.trackingState == TrackingState.Limited)
            {
                Debug.Log($"Image {trackedImage.referenceImage.name} is Limited. Waiting for full tracking.");
            }
            else
            {
                Debug.Log($"Image {trackedImage.referenceImage.name} is in state: {trackedImage.trackingState}");
            }
        }
    }

    /// <summary>
    /// Tries to perform a scan by searching among the active tracked images.
    /// </summary>
    /// <returns>True if a fully tracked image is found and its behavior is executed; otherwise, false.</returns>
    public bool Scan()
    {
        Debug.LogError("AAS:: SCANNING");

        if (isOverlayActive || isPendingResponse)
        {
            Debug.LogError($"AAS:: SCAN FAILED isOverlayActive: {isOverlayActive} == isPendingResponse: {isPendingResponse}");
            return false;
        }

        // Loop over active tracked images to select a candidate.
        ARTrackedImage candidate = null;
        foreach (var kv in activeTrackedImages)
        {
            // Use kv.Value to access the ARTrackedImage and its trackableId
            if (kv.Value.trackingState == TrackingState.Tracking)
            {
                candidate = kv.Value;
                break;
            }
        }

        if (candidate == null)
        {
            Debug.LogError("AAS:: SCAN FAILED: No active tracked image is in Tracking state.");
            return false;
        }

        Debug.LogError($"AAS:: Executing SCANNING behavior for {candidate.referenceImage.name}");
        FetchObjectAndExecuteBehavior(candidate.referenceImage.name, candidate.transform);
        return true;
    }

    public IEnumerator ShowLoadingThenExecute(Action actionToExecute, float waitSeconds = 3f)
    {
        // Simulate a loading screen delay for local testing
        ToggleLoadingScreen(true);
        yield return new WaitForSeconds(waitSeconds);
        actionToExecute.Invoke();
    }

    private void FetchObjectAndExecuteBehavior(string imageName, Transform imageTransform)
    {
        CurrentMarkerType = MarkerType.Image;
        if (isLocalTesting)
        {
            isPendingResponse = true;
            // For local testing using cached data
            switch (imageName)
            {
                case "ParentAlienVideo":
                    StartCoroutine(ShowLoadingThenExecute(() =>
                    {
                        CurrentType = ARType.Quiz;
                        StartCoroutine(ReadQuizPage(sampleQuiz));
                    }));
                    break;
                case "ParentFabVideo-1":
                    StartCoroutine(ShowLoadingThenExecute(() =>
                    {
                        CurrentType = ARType.OverlayVideo;
                        InstantiateAndConfigureOverlayVideo(sampleClip);
                    }));
                    break;
                case "ParentSunPrefab":
                    StartCoroutine(ShowLoadingThenExecute(() =>
                    {
                        CurrentType = ARType.PopupVideo;
                        InstantiateAndConfigurePopupVideo(sampleClip, imageTransform);
                    }));
                    break;
                case "ParentMerlionFab":
                    StartCoroutine(ShowLoadingThenExecute(() =>
                    {
                        var url = $"file:///{Application.dataPath}/../AssetBundle/Android/ParentMerlionFab";
                        CurrentType = ARType.Model;
                        StartCoroutine(LoadAndAttachModel(url, imageTransform, "ParentMerlionFab"));
                    }));
                    break;
            }
        }
        else
        {
            // Mark pending to avoid multiple marker processes and show loading screen
            isPendingResponse = true;
            ToggleLoadingScreen(true);

            // Fetch the object tied to the marker using imageName
            apiClient.GetObjectProperties(
                imageName,
                response =>
                {
                    Debug.Log($"Fetched object for {imageName}: Type={response.type}, Metadata={response.metadata}");
                    Debug.LogError($"AAS:: GetObjectProperties {imageName}: Type={response.type}, Metadata={response.metadata}");

                    switch (response.type)
                    {
                        case "video":
                            FetchAndDisplayVideo(response, imageTransform);
                            break;
                        case "quiz":
                            FetchAndDisplayQuiz(response, imageTransform);
                            break;
                        case "3d":
                            FetchAndDisplayModel(response, imageTransform);
                            break;
                        default:
                            Debug.LogWarning($"Unhandled type: {response.type}");
                            break;
                    }
                },
                error =>
                {
                    isPendingResponse = false;
                    ToggleLoadingScreen(false);
                    Debug.LogError($"Failed to fetch object for {imageName}: {error}");
                });
        }
    }

    private void ToggleHudCanvas(bool isEnable)
    {
        _hudCanvas.ToggleHUD(isEnable);
    }

    private void ToggleLoadingScreen(bool isEnable)
    {
        _loadingScreen.SetActive(isEnable);
    }

    public void OnPreviewClosed()
    {
        // Destroy the current spawned model or video
        Destroy(CurrentMovableObject);

        if (CurrentType == ARType.Model)
        {
            // Unload asset bundles if model is closed
            AssetBundle.UnloadAllAssetBundles(true);
        }
        CurrentMarkerType = MarkerType.Invalid;
        CurrentType = ARType.Invalid;
        isOverlayActive = false;
    }

    private void FetchAndDisplayVideo(APIClient.ShortURLResponse response, Transform imageTransform)
    {
        if (response.metadata == "overlay")
        {
            CurrentType = ARType.OverlayVideo;
            InstantiateAndConfigureOverlayVideo(response.short_url);
        }
        else if (response.metadata == "popup")
        {
            CurrentType = ARType.PopupVideo;
            InstantiateAndConfigurePopupVideo(response.short_url, imageTransform);
        }
        else
        {
            CurrentType = ARType.OverlayVideo;
            Debug.LogWarning("Unknown video metadata. Defaulting to popup.");
            InstantiateAndConfigureOverlayVideo(response.short_url);
        }
    }

    private void InstantiateAndConfigureOverlayVideo(string shortUrl)
    {
        Debug.Log($"Preparing to play overlay video from URL: {shortUrl}");
        Debug.LogError($"AAS:: InstantiateAndConfigureOverlayVideo Preparing to play overlay video from URL: {shortUrl}");
        if (overlayVideoPrefab.VideoPlayer != null)
        {
            StartCoroutine(PlayVideoWithAuth(shortUrl, overlayVideoPrefab.VideoPlayer, () =>
            {
                ToggleHudCanvas(false);
                isOverlayActive = true;
                isPendingResponse = false;
                overlayVideoPrefab.gameObject.SetActive(true);
                ToggleLoadingScreen(false);
                overlayVideoPrefab.SetOnCloseAction(() =>
                {
                    CurrentMarkerType = MarkerType.Invalid;
                    CurrentType = ARType.Invalid;
                    isOverlayActive = false;
                    ToggleHudCanvas(true);
                });
            }));
        }
    }

    private void InstantiateAndConfigurePopupVideo(string videoUrl, Transform imageTransform)
    {
        Debug.Log($"Preparing to play pop-up video from URL: {videoUrl}");
        CurrentMovableObject = Instantiate(popUpVideoPrefab, imageTransform);
        if (CurrentMarkerType == MarkerType.Image)
        {
            CurrentMovableObject.transform.localScale = Vector2.one * currentTrackable.size;
        }
        else if (CurrentMarkerType == MarkerType.QR)
        {
            CurrentMovableObject.transform.localScale = GetTrackableScale();
        }
        var videoPlayer = CurrentMovableObject.GetComponentInChildren<VideoPlayer>();
        if (videoPlayer != null)
        {
            StartCoroutine(PlayVideoWithAuth(videoUrl, videoPlayer, () =>
            {
                isOverlayActive = true;
                _hudCanvas.TogglePreview(true);
                isPendingResponse = false;
                ToggleLoadingScreen(false);
            }));
        }
    }

    private void FetchAndDisplayQuiz(APIClient.ShortURLResponse response, Transform imageTransform)
    {
        CurrentType = ARType.Quiz;
        try
        {
            Debug.Log($"Parsing metadata: {response.metadata}");
            var metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.metadata);
            if (metadata != null && metadata.ContainsKey("quiz_id"))
            {
                string quizId = metadata["quiz_id"].ToString();
                Debug.Log($"Fetching quiz data from API using quiz_id: {quizId}");
                apiClient.FetchQuizFromAPI(quizId,
                    quizResponse =>
                    {
                        Debug.Log($"Fetched quiz data: {quizResponse}");
                        StartCoroutine(ReadQuizPage(quizResponse));
                    },
                    error =>
                    {
                        Debug.LogError($"Failed to fetch quiz data: {error}");
                    });
            }
            else
            {
                Debug.LogWarning("Metadata does not contain a valid quiz_id. Defaulting to direct API call.");
                apiClient.CallAPI(response.short_url, "GET", null,
                    content =>
                    {
                        var quizData = JsonUtility.FromJson<APIClient.QuizResponse>(content);
                        StartCoroutine(ReadQuizPage(quizData));
                    },
                    error => Debug.LogError($"Failed to fetch quiz data from {response.short_url}: {error}"));
            }
        }
        catch (Exception ex)
        {
            isOverlayActive = false;
            isPendingResponse = false;
            ToggleLoadingScreen(false);
            Debug.LogError($"Error parsing metadata: {ex.Message}");
        }
    }

    private IEnumerator ReadQuizPage(APIClient.QuizResponse quizData)
    {
        yield return null;
        var isCompleted = false;

        apiClient.DownloadFileAsTexture(quizData.image, texture =>
        {
            isCompleted = true;
            quizPrefab.SetIcon(texture);
        }, error =>
        {
            Debug.LogError(error);
            quizPrefab.SetEmpty();
        });

        yield return new WaitUntil(() => isCompleted);
        Debug.Log("Displaying quiz page...");
        ToggleHudCanvas(false);
        quizPrefab.SetQuizData(quizData);
        quizPrefab.SetOnCloseAction(() =>
        {
            CurrentMarkerType = MarkerType.Invalid;
            CurrentType = ARType.Invalid;
            isOverlayActive = false;
            ToggleHudCanvas(true);
        });

        isOverlayActive = true;
        isPendingResponse = false;
        ToggleLoadingScreen(false);
    }

    private bool isPlaying = false;
    private IEnumerator PlayVideoWithAuth(string videoUrl, VideoPlayer videoPlayer, Action executeOnReady = null)
    {
        if (isPlaying)
        {
            Debug.LogWarning("PlayVideoWithAuth is already running!");
            yield break;
        }
        isPlaying = true;
        Debug.Log($"Fetching video with auth headers from: {videoUrl}");
        using (UnityWebRequest request = UnityWebRequest.Get(videoUrl))
        {
            if (!isLocalTesting)
            {
                request.SetRequestHeader("X-API-KEY", "dfca5061-3576-47a9-872c-99d80b3c8218");
            }
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Failed to fetch video: {request.error}");
                isPendingResponse = false;
                isOverlayActive = false;
                ToggleLoadingScreen(false);
                yield break;
            }
            Debug.Log("Video fetched successfully. Streaming to VideoPlayer...");
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoUrl;
            videoPlayer.Prepare();
            executeOnReady?.Invoke();
            videoPlayer.prepareCompleted += (source) =>
            {
                videoPlayer.Play();
            };
            videoPlayer.errorReceived += (source, message) =>
            {
                if (isOverlayActive)
                {
                    isPendingResponse = false;
                    isOverlayActive = false;
                    ToggleHudCanvas(true);
                }
                Debug.LogError($"VideoPlayer error: {message}");
            };
        }
        isPlaying = false;
    }

    private void FetchAndDisplayModel(APIClient.ShortURLResponse response, Transform imageTransform)
    {
        CurrentType = ARType.Model;
        var filename = response.short_url.Split('/').Last();
        if (!isLocalTesting)
        {
            var metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.metadata);
            if (metadata.ContainsKey("filename"))
            {
                filename = metadata["filename"].ToString();
            }
        }

        Debug.LogError($"AAS:: LoadAndAttachModel: {response.short_url}");
        StartCoroutine(LoadAndAttachModel(response.short_url, imageTransform, filename));
    }

    private IEnumerator LoadAndAttachModel(string modelUrl, Transform parentTransform, string filename)
    {
        using (UnityWebRequest webRequest = UnityWebRequestAssetBundle.GetAssetBundle(modelUrl))
        {
            yield return webRequest.SendWebRequest();
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    var bundle = DownloadHandlerAssetBundle.GetContent(webRequest);
                    Debug.LogError("AAS:: GetAssetBundle SUCCESS");
                    GameObject prefab = bundle.LoadAsset<GameObject>(filename);
                    CurrentMovableObject = Instantiate(prefab, parentTransform.position, Quaternion.identity);
                    isOverlayActive = true;
                    _hudCanvas.TogglePreview(true);
                    Debug.LogError("AAS:: LoadAndAttachModel SUCCESS");
#if UNITY_EDITOR
                    FixShaderForEditor.FixShadersForEditor(CurrentMovableObject);
#endif
                    break;
                default:
                    Debug.LogError("AAS:: GetAssetBundle FAILED");
                    isOverlayActive = false;
                    Debug.LogError(webRequest.error);
                    break;
            }
            ToggleLoadingScreen(false);
            isPendingResponse = false;
        }
    }

    public void ExecuteBehaviorFromShortURL(string shortcode)
    {
        Debug.Log($"Fetching object from short URL: {shortcode}");
        CurrentMarkerType = MarkerType.QR;

        if (isLocalTesting)
        {
            isPendingResponse = true;
            switch (shortcode)
            {
                case "jSzOjv":
                    StartCoroutine(ShowLoadingThenExecute(() =>
                    {
                        CurrentType = ARType.Quiz;
                        StartCoroutine(ReadQuizPage(sampleQuiz));
                    }));
                    break;
                case "rYb8cS":
                    StartCoroutine(ShowLoadingThenExecute(() =>
                    {
                        CurrentType = ARType.OverlayVideo;
                        InstantiateAndConfigureOverlayVideo(sampleClip);
                    }));
                    break;
                case "1V2S2S":
                    StartCoroutine(ShowLoadingThenExecute(() =>
                    {
                        CurrentType = ARType.PopupVideo;
                        InstantiateAndConfigurePopupVideo(sampleClip, GetQRTransform());
                    }));
                    break;
                case "1FENoV":
                    StartCoroutine(ShowLoadingThenExecute(() =>
                    {
                        var url = $"file:///{Application.dataPath}/../AssetBundle/Android/ParentMerlionFab";
                        CurrentType = ARType.Model;
                        StartCoroutine(LoadAndAttachModel(url, GetQRTransform(), "ParentMerlionFab"));
                    }));
                    break;
            }
        }
        else
        {
            isPendingResponse = true;
            ToggleLoadingScreen(true);
            apiClient.GetObjectProperties(
                shortcode,
                response =>
                {
                    Debug.Log($"Object fetched: Type={response.type}, Metadata={response.metadata}");
                    ExecuteBehaviorFromObject(response);
                },
                error =>
                {
                    Debug.LogError($"Failed to fetch object properties for short code {shortcode}: {error}");
                });
        }
    }

    public void ExecuteBehaviorFromObject(APIClient.ShortURLResponse response)
    {
        Debug.Log($"Executing behavior for object: Type={response.type}, Metadata={response.metadata}");
        Transform qrTransform = GetQRTransform();
        switch (response.type)
        {
            case "video":
                HandleVideo(response, qrTransform);
                break;
            case "quiz":
                FetchAndDisplayQuiz(response, null);
                break;
            case "3d":
                FetchAndDisplayModel(response, qrTransform);
                break;
            default:
                isPendingResponse = false;
                _hudCanvas.ToggleHUD(true);
                Debug.LogWarning($"Unhandled type: {response.type}");
                break;
        }
    }

    private void HandleVideo(APIClient.ShortURLResponse response, Transform imageTransform)
    {
        if (response.metadata.Contains("popup"))
        {
            CurrentType = ARType.PopupVideo;
            InstantiateAndConfigurePopupVideo(response.short_url, imageTransform);
        }
        else if (response.metadata.Contains("overlay"))
        {
            CurrentType = ARType.OverlayVideo;
            InstantiateAndConfigureOverlayVideo(response.short_url);
        }
        else
        {
            Debug.LogWarning($"Unknown metadata for video: {response.metadata}. Defaulting to popup.");
            CurrentType = ARType.PopupVideo;
            InstantiateAndConfigurePopupVideo(response.short_url, imageTransform);
        }
    }

    private Transform GetQRTransform()
    {
        Transform qrTransform = null;
        if (trackedList.Any())
        {
            // Returns the first trackable image with an empty reference image name (QR markers)
            var trackedImage = trackedList.FirstOrDefault(item => item.trackingState == TrackingState.Tracking && string.IsNullOrEmpty(item.referenceImage.name));
            if (trackedImage != null)
            {
                qrTransform = trackedImage.transform;
            }
        }
        return qrTransform;
    }

    private Vector2 GetTrackableScale()
    {
        Vector2 scale = Vector2.one;
        if (trackedList.Any())
        {
            var trackedImage = trackedList.FirstOrDefault(item => item.trackingState == TrackingState.Tracking && string.IsNullOrEmpty(item.referenceImage.name));
            if (trackedImage != null)
            {
                scale = trackedImage.size;
            }
        }
        return scale;
    }
}
