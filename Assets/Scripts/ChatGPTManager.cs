using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI;
using UnityEngine.Events;
using System;

public class ChatGPTManager : MonoBehaviour
{
    [TextArea(5, 20)]
    public string personality;
    [TextArea(5, 20)]
    public string scene;
    public string avatarName = "Oliver";
    public int maxResponseLimit = 20;
    private OpenAIApi openAI = new OpenAIApi();
    private List<ChatMessage> messages = new List<ChatMessage>();
    public UnityEvent<string> onResponseEvent = new UnityEvent<string>();

    public string GetInstructions()
    {
        string instructions = "You are a Unity developer called " + avatarName + " who is dressed in a blue suit participating at a healthcare conference in Baltimore. + \n" +
            "You are trying to persuade me to go to a booth to try a VR experience that you have built which will teach me to survey health care facilities.\n" +
            "You must answer in less than " + maxResponseLimit + " words.\n" +
            "Here is the information about your personality: \n" +
            personality + "\n" +
            "Here is the information about the scene around you: \n" +
            scene + "\n" +
            "Here is the message of the player: \n";
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
            messages.Add(chatResponse);
            onResponseEvent.Invoke(chatResponse.Content);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
