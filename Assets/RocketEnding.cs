using Unity.Netcode;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))] // 오디오 소스 컴포넌트를 강제로 추가합니다.
public class RocketEnding : NetworkBehaviour // 네트워크 통신을 위해 변경
{
    [Header("이동할 엔딩 씬 이름")]
    public string endingSceneName = "EndingScene";

    [Header("사운드 설정")]
    public AudioClip rocketSound; // 여기에 로켓 사운드를 넣을 겁니다.

    private AudioSource audioSource;
    private bool isTriggered = false; // 여러 명이 동시에 닿아도 한 번만 실행되게 하는 스위치

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 씬 전환과 타이머는 서버(방장)만 통제하며, 이미 실행 중이면 무시합니다.
        if (!IsServer || isTriggered) return;

        if (other.CompareTag("Player"))
        {
            isTriggered = true; // 스위치를 켜서 중복 실행 방지
            StartCoroutine(PlaySoundAndLoadScene());
        }
    }

    private IEnumerator PlaySoundAndLoadScene()
    {
        // 1. 모든 클라이언트에게 로켓 소리를 재생하라고 명령합니다.
        PlayRocketSoundClientRpc();

        // 2. 소리가 재생되는 동안 대기합니다. (예: 2.5초)
        // 이 시간 동안 로켓 소리를 들으며 씬 전환을 기다립니다.
        yield return new WaitForSeconds(2.5f);

        // 3. 약속된 시간이 끝나면 다 함께 엔딩 씬으로 이동합니다.
        NetworkManager.Singleton.SceneManager.LoadScene(endingSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    [ClientRpc]
    private void PlayRocketSoundClientRpc()
    {
        if (rocketSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(rocketSound);
            Debug.Log("🚀 로켓 사운드 재생 중!");
        }
    }
}