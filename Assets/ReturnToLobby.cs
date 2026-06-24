using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReturnToLobby : MonoBehaviour
{
    [Header("UI 연결")]
    public Button mainMenuButton; // 메인 메뉴로 돌아갈 버튼

    [Header("로비 씬 이름")]
    public string lobbySceneName = "Scene_Lobby"; // 정확한 로비 씬 이름 입력

    void Start()
    {
        // 버튼에 클릭 이벤트 연결
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(GoToMainMenu);
        }
    }

    private void GoToMainMenu()
    {
        // 1. 현재 접속 중인 멀티플레이 네트워크(서버 및 클라이언트)를 안전하게 종료합니다.
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // 2. 파괴되지 않고 넘어왔던 네트워크 오브젝트들이 정리될 수 있도록 아주 짧게 대기 후 로비 씬을 로드합니다.
        // (Shutdown 처리가 프레임 끝에 완료되므로, 씬 전환을 안전하게 처리하기 위함입니다.)
        SceneManager.LoadScene(lobbySceneName);

        Debug.Log("네트워크 종료 및 로비 씬으로 이동합니다.");
    }
}