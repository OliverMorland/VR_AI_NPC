using UnityEngine;
using UnityEngine.Events;
using Oculus.Voice.Dictation;
using TMPro;
using Meta.WitAi.TTS.Utilities;
using OpenAIForUnity;

public class AICharacter : MonoBehaviour
{
    public string displayName = "Oliver";
    public OpenAIAssistant aiAssistant; 
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
    public TMP_Text npcStatusLabel;
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
        voiceToText.DictationEvents.OnPartialTranscription.AddListener(OnPartialDescription);
        voiceToText.DictationEvents.OnFullTranscription.AddListener(OnFullTranscription);
        textToSpeechSpeaker.Events.OnAudioClipPlaybackStart.AddListener(OnTextToSpeechStarted);
        textToSpeechSpeaker.Events.OnAudioClipPlaybackFinished.AddListener(OnTextToSpeechFinished);
        aiAssistant.OnResponseRecieved.AddListener(OnAIAssistantResponse);
        onResponseEvent.AddListener(OnResponse);
        SetNPCState(NPCState.None);
        userView = Camera.main.transform;
        voiceInputLabel.text = "";
    }

    private void OnAIAssistantResponse(string response)
    {
        onResponseEvent.Invoke(response);
    }

    private void OnPartialDescription(string transcript)
    {
        if (currentNPCState == NPCState.IsListening)
        {
            voiceInputLabel.text = transcript;
        }
    }

    void OnFullTranscription(string transcription)
    {
        if (currentNPCState == NPCState.IsListening)
        {
            voiceInputLabel.text = transcription;
            aiAssistant.AskAssistant(transcription);
            SetNPCState(NPCState.IsThinking);
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
                npcStatusLabel.text = displayName;
                animator.SetTrigger(IDLE_TRIGGER);
                voiceToText.Deactivate();
                break;
            case NPCState.IsListening:
                npcStatusLabel.text = "Listening...";
                animator.SetTrigger(LISTEN_TRIGGER);
                voiceToText.Activate();
                break;
            case NPCState.IsThinking:
                npcStatusLabel.text = "Thinking...";
                animator.SetTrigger(THINK_TRIGGER);
                voiceToText.Deactivate();
                break;
            case NPCState.IsTalking:
                animator.SetTrigger(TALK_TRIGGER);
                npcStatusLabel.text = displayName;
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
