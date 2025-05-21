using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    //?public Dictionary<ulong, PlayerInfo> AllPlayerInfos{ get; private set; }
    public Dictionary<ulong, PlayerInfo> AllPlayerInfos;
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
        AllPlayerInfos = new Dictionary<ulong, PlayerInfo>();
    }
    void Start()
    {
        SceneManager.LoadScene("Start");
    }
    public void LoadScene(string sceneName)
    {
        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void StartGame(Dictionary<ulong, PlayerInfo> allPlayerInfos)
    {
        AllPlayerInfos = allPlayerInfos;

    }
}
