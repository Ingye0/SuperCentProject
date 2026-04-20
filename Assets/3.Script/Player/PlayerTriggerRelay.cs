using UnityEngine;
using UnityEngine.Events;

// 플레이어 진입/이탈을 유니티 이벤트로 넘기는 공용 트리거
public class PlayerTriggerRelay : MonoBehaviour
{
    [Header("이벤트")]
    [SerializeField] private UnityEvent onEnter;
    [SerializeField] private UnityEvent onExit;

    // 현재 트리거 안에 있는 플레이어
    public PlayerInventory CurrentPlayer { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        // 들어온 오브젝트에서 플레이어 인벤토리 찾기
        PlayerInventory inv = FindInv(other);

        // 플레이어가 아니면 무시
        if (inv == null)
            return;

        // 현재 플레이어 저장
        CurrentPlayer = inv;

        // 진입 이벤트 실행
        onEnter.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        // 나간 오브젝트에서 플레이어 인벤토리 찾기
        PlayerInventory inv = FindInv(other);

        // 플레이어가 아니면 무시
        if (inv == null)
            return;

        // 현재 저장된 플레이어가 아니면 무시
        if (CurrentPlayer != inv)
            return;

        // 이탈 이벤트 실행
        onExit.Invoke();

        // 현재 플레이어 비우기
        CurrentPlayer = null;
    }

    private PlayerInventory FindInv(Collider other)
    {
        // 플레이어인벤토리 찾기
        PlayerInventory inv = other.GetComponent<PlayerInventory>();

        return inv;
    }
}