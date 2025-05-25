using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Cinemachine;

public class GameCtrl : NetworkBehaviour
{
    [SerializeField]
    private Transform _canvas;
    [SerializeField]
    private CinemachineVirtualCamera _cameraCtrl;
    TMP_InputField _input;
    RectTransform _content;
    GameObject _dialogCell;

    public static GameCtrl Instance { get; private set; }

    public override void OnNetworkSpawn()
    {
        Instance = this;

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

        if (IsServer)
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
        //防止主机执行 主机(也是客户端)已经执行了 && 防止客户端显示两次
        if (!IsServer && NetworkManager.LocalClientId != playerInfo.id)
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

    public void SetCameraFollow(Transform target)
    {
        if (_cameraCtrl != null)
        {
            _cameraCtrl.Follow = target;
            _cameraCtrl.LookAt = target;
        }
    }

    public Vector3 GetSpanPos()
    {
        Vector3 pos = new Vector3();
        Vector3 offset = transform.forward*Random.Range(-10f, 10f)+transform.right*Random.Range(-10f, 10f);
        pos = transform.position + offset;
        return pos;
    }
}