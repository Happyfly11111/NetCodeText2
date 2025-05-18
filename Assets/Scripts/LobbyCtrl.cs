using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public struct PlayerInfo : INetworkSerializable //*在网络中传输的结构体
{
    public ulong id;
    public bool isReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref isReady);
    }
}



public class LobbyCtrl : NetworkBehaviour
{
    [SerializeField]
    private Transform _canvas;

    RectTransform _content;
    GameObject _originCell;

    Button _startBtn;
    Toggle _ready;
    Dictionary<ulong, PlayerListCell> _cellDictionary;
    Dictionary<ulong, PlayerInfo> _allPlayerInfo;

    public override void OnNetworkSpawn()
    {
        // 仅在服务器端注册客户端连接回调
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConn;//服务器执行 有新客户端连接时调用,会返回该新客户端的ID  
        }

        // 客户端和服务器都会执行的初始化逻辑
        _allPlayerInfo = new Dictionary<ulong, PlayerInfo>();
        _cellDictionary = new Dictionary<ulong, PlayerListCell>();
        _content = _canvas.Find("List/Viewport/Content").GetComponent<RectTransform>();
        _originCell = _content.Find("Cell").gameObject;
        _startBtn = _canvas.Find("StartBtn").GetComponent<Button>();
        _startBtn.onClick.AddListener(OnStartClick);
        _ready = _canvas.Find("Ready").GetComponent<Toggle>();
        _ready.onValueChanged.AddListener(OnReadyToggle);


        // 添加本地玩家//!添加服务器改脚本的字典
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.id = NetworkManager.LocalClientId;
        playerInfo.isReady = false;
        AddPlayer(playerInfo);

        base.OnNetworkSpawn();
    }

    void OnClientConn(ulong clientId)
    {
        //服务器添加玩家 //!添加服务器改脚本的字典
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.id = clientId;
        playerInfo.isReady = false;
        AddPlayer(playerInfo);

        UpdateAllPlayerInfo();//服务器将玩家信息发送给客户端 客户端更新玩家信息
    }

    void UpdateAllPlayerInfo()
    {
        foreach (var playerInfo in _allPlayerInfo)
        {
            UpdatePlayerInfoClientRpc(playerInfo.Value);
        }
    }

    [ClientRpc]//!会在当服务器的主机也调用
    void UpdatePlayerInfoClientRpc(PlayerInfo playerInfo)//客户端执行
    {
        if (!IsServer)//防止主机执行 主机(也是客户端)已经执行了
        {
            if (_allPlayerInfo.ContainsKey(playerInfo.id))//字典中包含客户端自己 
            {
                _allPlayerInfo[playerInfo.id] = playerInfo;///更新自己的信息
            }
            else//字典中不包含客户端自己 说明是新客户端
            {
                AddPlayer(playerInfo);
            }
            UpdatePlayerCells();
        }
    }

    void UpdatePlayerCells()
    {
        foreach (var playerInfo in _allPlayerInfo)
        {
            _cellDictionary[playerInfo.Key].SetReady(playerInfo.Value.isReady);
        }
    }

    private void OnReadyToggle(bool isOn)
    {
        //本地玩家准备状态改变
        _cellDictionary[NetworkManager.LocalClientId].SetReady(isOn);
        UpdatePlayerInfo(NetworkManager.LocalClientId, isOn);

        if (IsServer)
        {
            UpdateAllPlayerInfo();
        }
        else
        {
            //客户端把自己的准备数据发送给服务器
            UpdateAllPlayerInfoServerRpc(_allPlayerInfo[NetworkManager.LocalClientId]); 
        }
    }

    [ServerRpc (RequireOwnership =false)]
    void UpdateAllPlayerInfoServerRpc(PlayerInfo playerInfo)
    {
        _allPlayerInfo[playerInfo.id] = playerInfo;
        _cellDictionary[playerInfo.id].SetReady(playerInfo.isReady);
        UpdateAllPlayerInfo();//!服务器执行 让服务器更新所有玩家信息(多了一步)
    }

    void UpdatePlayerInfo(ulong id, bool isReady)
    {
        //由于 PlayerInfo 是值类型（结构体），直接修改会导致编译器报错。
        PlayerInfo playerInfo = _allPlayerInfo[id]; // 获取副本
        playerInfo.isReady = isReady;              // 修改副本
        _allPlayerInfo[id] = playerInfo;           // 存回字典
    }


    void OnStartClick()
    {

    }


    void AddPlayer(PlayerInfo playerInfo)
    {
        GameObject clone = Instantiate(_originCell, _content);

        PlayerListCell cell = clone.GetComponent<PlayerListCell>();
        cell.Initial(playerInfo);
        clone.SetActive(true);
        _cellDictionary.Add(playerInfo.id, cell);

        _allPlayerInfo.Add(playerInfo.id, playerInfo);
    }
}