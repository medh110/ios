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

    [Header("Dynamic Prefabs")]
    public OverlayVideoPage overlayVideoPrefab; // Prefab for overlay videos
    public GameObject popUpVideoPrefab;   // Prefab for pop-up videos    public GameObject videoPrefab;
    public QuizManager quizPrefab;

    [SerializeField]
    private GameObject _loadingScreen;
    [SerializeField]
    private MainHud _hudCanvas;
    [SerializeField]
    private bool _touchToScan;
    [SerializeField] 
    private bool _localTesting;
    
    private ARTrackedImage currentTrackable;
    private TrackableId currentTrackableId;

    private bool isOverlayActive = false;
    private bool isPendingResponse = false;

    private APIClient.QuizResponse sampleQuiz;
    private string sampleClip;
    private bool isLocalTesting;

    private List<ARTrackedImage> trackedList = new List<ARTrackedImage>();

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
        
        // This is just a sample data
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

    private void OnTrackedImagesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        // If any overlay UI is active like quiz or video disable image tracking
        if (isOverlayActive || isPendingResponse)
        {
            return;
        }

        // If tracked list still contains current tracked item must return
        if (eventArgs.updated.Contains(currentTrackable))
        {
            return;
        }

        trackedList.Clear();
        trackedList.AddRange(eventArgs.updated);

        foreach (var trackedImage in eventArgs.updated)
        {
            // Don't process marker if tracking state is limited, means marker is not tracked anymore
            if (trackedImage.trackingState == TrackingState.Limited)
            {
                continue;
            }
            // We only check for new marker if old marker is not detected
            if (trackedImage.trackableId != currentTrackableId)
            {
                // Must filter marker with empty it must always have a name
                if (!string.IsNullOrEmpty(trackedImage.referenceImage.name))
                {
                    currentTrackable = trackedImage;
                    currentTrackableId = trackedImage.trackableId;
                    if (!_touchToScan)
                    {
                        FetchObjectAndExecuteBehavior(trackedImage.referenceImage.name, trackedImage.transform);
                    }
                    break;
                }
            }
        }
    }

    public bool Scan()
    {
        // If any overlay UI is active like quiz or video disable image tracking
        if (isOverlayActive || isPendingResponse)
        {
            return false;
        }

        if (currentTrackable == null)
        {
            return false;
        }

        // If cached marker is not on Tracking state means no marker is currently tracked
        if (currentTrackable.trackingState != TrackingState.Tracking)
        {

            return false;
        }

        FetchObjectAndExecuteBehavior(currentTrackable.referenceImage.name, currentTrackable.transform);
        return true;
    }

    public IEnumerator ShowLoadingThenExecute(Action actionToExecute, float waitSeconds = 3f)
    {
        // This is just a simulation of loading screen for local testing
        // this is to simulate the wait time from backend for response
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
            // For local testing only using reference cached data
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
                        StartCoroutine(LoadAndAttachModel(url, imageTransform));
                    }));
                    break;
            }
        }
        else
        {
            // Set Pending response to avoid sending another marker image while one is still pending
            // Enable loading screen while waiting for response
            isPendingResponse = true;
            ToggleLoadingScreen(true);

            // Fetch the object tied to the marker using the `original` field
            apiClient.GetObjectProperties(
                imageName,
                response =>
                {
                    // Set pending response to false when result is received
                    // Disable Loading screen
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
                    // Set pending response to false when result is received
                    // Disable Loading screen
                    isPendingResponse = false;
                    ToggleLoadingScreen(false);

                    Debug.LogError($"Failed to fetch object for {imageName}: {error}");
                });
        }
    }

    private void ToggleHudCanvas(bool isEnable)
    {
        // Toggle the hud canvas for overlay and preview mode
        _hudCanvas.ToggleHUD(isEnable);
    }

    private void ToggleLoadingScreen(bool isEnable)
    {
        // This enable/disable loading screen
        _loadingScreen.SetActive(isEnable);
    }

    public void OnPreviewClosed()
    {
        // Destroy the current spawned model / video 
        Destroy(CurrentMovableObject);

        if (CurrentType == ARType.Model)
        {
            // We must unload the asset bundle upon closing the preview mode
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
            Debug.LogWarning("Unknown video metadata. Defaulting to popup.");
            InstantiateAndConfigurePopupVideo(response.short_url, imageTransform);
        }
    }

    private void InstantiateAndConfigureOverlayVideo(string shortUrl)
    {
        Debug.Log($"Preparing to play pop-up video from URL: {shortUrl}");
        // Instantiate the pop-up video prefab
        if (overlayVideoPrefab.VideoPlayer != null)
        {
            StartCoroutine(PlayVideoWithAuth(shortUrl, overlayVideoPrefab.VideoPlayer, () =>
            {
                // Must disable the HUD and set overlayActive to true to prevent 
                // system from detecting another marker
                ToggleHudCanvas(false);
                isOverlayActive = true;
                isPendingResponse = false;
                overlayVideoPrefab.gameObject.SetActive(true);
                ToggleLoadingScreen(false);
                overlayVideoPrefab.SetOnCloseAction(() =>
                {
                    // Enable HUD when close and set overlay to false to let the system know
                    // we can scan for another marker
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

        // Instantiate the pop-up video prefab
        CurrentMovableObject = Instantiate(popUpVideoPrefab, imageTransform);
        if (CurrentMarkerType == MarkerType.Image)
        {
            CurrentMovableObject.transform.localScale = Vector2.one * currentTrackable.size;
        }
        else if (CurrentMarkerType == MarkerType.QR) 
        {
            CurrentMovableObject.transform.localScale = GetTrackableScale();
        }
        var videoPlayer = CurrentMovableObject.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();

        if (videoPlayer != null)
        {
            StartCoroutine(PlayVideoWithAuth(videoUrl, videoPlayer, () =>
            {
                // Must disable the HUD and set overlayActive to true to prevent 
                // system from detecting another marker and enable preview mode for controls
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
            // Parse the metadata JSON to extract the quiz_id
            Debug.Log($"Parsing metadata: {response.metadata}");    

            var metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.metadata);
            if (metadata != null && metadata.ContainsKey("quiz_id"))
            {
                string quizId = metadata["quiz_id"].ToString();
                // Use FetchQuizFromAPI with the extracted quizId

                Debug.Log($"Fetching quiz data from API using quiz_id: {quizId}");
                apiClient.FetchQuizFromAPI(quizId,
                    quizResponse =>
                    {
                        // Process the quiz response (e.g. start a coroutine to read the quiz page)
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
                // Fallback to the original behavior using the short_url
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
        // Download the image need for quiz answer
        Debug.Log($"Fetching image for quiz: {quizData.image}");
        yield return null;

        /*
        using (UnityWebRequest request = UnityWebRequest.Get(quizData.image))
        {
            yield return request.SendWebRequest();
            switch (request.result)
            {
                case UnityWebRequest.Result.Success:
                    // Convert the downloaded byte to texture 
                    var data = request.downloadHandler.data;
                    var tex = new Texture2D(1, 1);
                    var isSuccess = ImageConversion.LoadImage(tex, data, false);
        
                    if (isSuccess)
                    {
                        // If conversion succeeds set the texture to the container
                        quizPrefab.SetIcon(tex);
                    }
                    else
                    {
                        quizPrefab.SetEmpty();
                    }
                    break;
                default:
                    Debug.LogError(request.error);
                    break;
            }
        }
        */
        Debug.Log($"Downloading image from URL: {quizData.image}");

        apiClient.DownloadFileAsTexture(quizData.image, texture =>
        {
            // When download and conversion succeed, set the icon.
            quizPrefab.SetIcon(texture);
        }, error =>
        {
            Debug.LogError(error);
            quizPrefab.SetEmpty();
        });


        Debug.Log("Displaying quiz page...");
        // Disable HUD and show the quiz page
        ToggleHudCanvas(false);
        quizPrefab.SetQuizData(quizData);
        quizPrefab.SetOnCloseAction(() =>
        {
            // Set overlay to false and enable hud when quiz page is closed
            CurrentMarkerType = MarkerType.Invalid;
            CurrentType = ARType.Invalid;
            isOverlayActive = false;
            ToggleHudCanvas(true);
        });

        // Set overlay to true to prevent system from scanning another marker
        isOverlayActive = true;
        isPendingResponse = false;
        ToggleLoadingScreen(false);
    }


    private bool isPlaying = false; // Prevent multiple executions
    private IEnumerator PlayVideoWithAuth(string videoUrl, UnityEngine.Video.VideoPlayer videoPlayer, Action executeOnReady = null)
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
            if (!isLocalTesting)
            {
                request.SetRequestHeader("X-API-KEY", "dfca5061-3576-47a9-872c-99d80b3c8218");
            }

            // Begin the request
            yield return request.SendWebRequest();

            // Handle errors
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Failed to fetch video: {request.error}");
                isPendingResponse = false;
                isOverlayActive = false;
                ToggleLoadingScreen(false);
                yield break;
            }

            Debug.Log("Video fetched successfully. Streaming to VideoPlayer...");

            // Prepare the video player with the downloaded data
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoUrl; // Use the URL only for metadata (not streaming)
            videoPlayer.Prepare();
            executeOnReady?.Invoke();

            videoPlayer.prepareCompleted += (source) =>
            {
                videoPlayer.Play();
            };

            // Error handling
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
        isPlaying = false; // Reset the flag
    }
    private void FetchAndDisplayModel(APIClient.ShortURLResponse response, Transform imageTransform)
    {
        CurrentType = ARType.Model;
        StartCoroutine(LoadAndAttachModel(response.short_url, imageTransform));
    }

    private System.Collections.IEnumerator LoadAndAttachModel(string modelUrl, Transform parentTransform)
    {
        // Download the asset bundle from URL
        using (UnityWebRequest webRequest = UnityWebRequestAssetBundle.GetAssetBundle(modelUrl))
        {
            yield return webRequest.SendWebRequest();
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    // Must replace this to the exact file name instead of getting the last part
                    // of the URL in case url doesn't supply the name
                    var filename = modelUrl.Split('/').Last();
                    var bundle = DownloadHandlerAssetBundle.GetContent(webRequest);

                    // Load the asset in the asset bundle and instantiate it in the game world
                    // assign the instantiated gameobject in CurrentMovableObject for controls
                    GameObject prefab = bundle.LoadAsset<GameObject>(filename);

                    CurrentMovableObject = Instantiate(prefab, parentTransform.position, Quaternion.identity);
                    isOverlayActive = true;
                    _hudCanvas.TogglePreview(true);

#if UNITY_EDITOR
                    // We only do this for editor, a certain issue exist that only happens in editor and this
                    // is the fix
                    FixShaderForEditor.FixShadersForEditor(CurrentMovableObject);
#endif
                    break;
                default:
                    isOverlayActive = false;
                    Debug.LogError(webRequest.error);
                    break;
            }

            // Set overlay to true to prevent system from scanning markers
            // and enable preview mode of HUD
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
            // For local testing only using reference cached data
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
                        StartCoroutine(LoadAndAttachModel(url, GetQRTransform()));
                    }));
                    break;
            }
        }
        else
        {
            isPendingResponse = true;
            apiClient.GetObjectProperties(
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

            case "model":
                FetchAndDisplayModel(response, qrTransform);
                break;

            default:
                isPendingResponse = false;
                _hudCanvas.ToggleHUD(true);
                Debug.LogWarning($"Unhandled type: {response.type}");
                break;
        }
    }
    private void HandleVideo(APIClient.ShortURLResponse response, Transform imageTranform)
    {
        if (response.metadata.Contains("popup"))
        {
            CurrentType = ARType.PopupVideo;
            InstantiateAndConfigurePopupVideo(response.short_url, imageTranform);
        }
        else if (response.metadata.Contains("overlay"))
        {
            CurrentType = ARType.OverlayVideo;
            InstantiateAndConfigureOverlayVideo(response.short_url); // No marker needed for QR code
        }
        else
        {
            Debug.LogWarning($"Unknown metadata for video: {response.metadata}. Defaulting to popup.");

            CurrentType = ARType.PopupVideo;
            InstantiateAndConfigurePopupVideo(response.short_url, imageTranform);
        }
    }

    private Transform GetQRTransform() 
    {
        Transform qrTransform = null;
        if (trackedList.Any())
        {
            // Returns the first trackable image with null as reference image name, QR doesn't have reference image name 
            var trackedImage = trackedList.First(item => item.trackingState == TrackingState.Tracking && string.IsNullOrEmpty(item.referenceImage.name));
            if (trackedImage != null)
            {
                qrTransform = trackedImage.transform;
            }
        }
        return qrTransform;
    }

    private Vector2 GetTrackableScale()
    {
        // Returns the QR size 
        Vector2 scale = Vector2.one;
        if (trackedList.Any())
        {
            // Returns the first trackable image with null as reference image name, QR doesn't have reference image name 
            var trackedImage = trackedList.First(item => item.trackingState == TrackingState.Tracking && string.IsNullOrEmpty(item.referenceImage.name));
            if (trackedImage != null)
            {
                scale = trackedImage.size;
            }
        }
        return scale;
    }
}
