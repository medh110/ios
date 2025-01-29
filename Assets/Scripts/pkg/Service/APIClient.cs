using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

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
        string url = endpoint; // Use the endpoint directly, assuming it's a full URL if it starts with "http"

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

    [System.Serializable]
    private class ImageApiResponse
    {
        public List<string> imageUrls; // Ensure this matches the API response format
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
}
