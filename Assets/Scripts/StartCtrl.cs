using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class StartCtrl : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Button createButton = GetComponent<Button>();
        createButton.onClick.AddListener(OnCreateClick);

        Button joinButton = GetComponent<Button>();
        joinButton.onClick.AddListener(OnJoinClick);
    }

    private void OnCreateClick()
    {
        NetworkManager.Singleton.StartHost();
    }

    private void OnJoinClick(){
        NetworkManager.Singleton.StartClient();

    }
}
