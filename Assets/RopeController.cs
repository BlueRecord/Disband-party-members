using Unity.Netcode;
using UnityEngine;

public class RopeController : NetworkBehaviour
{
    public GameObject chainVisualPrefab;

    // 네트워크로 공유되는 타겟 플레이어의 고유 ID
    public NetworkVariable<ulong> targetNetworkObjectId = new NetworkVariable<ulong>(ulong.MaxValue);

    private Rigidbody targetRigidbody;
    private Transform spawnedChain;
    private SpringJoint joint;

    public override void OnNetworkSpawn()
    {
        // 서버가 타겟 ID를 변경하면 클라이언트가 감지하고 로프를 연결
        targetNetworkObjectId.OnValueChanged += OnTargetChanged;

        if (targetNetworkObjectId.Value != ulong.MaxValue) ConnectRope(targetNetworkObjectId.Value);
    }

    private void OnTargetChanged(ulong previousValue, ulong newValue)
    {
        ConnectRope(newValue);
    }

    private void ConnectRope(ulong targetId)
    {
        if (targetId == ulong.MaxValue) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetId, out NetworkObject targetNetObj))
        {
            targetRigidbody = targetNetObj.GetComponent<Rigidbody>();

            if (joint == null)
            {
                joint = gameObject.AddComponent<SpringJoint>();
                joint.connectedBody = targetRigidbody;

                // [핵심 추가] 물리적으로 당기는 힘의 위치를 발끝이 아닌 허리/가슴 높이(Y: 1.0f)로 변경
                joint.autoConfigureConnectedAnchor = false;
                joint.anchor = new Vector3(0, 1.0f, 0);
                joint.connectedAnchor = new Vector3(0, 1.0f, 0);

                joint.spring = 500f;
                joint.damper = 50f;
                joint.minDistance = 0.5f;
                joint.maxDistance = 2.5f;
            }

            if (spawnedChain == null && chainVisualPrefab != null)
            {
                spawnedChain = Instantiate(chainVisualPrefab).transform;
            }
        }
    }

    void Update()
    {
        if (targetRigidbody != null && spawnedChain != null)
        {
            Vector3 myPos = transform.position + Vector3.up * 1.0f;
            Vector3 targetPos = targetRigidbody.position + Vector3.up * 1.0f;

            // 1. 위치: 중간 지점
            Vector3 midPoint = (myPos + targetPos) / 2f;
            spawnedChain.position = midPoint;

            // [핵심 수정] 2. 회전: Z축(LookAt)이 아닌 Y축(up)이 타겟을 향하도록 강제 지정
            Vector3 directionToTarget = (targetPos - myPos).normalized;
            spawnedChain.up = directionToTarget;

        }
    }
    public override void OnNetworkDespawn()
    {
        targetNetworkObjectId.OnValueChanged -= OnTargetChanged;
        if (spawnedChain != null) Destroy(spawnedChain.gameObject);
    }
}