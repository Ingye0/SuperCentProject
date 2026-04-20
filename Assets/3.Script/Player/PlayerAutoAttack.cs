using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 범위 안의 치킨 중 가장 가까운 치킨을 자동으로 공격하는 스크립트
// 실제 데미지는 애니메이션 이벤트 타이밍에 들어감
public class PlayerAutoAttack : MonoBehaviour
{
    [Header("공격 설정")]
    [SerializeField] private float attackDelay = 0.8f; // 한 번 공격 후 다음 공격까지 대기 시간
    [SerializeField] private int damage = 1;           // 한 번 공격할 때 줄 데미지

    [Header("바라보기 설정")]
    [SerializeField] private bool lookTarget = true;   // 공격 대상이 있으면 자동으로 바라볼지
    [SerializeField] private float turnSpeed = 720f;   // 회전 속도

    [Header("참조")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private Animator animator;

    private List<ChickenAI> chickens = new List<ChickenAI>(); // 범위 안 치킨 목록
    private Coroutine attackCo;
    private ChickenAI nowTarget;      // 현재 가장 가까운 대상
    private ChickenAI attackTarget;   // 이번 공격에서 실제로 때릴 대상으로 고정한 대상

    private bool isAttacking;         // 현재 공격 애니메이션 진행 중인지

    // Animator 파라미터 해시
    private readonly int attackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        // 인벤토리가 비어있으면 자동으로 가져옴
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();

        // Animator가 비어있으면 자식에서 자동으로 가져옴
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        // 혹시 기존 코루틴이 있으면 중복 실행 방지
        if (attackCo != null)
            StopCoroutine(attackCo);

        // 자동 공격 시작
        attackCo = StartCoroutine(CoAttack());
    }

    private void Update()
    {
        // 바라보기 기능을 안 쓰면 종료
        if (!lookTarget)
            return;

        // 현재 가장 가까운 치킨 찾기
        nowTarget = GetNear();

        if (nowTarget == null)
            return;

        // 비활성 치킨이면 무시
        if (!nowTarget.gameObject.activeInHierarchy)
            return;

        // 현재 타겟 방향으로 플레이어 회전
        Look(nowTarget.transform);
    }

    private void OnDisable()
    {
        // 비활성화될 때 코루틴 정리
        if (attackCo != null)
        {
            StopCoroutine(attackCo);
            attackCo = null;
        }

        // 목록과 상태 초기화
        chickens.Clear();
        nowTarget = null;
        attackTarget = null;
        isAttacking = false;
    }

    // 치킨이 범위 안에 들어오면 등록
    public void AddChicken(ChickenAI chicken)
    {
        if (chicken == null)
            return;

        if (!chickens.Contains(chicken))
            chickens.Add(chicken);
    }

    // 치킨이 범위를 나가면 제거
    public void RemoveChicken(ChickenAI chicken)
    {
        if (chicken == null)
            return;

        chickens.Remove(chicken);

        if (nowTarget == chicken)
            nowTarget = null;

        if (attackTarget == chicken)
            attackTarget = null;
    }

    // 자동 공격 코루틴
    private IEnumerator CoAttack()
    {
        while (true)
        {
            // 이미 공격 중이면 끝날 때까지 기다림
            if (isAttacking)
            {
                yield return null;
                continue;
            }

            // 가장 가까운 치킨 찾기
            nowTarget = GetNear();

            if (nowTarget != null && nowTarget.gameObject.activeInHierarchy)
            {
                // 이번 공격 대상을 고정
                attackTarget = nowTarget;

                // 공격 직전에 대상 쪽을 바라보게 함
                Look(attackTarget.transform);

                // 공격 시작
                isAttacking = true;

                // 공격 애니메이션 실행
                if (animator != null)
                    animator.SetTrigger(attackHash);

                // 공격 쿨타임 대기
                yield return new WaitForSeconds(attackDelay);

                // 다음 공격 가능 상태로 변경
                isAttacking = false;
            }
            else
            {
                // 공격 대상이 없으면 다음 프레임까지 대기
                yield return null;
            }
        }
    }

    // 애니메이션 이벤트에서 호출할 함수
    // 공격 모션의 실제 타격 타이밍에 연결하면 됨
    public void ApplyAttackDamage()
    {
        if (attackTarget == null)
            return;

        if (!attackTarget.gameObject.activeInHierarchy)
            return;

        attackTarget.Hit(damage, inventory);
        Clean();
    }

    // 현재 범위 안에서 가장 가까운 치킨 찾기
    private ChickenAI GetNear()
    {
        Clean();

        ChickenAI target = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < chickens.Count; i++)
        {
            ChickenAI c = chickens[i];

            if (c == null)
                continue;

            if (!c.gameObject.activeInHierarchy)
                continue;

            float dist = (c.transform.position - transform.position).sqrMagnitude;

            if (dist < minDist)
            {
                minDist = dist;
                target = c;
            }
        }

        return target;
    }

    // 특정 대상을 향해 부드럽게 회전
    private void Look(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            turnSpeed * Time.deltaTime
        );
    }

    // 죽었거나 비활성화된 치킨을 목록에서 제거
    private void Clean()
    {
        for (int i = chickens.Count - 1; i >= 0; i--)
        {
            if (chickens[i] == null || !chickens[i].gameObject.activeInHierarchy)
                chickens.RemoveAt(i);
        }
    }
}