using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어가 현재 바라보는 방향 기준으로
// 전방 원뿔 범위를 자동 공격하는 스크립트
// - 사냥터 안에 있으면 계속 공격
// - 공격 애니메이션 타이밍에 실제 데미지 적용
// - 닭을 때릴 때는 ApplyAttackDamage()가 애니메이션 이벤트에서 호출됨
public class PlayerAutoAttack : MonoBehaviour
{
    [Header("공격 설정")]
    [SerializeField] private float attackDelay = 0.8f; // 공격 간격
    [SerializeField] private int damage = 1;           // 기본 공격력

    [Header("전방 원뿔 범위")]
    [SerializeField] private float attackRange = 2.2f; // 공격 거리
    [SerializeField] private float attackAngle = 70f;  // 공격 각도

    [Header("무기 표시")]
    [SerializeField] private GameObject weaponObject;  // 무기 오브젝트

    [Header("사냥터 상태")]
    [SerializeField] private bool isInHuntZone;        // 사냥터 안에 있는지 여부

    [Header("참조")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Animator animator;

    private Coroutine attackCo;   // 자동 공격 루프 코루틴
    private bool isAttacking;     // 현재 공격 중인지
    private bool isRangeUpgraded; // 2단계 업그레이드가 이미 적용됐는지

    private Vector3 weaponOriginScale; // 무기 원래 크기 저장

    private readonly int attackHash = Animator.StringToHash("Attack");

    public int Damage => damage;
    public float AttackRange => attackRange;
    public float AttackAngle => attackAngle;
    public bool IsInHuntZone => isInHuntZone;

    private void Awake()
    {
        // 인벤토리가 비어 있으면 같은 오브젝트에서 찾기
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        // 애니메이터가 비어 있으면 자식에서 찾기
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // 무기의 원래 크기 저장
        if (weaponObject != null)
            weaponOriginScale = weaponObject.transform.localScale;

        RefreshWeapon();
    }

    private void OnEnable()
    {
        // 중복 실행 방지
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

    // 사냥터 진입 시 호출
    public void EnterHuntZone()
    {
        isInHuntZone = true;
        RefreshWeapon();
    }

    // 사냥터 이탈 시 호출
    public void ExitHuntZone()
    {
        isInHuntZone = false;
        RefreshWeapon();
    }

    // 1단계 업그레이드 적용
    public void UpgradeDamageLevel1()
    {
        damage = 2;
    }

    // 2단계 업그레이드 적용
    public void UpgradeDamageLevel2()
    {
        damage = 3;

        // 이미 적용된 상태면 중복 적용 방지
        if (isRangeUpgraded)
            return;

        attackRange *= 2f;
        isRangeUpgraded = true;

        if (weaponObject != null)
            weaponObject.transform.localScale = weaponOriginScale * 2f;
    }

    // 사냥터 안에 있을 때 자동 공격 반복
    private IEnumerator CoAttack()
    {
        while (true)
        {
            // 공격 중이면 다음 루프까지 대기
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

            // 공격 시작
            isAttacking = true;

            if (animator != null)
                animator.SetTrigger(attackHash);

            // attackDelay 동안 대기 후 다음 공격 가능
            yield return new WaitForSeconds(attackDelay);

            isAttacking = false;
        }
    }

    // 애니메이션 이벤트에서 호출
    // 실제 타격 시점에 전방 원뿔 범위 안의 닭에게 데미지 적용
    public void ApplyAttackDamage()
    {
        if (!isInHuntZone)
            return;

        // 무기 휘두르는 소리 재생
        if (AudioManager.I != null)
            AudioManager.I.PlayWeaponSwing();

        // 전방 원뿔 안의 닭 찾기
        List<ChickenAI> targets = FindTargetsInCone();

        for (int i = 0; i < targets.Count; i++)
        {
            ChickenAI chicken = targets[i];

            if (chicken == null)
                continue;

            if (!chicken.gameObject.activeInHierarchy)
                continue;

            // 각 닭에게 데미지 적용
            chicken.Hit(damage, inventory);
        }
    }

    // 현재 바라보는 방향 기준 전방 원뿔 안의 닭 찾기
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

            // 사거리 밖이면 제외
            if (distance > attackRange)
                continue;

            // 너무 가까우면 그냥 포함
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                result.Add(chicken);
                continue;
            }

            // 현재 전방과 목표 방향의 각도 계산
            float angle = Vector3.Angle(transform.forward, toTarget.normalized);

            // 공격 각도 안에 있으면 포함
            if (angle <= attackAngle * 0.5f)
                result.Add(chicken);
        }

        return result;
    }

    // 사냥터 안에 있을 때만 무기 보이기
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