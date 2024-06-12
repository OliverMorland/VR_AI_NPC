using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ReadyPlayerMe.Core;

public class NPCLoader : MonoBehaviour
{
    public string avatar_url = "https://models.readyplayer.me/6669a3e522f78f074aa87ee4.glb";
    GameObject avatar;

    // Start is called before the first frame update
    void Start()
    {
        AvatarObjectLoader avatarObjectLoader = new AvatarObjectLoader();
        avatarObjectLoader.OnCompleted += (_, arg) =>
        {
            Debug.Log("Avatar Loaded");
            avatar = arg.Avatar;
            avatar.transform.position = transform.position;
            avatar.transform.rotation = transform.rotation;
        };
        avatarObjectLoader.LoadAvatar(avatar_url);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
