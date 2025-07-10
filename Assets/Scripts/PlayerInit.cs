using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerInit : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        GameManager.Instance.OnStartGame.AddListener(OnStartGame);
        GameManager.Instance.OnLobbyLoaded.AddListener(OnLobbyLoaded);
        base.OnNetworkSpawn();
        Debug.Log($"[PlayerInit] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsLocalPlayer: {IsLocalPlayer}, OwnerClientId: {OwnerClientId}");
    }

    private void OnLobbyLoaded()
    {
        Transform[] childs = transform.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in childs)
        {
            if (child.GetComponent<Rigidbody>() != null)
            {
                child.GetComponent<Rigidbody>().isKinematic = true;
            }
        }
    }

    void OnStartGame()
    {
        Debug.Log($"[PlayerInit] OnStartGame - IsServer: {IsServer}, IsClient: {IsClient}, IsLocalPlayer: {IsLocalPlayer}, OwnerClientId: {OwnerClientId}");
        PlayerInfo playerInfo = GameManager.Instance.AllPlayerInfos[OwnerClientId];

        Transform body = transform.GetChild(playerInfo.gender);
        body.gameObject.SetActive(true);
        transform.GetChild(1 - playerInfo.gender).gameObject.SetActive(false);
        body.GetComponent<Rigidbody>().isKinematic = false;

        if (IsServer)
        {
            // 服务器计算出生点位置
            Vector3 spawnPos = GameCtrl.Instance.GetSpanPos();
            Debug.Log($"[PlayerInit] Server计算出生点 - SpawnPos: {spawnPos}, OwnerClientId: {OwnerClientId}");

            // 广播出生点位置给所有客户端
            BroadcastSpawnPositionClientRpc(spawnPos);

            // 延迟一帧设置位置
            StartCoroutine(DelaySetPosition(body, spawnPos));
        }

        //同步玩家信息
        PlayerSync playerSync = GetComponent<PlayerSync>();
        playerSync.SetTarget(playerInfo.gender);
        playerSync.enabled = true;//启用同步脚本

        if (IsLocalPlayer)
        {
            Transform cameraLookPoint = body.Find("CameraLookPoint");
            GameCtrl.Instance.SetCameraFollow(cameraLookPoint.transform);
            body.GetComponent<PlayerMove>().enabled = true; //!防止其他客户端执行 
            Debug.Log($"[PlayerInit] LocalPlayer当前位置 - Position: {body.transform.position}");
        }
    }

    [ClientRpc]
    void BroadcastSpawnPositionClientRpc(Vector3 spawnPos)
    {
        // 只在对应的客户端显示自己的出生点位置
        if (IsOwner)
        {
            // 通知GameCtrl更新UI显示
            GameCtrl.Instance.UpdateSpawnPositionText(spawnPos);
        }
    }

    IEnumerator DelaySetPosition(Transform body, Vector3 spawnPos)
    {
        yield return new WaitForFixedUpdate();
        Debug.Log($"[PlayerInit] Server延迟设置位置 - SpawnPos: {spawnPos}, Current: {body.transform.position}");

        // 确保Rigidbody不会干扰位置设置
        Rigidbody rb = body.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        // 禁用角色控制器
        CharacterController cc = body.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        // 设置位置
        body.transform.position = spawnPos;

        // 同步到所有客户端
        SetPositionClientRpc(spawnPos);

        // 确保位置设置成功
        yield return new WaitForFixedUpdate();
        if (Vector3.Distance(body.position, spawnPos) > 0.1f)
        {
            Debug.LogWarning($"[PlayerInit] Server位置设置失败，重试 - 目标: {spawnPos}, 当前: {body.position}");
            body.position = spawnPos;
        }

        // 恢复组件状态
        yield return new WaitForSeconds(0.2f);
        if (rb != null) rb.isKinematic = false;
        if (cc != null) cc.enabled = true;

        Debug.Log($"[PlayerInit] Server设置后的实际位置 - Position: {body.transform.position}");
    }

    [ClientRpc]
    void SetPositionClientRpc(Vector3 position)
    {
        Debug.Log($"[PlayerInit] ClientRpc收到位置 - Position: {position}, IsServer: {IsServer}, IsClient: {IsClient}, IsLocalPlayer: {IsLocalPlayer}, OwnerClientId: {OwnerClientId}");

        // 所有客户端都执行，包括主机客户端
        if (IsClient)
        {
            // 确保我们有玩家信息
            if (!GameManager.Instance.AllPlayerInfos.ContainsKey(OwnerClientId))
            {
                Debug.LogError($"[PlayerInit] 找不到玩家信息! OwnerClientId: {OwnerClientId}");
                return;
            }

            int gender = GameManager.Instance.AllPlayerInfos[OwnerClientId].gender;
            Transform body = transform.GetChild(gender);

            if (body == null)
            {
                Debug.LogError($"[PlayerInit] 找不到性别对应的子物体! 性别索引: {gender}");
                return;
            }

            Debug.Log($"[PlayerInit] 找到玩家身体，性别索引: {gender}");
            Vector3 oldPos = body.transform.position;

            // 禁用可能干扰位置的组件
            Rigidbody rb = body.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // 禁用角色控制器
            CharacterController cc = body.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            // 强制设置位置 - 使用多种方法确保位置正确设置
            body.transform.position = position;

            // 如果是根物体的子物体，也设置其世界坐标
            if (body.parent != null)
            {
                body.position = position;
            }

            // 如果是本地玩家，额外确保位置正确
            if (IsOwner || IsLocalPlayer)
            {
                // 对于本地玩家，使用瞬移方式设置位置
                body.position = position;

                // 通知相机系统更新位置
                if (GameCtrl.Instance != null)
                {
                    Transform cameraLookPoint = body.Find("CameraLookPoint");
                    if (cameraLookPoint != null)
                    {
                        GameCtrl.Instance.SetCameraFollow(cameraLookPoint);
                    }
                }
            }

            // 启动位置验证协程，多次检查确保位置设置成功
            StartCoroutine(VerifyPosition(body, position, rb, cc));

            Debug.Log($"[PlayerInit] Client设置位置 - OldPos: {oldPos}, NewPos: {body.transform.position}, 玩家ID: {OwnerClientId}");
        }
    }

    IEnumerator VerifyPosition(Transform body, Vector3 targetPosition, Rigidbody rb = null, CharacterController cc = null)
    {
        // 等待一帧后检查位置
        yield return new WaitForFixedUpdate();
        if (Vector3.Distance(body.position, targetPosition) > 0.1f)
        {
            Debug.LogWarning($"[PlayerInit] 位置验证失败 - 目标位置: {targetPosition}, 当前位置: {body.position}");
            body.position = targetPosition; // 再次尝试设置
        }

        // 等待0.5秒后再次检查位置
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"[PlayerInit] 位置验证 - 0.5秒后位置: {body.position}, 目标位置: {targetPosition}");

        // 恢复组件状态
        if (rb != null) rb.isKinematic = false;
        if (cc != null) cc.enabled = true;
    }
}
