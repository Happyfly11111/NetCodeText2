using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public struct PlayerInfo : INetworkSerializable //*在网络中传输的结构体
{
    public ulong id;
    public bool isReady;
    public int gender;
    public string name;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter //*序列化 反序列化
    {
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref isReady);
        serializer.SerializeValue(ref gender);
        serializer.SerializeValue(ref name);
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
    TMP_InputField _nameInputField;
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
        _startBtn.interactable = false;
        _startBtn.onClick.AddListener(OnStartClick);
        _ready = _canvas.Find("Ready").GetComponent<Toggle>();
        _ready.onValueChanged.AddListener(OnReadyToggle);
        _nameInputField = _canvas.Find("Name/NameInput").GetComponent<TMP_InputField>();
        _nameInputField.onEndEdit.AddListener(OnEndEdit);


        //添加本地玩家//!(目前是主机才会运行这部分代码) 因为其他客户端直接执行OnClientConn
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.id = NetworkManager.LocalClientId;
        playerInfo.isReady = false;
        playerInfo.gender = 0;
        playerInfo.name = "玩家" + playerInfo.id.ToString();
        _nameInputField.text = playerInfo.name;

        AddPlayer(playerInfo);

        Toggle maleT = _canvas.Find("Gender/MaleT").GetComponent<Toggle>();
        maleT.isOn = true;
        maleT.onValueChanged.AddListener(OnMaleToggle);
        Toggle femaleT = _canvas.Find("Gender/FemaleT").GetComponent<Toggle>();
        femaleT.isOn = false;
        femaleT.onValueChanged.AddListener(OnFemaleToggle);
        BodyCtrl.Instance.SwitchGender(0);

        base.OnNetworkSpawn();
    }

    void OnClientConn(ulong clientId)
    {
        //服务器添加玩家 //!添加服务器改脚本的字典
        PlayerInfo playerInfo = new PlayerInfo();
        playerInfo.id = clientId;
        playerInfo.isReady = false;
        playerInfo.gender = 0;
        playerInfo.name = "玩家" + clientId.ToString();
        AddPlayer(playerInfo);

        UpdateAllPlayerInfo();//服务器将玩家信息发送给客户端 客户端更新玩家信息
    }

    void UpdateAllPlayerInfo()
    {
        bool canGo = true;
        foreach (var playerInfo in _allPlayerInfo)//服务器遍历就行
        {
            if (!playerInfo.Value.isReady)//如果有一个人没有准备好
            {
                canGo = false;
            }

            UpdatePlayerInfoClientRpc(playerInfo.Value);
        }
        
        _startBtn.interactable = canGo; //所有玩家都准备好才能点击开始按钮
        // if (canGo)//所有玩家都准备好
        // {
        //     _startBtn.interactable = true;
        // }
        // else
        // {
        //     _startBtn.interactable = false;
        // }
    }

    [ClientRpc]//!会在当服务器的主机也调用 //所有客户端执行
    void UpdatePlayerInfoClientRpc(PlayerInfo playerInfo)
    {
        if (!IsServer)//防止主机执行 主机(也是客户端)已经执行了
        {
            if (_allPlayerInfo.ContainsKey(playerInfo.id))//字典中包含客户端自己 
            {
                _allPlayerInfo[playerInfo.id] = playerInfo;//!更新自己的信息 不用AddPlayer 因为已经在OnNetworkSpawn中添加了
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
            //_cellDictionary[playerInfo.Key].SetReady(playerInfo.Value.isReady);
            _cellDictionary[playerInfo.Key].UpdatePlayerInfo(playerInfo.Value);
        }
    }

    private void OnReadyToggle(bool isOn)
    {
        //本地玩家准备状态改变
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

    [ServerRpc(RequireOwnership = false)]
    void UpdateAllPlayerInfoServerRpc(PlayerInfo playerInfo)
    {
        _allPlayerInfo[playerInfo.id] = playerInfo;
        _cellDictionary[playerInfo.id].UpdatePlayerInfo(playerInfo);
        UpdateAllPlayerInfo();//!服务器执行 让服务器更新所有玩家信息(多了一步)
    }

    void UpdatePlayerInfo(ulong id, bool isOn)
    {
        //由于 PlayerInfo 是值类型（结构体），直接修改会导致编译器报错。
        PlayerInfo playerInfo = _allPlayerInfo[id]; // 获取副本
        playerInfo.isReady = isOn;              // 修改副本
        _allPlayerInfo[id] = playerInfo;           // 存回字典

        _cellDictionary[NetworkManager.LocalClientId].SetReady(isOn);
    }

    void OnMaleToggle(bool isOn)
    {
        if (isOn)
        {
            //更新数据
            PlayerInfo playerInfo = _allPlayerInfo[NetworkManager.LocalClientId];
            playerInfo.gender = 0;
            _allPlayerInfo[NetworkManager.LocalClientId] = playerInfo;
            //更新UI
            _cellDictionary[NetworkManager.LocalClientId].UpdatePlayerInfo(playerInfo);

            if (IsServer)
            {
                UpdateAllPlayerInfo();
            }
            else
            {
                UpdateAllPlayerInfoServerRpc(playerInfo);
            }
            BodyCtrl.Instance.SwitchGender(0);
        }

    }

    void OnFemaleToggle(bool isOn)
    {
        if (isOn)
        {
            //更新数据
            PlayerInfo playerInfo = _allPlayerInfo[NetworkManager.LocalClientId];
            playerInfo.gender = 1;
            _allPlayerInfo[NetworkManager.LocalClientId] = playerInfo;
            //更新UI
            _cellDictionary[NetworkManager.LocalClientId].UpdatePlayerInfo(playerInfo);

            if (IsServer)
            {
                UpdateAllPlayerInfo();
            }
            else
            {
                UpdateAllPlayerInfoServerRpc(playerInfo);
            }
            BodyCtrl.Instance.SwitchGender(1);
        }

    }

    void OnEndEdit(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        PlayerInfo playerInfo = _allPlayerInfo[NetworkManager.LocalClientId];
        playerInfo.name = text;
        _allPlayerInfo[NetworkManager.LocalClientId] = playerInfo;
        _cellDictionary[NetworkManager.LocalClientId].UpdatePlayerInfo(playerInfo);
        if (IsServer)
        {
            UpdateAllPlayerInfo();
        }
        else
        {
            UpdateAllPlayerInfoServerRpc(playerInfo);
        }
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