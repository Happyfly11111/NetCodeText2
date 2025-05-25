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
        _syncTransform.position = _syncPos.Value;
        _syncTransform.rotation = _syncRot.Value;
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
