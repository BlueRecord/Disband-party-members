using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class ChainSpawnManager : NetworkBehaviour
{
    public int requiredPlayers = 4;

    public override void OnNetworkSpawn()
    {
        if (IsServer) NetworkManager.Singleton.OnClientConnectedCallback += CheckAndConnectPlayers;
    }

    private void CheckAndConnectPlayers(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == requiredPlayers)
        {
            StartCoroutine(LinkPlayersRoutine());
        }
    }

    private IEnumerator LinkPlayersRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        var clients = NetworkManager.Singleton.ConnectedClientsList;

        for (int i = 0; i < clients.Count - 1; i++)
        {
            NetworkObject playerA = clients[i].PlayerObject;
            NetworkObject playerB = clients[i + 1].PlayerObject;

            // Player A의 RopeController를 찾아 타겟 ID를 B의 고유 ID로 갱신
            RopeController rope = playerA.GetComponent<RopeController>();
            if (rope != null)
            {
                rope.targetNetworkObjectId.Value = playerB.NetworkObjectId;
            }
        }
        Debug.Log("✅ 4명의 플레이어가 체인으로 완벽히 연결되었습니다!");
    }
}