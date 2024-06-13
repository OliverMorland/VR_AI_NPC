using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using UnityEngine.Events;
using System;
using Oculus.Voice;
using Oculus.Voice.Dictation;

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

    [Serializable]
    public struct NPCAction
    {
        public string actionKeyword;
        [TextArea(3, 5)]
        public string actionDescription;
        public UnityEvent actionEvent;
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

    // Start is called before the first frame update
    void Start()
    {
        voiceToText.DictationEvents.OnFullTranscription.AddListener(AskChatGPT);;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
