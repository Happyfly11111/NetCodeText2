using Unity.Netcode;
using UnityEngine;

public class PlayerAnimationSync : NetworkBehaviour
{
    private Animator animator;
    // 修改NetworkVariable的权限设置，确保所有客户端都能接收数据，但写入权限改为Server
    private NetworkVariable<float> moveSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isJumping = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int genderIndex = 0;

    void Awake()
    {
        // 先尝试获取当前对象上的Animator
        animator = GetComponent<Animator>();

        // 确保组件在Awake时就已启用，防止NetworkObject跳过它
        if (!enabled)
        {
            Debug.LogWarning("[PlayerAnimationSync] Awake - 组件被禁用，正在启用...");
            enabled = true;
        }

        Debug.Log($"[PlayerAnimationSync] Awake - 组件已初始化，启用状态: {enabled}");
    }

    // 设置目标性别，以便找到正确的子物体上的Animator
    public void SetTarget(int gender)
    {
        genderIndex = gender;
        // 在对应性别的子物体上查找Animator
        animator = transform.GetChild(gender).GetComponent<Animator>();

        if (animator != null)
        {
            Debug.Log($"[PlayerAnimationSync] 找到Animator组件，性别索引: {gender}");
        }
        else
        {
            Debug.LogError($"[PlayerAnimationSync] 未能在子物体上找到Animator组件，性别索引: {gender}");
        }
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[PlayerAnimationSync] OnNetworkSpawn - IsServer: {IsServer}, IsClient: {IsClient}, IsLocalPlayer: {IsLocalPlayer}, OwnerClientId: {OwnerClientId}");

        // 监听网络变量变化
        moveSpeed.OnValueChanged += OnMoveSpeedChanged;
        isJumping.OnValueChanged += OnJumpingChanged;

        // 如果animator为空，尝试从父物体获取PlayerInfo来确定性别
        if (animator == null)
        {
            PlayerInit playerInit = GetComponent<PlayerInit>();
            if (playerInit != null)
            {
                Debug.Log($"[PlayerAnimationSync] 找到PlayerInit组件");
            }
            else
            {
                Debug.LogWarning($"[PlayerAnimationSync] 未找到PlayerInit组件，无法确定性别");
            }
        }
    }

    // 本地玩家调用此方法
    public void SetMoveSpeed(float speed)
    {
        if (IsOwner)
        {
            // 直接设置本地值，避免延迟
            if (animator != null)
            {
                animator.SetFloat("NormalMoveSpeed", speed);
            }

            // 发送到服务器以同步到其他客户端
            UpdateMoveSpeedServerRpc(speed);

            Debug.Log($"[PlayerAnimationSync] 本地设置移动速度: {speed}, OwnerClientId: {OwnerClientId}");
        }
    }

    // 本地玩家调用此方法
    public void SetJumping(bool jumping)
    {
        if (IsOwner)
        {
            // 直接触发本地动画，避免延迟
            if (jumping && animator != null)
            {
                animator.SetTrigger("Jump");
            }

            // 发送到服务器以同步到其他客户端
            UpdateJumpingServerRpc(jumping);

            Debug.Log($"[PlayerAnimationSync] 本地设置跳跃: {jumping}, OwnerClientId: {OwnerClientId}");
        }
    }

    [ServerRpc]
    private void UpdateMoveSpeedServerRpc(float speed)
    {
        // 在服务器上验证发送者是否为对象所有者
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(OwnerClientId) &&
            NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.NetworkObjectId == NetworkObjectId)
        {
            // 验证通过，在服务器上设置值，这会同步到所有客户端
            moveSpeed.Value = speed;
            Debug.Log($"[PlayerAnimationSync] 服务器接收到移动速度: {speed}, 来自客户端: {OwnerClientId}, 验证通过");
        }
        else
        {
            Debug.LogWarning($"[PlayerAnimationSync] 服务器拒绝移动速度更新: {speed}, 来自客户端: {OwnerClientId}, 验证失败");
        }
    }

    [ServerRpc]
    private void UpdateJumpingServerRpc(bool jumping)
    {
        // 在服务器上验证发送者是否为对象所有者
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(OwnerClientId) &&
            NetworkManager.Singleton.ConnectedClients[OwnerClientId].PlayerObject.NetworkObjectId == NetworkObjectId)
        {
            // 验证通过，在服务器上设置值，这会同步到所有客户端
            isJumping.Value = jumping;
            Debug.Log($"[PlayerAnimationSync] 服务器接收到跳跃状态: {jumping}, 来自客户端: {OwnerClientId}, 验证通过");
        }
        else
        {
            Debug.LogWarning($"[PlayerAnimationSync] 服务器拒绝跳跃状态更新: {jumping}, 来自客户端: {OwnerClientId}, 验证失败");
        }
    }

    // 网络变量变化回调
    private void OnMoveSpeedChanged(float previous, float current)
    {
        // 跳过本地玩家，因为已经在SetMoveSpeed中直接设置了
        if (IsOwner)
            return;

        if (animator != null)
        {
            animator.SetFloat("NormalMoveSpeed", current);
            Debug.Log($"[PlayerAnimationSync] 客户端接收到移动速度: {current}, OwnerClientId: {OwnerClientId}, IsLocalPlayer: {IsLocalPlayer}");
        }
        else
        {
            Debug.LogError($"[PlayerAnimationSync] Animator为空！OwnerClientId: {OwnerClientId}");
            // 尝试重新获取Animator
            if (genderIndex > 0)
            {
                animator = transform.GetChild(genderIndex).GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetFloat("NormalMoveSpeed", current);
                    Debug.Log($"[PlayerAnimationSync] 重新获取Animator成功，设置速度: {current}");
                }
            }
        }
    }

    private void OnJumpingChanged(bool previous, bool current)
    {
        // 跳过本地玩家，因为已经在SetJumping中直接设置了
        if (IsOwner)
            return;

        if (current && animator != null)
        {
            animator.SetTrigger("Jump");
            Debug.Log($"[PlayerAnimationSync] 客户端接收到跳跃状态: {current}, OwnerClientId: {OwnerClientId}");
        }
        else if (current)
        {
            Debug.LogError($"[PlayerAnimationSync] 无法触发跳跃动画，Animator为空！OwnerClientId: {OwnerClientId}");
        }
    }
}