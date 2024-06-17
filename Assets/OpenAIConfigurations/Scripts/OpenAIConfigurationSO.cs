using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Open AI Configuration", menuName = "Scriptable Objects/Create Open AI Configuration")]
public class OpenAIConfigurationSO : ScriptableObject
{
    public string secretAPIKey = "";
    public string organizationId = "";
}
