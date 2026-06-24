using Unity.Netcode;
using UnityEngine;

public class SharedStaminaManager : NetworkBehaviour
{
    // 싱글톤 패턴으로 어디서든 쉽게 접근 가능하게 추상화
    public static SharedStaminaManager Instance { get; private set; }

    [Header("공유 스테미나 설정")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 15f;
    public float staminaRegenRate = 10f;
    public float jumpStaminaCost = 20f;

    // 서버가 연산 권한을 독점하고 클라이언트는 읽기만 수행합니다.
    public NetworkVariable<float> currentStamina = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (!IsServer) return; // 중앙 연산은 서버에서만 처리

        int runningCount = 0;

        // 접속한 모든 플레이어의 달리기 상태를 폴링(Polling)
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                var player = client.PlayerObject.GetComponent<PlayerController>();
                if (player != null && player.isPlayerRunningNet.Value)
                {
                    runningCount++;
                }
            }
        }

        // 달리는 사람이 한 명이라도 있다면
        if (runningCount > 0)
        {
            // 달리는 인원수에 비례해서 스태미나를 깎습니다. 
            // (한 명일 때와 동일한 속도로 깎으려면 runningCount 곱하기를 제거하세요)
            currentStamina.Value -= (staminaDrainRate * runningCount) * Time.fixedDeltaTime;
        }
        else
        {
            // 아무도 달리지 않으면 회복
            currentStamina.Value += staminaRegenRate * Time.fixedDeltaTime;
        }

        currentStamina.Value = Mathf.Clamp(currentStamina.Value, 0f, maxStamina);
    }

    // 점프처럼 일회성으로 크게 소모될 때 호출할 함수
    public void ConsumeStamina(float amount)
    {
        if (!IsServer) return;
        currentStamina.Value -= amount;
        currentStamina.Value = Mathf.Clamp(currentStamina.Value, 0f, maxStamina);
    }
}