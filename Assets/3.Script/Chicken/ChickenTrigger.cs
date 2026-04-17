using UnityEngine;

// 치킨 주변 트리거
// 플레이어가 들어오면 플레이어 자동공격 대상에 이 치킨을 등록
public class ChickenTrigger : MonoBehaviour
{
    [SerializeField] private ChickenAI chicken;

    private void Awake()
    {
        if (chicken == null)
            chicken = GetComponentInParent<ChickenAI>();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerAutoAttack player = other.GetComponent<PlayerAutoAttack>();

        if (player == null)
            player = other.GetComponentInParent<PlayerAutoAttack>();

        if (player == null)
            return;

        player.AddChicken(chicken);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerAutoAttack player = other.GetComponent<PlayerAutoAttack>();

        if (player == null)
            player = other.GetComponentInParent<PlayerAutoAttack>();

        if (player == null)
            return;

        player.RemoveChicken(chicken);
    }
}