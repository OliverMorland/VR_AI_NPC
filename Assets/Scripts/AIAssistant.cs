using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class AIAssistant : MonoBehaviour
{
    public OpenAIConfigurationSO openAIConfig;
    public string assistantId = "asst_iQOTrPcuusPL2UEvd5gNjTHl";
    public UnityEvent<string> OnResponseRecieved;
    public string testMessage = "Hello, who are you?";
    private string model = "gpt-3.5-turbo";
    private string threadId;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(CreateNewThread());
    }

    IEnumerator CreateNewThread()
    {
        UnityWebRequest createMessageRequest = ConfigureRequest("https://api.openai.com/v1/threads", "POST", null);
        yield return createMessageRequest.SendWebRequest();
        if (RequestHasFailed(createMessageRequest))
        {
            Debug.LogError(createMessageRequest.error);
            yield break;
        }

        // Parse the response
        string responseText = createMessageRequest.downloadHandler.text;
        CreateThreadResponse createThreadResponse = JsonUtility.FromJson<CreateThreadResponse>(responseText);
        threadId = createThreadResponse.id;
    }

    [ContextMenu("Send Test Message")]
    void SendMTestMessage()
    {
        StartCoroutine(SendMessage());
    }

    private IEnumerator SendMessage()
    {
        Debug.Log("Sending message");
        //Create message
        AddMessageToThread body = new AddMessageToThread();
        body.role = "user";
        body.content = testMessage;
        string bodyJson = JsonUtility.ToJson(body);
        byte[] addMessageBodyRaw = Encoding.UTF8.GetBytes(bodyJson);
        UnityWebRequest addMessageRequest = ConfigureRequest("https://api.openai.com/v1/threads/" + threadId + "/messages", "POST", addMessageBodyRaw);
        yield return addMessageRequest.SendWebRequest();
        if (RequestHasFailed(addMessageRequest))
        {
            Debug.LogError(addMessageRequest.error);
            yield break;
        }

        //Create run
        CreateRunBody createRunBody = new CreateRunBody();
        createRunBody.assistant_id = assistantId;
        string createRunBodyJson = JsonUtility.ToJson(createRunBody);
        byte[] createRunBodyRaw = Encoding.UTF8.GetBytes(createRunBodyJson);
        UnityWebRequest creatRunBodyRequest = ConfigureRequest("https://api.openai.com/v1/threads/" + threadId + "/runs", "POST", createRunBodyRaw);
        yield return creatRunBodyRequest.SendWebRequest();
        if (RequestHasFailed(creatRunBodyRequest))
        {
            Debug.LogError(creatRunBodyRequest.error);
            yield break;
        }

        //Get Messages
        CreateRunResponse runResponse = JsonUtility.FromJson<CreateRunResponse>(creatRunBodyRequest.downloadHandler.text);
        string runId = runResponse.id;
        Debug.Log("Running with model: " + runResponse.model);
        Message[] messages = new Message[0];
        int requestAttempts = 0;
        while (MessagesHaveContents(messages) == false)
        {
            yield return new WaitForSeconds(0.5f);
            UnityWebRequest getMessagesRequest = ConfigureRequest("https://api.openai.com/v1/threads/" + threadId + "/messages?run_id=" + runId, "GET", null);
            yield return getMessagesRequest.SendWebRequest();
            requestAttempts++;
            if (RequestHasFailed(getMessagesRequest))
            {
                Debug.LogError(getMessagesRequest.error);
                yield break;
            }
            GetMessagesResponse getMessagesResponse = JsonUtility.FromJson<GetMessagesResponse>(getMessagesRequest.downloadHandler.text);
            messages = getMessagesResponse.data;
        }
        Debug.Log($"Message found after {requestAttempts} attempts");
        Message firstMessage = messages[0];
        MessageContent[] messageContents = firstMessage.content;
        Debug.Log("Message Contents length: " + messageContents.Length);
        foreach (var content in messageContents)
        {
            Debug.Log(content.text.value);
            OnResponseRecieved.Invoke(content.text.value);
        }
    }

    bool MessagesHaveContents(Message[] messages)
    {
        if (messages.Length > 0)
        {
            foreach (Message message in messages)
            {
                if (message.content != null)
                {
                    if (message.content.Length > 0)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    bool RequestHasFailed(UnityWebRequest unityWebRequest)
    {
        if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError || unityWebRequest.result == UnityWebRequest.Result.ProtocolError)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    UnityWebRequest ConfigureRequest(string apiEndPoint, string requestType, byte[] bodyRaw)
    {
        UnityWebRequest request = new UnityWebRequest(apiEndPoint, requestType);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
        request.SetRequestHeader("Authorization", "Bearer " + openAIConfig.secretAPIKey);
        return request;
    }

    [System.Serializable]
    public struct CreateThreadResponse
    {
        public string id;
    }

    [System.Serializable]
    public struct AddMessageToThread
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public struct CreateRunBody
    {
        public string assistant_id;
    }

    [System.Serializable]
    public struct CreateRunResponse
    {
        public string id;
        public string last_error;
        public string instructions;
        public string model;
    }

    [System.Serializable]
    public struct GetMessagesResponse
    {
        public Message[] data;
    }

    [System.Serializable]
    public struct Message
    {
        public string id;
        public string role;
        public MessageContent[] content;
    }

    [System.Serializable]
    public struct MessageContent
    {
        public string type;
        public MessageText text;
    }

    [System.Serializable]
    public struct MessageText
    {
        public string value;
        public string [] annotations;
    }



}
