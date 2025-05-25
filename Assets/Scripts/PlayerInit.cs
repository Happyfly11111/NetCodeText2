using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerInit : NetworkBehaviour
{

    public override void OnNetworkSpawn()
    {
        GameManager.Instance.OnStartGame.AddListener(OnStartGame);
        base.OnNetworkSpawn();
    }

    void OnStartGame()
    {
        PlayerInfo playerInfo = GameManager.Instance.AllPlayerInfos[OwnerClientId];//!在本地和服务器上执行 不能用NetworkManager.Singleton.LocalClientId
        Transform body = transform.GetChild(playerInfo.gender);
        body.gameObject.SetActive(true);
        //设置玩家出生点位置
        body.transform.position = GameCtrl.Instance.GetSpanPos();
        //同步玩家信息
        PlayerSync playerSync = GetComponent<PlayerSync>();
        playerSync.SetTarget(playerInfo.gender);
        playerSync.enabled = true;//启用同步脚本


        if (IsLocalPlayer)
        {
            GameCtrl.Instance.SetCameraFollow(body);
            body.GetComponent<PlayerMove>().enabled = true; //!防止其他客户端执行 
        }
    }
}
