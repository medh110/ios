using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO;

public class APIClient : MonoBehaviour
{
    private const string API_KEY = "dfca5061-3576-47a9-872c-99d80b3c8218";
    private const string BASE_URL = "https://epy.digital";

    public void CallAPI(string endpoint, string method, object requestBody = null, Action<string> onSuccess = null, Action<string> onError = null)
    {
        StartCoroutine(SendRequest(endpoint, method, requestBody, onSuccess, onError));
    }

    private IEnumerator SendRequest(string endpoint, string method, object requestBody, Action<string> onSuccess, Action<string> onError)
    {
        string url = endpoint; // Use the endpoint directly, assuming it's a full URL if it starts with "https"

        // Ensure the endpoint is treated correctly
        if (!endpoint.StartsWith("https"))
        {
            url = BASE_URL + endpoint; // Concatenate only if endpoint is relative
        }
        UnityWebRequest request;

        Debug.Log($"Sending {method} request to: {url}");

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Content-Type", "application/json");
        }
        else if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            string jsonBody = JsonConvert.SerializeObject(requestBody);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
        }
        else if (method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
        {
            request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT);
            string jsonBody = JsonConvert.SerializeObject(requestBody);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
        }
        else
        {
            Debug.LogError($"Unsupported HTTP method: {method}");
            onError?.Invoke("Unsupported HTTP method: " + method);
            yield break;
        }

        request.SetRequestHeader("X-API-KEY", API_KEY);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = 30; // Set a timeout in seconds
        Debug.Log($"Request headers");
        foreach (var header in request.GetRequestHeader("X-API-KEY"))
        {
            Debug.Log($"Header: {header}");
        }

        yield return request.SendWebRequest();

        // Handle the response outside of the `yield` block
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Request error: {request.error}");
            onError?.Invoke(request.error);
        }
        else
        {
            try
            {
                Debug.Log($"Request succeeded. Response length: {request.downloadHandler.text.Length}");
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Response handling failed: {ex.Message}");
                onError?.Invoke($"Exception during response handling: {ex.Message}");
            }
        }
    }


    // Endpoint: Generate Multiple Short URLs
    public void GenerateShortURLs(int count, string domain, Action<List<ShortURLResponse>> onSuccess, Action<string> onError)
    {
        var requestBody = new { count = count, domain = domain };
        CallAPI("/generate", "POST", requestBody,
            response =>
            {
                try
                {
                    var parsedResponse = JsonConvert.DeserializeObject<List<ShortURLResponse>>(response);
                    onSuccess?.Invoke(parsedResponse);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Parsing error: {e.Message}");
                }
            },
            onError);
    }

    // Endpoint: Retrieve URLs by Domain
    public void RetrieveURLsByDomain(string domain, Action<List<ShortURLResponse>> onSuccess, Action<string> onError)
    {
        string endpoint = $"/retrieve?domain={domain}";
        CallAPI(endpoint, "GET", null,
            response =>
            {
                try
                {
                    var parsedResponse = JsonConvert.DeserializeObject<List<ShortURLResponse>>(response);
                    onSuccess?.Invoke(parsedResponse);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Parsing error: {e.Message}");
                }
            },
            onError);
    }

    // Endpoint: Get Object Properties
    public void GetObjectProperties(string shortCode, Action<ShortURLResponse> onSuccess, Action<string> onError)
    {
        string endpoint = $"/get_object?short_code={shortCode}";
        CallAPI(endpoint, "GET", null,
            response =>
            {
                try
                {
                    var parsedResponse = JsonConvert.DeserializeObject<ShortURLResponse>(response);
                    onSuccess?.Invoke(parsedResponse);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Parsing error: {e.Message}");
                }
            },
            onError);
    }

    // Endpoint: Update Object Properties
    public void UpdateObjectProperties(string shortCode, string type, string original, string metadata, Action onSuccess, Action<string> onError)
    {
        var requestBody = new { short_code = shortCode, type = type, original = original, metadata = metadata };
        CallAPI("/update_object", "PUT", requestBody,
            response => onSuccess?.Invoke(),
            onError);
    }

    // Endpoint: Create Quiz
    public void CreateQuiz(string questions, string answerA, string answerB, string answerC, string correctAnswer, string explanation, string image, Action onSuccess, Action<string> onError)
    {
        var requestBody = new
        {
            questions,
            answer_a = answerA,
            answer_b = answerB,
            answer_c = answerC,
            correct_answer = correctAnswer,
            explanation,
            image
        };
        CallAPI("/create_quiz", "POST", requestBody,
            response => onSuccess?.Invoke(),
            onError);
    }
    public void RetrieveAllQuizzes(Action<List<QuizResponse>> onSuccess, Action<string> onError)
    {
        CallAPI("/retrieve_quizzes", "GET", null,
            response =>
            {
                try
                {
                    Debug.Log("Deserialization started...");

                    var parsedResponse = JsonConvert.DeserializeObject<List<QuizResponse>>(response);

                    if (parsedResponse == null)
                    {
                        Debug.LogError("Parsed response is null.");
                        onError?.Invoke("Parsed response is null.");
                        return;
                    }

                    Debug.Log($"Parsed {parsedResponse.Count} quizzes.");

                    foreach (var quiz in parsedResponse)
                    {
                        if (quiz.questions == null || quiz.correct_answer == null)
                        {
                            Debug.LogError($"Quiz has null fields. ID={quiz.id}");
                        }
                        Debug.Log($"Parsed quiz: ID={quiz.id}, Question={quiz.questions}");
                    }

                    onSuccess?.Invoke(parsedResponse);

                    Debug.Log("Deserialization completed.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Deserialization failed: {e.Message}");
                    Debug.LogError($"Raw Response: {response}");
                    onError?.Invoke($"Parsing error: {e.Message}");
                }
            },
            error =>
            {
                Debug.LogError($"API Call Error: {error}");
                onError?.Invoke(error);
            });
    }

    public void FetchImageUrls(Action<List<string>> onSuccess, Action<string> onError)
    {
        // Endpoint for fetching image URLs
        string endpoint = "/images";

        // Use the centralized CallAPI method
        CallAPI(endpoint, "GET", null,
            response =>
            {
                try
                {
                    // Deserialize the response into a list of image URLs
                    var parsedResponse = JsonConvert.DeserializeObject<ImageApiResponse>(response);
                    if (parsedResponse != null && parsedResponse.imageUrls != null)
                    {
                        onSuccess?.Invoke(parsedResponse.imageUrls);
                    }
                    else
                    {
                        onError?.Invoke("Invalid API response or no image URLs found.");
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parsing error: {ex.Message}");
                }
            },
            error =>
            {
                onError?.Invoke($"API call failed: {error}");
            });
    }
    // Updated FetchFileCollection method
    public void FetchFileCollection(string collection_id, Action<FileCollectionResponse> onSuccess, Action<string> onError)
    {
        // Endpoint for fetching file collection
        string endpoint = $"/get_file_collection?id={collection_id}";

        // Use the centralized CallAPI method
        CallAPI(endpoint, "GET", null,
            response =>
            {
                try
                {
                    // Deserialize the response into a FileCollectionResponse object
                    var parsedResponse = JsonConvert.DeserializeObject<FileCollectionResponse>(response);
                    if (parsedResponse != null && parsedResponse.files != null)
                    {
                        onSuccess?.Invoke(parsedResponse);
                    }
                    else
                    {
                        onError?.Invoke("Invalid API response or no files found.");
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Parsing error: {ex.Message}");
                }
            },
            error =>
            {
                onError?.Invoke($"API call failed: {error}");
            });
    }


    [System.Serializable]
    private class ImageApiResponse
    {
        public List<string> imageUrls; // Ensure this matches the API response format
    }

    public void FetchQuizFromAPI(string quizId, Action<QuizResponse> onSuccess, Action<string> onError)
    {
        Debug.Log($"Fetching quiz with ID: {quizId}");
        string endpoint = $"/retrieve_quiz?id={quizId}";

        CallAPI(endpoint, "GET", null,
            response =>
            {
                try
                {
                    Debug.Log("Deserializing API response...");
                    // Deserialize the API response into a QuizResponse object
                    var parsedResponse = JsonConvert.DeserializeObject<QuizResponse>(response);
                    if (parsedResponse != null)
                    {
                        Debug.Log("Quiz data retrieved successfully.");
                        onSuccess?.Invoke(parsedResponse);
                    }
                    else
                    {
                        Debug.Log("Invalid API response or no quiz data found.");
                        onError?.Invoke("Invalid API response or no quiz data found.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Log($"Parsing error: {ex.Message}");
                    onError?.Invoke($"Parsing error: {ex.Message}");
                }
            },
            error =>
            {

                Debug.Log($"API call failed: {error}");
                onError?.Invoke($"API call failed: {error}");
            });
    }

    /// <summary>
    /// Retrieves a file from the given URL and returns the file data as a byte array.
    /// </summary>
    /// <param name="fileUrl">The URL of the file to download.</param>
    /// <param name="onSuccess">Callback invoked with the file data on success.</param>
    /// <param name="onError">Callback invoked with an error message on failure.</param>
    public void FetchFileFromAPI(string fileUrl, Action<byte[]> onSuccess, Action<string> onError)
    {
        StartCoroutine(FetchFileCoroutine(fileUrl, onSuccess, onError));
    }

    /// <summary>
    /// Coroutine that downloads a file using UnityWebRequest.
    /// </summary>
    private IEnumerator FetchFileCoroutine(string fileUrl, Action<byte[]> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(fileUrl))
        {
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                onError?.Invoke($"Error fetching file: {request.error}");
            }
            else
            {
                onSuccess?.Invoke(request.downloadHandler.data);
            }
        }
    }

    // Optional: A helper method if you want to directly retrieve an image as a Texture2D
    public void FetchImageFromAPI(string imageUrl, Action<Texture2D> onSuccess, Action<string> onError)
    {
        StartCoroutine(FetchImageCoroutine(imageUrl, onSuccess, onError));
    }

    private IEnumerator FetchImageCoroutine(string imageUrl, Action<Texture2D> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                onError?.Invoke($"Error fetching image: {request.error}");
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                onSuccess?.Invoke(texture);
            }
        }
    }

    public void FetchQuestionsFromAPI(Action<List<quizQuestions>> onSuccess, Action<string> onError)
    {
        RetrieveAllQuizzes(
            quizzes =>
            {
                try
                {
                    var questions = new List<quizQuestions>();

                    foreach (var quiz in quizzes)
                    {
                        questions.Add(new quizQuestions
                        {
                            Q = quiz.questions ?? "Unknown Question",
                            A = quiz.answer_a ?? "N/A",
                            B = quiz.answer_b ?? "N/A",
                            C = quiz.answer_c ?? "N/A",
                            D = quiz.answer_d ?? "N/A",
                            answer = quiz.correct_answer ?? "N/A"
                        });
                    }

                    if (questions.Count > 0)
                    {
                        Debug.Log("Questions populated successfully.");
                        onSuccess?.Invoke(questions);
                    }
                    else
                    {
                        Debug.LogError("No questions retrieved from API.");
                        onError?.Invoke("No questions available.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error processing quizzes: {e.Message}");
                    onError?.Invoke($"Error processing quizzes: {e.Message}");
                }
            },
            error =>
            {
                Debug.LogError("Failed to fetch quizzes: " + error);
                onError?.Invoke(error);
            });
    }



    /// <summary>
    /// Downloads a file (assumed to be an image) by its fileId, converts it into a Texture2D, and returns the texture via the callback.
    /// </summary>
    /// <param name="fileId">The unique file ID to download.</param>
    /// <param name="onSuccess">Callback returning the Texture2D on success.</param>
    /// <param name="onError">Callback returning an error message on failure.</param>
    public void DownloadFileAsTexture(string fileId, Action<Texture2D> onSuccess, Action<string> onError)
    {
        StartCoroutine(DownloadFileAsTextureCoroutine(fileId, onSuccess, onError));
    }

    private IEnumerator DownloadFileAsTextureCoroutine(string fileId, Action<Texture2D> onSuccess, Action<string> onError)
    {
        // Build the URL using the fileId.
        string url = $"{BASE_URL}/download_file?file_id={fileId}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("X-API-KEY", API_KEY);
        request.timeout = 30; // Set a timeout if needed

        Debug.Log($"Downloading file as texture from: {url}");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            byte[] data = request.downloadHandler.data;
            // Create a temporary texture
            Texture2D texture = new Texture2D(2, 2);
            bool loadSuccess = ImageConversion.LoadImage(texture, data, false);
            texture.hideFlags = HideFlags.NotEditable;
            texture.ignoreMipmapLimit = false;
            if (loadSuccess)
            {
                Debug.Log("Image downloaded and converted successfully.");
                onSuccess?.Invoke(texture);
            }
            else
            {
                Debug.LogError("Failed to convert downloaded data to Texture2D.");
                onError?.Invoke("Failed to convert downloaded data to Texture2D.");
            }
        }
        else
        {
            Debug.LogError($"Error downloading file: {request.error}");
            onError?.Invoke(request.error);
        }
    }

    /// <summary>
    /// Downloads a file by its fileId from the download_file endpoint and caches it.
    /// The file is saved in Application.persistentDataPath and the metadata is stored in a sidecar file.
    /// </summary>
    /// <param name="fileId">The file ID to download.</param>
    /// <param name="metadata">The metadata string associated with the file.</param>
    /// <param name="onSuccess">Callback that returns the cached file path and metadata.</param>
    /// <param name="onError">Callback for errors.</param>
    public void DownloadAndCacheFile(string fileId, string metadata, Action<string, string> onSuccess, Action<string> onError)
    {
        // Build the URL using the fileId.
        string url = $"{BASE_URL}/download_file?file_id={fileId}";
        // Use fileId as the file name. (Optionally, you might want to append an extension.)
        string filePath = Path.Combine(Application.persistentDataPath, fileId);
        // Metadata file path (a simple approach using a sidecar file)
        string metadataPath = filePath + ".meta";

        // Check if file is already cached.
        if (File.Exists(filePath))
        {
            Debug.Log($"File {fileId} already cached at: {filePath}");
            onSuccess?.Invoke(filePath, metadata);
            return;
        }

        StartCoroutine(DownloadFileCoroutine(url, filePath, metadataPath, metadata, onSuccess, onError));
    }


    private IEnumerator DownloadFileCoroutine(string url, string filePath, string metadataPath, string metadata,
        Action<string, string> onSuccess, Action<string> onError)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("X-API-KEY", API_KEY);
        // Download directly to a file.
        request.downloadHandler = new DownloadHandlerFile(filePath);
        request.timeout = 30; // Adjust timeout as needed

        Debug.Log($"Downloading file from: {url}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Download error: {request.error}");
            onError?.Invoke(request.error);
        }
        else
        {
            // Save metadata to a separate file.
            try
            {
                File.WriteAllText(metadataPath, metadata);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to cache metadata: {ex.Message}");
                // Continue even if caching metadata fails.
            }
            Debug.Log($"File downloaded and cached at: {filePath}");
            onSuccess?.Invoke(filePath, metadata);
        }
    }

    public void DownloadFileCollectionFiles(FileCollectionResponse collectionResponse, Action<List<CachedFile>> onSuccess, Action<string> onError)
    {
        StartCoroutine(DownloadFileCollectionCoroutine(collectionResponse, onSuccess, onError));
    }

    private IEnumerator DownloadFileCollectionCoroutine(FileCollectionResponse collectionResponse, Action<List<CachedFile>> onSuccess, Action<string> onError)
    {
        List<CachedFile> cachedFiles = new List<CachedFile>();

        foreach (var file in collectionResponse.files)
        {
            bool downloadComplete = false;
            string cachedFilePath = null;
            string fileMetadata = file.metadata; // Already provided in the response.
            string errorMessage = null;

            // Start downloading the file.
            DownloadAndCacheFile(file.file_id, fileMetadata, (path, meta) =>
            {
                cachedFilePath = path;
                downloadComplete = true;
            }, (error) =>
            {
                errorMessage = error;
                downloadComplete = true;
            });

            // Wait until the download callback is called.
            while (!downloadComplete)
            {
                yield return null;
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                onError?.Invoke($"Error downloading file {file.file_id}: {errorMessage}");
                yield break;
            }

            cachedFiles.Add(new CachedFile { fileId = file.file_id, filePath = cachedFilePath, metadata = fileMetadata });
        }

        onSuccess?.Invoke(cachedFiles);
    }


[System.Serializable]
    public class QuizResponse
    {
        public int id { get; set; }
        public string questions { get; set; }
        public string answer_a { get; set; }
        public string answer_b { get; set; }
        public string answer_c { get; set; }
        public string answer_d { get; set; }
        public string correct_answer { get; set; }
        public string explanation { get; set; }
        public string image { get; set; }
    }

    [System.Serializable]
    // Classes for Parsing Responses
    public class ShortURLResponse
    {
        public string short_code { get; set; }
        public string short_url { get; set; }
        public string qr_code { get; set; }
        public string type { get; set; }
        public string original { get; set; }
        public string domain { get; set; }
        public string metadata { get; set; }
    }

    // New classes to match the API response structure
    [System.Serializable]
    public class FileCollectionResponse
    {
        public Collection collection { get; set; }
        public List<FileData> files { get; set; }
    }

    [System.Serializable]
    public class Collection
    {
        public string id { get; set; }
        public string name { get; set; }
        public string permission { get; set; }
        public string user_id { get; set; }
        public long created_at { get; set; }
        public long updated_at { get; set; }
    }

    [System.Serializable]
    public class FileData
    {
        public string id { get; set; }
        public string file_id { get; set; }
        public string collection_id { get; set; }
        public string metadata { get; set; }
        public int created_at { get; set; }
        public int updated_at { get; set; }
    }
    public class CachedFile
    {
        public string fileId;
        public string filePath;
        public string metadata;
    }
}
