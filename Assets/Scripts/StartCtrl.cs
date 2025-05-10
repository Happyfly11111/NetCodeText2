using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartCtrl : MonoBehaviour
{
    [SerializeField]
    Transform _canvas;
    [SerializeField]
    private TMP_InputField _ip;

    // Start is called before the first frame update
    void Start()
    {
        Button createButton = _canvas.Find("CreateBtn").GetComponent<Button>();
        createButton.onClick.AddListener(OnCreateClick);

        Button joinButton = _canvas.Find("JoinBtn").GetComponent<Button>();
        joinButton.onClick.AddListener(OnJoinClick);

        _ip = _canvas.Find("IP").GetComponent<TMP_InputField>();
    }

    private void OnCreateClick()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(_ip.text, 7777);

        NetworkManager.Singleton.StartHost();

        // 检查GameManager.Instance是否为null
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScene("Lobby");
        }
        else
        {
            Debug.LogError("GameManager.Instance is null. Please ensure GameManager is initialized.");
        }
    }

    private void OnJoinClick()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(_ip.text, 7777);

        NetworkManager.Singleton.StartClient();
        
    }
}
