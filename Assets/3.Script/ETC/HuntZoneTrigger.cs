using UnityEngine;

// 사냥터 트리거
// - 플레이어가 들어오면 자동 공격 활성
// - 플레이어가 나가면 자동 공격 비활성
public class HuntZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerAutoAttack attack = other.GetComponent<PlayerAutoAttack>();

        if (attack == null)
            attack = other.GetComponentInParent<PlayerAutoAttack>();

        if (attack == null)
            return;

        attack.EnterHuntZone();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerAutoAttack attack = other.GetComponent<PlayerAutoAttack>();

        if (attack == null)
            attack = other.GetComponentInParent<PlayerAutoAttack>();

        if (attack == null)
            return;

        attack.ExitHuntZone();
    }
}