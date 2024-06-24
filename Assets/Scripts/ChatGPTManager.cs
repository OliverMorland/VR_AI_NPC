using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using UnityEngine.Events;
using System;
using Oculus.Voice.Dictation;
using TMPro;
using Meta.WitAi.TTS.Utilities;
using OpenAIForUnity;

public class ChatGPTManager : MonoBehaviour
{
    public string avatarName = "Oliver";
    public int maxResponseLimit = 25;
    public AIAssistant aiAssistant;
    private OpenAIApi openAI = new OpenAIApi();
    private List<ChatMessage> messages = new List<ChatMessage>();
    public UnityEvent<string> onResponseEvent = new UnityEvent<string>();
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
        voiceToText.DictationEvents.OnPartialTranscription.AddListener(OnPartialDescription);
        voiceToText.DictationEvents.OnFullTranscription.AddListener(OnFullTranscription);
        textToSpeechSpeaker.Events.OnAudioClipPlaybackStart.AddListener(OnTextToSpeechStarted);
        textToSpeechSpeaker.Events.OnAudioClipPlaybackFinished.AddListener(OnTextToSpeechFinished);
        aiAssistant.OnResponseRecieved.AddListener(OnAIAssistantResponse);
        onResponseEvent.AddListener(OnResponse);
        userView = Camera.main.transform;
        voiceInputLabel.text = "";
    }

    private void OnAIAssistantResponse(string response)
    {
        onResponseEvent.Invoke(response);
    }

    private void OnPartialDescription(string transcript)
    {
        voiceInputLabel.text = transcript;
    }

    void OnFullTranscription(string transcription)
    {
        voiceInputLabel.text = transcription;
        //AskChatGPT(transcription);
        aiAssistant.AskAssistant(transcription);
        SetNPCState(NPCState.IsThinking);
    }

    public async void AskChatGPT(string newText)
    {
        ChatMessage systemMessage = new ChatMessage();
        systemMessage.Role = "system";
        systemMessage.Content = "Your name is Oliver and you are surveying this bedroom in a long term care facility for safety risks. There is a smelly plate with old food lying around the bed, a syringe has been left in the trash can and a candle has been placed too close to an oxygen machine. You're in a bad mood and are reluctant to answer questions because you still haven't finished your job and and a football match is happening soon that you want to watch. England are playing Denmark in the European championships and it's an important game. All your answers must be less than 100 words long.";
        ChatMessage newMessage = new ChatMessage();
        newMessage.Content = newText;
        newMessage.Role = "user";
        messages.Add(systemMessage);
        messages.Add(newMessage);

        CreateChatCompletionRequest request = new CreateChatCompletionRequest();
        request.Messages = messages;
        request.Model = "gpt-3.5-turbo";

        var response = await openAI.CreateChatCompletion(request);

        if (response.Choices != null && response.Choices.Count > 0)
        {
            var chatResponse = response.Choices[0].Message;
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

        switch (currentNPCState)
        {
            case NPCState.None:
                //Do nothing
                npcFeedbackText.text = "";
                animator.SetTrigger(IDLE_TRIGGER);
                voiceToText.Deactivate();
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
                voiceInputLabel.text = "";
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
