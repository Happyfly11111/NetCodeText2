using System;
using Cinemachine.Utility;
using UnityEngine;
using Unity.Netcode;

[AddComponentMenu("Custom/Player Move")]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float velocityDamping = 0.5f;

    public enum ForwardMode
    {
        Camera,
        Player,
        World
    }

    [Header("Control Settings")]
    [SerializeField] private ForwardMode inputForward = ForwardMode.Camera;
    [SerializeField] private bool rotatePlayer = true;

    // Events
    public Action EnterAction;

    // Private fields
    private Vector3 m_moveDirection;
    private Animator m_animator;
    private Rigidbody m_rigidbody;
    private PlayerAnimationSync m_animationSync;

    // Animation parameter names
    private static readonly string ANIM_MOVE_SPEED = "NormalMoveSpeed";
    private static readonly int MOVE_SPEED_HASH = Animator.StringToHash(ANIM_MOVE_SPEED);

    private bool m_hasAnimator;

    private void Reset()
    {
        speed = 5f;
        inputForward = ForwardMode.Camera;
        rotatePlayer = true;
        velocityDamping = 0.5f;
    }

    private void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_animator = GetComponent<Animator>();
        m_animationSync = GetComponent<PlayerAnimationSync>();

        // 检查动画器参数
        if (m_animator != null)
        {
            m_hasAnimator = true;
        }

        // 配置Rigidbody
        m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        m_rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        m_rigidbody.mass = 1f;
        m_rigidbody.drag = 0f;
    }

    private void Update()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        // 只有本地玩家才处理输入和更新动画
        bool isLocalPlayer = IsLocalPlayer();

        if (isLocalPlayer)
        {
            HandleInput();
        }

        // 动画更新对所有玩家都执行，但本地玩家和远程玩家的处理方式不同
        UpdateAnimator();
#else
        InputSystemHelper.EnableBackendsWarningMessage();
#endif
    }

    private void FixedUpdate()
    {
        // 只有本地玩家才处理移动
        if (IsLocalPlayer())
        {
            HandleMovement();
        }
    }

    // 检查是否是本地玩家
    private bool IsLocalPlayer()
    {
        // 检查父物体是否有NetworkBehaviour组件
        NetworkBehaviour networkParent = transform.parent?.GetComponent<NetworkBehaviour>();
        if (networkParent != null)
        {
            return networkParent.IsLocalPlayer;
        }

        // 如果找不到NetworkBehaviour组件，则假设是本地玩家
        // 这种情况可能发生在单人游戏或测试中
        return true;
    }

    private void HandleMovement()
    {
        Vector3 fwd = GetForwardDirection();
        if (fwd.sqrMagnitude < 0.01f)
        {
            Debug.LogWarning("[PlayerMove] 前向向量为零，可能影响移动计算");
            fwd = Vector3.forward; // 使用默认前向方向
        }

        // 获取输入并记录日志
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Debug.Log($"[PlayerMove] 输入值: 水平={horizontalInput}, 垂直={verticalInput}");

        Quaternion inputFrame = Quaternion.LookRotation(fwd, Vector3.up);
        Vector3 input = new Vector3(horizontalInput, 0, verticalInput);
        input = Vector3.ClampMagnitude(input, 1f);
        m_moveDirection = inputFrame * input;

        // 记录移动方向
        Debug.Log($"[PlayerMove] 移动方向: {m_moveDirection}, 大小: {m_moveDirection.magnitude}");

        // 检查是否有输入
        bool hasInput = Mathf.Abs(horizontalInput) > 0.01f || Mathf.Abs(verticalInput) > 0.01f;

        // 计算目标速度
        Vector3 targetVelocity;
        if (hasInput)
        {
            // 有输入时正常移动
            targetVelocity = m_moveDirection * speed;
        }
        else
        {
            // 无输入时迅速停止水平移动
            targetVelocity = Vector3.zero;
        }

        // 保持垂直速度（重力影响）
        targetVelocity.y = m_rigidbody.velocity.y;

        // 使用插值平滑过渡到目标速度
        Vector3 currentVelocity = m_rigidbody.velocity;

        // 无输入时使用更高的阻尼系数来快速停止
        float currentDamping = hasInput ? velocityDamping : 0.9f;

        Vector3 newVelocity = Vector3.Lerp(currentVelocity, targetVelocity, currentDamping);
        m_rigidbody.velocity = newVelocity;

        // 记录速度信息
        Debug.Log($"[PlayerMove] 速度: 当前={currentVelocity.magnitude}, 目标={targetVelocity.magnitude}, 新={newVelocity.magnitude}");

        // 处理旋转
        if (rotatePlayer && m_moveDirection.sqrMagnitude > 0.01f)
        {
            RotateTowardsMovement();
        }
    }

    private Vector3 GetForwardDirection()
    {
        Vector3 fwd;
        switch (inputForward)
        {
            case ForwardMode.Camera:
                // 检查主相机是否存在
                if (Camera.main != null)
                {
                    fwd = Camera.main.transform.forward;
                    Debug.Log($"[PlayerMove] 使用相机前向: {fwd}");
                }
                else
                {
                    Debug.LogWarning("[PlayerMove] 找不到主相机，使用世界前向代替");
                    fwd = Vector3.forward;
                }
                break;

            case ForwardMode.Player:
                fwd = transform.forward;
                Debug.Log($"[PlayerMove] 使用玩家前向: {fwd}");
                break;

            case ForwardMode.World:
            default:
                fwd = Vector3.forward;
                Debug.Log($"[PlayerMove] 使用世界前向: {fwd}");
                break;
        }

        // 确保向量有效
        if (fwd.sqrMagnitude < 0.001f)
        {
            Debug.LogWarning("[PlayerMove] 前向向量无效，使用默认值");
            fwd = Vector3.forward;
        }

        // 移除Y分量，保持在水平面上
        fwd.y = 0;
        Vector3 normalized = fwd.normalized;

        // 确保归一化后的向量有效
        if (float.IsNaN(normalized.x) || float.IsNaN(normalized.z))
        {
            Debug.LogError("[PlayerMove] 归一化后的前向向量包含NaN值，使用默认值");
            return Vector3.forward;
        }

        return normalized;
    }

    private void RotateTowardsMovement()
    {
        if (m_moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                (inputForward == ForwardMode.Player && Vector3.Dot(GetForwardDirection(), m_moveDirection) < 0)
                    ? -m_moveDirection
                    : m_moveDirection);

            m_rigidbody.MoveRotation(Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime));
        }
    }

    private void UpdateAnimator()
    {
        if (!m_hasAnimator)
        {
            Debug.LogError("[PlayerMove] 没有找到Animator组件！");
            return;
        }

        bool isLocalPlayer = IsLocalPlayer();
        float finalMoveSpeed = 0f;

        if (isLocalPlayer)
        {
            // 本地玩家：使用输入和物理速度计算移动速度
            // 使用速度大小而不是方向大小来计算移动速度
            // 这样即使玩家被外力推动也能正确显示动画
            Vector3 horizontalVelocity = new Vector3(m_rigidbody.velocity.x, 0, m_rigidbody.velocity.z);
            float velocityMagnitude = horizontalVelocity.magnitude;

            // 检查是否有输入
            bool hasInput = m_moveDirection.sqrMagnitude > 0.01f;

            // 计算最终的移动速度值
            if (hasInput)
            {
                // 有输入时使用方向和速度的组合
                float directionMagnitude = m_moveDirection.magnitude;
                finalMoveSpeed = Mathf.Max(directionMagnitude, velocityMagnitude / speed);
            }
            else
            {
                // 无输入时只使用实际速度，并确保速度低于阈值时显示为停止
                finalMoveSpeed = velocityMagnitude < 0.1f ? 0f : velocityMagnitude / speed;
            }

            // 确保值在0-1范围内
            finalMoveSpeed = Mathf.Clamp01(finalMoveSpeed);

            Debug.Log($"[PlayerMove] 本地玩家移动速度: {finalMoveSpeed}, 方向大小: {m_moveDirection.magnitude}, 速度大小: {velocityMagnitude / speed}");
        }
        else
        {
            // 远程玩家：不处理动画，由PlayerAnimationSync负责
            // 这里不需要设置动画参数，因为它会由网络同步
            return;
        }

        // 设置本地动画参数
        m_animator.SetFloat(MOVE_SPEED_HASH, finalMoveSpeed);

        // 同步动画数据到网络 - 只有本地玩家才发送
        if (isLocalPlayer)
        {
            if (m_animationSync == null)
            {
                // 尝试在父物体上查找PlayerAnimationSync组件
                m_animationSync = transform.parent.GetComponent<PlayerAnimationSync>();

                // 如果仍然找不到，尝试在整个层级中查找
                if (m_animationSync == null)
                {
                    m_animationSync = transform.root.GetComponentInChildren<PlayerAnimationSync>();
                }
            }

            if (m_animationSync != null)
            {
                // 确保停止状态立即同步
                if (finalMoveSpeed == 0f && m_lastSentMoveSpeed > 0f)
                {
                    m_animationSync.SetMoveSpeed(0f);
                    m_lastSentMoveSpeed = 0f;
                    Debug.Log("[PlayerMove] 发送停止状态到网络");
                }
                // 其他情况只有当速度值变化明显时才发送更新
                else if (Mathf.Abs(finalMoveSpeed - m_lastSentMoveSpeed) > 0.05f ||
                    (finalMoveSpeed > 0 && m_lastSentMoveSpeed == 0))
                {
                    m_animationSync.SetMoveSpeed(finalMoveSpeed);
                    m_lastSentMoveSpeed = finalMoveSpeed;
                    Debug.Log($"[PlayerMove] 发送移动速度到网络: {finalMoveSpeed}");
                }
            }
            else
            {
                Debug.LogWarning($"[PlayerMove] 未找到PlayerAnimationSync组件，无法同步动画！");
            }
        }
    }

    // 记录上次发送的移动速度，避免频繁发送相同的值
    private float m_lastSentMoveSpeed = -1;

    private void HandleInput()
    {
        // 检测回车键
        if (Input.GetKeyDown(KeyCode.Return) && EnterAction != null)
        {
            EnterAction();
        }

        // 添加跳跃输入检测
        if (Input.GetKeyDown(KeyCode.Space) && m_animationSync != null)
        {
            m_animationSync.SetJumping(true);
        }

        // 当按键释放时，确保立即重置移动方向
        // 这是为了解决GetAxis可能会有渐变效果导致角色持续移动的问题
        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.S) ||
            Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D) ||
            Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.DownArrow) ||
            Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow))
        {
            // 检查是否所有方向键都已释放
            bool allKeysReleased =
                !Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) &&
                !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D) &&
                !Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow) &&
                !Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow);

            if (allKeysReleased)
            {
                // 立即重置移动方向，确保角色停止
                m_moveDirection = Vector3.zero;

                // 立即通知动画系统停止移动动画
                if (m_animationSync != null && m_lastSentMoveSpeed > 0)
                {
                    m_animationSync.SetMoveSpeed(0f);
                    m_lastSentMoveSpeed = 0f;
                    Debug.Log("[PlayerMove] 按键释放，立即停止移动");
                }
            }
        }
    }
}
