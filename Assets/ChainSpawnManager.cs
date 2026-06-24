using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class ChainSpawnManager : NetworkBehaviour
{
    // requiredPlayers 변수와 OnClientConnectedCallback 이벤트 연결을 제거합니다.

    public override void OnNetworkSpawn()
    {
        // 서버(방장) 측에서 게임 씬 로딩 직후 스폰되었을 때 체인 연결 루틴을 시작합니다.
        if (IsServer)
        {
            StartCoroutine(LinkPlayersRoutine());
        }
    }

    private IEnumerator LinkPlayersRoutine()
    {
        // 모든 클라이언트의 캐릭터 오브젝트가 씬에 완벽히 스폰될 때까지 잠시 대기합니다.
        yield return new WaitForSeconds(1.0f);

        var clients = NetworkManager.Singleton.ConnectedClientsList;

        // 접속 인원이 최소 2명 이상일 때만 체인을 연결합니다.
        if (clients.Count >= 2)
        {
            for (int i = 0; i < clients.Count - 1; i++)
            {
                NetworkObject playerA = clients[i].PlayerObject;
                NetworkObject playerB = clients[i + 1].PlayerObject;

                // PlayerA와 PlayerB가 정상적으로 존재하는지 확인
                if (playerA != null && playerB != null)
                {
                    RopeController rope = playerA.GetComponent<RopeController>();
                    if (rope != null)
                    {
                        // Player A의 타겟을 B의 고유 네트워크 ID로 갱신
                        rope.targetNetworkObjectId.Value = playerB.NetworkObjectId;
                    }
                }
            }
            Debug.Log($"✅ {clients.Count}명의 플레이어가 체인으로 완벽히 연결되었습니다!");
        }
        else
        {
            Debug.Log("⚠️ 플레이어가 혼자이므로 체인을 연결하지 않습니다.");
        }
    }
}