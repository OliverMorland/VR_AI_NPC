using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using static AIAssistant;

public class AIAssistant : MonoBehaviour
{
    public OpenAIConfigurationSO openAIConfig;
    public string assistantId = "asst_iQOTrPcuusPL2UEvd5gNjTHl";
    public UnityEvent<string> OnResponseRecieved;
    public string testMessage = "Hello, who are you?";
    private string threadId;
    const string apiEndPointRoot = "https://api.openai.com/v1/threads"; 

    // Start is called before the first frame update
    void Start()
    {
        CreateNewThread_Cleaner();
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

    void CreateNewThread_Cleaner()
    {
        WebRequestData requestData = new WebRequestData();
        requestData.path = apiEndPointRoot;
        requestData.methodType = WebRequestData.MethodType.POST;
        DispatchWebRequest(requestData,
        failedResult =>
        {
            Debug.LogError(failedResult);
        },
        successResult =>
        {
            CreateThreadResponse createThreadResponse = JsonUtility.FromJson<CreateThreadResponse>(successResult);
            threadId = createThreadResponse.id;
        });
    }

    [ContextMenu("Send Test Message")]
    void SendTestMessage()
    {
        StartCoroutine(AskAssistantAsync(testMessage));
    }

    public void AskAssistant(string userMessage)
    {
        StartCoroutine(AskAssistantAsync(userMessage));
    }

    private IEnumerator AskAssistantAsync(string userMessage)
    {
        Debug.Log("Sending message");
        //Create message
        AddMessageToThread body = new AddMessageToThread();
        body.role = "user";
        body.content = userMessage;
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

    [ContextMenu("Cleaner Functions Test")]
    void AddMessage_Cleaner()
    {
        DispatchAddMessageRequest("Hello, who are you?", 
        failedResult =>
        {
            Debug.LogError(failedResult);
        },
        succeededResult =>
        {
            //CreateRun_Cleaner
            Debug.Log(succeededResult);
            RunMessages_Cleaner();
        });
    }

    void RunMessages_Cleaner()
    {
        DispatchCreateRunRequest(
        failedResult =>
        {
            Debug.LogError(failedResult);
        },
        succeededResult =>
        {
            Debug.Log(succeededResult);
            CreateRunResponse runResponse = JsonUtility.FromJson<CreateRunResponse>(succeededResult);
            string runId = runResponse.id;
            StartCoroutine(GetResponse_Cleaner(runId));
        });
    }

    IEnumerator GetResponse_Cleaner(string runId)
    {
        Message[] messages = new Message[0];
        int requestAttempts = 0;
        while (MessagesHaveContents(messages) == false)
        {
            yield return new WaitForSeconds(0.5f);
            UnityWebRequest getMessagesRequest = CreateListMessagesRequest(runId);
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
        string response = GetResponseFromMessages(messages);
        Debug.Log(response);
        OnResponseRecieved.Invoke(response);
    }

    string GetResponseFromMessages(Message[] messages)
    {
        string response = "";
        Message firstMessage = messages[0];
        MessageContent[] messageContents = firstMessage.content;
        Debug.Log("Message Contents length: " + messageContents.Length);
        foreach (var content in messageContents)
        {
            response = content.text.value;
        }
        return response;
    }

    void DispatchAddMessageRequest(string userMessage, Action<string> onRequestFailed, Action<string> onRequestSucceeded)
    {
        WebRequestData requestData = new WebRequestData();
        requestData.path = $"{apiEndPointRoot}/{threadId}/messages";
        requestData.methodType = WebRequestData.MethodType.POST;
        requestData.body = CreateAddMessageRequestBody(userMessage);
        DispatchWebRequest(requestData, onRequestFailed, onRequestSucceeded);
    }

    void DispatchCreateRunRequest(Action<string> onRequestFailed, Action<string> onRequestSucceeded)
    {
        WebRequestData requestData = new WebRequestData();
        requestData.path = $"{apiEndPointRoot}/{threadId}/runs";
        requestData.methodType = WebRequestData.MethodType.POST;
        requestData.body = CreateRunRequestBody(assistantId);
        DispatchWebRequest(requestData, onRequestFailed, onRequestSucceeded);   
    }

    UnityWebRequest CreateListMessagesRequest(string runId)
    {
        WebRequestData requestData = new WebRequestData();
        requestData.path = $"{apiEndPointRoot}/{threadId}/messages?run_id={runId}";
        requestData.methodType = WebRequestData.MethodType.GET;
        return CreateWebRequest(requestData);
    }

    public struct WebRequestData
    {
        public string path;
        public enum MethodType { POST, GET}
        public MethodType methodType;
        public byte[] body;
    }

    void DispatchWebRequest(WebRequestData webRequestData, Action<string> onRequestFailed, Action<string> onRequestSucceeded)
    {
        //string methodAsString = GetMethodAsString(webRequestData.methodType);
        //UnityWebRequest webRequest = new UnityWebRequest(webRequestData.path, methodAsString);
        //webRequest.uploadHandler = new UploadHandlerRaw(webRequestData.body);
        //webRequest.downloadHandler = new DownloadHandlerBuffer();
        //webRequest.SetRequestHeader("Content-Type", "application/json");
        //webRequest.SetRequestHeader("OpenAI-Beta", "assistants=v2");
        //webRequest.SetRequestHeader("Authorization", "Bearer " + openAIConfig.secretAPIKey);
        UnityWebRequest webRequest = CreateWebRequest(webRequestData);
        StartCoroutine(DispatchWebRequestAsync(webRequest, onRequestFailed, onRequestSucceeded));
    }

    UnityWebRequest CreateWebRequest(WebRequestData webRequestData)
    {
        string methodAsString = GetMethodAsString(webRequestData.methodType);
        UnityWebRequest webRequest = new UnityWebRequest(webRequestData.path, methodAsString);
        webRequest.uploadHandler = new UploadHandlerRaw(webRequestData.body);
        webRequest.downloadHandler = new DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Content-Type", "application/json");
        webRequest.SetRequestHeader("OpenAI-Beta", "assistants=v2");
        webRequest.SetRequestHeader("Authorization", "Bearer " + openAIConfig.secretAPIKey);
        return webRequest;
    }

    IEnumerator DispatchWebRequestAsync(UnityWebRequest webRequest, Action<string> onRequestFailed, Action<string> onRequestSucceeded)
    {
        yield return webRequest.SendWebRequest();
        if (RequestHasFailed(webRequest))
        {
            onRequestFailed.Invoke(webRequest.error);
        }
        else
        {
            onRequestSucceeded.Invoke(webRequest.downloadHandler.text);
        }
    }

    string GetMethodAsString(WebRequestData.MethodType methodType)
    {
        string methodAsString = "";
        switch (methodType)
        {
            case WebRequestData.MethodType.POST:
                methodAsString = "POST";
                break;
            case WebRequestData.MethodType.GET:
                methodAsString = "GET";
                break;
            default:
                methodAsString = "GET";
                break;
        }
        return methodAsString;
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

    byte[] CreateAddMessageRequestBody(string userMessage)
    {
        AddMessageToThread body = new AddMessageToThread();
        body.role = "user";
        body.content = "Hello, who are you?";
        string bodyJson = JsonUtility.ToJson(body);
        return Encoding.UTF8.GetBytes(bodyJson);
    }

    [System.Serializable]
    public struct CreateRunBody
    {
        public string assistant_id;
    }

    byte[] CreateRunRequestBody(string assistantId)
    {
        CreateRunBody body = new CreateRunBody();
        body.assistant_id = assistantId;
        string bodyJson = JsonUtility.ToJson(body);
        return Encoding.UTF8.GetBytes(bodyJson);
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
