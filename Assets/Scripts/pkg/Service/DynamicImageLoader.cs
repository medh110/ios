using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class DynamicImageLoader : MonoBehaviour
{
    [Header("Dependencies")]
    public APIClient apiClient; // Reference to APIClient
    public ARTrackedImageManager trackedImageManager; // Reference to ARTrackedImageManager



    private string cacheFolder; // Path for cached images
    private List<Texture2D> cachedTextures = new List<Texture2D>(); // Loaded textures

    void Start()
    {
        // Ensure a cache folder exists
        cacheFolder = Path.Combine(Application.persistentDataPath, "ImageCache");
        if (!Directory.Exists(cacheFolder))
        {
            Directory.CreateDirectory(cacheFolder);
        }

        // Fetch images and update AR library
        StartCoroutine(FetchAndLoadImagesCoroutine());
    }

    private IEnumerator FetchAndLoadImagesCoroutine()
    {
        bool isFetchComplete = false;
        List<string> imageUrls = null;

        // Fetch image URLs using the APIClient
        apiClient.FetchImageUrls(
            urls =>
            {
                imageUrls = urls;
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
            string fileName = Path.GetFileName(url);
            string filePath = Path.Combine(cacheFolder, fileName);

            Texture2D texture;

            if (File.Exists(filePath))
            {
                // Load cached image
                byte[] imageBytes = File.ReadAllBytes(filePath);
                texture = new Texture2D(2, 2);
                texture.LoadImage(imageBytes);
            }
            else
            {
                // Download and cache the image asynchronously
                Task<Texture2D> downloadTask = DownloadImageAsync(url, filePath);
                yield return new WaitUntil(() => downloadTask.IsCompleted);

                if (downloadTask.Exception != null)
                {
                    Debug.LogError($"Error downloading image from {url}: {downloadTask.Exception}");
                    continue;
                }

                texture = downloadTask.Result;
            }

            cachedTextures.Add(texture);

            yield return null; // Allow Unity to render frames
        }

        // Add images to the AR library
        UpdateReferenceLibrary();
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

    private void UpdateReferenceLibrary()
    {
        if (!(trackedImageManager.referenceLibrary is MutableRuntimeReferenceImageLibrary mutableLibrary))
        {
            Debug.LogError("The reference library is not mutable. Ensure AR Foundation 4.0+ is installed.");
            return;
        }

        foreach (var texture in cachedTextures)
        {
            string imageName = "DynamicImage_" + cachedTextures.IndexOf(texture);

            // Add image to the library
            var jobHandle = mutableLibrary.ScheduleAddImageJob(texture, imageName, 0.1f); // Adjust physical size (meters)
            jobHandle.Complete();
        }

        Debug.Log("Dynamically loaded images added to the AR library.");
    }

    [System.Serializable]
    private class ImageApiResponse
    {
        public List<string> imageUrls; // API response format
    }
}
