using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerSync : NetworkBehaviour
{
    NetworkVariable<Vector3> _syncPos = new NetworkVariable<Vector3>();
    NetworkVariable<Quaternion> _syncRot = new NetworkVariable<Quaternion>();
    Transform _syncTransform;


    public void SetTarget(int gender)
    {
        _syncTransform = transform.GetChild(gender);
    }

    void Update()
    {
        if (IsLocalPlayer)
        {
            UpLoadTransform();
        }
    }

    void FixedUpdate()
    {
        if (!IsLocalPlayer)
        {
            SyncTransform();
        }
    }
    void SyncTransform()
    {
        // 只同步位置和旋转，不影响物理系统
        Vector3 oldPos = _syncTransform.position;
        Quaternion oldRot = _syncTransform.rotation;

        _syncTransform.position = _syncPos.Value;
        _syncTransform.rotation = _syncRot.Value;

        // 如果位置发生了明显变化，记录日志
        if (Vector3.Distance(oldPos, _syncPos.Value) > 0.1f)
        {
            Debug.Log($"[PlayerSync] 同步位置: 旧={oldPos}, 新={_syncPos.Value}, OwnerClientId={OwnerClientId}");
        }

        // 如果这个物体有Rigidbody，重置其速度以防止自动移动
        Rigidbody rb = _syncTransform.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void UpLoadTransform()
    {
        if (IsServer)//主机执行 （本地+服务器）
        {
            _syncPos.Value = _syncTransform.position;
            _syncRot.Value = _syncTransform.rotation;
        }
        else
        {
            UpLoadTransformServerRpc(_syncTransform.position, _syncTransform.rotation);
        }
    }

    [ServerRpc]
    void UpLoadTransformServerRpc(Vector3 pos, Quaternion rot)
    {
        _syncPos.Value = pos;
        _syncRot.Value = rot;
    }
}
