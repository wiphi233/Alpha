using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
//using UnityEditor.SceneManagement; 
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
//using UnityEngine.UIElements;


[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public float rotationSpeed = 10f;
    private static readonly WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
    public GameObject WorldCanvas;
    public GameObject LeftJoystick;
    public GameObject JumpButton;
    public GameObject ButtonPrompts;
    public GameObject ButtonPromptsText;
    public GameObject WhiteScreen;
    public GameObject VirtualCameraObject;
    private TextMeshProUGUI buttonPromptsTMP;
    private GameObject RecentStele;
    private bool inMaze = false;
    private Vector3 mazeEndPosition;
    private GameObject mazeGameObject;

    [HideInInspector] public bool isTouchingUI = false;
    [HideInInspector] public static PlayerMovement Instance = null;
    //private Camera mainCamera;
    private CinemachineVirtualCamera virtualCamera;
    //private CinemachinePOV cinemachinePOV;
    private CinemachineInputProvider cinemachineInputProvider;
    private CinemachineBrain brain;

    [Header("移动设置 Movement Settings")]
    private float walkSpeed = 10f;
    private float runSpeed = 25f;

    //[Header("鼠标设置 Mouse Look Settings")]
    private float mouseSensitivity = 0.1f;  // 范围改为 0.01-1
    private float smoothTime = 0.1f;  // 平滑时间，减少卡顿
    private float maxLookAngle = 80f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private bool isInited = false;
    private Vector2 currentLookInput = Vector2.zero;
    private Vector2 smoothLookVelocity = Vector2.zero;
    private Vector2 lastMousePosition;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Rigidbody rigidbodyComponent;
    private float jumpHeight = 10f;
    public float maxRotationPerFrame = 15f;   // 每帧最大旋转角度（度）

    public float speedMultiplier = 1f;

    [Header("声音设置 Audio Settings")]
    //private float seaLevelY = -30f;           // 海平面 Y 坐标
    private float fullSubmergeThreshold = WorldConfig.waterLevelHeight * WorldConfig.heightMultiplier; // 完全没入的水下起始 Y 值
    private float maxWaveDistance = 64f;       // 海浪声最大影响距离（米）
    private float fadeSpeed = 2f;              // 音量渐变速度

    private float targetWaveVolume = 0f;
    private float targetDiveVolume = 0f;

    private AudioSource waveSource;
    private AudioSource diveSource;
    private GameObject wavePlayerObject;
    private GameObject divePlayerObject;

    [Header("Scripts Instance References 脚本实例引用")]
    public GameObject signalCenterObject;
    private ISignalCenter signalCenter;

    void Awake()
    {
        if (Instance == null)
        {
            rigidbodyComponent = GetComponent<Rigidbody>();
            inputActions = new PlayerInputActions();
            Instance = this;
        }
        else
        {
            Debug.LogWarning("重复的 PlayerMovement 实例被销毁！");
            Destroy(gameObject);
        }
        if (WorldCanvas == null || LeftJoystick == null || JumpButton == null || ButtonPrompts == null ||
            ButtonPromptsText == null || WhiteScreen == null || VirtualCameraObject == null || signalCenterObject == null)
        {
            Debug.LogError("PlayerMovement参数未设置完全！");
        }
        virtualCamera = VirtualCameraObject.GetComponent<CinemachineVirtualCamera>();
        //cinemachinePOV = virtualCamera.GetCinemachineComponent<CinemachinePOV>();
        //virtualCamera.GetCinemachineComponent<CinemachineInputProvider>();
        //cinemachineInputProvider = VirtualCameraObject.GetComponent<CinemachineInputProvider>();
        //cinemachineInputProvider.enabled = false;
        brain = Camera.main.GetComponent<CinemachineBrain>();

        signalCenter = signalCenterObject.GetComponent<ISignalCenter>();
        buttonPromptsTMP = ButtonPromptsText.GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        signalCenter.Subscribe(SignalType.ChangeGameSettings, OnChangeGameSettings);
        //signalCenter.Subscribe(SignalType.WorldLoaded, OnWorldLoaded);
        Vector3 currentPlayerRot = transform.localEulerAngles;
        yRotation = currentPlayerRot.y;
        if (xRotation > 180) xRotation -= 360;
        if (yRotation > 180) yRotation -= 360;
        lastMousePosition = Mouse.current.position.ReadValue();
        StartCoroutine(NearItemsCheck());
    }

    //void OnWorldLoaded(GameObject sender, object data)
    //{
    //    //cinemachineInputProvider.enabled = true;
    //}

    public void OnChangeGameSettings(GameObject sender, object data)
    {
        int speed = Convert.ToInt32(Alpha.gameSettings.Categories["World"]["MouseSensitivityMultiplier"].CurrentValue);
        //cinemachinePOV.m_HorizontalAxis.m_MaxSpeed = speed;
        //cinemachinePOV.m_VerticalAxis.m_MaxSpeed = speed;
    }

    /// <summary>
    /// 遍历所有 Water 对象，找到距玩家最近的表面点，并返回该点及距离（欧氏距离）
    /// </summary>
    private Vector3 FindClosestWaterSurfacePoint(out float minDistance)
    {
        minDistance = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;
        Vector3 playerPos = transform.position;
        GameObject water = FindClosestWithTagInRadius(playerPos, 128, "Water", ~0);
        if (water == null) return closestPoint;

        // 使用 Collider 获取最近点（最精确，考虑形状边界）
        Collider col = water.GetComponent<Collider>();
        if (col != null)
        {
            Vector3 pointOnCol = col.ClosestPoint(playerPos);
            float dist = Vector3.Distance(playerPos, pointOnCol);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPoint = pointOnCol;
            }
        }
        return closestPoint;
    }

    private void UpdateAudio()
    {
        // 1. 找到离玩家最近的水面点（用于海浪声源位置）
        Vector3 closestPointOnWater = FindClosestWaterSurfacePoint(out float distanceToWater);

        // 2. 更新海浪声源的位置（实现 3D 效果的关键）
        if (closestPointOnWater != Vector3.zero)
        {
            //Debug.Log($"closestPointOnWater: {closestPointOnWater}");
            waveSource.transform.position = closestPointOnWater;
        }
        //float kSquared = maxWaveDistance * maxWaveDistance;
        //for (float i = -maxWaveDistance; i <= maxWaveDistance; i++)
        //{
        //    float maxJ = (float)Math.Sqrt(kSquared - i * i);
        //    for (float j = -maxJ; j <= maxJ; j++)
        //    {
        //        float x = i + transform.position.x;
        //        float z = j + transform.position.z;

        //    }
        //}

        // 4. 决定目标音量
        if (transform.position.y < fullSubmergeThreshold)
        {
            targetWaveVolume = 0f;
            targetDiveVolume = 1f;
        }
        else
        {
            // 靠近水面时海浪声变大（距离越近越大）
            float t = 1f - Mathf.Clamp01(distanceToWater / maxWaveDistance);
            targetWaveVolume = t;
            targetDiveVolume = 0f;
        }

        // 5. 平滑过渡音量
        waveSource.volume = Mathf.Lerp(waveSource.volume, targetWaveVolume, fadeSpeed * Time.deltaTime);
        diveSource.volume = Mathf.Lerp(diveSource.volume, targetDiveVolume, fadeSpeed * Time.deltaTime);
        AudioManager.bgmSource.volume = 1f - diveSource.volume;
        //Debug.Log($"Distance to water: {distanceToWater}, targetWaveVolume: {targetWaveVolume}, waveSource.volume: {waveSource.volume}, targetDiveVolume: {targetDiveVolume}");
    }

    private static Collider[] results = new Collider[50];

    public static GameObject FindClosestWithTagInRadius(Vector3 center, float radius, string tag = null, int layerMask = ~0)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, results, layerMask);

        float closestSqrDist = float.PositiveInfinity;
        GameObject closestObj = null;

        for (int i = 0; i < hitCount; i++)
        {
            GameObject go = results[i].gameObject;
            if (tag == null || go.CompareTag(tag))
            {
                float sqrDist = (go.transform.position - center).sqrMagnitude;
                if (sqrDist < closestSqrDist)
                {
                    closestSqrDist = sqrDist;
                    closestObj = go;
                }
            }
        }
        return closestObj;
    }

    private IEnumerator NearItemsCheck()
    {
        while (true)
        {
            if (inMaze)
            {
                if (Vector3.Distance(mazeEndPosition, transform.position) < 1.5f)
                {
                    var group = WhiteScreen.GetComponent<CanvasGroup>();
                    WhiteScreen.SetActive(true);
                    const float animationSpeed = 1.5f;
                    DOVirtual.Float(0f, 1f, animationSpeed, value =>
                    {
                        group.alpha = value;
                    });
                    DOVirtual.DelayedCall(animationSpeed, () =>
                    {
                        Destroy(mazeGameObject);
                        inMaze = false;
                        DOVirtual.Float(1f, 0f, animationSpeed, value =>
                        {
                            group.alpha = value;
                        });
                        DOVirtual.DelayedCall(animationSpeed, () =>
                        {
                            WhiteScreen.SetActive(false);
                        });
                    });
                    yield return new WaitForSeconds(animationSpeed * 2);
                }
            }
            else
            {
                RecentStele = FindClosestWithTagInRadius(transform.position, 10f, "Stele");
                if (States.IsLockUI == false)
                {
                    if (RecentStele != null)
                    {
                        ButtonPrompts.SetActive(true);
                        buttonPromptsTMP.SetText("按 <b>R</b> 键激活传送石碑");
                    }
                    else
                    {
                        ButtonPrompts.SetActive(false);
                    }
                }
            }
            if (transform.position.y < -160f)
            {
                Tp(new Vector3(transform.position.x, 200, transform.position.z));
                buttonPromptsTMP.SetText("您已掉出地图，已为您自动传送");
                ButtonPrompts.SetActive(true);
                yield return _waitForSeconds0_5;
                Debug.Log("检测到玩家掉出地图，自动传送");
            }
            yield return _waitForSeconds0_5;
        }

    }

    void Tp(Vector3 position)
    {
        rigidbodyComponent.velocity = Vector3.zero;
        rigidbodyComponent.angularVelocity = Vector3.zero;

        // 然后再设置为 kinematic 来传送
        rigidbodyComponent.isKinematic = true;
        transform.position = position;
        rigidbodyComponent.isKinematic = false;
    }

    //void LateUpdate()
    //{
    //    if (brain != null)
    //    {
    //        float targetY = brain.transform.eulerAngles.y;

    //        // 平滑插值当前角度到目标角度
    //        Quaternion targetRotation = Quaternion.Euler(0, targetY, 0);
    //        transform.rotation = Quaternion.Slerp(
    //            transform.rotation,
    //            targetRotation,
    //            rotationSpeed * Time.deltaTime
    //        );
    //    }
    //}

    void Update()
    {
        if (GameStateManager.Get() == GameState.LoadingCompleted)
        {
            if (!isInited)
            {
                rigidbodyComponent.isKinematic = false;
                WorldCanvas.SetActive(true);
                if (Application.isMobilePlatform)
                {
                    // 移动端
                    LeftJoystick.SetActive(true);
                    JumpButton.SetActive(false);
                }
                else
                {
                    // PC端 / Editor
                    LeftJoystick.SetActive(false);
                    JumpButton.SetActive(false);
                }

                isInited = true;
                inputActions.Player.Enable();
                inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
                inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
                inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
                inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
                inputActions.Player.Jump.performed += OnJump;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                wavePlayerObject = new GameObject("WaveAudioPlayer");
                divePlayerObject = new GameObject("DiveAudioPlayer");
                // 初始化音频源设置
                waveSource = wavePlayerObject.AddComponent<AudioSource>();
                waveSource.clip = AudioManager.Instance.wave;
                waveSource.spatialBlend = 1f;          // 纯 3D 声音
                waveSource.loop = true;
                waveSource.playOnAwake = false;
                waveSource.Play();
                diveSource = divePlayerObject.AddComponent<AudioSource>();
                diveSource.clip = AudioManager.Instance.dive;
                diveSource.loop = true;
                diveSource.playOnAwake = false;
                diveSource.Play();
                UpdateAudio();
            }
            UpdateCursorLock();
            if (!InputFocusManager.IsFocused)
            {
                HandleMovement();
            }

            //if ((!isTouchingUI) && (!InputFocusManager.IsFocused))
            if (!InputFocusManager.IsFocused)
            {
                HandleSmoothCameraControl();
            }
            float runFOV = 80f, walkFOV = 75f, updFOV = 40f;


            if (Keyboard.current?.leftShiftKey.isPressed == true && virtualCamera.m_Lens.FieldOfView != runFOV)
            {
                virtualCamera.m_Lens.FieldOfView += updFOV * Time.deltaTime;
                if (virtualCamera.m_Lens.FieldOfView > runFOV) virtualCamera.m_Lens.FieldOfView = runFOV;
            }
            else if (Keyboard.current?.leftShiftKey.isPressed == false && virtualCamera.m_Lens.FieldOfView != walkFOV)
            {
                virtualCamera.m_Lens.FieldOfView -= updFOV * Time.deltaTime;
                if (virtualCamera.m_Lens.FieldOfView < walkFOV) virtualCamera.m_Lens.FieldOfView = walkFOV;
            }
            if (Keyboard.current?.rKey.isPressed == true && RecentStele != null && (!inMaze))
            {
                inMaze = true;
                MazeGenerator.playCount += 2;

                Tp(new Vector3(1, 1002, 1));

                const double k = 0.1;
                const int maxL = 50;
                const int minL = 5;
                const int x0 = maxL / 2;
                int length = Math.Min(
                    Math.Max(
                        (int)Math.Round(maxL / (1 + Math.Exp(-k * (MazeGenerator.playCount + 4 - x0)))),
                        minL
                    ),
                    maxL
                );
                if (length % 2 == 0) length += 1;  // 确保迷宫尺寸为奇数，便于生成
                Debug.Log($"迷宫生成：大小：{length}*{length} 游玩次数：{MazeGenerator.playCount}");
                ButtonPrompts.SetActive(false);
                MazeGenerator.Instance.CreateMaze(new Vector3(0, 1000, 0), new Vector2Int(1, 1), new Vector2Int(length, length), out Vector3 endPosition, out GameObject mazeObject, 1f, 0f, (int)Time.time);
                mazeEndPosition = endPosition;
                mazeGameObject = mazeObject;
            }
            UpdateAudio();
        }
    }

    void OnJump(InputAction.CallbackContext context)
    {
        if (InputFocusManager.IsFocused)
        {
            return;
        }
        if (IsGrounded() && (!inMaze))
        {
            rigidbodyComponent.AddForce(jumpHeight * speedMultiplier * Vector3.up, ForceMode.Impulse);
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position - new Vector3(0, 1, 0), Vector3.down, 0.45f);
    }

    void HandleMovement()
    {
        if (moveInput == Vector2.zero) return;
        Vector3 forward = virtualCamera.transform.forward;
        Vector3 right = virtualCamera.transform.right;

        // 去掉垂直分量，保持水平移动
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x);
        moveDirection.Normalize();
        float currentSpeed = Keyboard.current?.leftShiftKey.isPressed == true ? runSpeed : walkSpeed;
        Vector3 targetVelocity = currentSpeed * Time.deltaTime * moveDirection;
        targetVelocity.x *= 100f * speedMultiplier;
        targetVelocity.z *= 100f * speedMultiplier;
        rigidbodyComponent.AddForce(targetVelocity.x, 0, targetVelocity.z);
    }

    void HandleSmoothCameraControl()
    {
        currentLookInput = Vector2.SmoothDamp(
            currentLookInput,
            lookInput,
            ref smoothLookVelocity,
            smoothTime
        );

        float lookX = currentLookInput.x * mouseSensitivity;
        float lookY = currentLookInput.y * mouseSensitivity;

        // 限制单帧旋转量
        lookX = Mathf.Clamp(lookX, -maxRotationPerFrame, maxRotationPerFrame);
        lookY = Mathf.Clamp(lookY, -maxRotationPerFrame, maxRotationPerFrame);

        xRotation -= lookY;
        yRotation += lookX;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        //brain.transform.eulerAngles = new Vector3(xRotation, yRotation, 0);
        virtualCamera.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        //brain.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // eulerAngles
        //virtualCamera ..transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    void OnDestroy()
    {
        inputActions?.Dispose();
    }

    void UpdateCursorLock()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!IsPointerOverUI())
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }

    // 检查鼠标是否在 UI 上
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        return results.Count > 0;
    }
}