using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))] // 이 스크립트를 넣으면 AudioSource가 자동으로 붙습니다.
public class PlayerController : NetworkBehaviour
{
    [Header("이동 및 점프 설정")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 5f;

    [Header("바닥 판정 설정")]
    public LayerMask groundLayer;
    public float groundDistance = 0.2f;

    [Header("UI 캔버스 설정")]
    public Slider staminaSlider;
    public GameObject uiCanvas;

    [Header("사운드 설정")]
    public AudioClip jumpSound;
    public AudioClip walkSound;
    public float walkStepInterval = 0.5f; // 걷는 발소리 간격
    public float runStepInterval = 0.3f;  // 뛰는 발소리 간격

    [Header("컴포넌트 참조")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private Animator animator;
    private AudioSource audioSource;
    private bool isGrounded;
    private float stepTimer; // 발소리 타이머

    // 클라이언트가 자신의 달리기 상태를 서버에 보고하기 위한 네트워크 변수
    public NetworkVariable<bool> isPlayerRunningNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private float inputX;
    private float inputZ;
    private bool isRunningInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate; //
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>(); // 오디오 컴포넌트 할당

        // 3D 사운드 설정 (거리에 따라 소리 크기가 달라지도록 세팅)
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 20f;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
            if (uiCanvas != null) uiCanvas.SetActive(false); //
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; //

        if (staminaSlider != null && SharedStaminaManager.Instance != null)
        {
            staminaSlider.maxValue = SharedStaminaManager.Instance.maxStamina;
            staminaSlider.value = SharedStaminaManager.Instance.currentStamina.Value; //
        }
        SnapToGround(); //
    }

    void SnapToGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 1.0f, Vector3.down, out hit, 5.0f, groundLayer))
        {
            transform.position = hit.point + Vector3.up * 0.5f;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero; //
        }
    }

    void Update()
    {
        // 1. 공통 처리 구역 (나와 남의 캐릭터 모두 해당)
        // 물리 엔진의 속도(linearVelocity)를 읽어서 바닥에 닿아있고 이동 중이면 누구나 발소리를 냅니다.
        Vector3 spherePosition = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.CheckSphere(spherePosition, groundDistance, groundLayer); //

        HandleFootsteps();

        // 2. 로컬 플레이어 구역 (오직 내 컴퓨터에서만 키보드 입력을 받음)
        if (!IsOwner) return; //

        inputX = Input.GetAxisRaw("Horizontal");
        inputZ = Input.GetAxisRaw("Vertical");
        isRunningInput = Input.GetKey(KeyCode.LeftShift); //

        if (Mathf.Abs(inputX) < 0.1f) inputX = 0f;
        if (Mathf.Abs(inputZ) < 0.1f) inputZ = 0f; //

        if (animator != null)
        {
            animator.SetFloat("InputX", inputX);
            animator.SetFloat("InputZ", inputZ);
            animator.SetBool("isGrounded", isGrounded); //
        }

        float mouseX = Input.GetAxis("Mouse X") * 2f;
        transform.Rotate(Vector3.up * mouseX); //

        // 점프 처리
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            if (SharedStaminaManager.Instance.currentStamina.Value >= SharedStaminaManager.Instance.jumpStaminaCost) //
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse); //
                RequestJumpStaminaServerRpc();
                PlayJumpSoundServerRpc(); // 서버를 통해 모두에게 점프 소리 방송
            }
        }

        if (staminaSlider != null && SharedStaminaManager.Instance != null)
        {
            staminaSlider.value = SharedStaminaManager.Instance.currentStamina.Value; //
        }
    }

    // ---------------- [사운드 로직] ----------------

    private void HandleFootsteps()
    {
        // X, Z축의 속도만 계산해서 실제로 이동 중인지 판별합니다.
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        bool isMoving = horizontalVelocity.magnitude > 0.5f;

        if (isGrounded && isMoving)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                if (walkSound != null)
                {
                    // [꿀팁] 발소리 피치(음높이)를 살짝 랜덤으로 섞어주면 기계음 같지 않고 훨씬 자연스럽습니다.
                    audioSource.pitch = Random.Range(0.85f, 1.1f);
                    audioSource.PlayOneShot(walkSound, 0.4f); // 볼륨 40%
                }

                // 달리고 있는지(NetworkVariable 참조)에 따라 다음 발소리 타이머 간격을 조절합니다.
                stepTimer = isPlayerRunningNet.Value ? runStepInterval : walkStepInterval;
            }
        }
        else
        {
            // 멈추면 타이머를 0으로 초기화해, 다음 이동 시 즉각 발소리가 나도록 합니다.
            stepTimer = 0f;
        }
    }

    // 점프 소리를 서버에 요청
    [ServerRpc]
    private void PlayJumpSoundServerRpc()
    {
        PlayJumpSoundClientRpc();
    }

    // 서버가 모든 클라이언트 화면에 소리 재생 명령을 내림
    [ClientRpc]
    private void PlayJumpSoundClientRpc()
    {
        if (jumpSound != null && audioSource != null)
        {
            audioSource.pitch = 1f; // 점프 소리는 원래 높이 그대로
            audioSource.PlayOneShot(jumpSound, 0.8f);
        }
    }

    // ---------------- [물리/이동 로직] ----------------

    [ServerRpc]
    private void RequestJumpStaminaServerRpc()
    {
        SharedStaminaManager.Instance.ConsumeStamina(SharedStaminaManager.Instance.jumpStaminaCost); //
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        bool hasStamina = SharedStaminaManager.Instance != null && SharedStaminaManager.Instance.currentStamina.Value > 0;
        bool isRunning = isRunningInput && (inputX != 0 || inputZ != 0) && hasStamina; //

        isPlayerRunningNet.Value = isRunning; //

        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        Vector3 moveDir = (transform.forward * inputZ + transform.right * inputX).normalized;
        Vector3 targetVelocity = moveDir * currentSpeed; //

        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z); //
    }
}