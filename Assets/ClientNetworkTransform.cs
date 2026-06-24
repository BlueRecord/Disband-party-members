using Unity.Netcode.Components;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    // 위치 제어 권한을 서버가 아닌 클라이언트(Owner)에게 넘겨줍니다.
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}