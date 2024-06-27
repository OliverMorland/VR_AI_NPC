using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Integrations;
using Meta.WitAi.TTS.Utilities;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpeakerVoiceTester : MonoBehaviour
{
    public TTSWit textToSpeech;
    public TTSSpeaker speaker;
    public Button changeVoiceButton;
    public TMP_Text voiceButtonText;

    TTSVoiceSettings[] voiceSettings;
    int counter = 0;

    // Start is called before the first frame update
    void Start()
    {
        voiceSettings = textToSpeech.GetAllPresetVoiceSettings();
        changeVoiceButton.onClick.AddListener(OnButtonClicked);
        voiceButtonText.text = speaker.VoiceID;
    }

    void OnButtonClicked()
    {
        counter++;
        if (counter == voiceSettings.Length)
        {
            counter = 0;
        }
        speaker.SetVoiceOverride(voiceSettings[counter]);
        voiceButtonText.text = voiceSettings[counter].SettingsId;
        speaker.Speak("Hello, my name is Oliver.");
    }
}
