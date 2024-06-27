using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace OpenAIForUnity
{
    public class OpenAIAssistant : MonoBehaviour
    {
        public OpenAIConfigurationSO openAIConfig;
        public string assistantId = "asst_iQOTrPcuusPL2UEvd5gNjTHl";
        public UnityEvent<string> OnResponseRecieved;
        private string threadId;
        const string apiEndPointRoot = "https://api.openai.com/v1/threads";
        const float LIST_MESSAGES_INTERVAL = 0.5f;
        [TextArea(3,15)] public string initialMessage = "";

        // Start is called before the first frame update
        void Start()
        {
            CreateNewThread();
        }

        void CreateNewThread()
        {
            WebRequestData requestData = new WebRequestData();
            requestData.path = apiEndPointRoot;
            requestData.methodType = WebRequestData.MethodType.POST;
            //requestData.body = CreateNewThreadRequestBody();
            DispatchWebRequest(requestData,
            failedResult =>
            {
                Debug.LogError(failedResult);
            },
            successResult =>
            {
                CreateThreadResult createThreadResponse = JsonUtility.FromJson<CreateThreadResult>(successResult);
                threadId = createThreadResponse.id;
                //if (!string.IsNullOrEmpty(initialMessage))
                //{
                //    AskAssistant(initialMessage);
                //}
            });
        }

        byte[] CreateNewThreadRequestBody()
        {
            CreateThreadBody body = new CreateThreadBody();
            body.messages = CreateFirstMessages();
            string bodyJson = JsonUtility.ToJson(body);
            return Encoding.UTF8.GetBytes(bodyJson);
        }

        private Message[] CreateFirstMessages()
        {
            Message firstMessage = new Message();
            MessageContent content = new MessageContent();
            content.text.value = initialMessage;
            content.type = "text";
            firstMessage.content = new MessageContent[] { content };
            firstMessage.role = "assistant";
            return new Message[] { firstMessage };
        }

        void DispatchWebRequest(WebRequestData webRequestData, Action<string> onRequestFailed, Action<string> onRequestSucceeded)
        {
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

        public void AskAssistant(string userMessage)
        {
            AddMessage(userMessage);
        }

        void AddMessage(string userMessage)
        {
            DispatchAddMessageRequest(userMessage,
            failedResult =>
            {
                Debug.LogError(failedResult);
            },
            succeededResult =>
            {
                Debug.Log(succeededResult);
                RunMessages();
            });
        }

        public void AddAssistantMessage()
        {

        }

        void RunMessages()
        {
            DispatchRunRequest(
            failedResult =>
            {
                Debug.LogError(failedResult);
            },
            succeededResult =>
            {
                Debug.Log(succeededResult);
                RunResult runResult = JsonUtility.FromJson<RunResult>(succeededResult);
                string runId = runResult.id;
                StartCoroutine(WaitForAssistantResponse(runId));
            });
        }

        [ContextMenu("List Messages")]
        void ListMessages()
        {
            StartCoroutine(ListMessagesAsync());
        }

        IEnumerator ListMessagesAsync()
        {
            WebRequestData requestData = new WebRequestData();
            requestData.path = $"{apiEndPointRoot}/{threadId}/messages";
            requestData.methodType = WebRequestData.MethodType.GET;
            UnityWebRequest webRequest = CreateWebRequest(requestData);
            yield return webRequest.SendWebRequest();
            if (RequestHasFailed(webRequest))
            {
                Debug.LogError(webRequest.error);
            }
            else
            {
                Debug.Log(webRequest.downloadHandler.text);
            }
        }

        IEnumerator WaitForAssistantResponse(string runId)
        {
            Message[] messages = new Message[0];
            int requestAttempts = 0;
            while (MessagesHaveContents(messages) == false)
            {
                yield return new WaitForSeconds(LIST_MESSAGES_INTERVAL);
                UnityWebRequest getMessagesRequest = CreateListMessagesRequest(runId);
                yield return getMessagesRequest.SendWebRequest();
                requestAttempts++;
                if (RequestHasFailed(getMessagesRequest))
                {
                    Debug.LogError(getMessagesRequest.error);
                    yield break;
                }
                ListMessagesResult getMessagesResponse = JsonUtility.FromJson<ListMessagesResult>(getMessagesRequest.downloadHandler.text);
                messages = getMessagesResponse.data;
            }
            Debug.Log($"Message found after {requestAttempts} attempts");
            string response = GetResponseFromMessages(messages);
            Debug.Log(response);
            OnResponseRecieved.Invoke(response);
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

        UnityWebRequest CreateListMessagesRequest(string runId)
        {
            WebRequestData requestData = new WebRequestData();
            requestData.path = $"{apiEndPointRoot}/{threadId}/messages?run_id={runId}";
            requestData.methodType = WebRequestData.MethodType.GET;
            return CreateWebRequest(requestData);
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

        byte[] CreateAddMessageRequestBody(string userMessage)
        {
            AddMessageRequestBody body = new AddMessageRequestBody();
            body.role = "user";
            body.content = userMessage;
            string bodyJson = JsonUtility.ToJson(body);
            return Encoding.UTF8.GetBytes(bodyJson);
        }

        void DispatchRunRequest(Action<string> onRequestFailed, Action<string> onRequestSucceeded)
        {
            WebRequestData requestData = new WebRequestData();
            requestData.path = $"{apiEndPointRoot}/{threadId}/runs";
            requestData.methodType = WebRequestData.MethodType.POST;
            requestData.body = CreateRunRequestBody(assistantId);
            DispatchWebRequest(requestData, onRequestFailed, onRequestSucceeded);
        }

        byte[] CreateRunRequestBody(string assistantId)
        {
            RunRequestBody body = new RunRequestBody();
            body.assistant_id = assistantId;
            string bodyJson = JsonUtility.ToJson(body);
            return Encoding.UTF8.GetBytes(bodyJson);
        }

        public struct WebRequestData
        {
            public string path;
            public enum MethodType { POST, GET }
            public MethodType methodType;
            public byte[] body;
        }

        [System.Serializable]
        public struct CreateThreadResult
        {
            public string id;
        }

        [System.Serializable]
        public struct AddMessageRequestBody
        {
            public string role;
            public string content;
        }

        [System.Serializable]
        public struct RunRequestBody
        {
            public string assistant_id;
        }

        [System.Serializable]
        public struct RunResult
        {
            public string id;
            public string last_error;
            public string instructions;
            public string model;
        }

        [System.Serializable]
        public struct ListMessagesResult
        {
            public Message[] data;
        }

        [System.Serializable]
        public struct CreateThreadBody
        {
            public Message[] messages;
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
            public string[] annotations;
        }

    }
}
