using UnityEngine;
using UnityEngine.Events;
using Oculus.Voice.Dictation;
using TMPro;
using Meta.WitAi.TTS.Utilities;
using OpenAIForUnity;
using System;
using System.Collections;

public class AICharacter : MonoBehaviour
{
    public string displayName = "Oliver";
    [TextArea(3, 15)]public string initialMessage = "";
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
    [Range(0, 1f)] public float rotationFactor = 0.5f;
    bool isTurningToFaceUser = false;
    public enum NPCState { None, IsListening, IsThinking, IsTalking};
    public NPCState currentNPCState = NPCState.None;
    public Animator animator;
    const string LISTEN_TRIGGER = "Listen";
    const string THINK_TRIGGER = "Think";
    const string TALK_TRIGGER = "Talk";
    const string IDLE_TRIGGER = "Idle";
    bool isFirstInteraction = true;


    // Start is called before the first frame update
    void Start()
    {
        voiceToText.DictationEvents.OnPartialTranscription.AddListener(OnPartialDescription);
        voiceToText.DictationEvents.OnFullTranscription.AddListener(OnFullTranscription);
        textToSpeechSpeaker.Events.OnAudioClipPlaybackStart.AddListener(OnTextToSpeechStarted);
        textToSpeechSpeaker.Events.OnPlaybackQueueComplete.AddListener(OnTextToSpeechFinished);
        aiAssistant.OnResponseRecieved.AddListener(OnAIAssistantResponse);
        onResponseEvent.AddListener(OnResponse);
        SetNPCState(NPCState.None);
        animator.ResetTrigger(IDLE_TRIGGER);
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

    [ContextMenu("Ask Test Message")]
    void AskTestMessage()
    {
        OnFullTranscription("Anything else?");
    }

    // Update is called once per frame
    void Update()
    {
        WaitForUserToApproachToStartListening();
        if (isTurningToFaceUser)
        {
            TurnToFaceUser();
        }
    }

    private void WaitForUserToApproachToStartListening()
    {
        Vector3 userToHeadVector = avatarTransform.position - userView.transform.position;
        float distanceSquared = userToHeadVector.sqrMagnitude;
        if (distanceSquared < minConversationRange)
        {
            if (currentNPCState == NPCState.None)
            {
                SetNPCState(NPCState.IsListening);
            }
            if (isFirstInteraction && !string.IsNullOrEmpty(initialMessage))
            {
                StartCoroutine(SendInitialMessageAfterDelay());
                isFirstInteraction = false;
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

    IEnumerator SendInitialMessageAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        SetNPCState(NPCState.IsThinking);
        textToSpeechSpeaker.Speak(initialMessage);

    }

    private void TurnToFaceUser()
    {
        Vector3 directionToUser = GetDirectionToUser();
        Debug.DrawLine(avatarTransform.position, directionToUser * 3f, Color.yellow);
        Quaternion lookAtRotation = Quaternion.LookRotation(directionToUser);
        transform.rotation = Quaternion.Lerp(transform.rotation, lookAtRotation, rotationFactor);
    }

    Vector3 GetDirectionToUser()
    {
        Vector3 directionToUser = (userView.transform.position - transform.position);
        directionToUser.y = 0;
        directionToUser.Normalize();
        return directionToUser;
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
                isTurningToFaceUser = false;
                voiceToText.Deactivate();
                break;
            case NPCState.IsListening:
                npcStatusLabel.text = "Listening...";
                animator.SetTrigger(LISTEN_TRIGGER);
                isTurningToFaceUser = true;
                voiceToText.Activate();
                break;
            case NPCState.IsThinking:
                npcStatusLabel.text = "Thinking...";
                animator.SetTrigger(THINK_TRIGGER);
                isTurningToFaceUser = false;
                voiceToText.Deactivate();
                break;
            case NPCState.IsTalking:
                animator.SetTrigger(TALK_TRIGGER);
                npcStatusLabel.text = displayName;
                isTurningToFaceUser = true;
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

    private void OnTextToSpeechFinished()
    {
        SetNPCState(NPCState.None);
    }
}
