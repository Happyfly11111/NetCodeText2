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
    [SerializeField]
    private string _defaultIp = "127.0.0.1"; // 默认IP地址

    // Start is called before the first frame update
    void Start()
    {
        Button createButton = _canvas.Find("CreateBtn").GetComponent<Button>();
        createButton.onClick.AddListener(OnCreateClick);

        Button joinButton = _canvas.Find("JoinBtn").GetComponent<Button>();
        joinButton.onClick.AddListener(OnJoinClick);

        _ip = _canvas.Find("IP").GetComponent<TMP_InputField>();

        // 设置默认IP地址
        _ip.text = _defaultIp;
    }

    private void OnCreateClick()
    {
        string ip = GetValidatedIP(_ip.text);
        if (ip == null) return;

        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(ip, 7777);

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
        string ip = GetValidatedIP(_ip.text);
        if (ip == null) return;

        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        transport.SetConnectionData(ip, 7777);

        NetworkManager.Singleton.StartClient();

    }
    private string GetValidatedIP(string ip)
    {
        // 如果用户没有输入，使用默认IP
        if (string.IsNullOrEmpty(ip.Trim()))
        {
            ip = _defaultIp;
            Debug.Log($"使用默认IP: {ip}");
            return ip;
        }

        // 简单验证IP格式（可以根据需要扩展更严格的验证）
        if (ip == "localhost" || System.Text.RegularExpressions.Regex.IsMatch(ip,
            @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
        {
            return ip;
        }
        else
        {
            Debug.LogError($"无效的IP地址: {ip}");
            return null;
        }
    }
}
