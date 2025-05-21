using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.VisualScripting;

public class GameCtrl : NetworkBehaviour
{
    [SerializeField]
    private Transform _canvas;
    TMP_InputField _input;
    RectTransform _content;
    GameObject _dialogCell;
    
    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn");
        _input = _canvas.Find("Dialog/Input").GetComponent<TMP_InputField>();
        _content = _canvas.Find("Dialog/DialogPanel/Viewport/Content").GetComponent<RectTransform>();
        _dialogCell = _content.Find("Cell").gameObject;
        Button sendBtn = _canvas.Find("Dialog/SendBtn").GetComponent<Button>();
        sendBtn.onClick.AddListener(OnSendClick);

        base.OnNetworkSpawn();
    }

    void OnSendClick()
    {
        Debug.Log("SendBtn Clicked");
        if (string.IsNullOrEmpty(_input.text))
        {
            return;
        }

        PlayerInfo playerInfo = GameManager.Instance.AllPlayerInfos[NetworkManager.Singleton.LocalClientId];

        AddDialogCell(playerInfo.name, _input.text);
        
        if(IsServer)
        {
            SendMsgToOthersClientRpc(playerInfo, _input.text);
        }
        else
        {
            SendMsgToOthersServerRpc(playerInfo, _input.text);
        }
    }

    [ClientRpc]
    void SendMsgToOthersClientRpc(PlayerInfo playerInfo, string content)
    {
        if (!IsServer && NetworkManager.LocalClientId != playerInfo.id)//防止主机执行 主机(也是客户端)已经执行了 //防止两次说的话
        {
            AddDialogCell(playerInfo.name, content);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SendMsgToOthersServerRpc(PlayerInfo playerInfo, string content)
    {
        AddDialogCell(playerInfo.name, content);
        SendMsgToOthersClientRpc(playerInfo, content);
    }

    void AddDialogCell(string playerName, string content)
    {
        GameObject clone = Instantiate(_dialogCell, _content);
        clone.SetActive(true);
        clone.AddComponent<DialogCell>().Initial(playerName, content);
    }
}
