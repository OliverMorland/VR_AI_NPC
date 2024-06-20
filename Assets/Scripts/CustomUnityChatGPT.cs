using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class CustomUnityChatGPT : MonoBehaviour
{
    // Replace with your OpenAI API key
    private string apiKey = "";
    // Replace with your OpenAI model, e.g., "gpt-3.5-turbo"
    private string model = "gpt-3.5-turbo";
    // Replace with your API endpoint, e.g., "https://api.openai.com/v1/chat/completions"
    private string apiEndpoint = "https://api.openai.com/v1/chat/completions";

    NetworkReachability networkReachability;

    public UnityEvent<string> OnResponseRecieved;

    private void Start()
    {
        StartCoroutine(TestNetworkConnectivity());
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            networkReachability = Application.internetReachability;
            Debug.Log("OLILOG Network is not reachable");
        }
        else
        {
            Debug.Log("OLILOG Network is reachable");
        }
    }

    // Method to send a message to OpenAI and receive a response
    public void SendMessageToOpenAI(string userInput)
    {
        StartCoroutine(SendRequest(userInput));
    }

    private IEnumerator SendRequest(string userInput)
    {
        // Create the JSON payload
        string jsonPayload = "{\"model\": \"" + model + "\", \"messages\": [{\"role\": \"user\", \"content\": \"" + userInput + "\"}]}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        // Create the UnityWebRequest
        UnityWebRequest request = new UnityWebRequest(apiEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        networkReachability = NetworkReachability.NotReachable;

        // Send the request and wait for a response
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            // Parse the response
            string responseText = request.downloadHandler.text;
            // Process the response as needed
            OpenAIResponse response = JsonUtility.FromJson<OpenAIResponse>(responseText);
            if (response != null && response.choices != null && response.choices.Length > 0)
            {
                string content = response.choices[0].message.content;
                OnResponseRecieved.Invoke(content);
                Debug.Log("AI Response: " + content);
                // Process the response as needed
            }
        }
    }

    private IEnumerator TestNetworkConnectivity()
    {
        UnityWebRequest request = UnityWebRequest.Get("https://www.google.com");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("OLILOG Network Test Error: " + request.error);
        }
        else
        {
            Debug.Log("OLILOG Network Test Success: " + request.downloadHandler.text);
        }
    }

}

[System.Serializable]
public class OpenAIResponse
{
    public Choice[] choices;
}

[System.Serializable]
public class Choice
{
    public Message message;
}

[System.Serializable]
public class Message
{
    public string role;
    public string content;
}
