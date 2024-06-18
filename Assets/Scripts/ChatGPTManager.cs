using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using UnityEngine.Events;
using System;
using Oculus.Voice.Dictation;
using TMPro;
using Meta.WitAi.TTS.Utilities;

public class ChatGPTManager : MonoBehaviour
{
    [TextArea(5, 20)]
    public string personality;
    [TextArea(5, 20)]
    public string scene;
    public string avatarName = "Oliver";
    public int maxResponseLimit = 25;
    private OpenAIApi openAI = new OpenAIApi();
    private List<ChatMessage> messages = new List<ChatMessage>();
    public UnityEvent<string> onResponseEvent = new UnityEvent<string>();
    public List<NPCAction> npcActions = new List<NPCAction>();
    public AppDictationExperience voiceToText;
    public TMP_Text voiceInputLabel;
    public OpenAIConfigurationSO openAIConfiguration;
    public TTSSpeaker textToSpeechSpeaker;
    string currentOpenAIResponse;

    [Header("Physical Interaction Settings")]
    public float minConversationRange = 5f;
    Transform userView;
    public Transform avatarTransform;
    public TMP_Text npcFeedbackText;
    public enum NPCState { None, IsListening, IsThinking, IsTalking};
    public NPCState currentNPCState = NPCState.None;
    public Animator animator;
    const string LISTEN_TRIGGER = "Listen";
    const string THINK_TRIGGER = "Think";
    const string TALK_TRIGGER = "Talk";
    const string IDLE_TRIGGER = "Idle";

    [Serializable]
    public struct NPCAction
    {
        public string actionKeyword;
        [TextArea(3, 5)]
        public string actionDescription;
        public UnityEvent actionEvent;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (openAIConfiguration == null && string.IsNullOrEmpty(openAIConfiguration.secretAPIKey))
        {
            Debug.LogError("No Secret Api key for Open AI, please create a configuration");
        }
        else
        {
            openAI = new OpenAIApi(openAIConfiguration.secretAPIKey);
        }
        voiceToText.DictationEvents.OnFullTranscription.AddListener(OnFullTranscription);;
        textToSpeechSpeaker.Events.OnAudioClipPlaybackStart.AddListener(OnTextToSpeechStarted);
        textToSpeechSpeaker.Events.OnAudioClipPlaybackFinished.AddListener(OnTextToSpeechFinished);
        onResponseEvent.AddListener(OnResponse);
        userView = Camera.main.transform;
        voiceInputLabel.text = "";
    }

    void OnFullTranscription(string transcription)
    {
        voiceInputLabel.text = transcription;
        AskChatGPT(transcription);
        SetNPCState(NPCState.IsThinking);
    }

    public string GetInstructions()
    {
        string instructions = "You are a Unity developer called " + avatarName + " who works for Swingtech consulting. + \n" +
            "You specialize in VR development\n" +
            "You must answer in less than " + maxResponseLimit + " words.\n" +
            "Here is the information about your personality: \n" +
            personality + "\n" +
            "Here is the information about the scene around you: \n" +
            scene + "\n" +
            BuildActionInstructions() +
            "Here is the message of the player: \n";
        return instructions;
    }

    public string BuildActionInstructions()
    {
        string instructions = "";
        foreach (NPCAction action in npcActions)
        {
            instructions += "if I imply that I want you to do the following: " + action.actionDescription
                + ". You must add to your answer the following key word: " + action.actionKeyword + ". \n";
        }
        return instructions;
    }

    public async void AskChatGPT(string newText)
    {
        ChatMessage newMessage = new ChatMessage();
        newMessage.Content = GetInstructions() + newText;
        newMessage.Role = "user";
        messages.Add(newMessage);

        CreateChatCompletionRequest request = new CreateChatCompletionRequest();
        request.Messages = messages;
        request.Model = "gpt-3.5-turbo";

        var response = await openAI.CreateChatCompletion(request);

        if (response.Choices != null && response.Choices.Count > 0)
        {
            var chatResponse = response.Choices[0].Message;

            foreach (NPCAction action in npcActions)
            {
                if (chatResponse.Content.Contains(action.actionKeyword))
                {
                    //string textNoKeyword = chatResponse.Content.Replace(action.actionKeyword, "");
                    //chatResponse.Content = textNoKeyword;
                    action.actionEvent.Invoke();
                }
            }

            messages.Add(chatResponse);
            onResponseEvent.Invoke(chatResponse.Content);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 userToHeadVector = avatarTransform.position - userView.transform.position;
        float distanceSquared = userToHeadVector.sqrMagnitude;
        if (distanceSquared < minConversationRange)
        {
            if (currentNPCState == NPCState.None)
            {
                SetNPCState(NPCState.IsListening);
            }
        }
        else
        {
            if (currentNPCState == NPCState.IsListening)
            {
                SetNPCState(NPCState.None);
            }
        }
    }

    void SetNPCState(NPCState desiredNPCState)
    {
        currentNPCState = desiredNPCState;
        Debug.Log("Changing State to: " + desiredNPCState.ToString());

        switch (currentNPCState)
        {
            case NPCState.None:
                //Do nothing
                npcFeedbackText.text = "";
                animator.SetTrigger(IDLE_TRIGGER);
                break;
            case NPCState.IsListening:
                npcFeedbackText.text = "Listening...";
                animator.SetTrigger(LISTEN_TRIGGER);
                voiceToText.Activate();
                break;
            case NPCState.IsThinking:
                npcFeedbackText.text = "Thinking...";
                animator.SetTrigger(THINK_TRIGGER);
                voiceToText.Deactivate();
                break;
            case NPCState.IsTalking:
                animator.SetTrigger(TALK_TRIGGER);
                npcFeedbackText.text = currentOpenAIResponse;
                break;
        }
    }

    void OnResponse(string message)
    {
        currentOpenAIResponse = message;
    }

    void OnTextToSpeechStarted(AudioClip clip)
    {
        SetNPCState(NPCState.IsTalking);
    }

    private void OnTextToSpeechFinished(AudioClip clip)
    {
        SetNPCState(NPCState.None);
    }
}
