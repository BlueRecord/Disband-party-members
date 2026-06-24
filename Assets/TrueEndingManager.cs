using Unity.Netcode;
using UnityEngine;

public class TrueEndingManager : NetworkBehaviour
{
    [Header("입력 설정")]
    public KeyCode nextSceneKey = KeyCode.Return; // 기본값: Enter 키

    [Header("이동할 씬 이름")]
    public string finalSceneName = "FinalCreditScene";

    private bool isTriggered = false; // 중복 실행 방지용 스위치

    void Update()
    {
        // 🌟 서버(방장)이고, 아직 실행되지 않았으며, 지정된 키(Enter 등)를 눌렀을 때 작동
        if (IsServer && !isTriggered && Input.GetKeyDown(nextSceneKey))
        {
            isTriggered = true; // 스위치를 켜서 여러 번 눌리는 것을 방지
            Debug.Log("입력 감지 완료! 지연 없이 즉시 다음 씬으로 넘어갑니다.");

            // 코루틴이나 딜레이 없이 즉시 다음 씬 로드
            NetworkManager.Singleton.SceneManager.LoadScene(finalSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }
}