using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
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

    [Header("Rigidbody 설정")]
    private Rigidbody rb;
    public Transform cameraTransform;
    private Animator animator;
    private bool isGrounded;

    // [핵심] 클라이언트가 자신의 달리기 상태를 서버에 보고하기 위한 네트워크 변수
    public NetworkVariable<bool> isPlayerRunningNet = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner // 조작 권한은 나 자신
    );

    private float inputX;
    private float inputZ;
    private bool isRunningInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            if (cameraTransform != null) cameraTransform.gameObject.SetActive(false);
            if (uiCanvas != null) uiCanvas.SetActive(false);
        }
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 공유 매니저의 데이터를 참조하여 슬라이더 설정
        if (staminaSlider != null && SharedStaminaManager.Instance != null)
        {
            staminaSlider.maxValue = SharedStaminaManager.Instance.maxStamina;
            staminaSlider.value = SharedStaminaManager.Instance.currentStamina.Value;
        }
        SnapToGround();
    }

    void SnapToGround()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 1.0f, Vector3.down, out hit, 5.0f, groundLayer))
        {
            transform.position = hit.point + Vector3.up * 0.5f;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (IsOwner) isGrounded = true;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        inputX = Input.GetAxisRaw("Horizontal");
        inputZ = Input.GetAxisRaw("Vertical");
        isRunningInput = Input.GetKey(KeyCode.LeftShift);

        if (Mathf.Abs(inputX) < 0.1f) inputX = 0f;
        if (Mathf.Abs(inputZ) < 0.1f) inputZ = 0f;

        Vector3 spherePosition = transform.position + Vector3.up * 0.1f;
        isGrounded = Physics.CheckSphere(spherePosition, groundDistance, groundLayer);

        if (animator != null)
        {
            animator.SetFloat("InputX", inputX);
            animator.SetFloat("InputZ", inputZ);
            animator.SetBool("isGrounded", isGrounded);
        }

        float mouseX = Input.GetAxis("Mouse X") * 2f;
        transform.Rotate(Vector3.up * mouseX);

        // 점프 시 중앙 관리자의 스태미나 잔량 확인
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            if (SharedStaminaManager.Instance.currentStamina.Value >= SharedStaminaManager.Instance.jumpStaminaCost)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                RequestJumpStaminaServerRpc(); // 서버에 차감 요청
            }
        }

        // UI 업데이트
        if (staminaSlider != null && SharedStaminaManager.Instance != null)
        {
            staminaSlider.value = SharedStaminaManager.Instance.currentStamina.Value;
        }
    }

    // 서버에게 "나 점프했으니까 공유 스태미나 깎아줘"라고 요청
    [ServerRpc]
    private void RequestJumpStaminaServerRpc()
    {
        SharedStaminaManager.Instance.ConsumeStamina(SharedStaminaManager.Instance.jumpStaminaCost);
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        // 중앙 서버 스태미나 잔량 체크
        bool hasStamina = SharedStaminaManager.Instance != null && SharedStaminaManager.Instance.currentStamina.Value > 0;
        bool isRunning = isRunningInput && (inputX != 0 || inputZ != 0) && hasStamina;

        // 내 달리기 상태를 NetworkVariable에 할당하여 서버가 읽어갈 수 있도록 처리
        isPlayerRunningNet.Value = isRunning;

        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        Vector3 moveDir = (transform.forward * inputZ + transform.right * inputX).normalized;
        Vector3 targetVelocity = moveDir * currentSpeed;

        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
    }
}