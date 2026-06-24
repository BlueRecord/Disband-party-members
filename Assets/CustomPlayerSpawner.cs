using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class CustomPlayerSpawner : MonoBehaviour
{
    [Header("순서대로 스폰할 플레이어 프리팹 4개")]
    public List<GameObject> playerPrefabs;

    private void Start()
    {
        // NetworkManager의 접속 승인 과정을 가로채서 우리가 만든 함수(ApprovalCheck)로 연결합니다.
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // 1. 접속을 무조건 승인하고, 플레이어 오브젝트를 생성하도록 허락함
        response.Approved = true;
        response.CreatePlayerObject = true;

        // 2. 현재 접속한 인원수를 확인하여 0, 1, 2, 3번 인덱스를 순서대로 가져옴
        // (예: 첫 번째 접속(호스트)=0, 두 번째=1, 세 번째=2...)
        int playerIndex = NetworkManager.Singleton.ConnectedClientsIds.Count % playerPrefabs.Count;

        // 3. 해당 순서의 프리팹 고유 해시(Hash) 값을 찾아 네트워크 매니저에게 전달
        uint prefabHash = playerPrefabs[playerIndex].GetComponent<NetworkObject>().PrefabIdHash;
        response.PlayerPrefabHash = prefabHash;

        // [핵심 추가] 플레이어가 스폰될 때 위치를 강제로 2미터씩 띄워놓습니다. 
        // (Y축은 1.0f로 두어 바닥 아래로 떨어지는 것을 막습니다.)
        response.Position = new Vector3(playerIndex * 2.0f, 0f, 0f);
        response.Rotation = Quaternion.identity;

        Debug.Log($"플레이어 스폰됨: {playerPrefabs[playerIndex].name}");
    }
}