using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class FinalSceneManager : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // 씬이 로딩되고 네트워크 오브젝트가 활성화되면, 서버(방장)가 순간이동 지시를 시작합니다.
        if (IsServer)
        {
            StartCoroutine(TeleportPlayersRoutine());
        }
    }

    private IEnumerator TeleportPlayersRoutine()
    {
        // 씬 로딩 후 클라이언트들의 환경이 완전히 준비될 때까지 잠시 대기합니다.
        yield return new WaitForSeconds(0.5f);

        // 1. FinalCreditScene 안에 있는 "SpawnPoint" 태그를 모두 찾습니다.
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        var clients = NetworkManager.Singleton.ConnectedClientsList;

        // 2. 접속 중인 모든 플레이어에게 순서대로 좌표를 할당합니다.
        for (int i = 0; i < clients.Count; i++)
        {
            if (clients[i].PlayerObject != null)
            {
                // 기본 좌표 (태그를 깜빡했을 경우 대비)
                Vector3 targetPos = new Vector3(i * 2.0f, 1f, 0f);
                Quaternion targetRot = Quaternion.identity;

                // 스폰 포인트가 존재하면 해당 좌표를 사용
                if (spawnPoints != null && i < spawnPoints.Length)
                {
                    targetPos = spawnPoints[i].transform.position;
                    targetRot = spawnPoints[i].transform.rotation;
                }

                // 3. 각 플레이어의 고유 ID와 이동할 위치를 담아 클라이언트들에게 전송합니다.
                TeleportPlayerClientRpc(clients[i].PlayerObject.NetworkObjectId, targetPos, targetRot);
            }
        }
    }

    // 서버가 모든 클라이언트에게 뿌리는 명령 (하지만 자기 캐릭터만 움직이도록 내부에서 필터링)
    [ClientRpc]
    private void TeleportPlayerClientRpc(ulong networkObjectId, Vector3 newPosition, Quaternion newRotation)
    {
        // 전달받은 ID를 가진 오브젝트를 찾습니다.
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject playerObj))
        {
            // [핵심] ClientNetworkTransform을 사용 중이므로, IsOwner(자기 자신)인 경우에만 위치를 바꿉니다.
            if (playerObj.IsOwner)
            {
                // 위치와 바라보는 방향 세팅
                playerObj.transform.position = newPosition;
                playerObj.transform.rotation = newRotation;

                // 추락하면서 생긴 가속도가 남아있지 않도록 물리 엔진(Rigidbody) 초기화
                Rigidbody rb = playerObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                Debug.Log("새로운 스폰 포인트로 텔레포트 완료!");
            }
        }
    }
}