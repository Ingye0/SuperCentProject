using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어가 현재 바라보는 방향 기준으로
// 전방 원뿔 범위를 자동 공격하는 스크립트
// - 사냥터 안에 있으면 항상 공격
// - 공격 애니메이션 타이밍에 전방 원뿔 범위 데미지 적용
// - 1단계 업그레이드 : 공격력 2
// - 2단계 업그레이드 : 공격력 3 + 공격 거리 2배 + 무기 크기 2배
public class PlayerAutoAttack : MonoBehaviour
{
    [Header("공격 설정")]
    [SerializeField] private float attackDelay = 0.8f; // 한 번 공격 후 다음 공격까지 대기 시간
    [SerializeField] private int damage = 1;           // 기본 공격력 1

    [Header("전방 원뿔 범위")]
    [SerializeField] private float attackRange = 2.2f; // 공격 거리
    [SerializeField] private float attackAngle = 70f;  // 공격 각도

    [Header("무기 표시")]
    [SerializeField] private GameObject weaponObject;  // 플레이어 자식 무기 오브젝트

    [Header("사냥터 상태")]
    [SerializeField] private bool isInHuntZone;        // 사냥터 안에 있는지 여부

    [Header("참조")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Animator animator;

    private Coroutine attackCo;
    private bool isAttacking;
    private bool isRangeUpgraded;

    // 무기 원래 크기 저장
    private Vector3 weaponOriginScale;

    private readonly int attackHash = Animator.StringToHash("Attack");

    public int Damage => damage;
    public float AttackRange => attackRange;
    public float AttackAngle => attackAngle;
    public bool IsInHuntZone => isInHuntZone;

    private void Awake()
    {
        // 인벤토리가 비어있으면 자동으로 가져오기
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        // Animator가 비어있으면 자식에서 자동으로 찾기
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // 무기 원래 크기 저장
        if (weaponObject != null)
            weaponOriginScale = weaponObject.transform.localScale;

        RefreshWeapon();
    }

    private void OnEnable()
    {
        // 중복 코루틴 방지
        if (attackCo != null)
            StopCoroutine(attackCo);

        attackCo = StartCoroutine(CoAttack());
        RefreshWeapon();
    }

    private void OnDisable()
    {
        // 비활성화 시 코루틴 정리
        if (attackCo != null)
        {
            StopCoroutine(attackCo);
            attackCo = null;
        }

        isAttacking = false;
        RefreshWeapon();
    }

    // 사냥터 진입
    public void EnterHuntZone()
    {
        isInHuntZone = true;
        RefreshWeapon();
    }

    // 사냥터 이탈
    public void ExitHuntZone()
    {
        isInHuntZone = false;
        RefreshWeapon();
    }

    // 1단계 업그레이드
    // 공격력 2로 변경
    public void UpgradeDamageLevel1()
    {
        damage = 2;
    }

    // 2단계 업그레이드
    // - 공격력 3
    // - 공격 각도는 그대로
    // - 공격 거리만 2배
    // - 무기 크기도 2배
    public void UpgradeDamageLevel2()
    {
        damage = 3;

        if (isRangeUpgraded)
            return;

        attackRange *= 2f;
        isRangeUpgraded = true;

        if (weaponObject != null)
            weaponObject.transform.localScale = weaponOriginScale * 2f;
    }

    // 사냥터 안에 있으면 항상 공격 반복
    private IEnumerator CoAttack()
    {
        while (true)
        {
            // 이미 공격 중이면 대기
            if (isAttacking)
            {
                yield return null;
                continue;
            }

            // 사냥터 밖이면 공격 안 함
            if (!isInHuntZone)
            {
                yield return null;
                continue;
            }

            // 대상이 있든 없든 공격 시작
            isAttacking = true;

            if (animator != null)
                animator.SetTrigger(attackHash);

            yield return new WaitForSeconds(attackDelay);

            isAttacking = false;
        }
    }

    // 애니메이션 이벤트에서 호출
    // 실제 타격 시점에 전방 원뿔 범위 안의 닭들에게 데미지 적용
    public void ApplyAttackDamage()
    {
        if (!isInHuntZone)
            return;

        List<ChickenAI> targets = FindTargetsInCone();

        for (int i = 0; i < targets.Count; i++)
        {
            ChickenAI chicken = targets[i];

            if (chicken == null)
                continue;

            if (!chicken.gameObject.activeInHierarchy)
                continue;

            chicken.Hit(damage, inventory);
        }
    }

    // 현재 플레이어가 바라보는 방향 기준으로
    // 전방 원뿔 범위 안의 닭 목록 찾기
    private List<ChickenAI> FindTargetsInCone()
    {
        List<ChickenAI> result = new List<ChickenAI>();
        ChickenAI[] allChickens = FindObjectsOfType<ChickenAI>();

        for (int i = 0; i < allChickens.Length; i++)
        {
            ChickenAI chicken = allChickens[i];

            if (chicken == null)
                continue;

            if (!chicken.gameObject.activeInHierarchy)
                continue;

            Vector3 toTarget = chicken.transform.position - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;

            if (distance > attackRange)
                continue;

            if (toTarget.sqrMagnitude < 0.0001f)
            {
                result.Add(chicken);
                continue;
            }

            float angle = Vector3.Angle(transform.forward, toTarget.normalized);

            if (angle <= attackAngle * 0.5f)
                result.Add(chicken);
        }

        return result;
    }

    // 사냥터 안에 있을 때만 무기 표시
    private void RefreshWeapon()
    {
        if (weaponObject == null)
            return;

        weaponObject.SetActive(isInHuntZone);
    }

    // 디버그용 공격 범위 표시
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 leftDir = Quaternion.Euler(0f, -attackAngle * 0.5f, 0f) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0f, attackAngle * 0.5f, 0f) * transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * attackRange);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * attackRange);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * attackRange);
    }
}