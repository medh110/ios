using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DynamicImageLoader : MonoBehaviour
{
    [Header("Dependencies")]
    public APIClient apiClient; // Reference to APIClient
    public ARTrackedImageManager trackedImageManager; // Reference to ARTrackedImageManager
    public XRReferenceImageLibrary  referenceLibrary;
    
    private string cacheFolder; // Path for cached images
    private List<Texture2D> cachedTextures = new List<Texture2D>(); // Loaded textures

    private string CollectionKey = "de42a8b3-b355-48a6-8d97-eebac33031bc";
    private RuntimeReferenceImageLibrary RuntimeReferenceLibrary;
    
    //For testing only
    private string assetBundleUrl = $"file:///{Application.dataPath}/../AssetBundle/Android/referenceimagelib";

    void Start()
    {
        // Ensure a cache folder exists
        cacheFolder = Path.Combine(Application.persistentDataPath, "ImageCache");
        if (!Directory.Exists(cacheFolder))
        {
            Directory.CreateDirectory(cacheFolder);
        }

        // Fetch images and update AR library
        // StartCoroutine(FetchAndLoadImagesCoroutine());
        StartCoroutine(DownloadReferenceImageLibrary(assetBundleUrl));
    }

    private IEnumerator DownloadReferenceImageLibrary(string referenceLibraryUrl)
    {
        using (UnityWebRequest webRequest = UnityWebRequestAssetBundle.GetAssetBundle(referenceLibraryUrl))
        {
            yield return webRequest.SendWebRequest();
            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    var bundle = DownloadHandlerAssetBundle.GetContent(webRequest);

                    // Load the asset in the asset bundle and instantiate it in the game world
                    // assign the instantiated gameobject in CurrentMovableObject for controls
                    var libraries = bundle.LoadAllAssets<XRReferenceImageLibrary>();
                    foreach (var keyname in bundle.GetAllAssetNames())
                    {
                        Debug.LogError(keyname);
                    }
                    
                    XRReferenceImageLibrary referenceImageLibrary = libraries.First();
                    RuntimeReferenceLibrary = trackedImageManager.CreateRuntimeLibrary(referenceImageLibrary);
                    trackedImageManager.referenceLibrary = RuntimeReferenceLibrary;
                    trackedImageManager.enabled = true;
                    break;
            }
        }
    }

    private IEnumerator FetchAndLoadImagesCoroutine()
    {
        bool isFetchComplete = false;
        List<APIClient.FileData> imageUrls = null;

        // Fetch image URLs using the APIClient
        apiClient.FetchFileCollection(CollectionKey,
            collection =>
            {
                imageUrls = collection.files;
                isFetchComplete = true;
            },
            error =>
            {
                Debug.LogError($"Error fetching image URLs: {error}");
                isFetchComplete = true;
            });

        // Wait until the fetch is complete
        yield return new WaitUntil(() => isFetchComplete);

        if (imageUrls == null)
        {
            Debug.LogError("Failed to fetch image URLs.");
            yield break;
        }

        // Process each image URL
        foreach (var url in imageUrls)
        {
            string fileName = $"{url.file_id}.jpg";
            string filePath = Path.Combine(cacheFolder, fileName);

            Texture2D texture;

            if (File.Exists(filePath))
            {
                // Load cached image
                byte[] imageBytes = File.ReadAllBytes(filePath);
                texture = new Texture2D(2, 2);
                texture.LoadImage(imageBytes, false);
                texture.Apply();
            }
            else
            {
                var downloadComplete = false;
                var downloadFailed = false;
                Texture2D textureLoad = null;
                //Download and cache the image asynchronously
                apiClient.DownloadFileAsTexture(url.file_id, tex =>
                {
                    Debug.LogError(url.file_id);
                    textureLoad = tex;
                    downloadComplete = true;
                }, err =>
                {
                    Debug.LogError(err);
                    downloadFailed = true;
                });
                
                yield return new WaitUntil(() => downloadComplete || downloadFailed);

                if (downloadFailed)
                {
                    continue;
                }

                texture = textureLoad;
            }
            cachedTextures.Add(texture);
            yield return null; // Allow Unity to render frames
        }

        // Add images to the AR library
        StartCoroutine(UpdateReferenceLibrary());
    }

    private async Task<Texture2D> DownloadImageAsync(string url, string filePath)
    {
        using (HttpClient client = new HttpClient())
        {
            byte[] imageBytes = await client.GetByteArrayAsync(url);
            File.WriteAllBytes(filePath, imageBytes);

            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageBytes);
            return texture;
        }
    }

    private IEnumerator UpdateReferenceLibrary()
    {
        RuntimeReferenceLibrary = trackedImageManager.CreateRuntimeLibrary(referenceLibrary);
        if (!(RuntimeReferenceLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary))
        {
            Debug.LogError("The reference library is not mutable. Ensure AR Foundation 4.0+ is installed.");
            yield break;
        }

        var jobHandles = new List<AddReferenceImageJobState>();
        foreach (var texture in cachedTextures)
        {
            string imageName = "DynamicImage_" + cachedTextures.IndexOf(texture);
            // Add image to the library
            var imageJobState = mutableLibrary.ScheduleAddImageWithValidationJob(texture, imageName, 0.5f);
            var jobHandle = imageJobState.jobHandle;
            jobHandle.Complete();
            jobHandles.Add(imageJobState);
        }

        yield return new WaitUntil(() => jobHandles.All(_ => _.jobHandle.IsCompleted));
        
        trackedImageManager.referenceLibrary = mutableLibrary;
        trackedImageManager.enabled = true;

        Debug.Log("Dynamically loaded images added to the AR library.");
    }

    [System.Serializable]
    private class ImageApiResponse
    {
        public List<string> imageUrls; // API response format
    }
}
