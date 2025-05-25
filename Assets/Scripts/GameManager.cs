using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.Events;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public Dictionary<ulong, PlayerInfo> AllPlayerInfos{ get; private set; }
    

    public UnityEvent OnStartGame;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        SceneManager.LoadScene(1);
        AllPlayerInfos = new Dictionary<ulong, PlayerInfo>();
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        NetworkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;//所有玩家加载完成后执行 包括主机和客户端
    }


    private void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode,
     List<ulong> clientCompleted,List<ulong> clientFailed)
    {
        if (sceneName == "Game") 
        {
            OnStartGame.Invoke(); //通知所有监听者游戏开始
        }
    }


    public void LoadScene(string sceneName)
    {
        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

   
    public void StartGame(Dictionary<ulong, PlayerInfo> allPlayerInfos)
    {
        AllPlayerInfos = allPlayerInfos;

        UpdateAllPlayerInfo();

    }

    void UpdateAllPlayerInfo()
    {
        foreach (var playerInfo in AllPlayerInfos)//服务器遍历就行
        {
            UpdatePlayerInfoClientRpc(playerInfo.Value);
        }
    }

    [ClientRpc]//!会在当服务器的主机也调用 //所有客户端执行
    void UpdatePlayerInfoClientRpc(PlayerInfo playerInfo)
    {
        if (!IsServer)//防止主机执行 主机(也是客户端)已经执行了
        {
            if (AllPlayerInfos.ContainsKey(playerInfo.id))//字典中包含客户端自己 
            {
                AllPlayerInfos[playerInfo.id] = playerInfo;//!更新自己的信息 不用AddPlayer 因为已经在OnNetworkSpawn中添加了
            }
            else//字典中不包含客户端自己 说明是新客户端
            {
                AllPlayerInfos.Add(playerInfo.id, playerInfo);
            }
        }
    }
}
