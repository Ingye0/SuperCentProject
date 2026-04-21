using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 자동 사냥 알바
// - 가장 가까운 닭을 찾아 이동
// - 공격 범위 안에 들어오면 공격
// - 닭 체력 3 기준으로 3번 맞으면 처치
// - 처치한 생닭은 프라이어로 바로 전달
// - 이동/회전을 부드럽게 해서 텔레포트처럼 보이지 않게 처리
public class WorkerHunterAI : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 2.5f;   // 알바 이동 속도
    [SerializeField] private float rotateSpeed = 360f; // 회전 속도
    [SerializeField] private float arriveDistance = 0.1f; // 거의 도착했다고 보는 거리

    [Header("타겟 유지")]
    [SerializeField] private float retargetDistance = 6f; // 타겟이 너무 멀어지면 새 타겟 탐색

    [Header("공격")]
    [SerializeField] private float attackRange = 1.5f; // 공격 가능한 거리
    [SerializeField] private float attackDelay = 0.8f; // 한 번 공격 후 다음 공격까지 대기 시간
    [SerializeField] private int damage = 1;           // 알바 공격력 1

    [Header("참조")]
    [SerializeField] private Fryer fryer;              // 잡은 생닭을 바로 넣어줄 프라이어
    [SerializeField] private Animator animator;        // 알바 애니메이터
    [SerializeField] private CharacterController controller; // 이동용 컨트롤러

    private Coroutine loopCo;          // 메인 행동 코루틴
    private bool isAttacking;          // 현재 공격 중인지
    private ChickenAI targetChicken;   // 현재 추적 중인 닭
    private ChickenAI attackTarget;    // 이번 공격에서 실제로 때릴 닭

    // 닭별 누적 데미지 저장
    // 닭 체력이 3이므로 3 누적되면 처치
    private Dictionary<ChickenAI, int> damageMap = new Dictionary<ChickenAI, int>();

    // Animator 파라미터 해시
    private readonly int isWalkHash = Animator.StringToHash("IsWalk");
    private readonly int attackHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        // Animator가 비어있으면 자식에서 자동으로 찾기
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // CharacterController가 비어있으면 자동으로 찾기
        if (controller == null)
            controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        // 혹시 기존 루프가 돌고 있으면 중복 실행 방지
        if (loopCo != null)
            StopCoroutine(loopCo);

        // 알바 행동 시작
        loopCo = StartCoroutine(CoLoop());
    }

    private void OnDisable()
    {
        // 비활성화될 때 코루틴 정리
        if (loopCo != null)
        {
            StopCoroutine(loopCo);
            loopCo = null;
        }

        // 상태 초기화
        isAttacking = false;
        targetChicken = null;
        attackTarget = null;

        // 걷기 애니메이션 종료
        SetWalk(false);
    }

    // 생성 직후 외부에서 프라이어를 연결해주기 위한 함수
    public void SetFryer(Fryer newFryer)
    {
        fryer = newFryer;
    }

    // 알바의 메인 행동 루프
    // 1. 공격 중이면 대기
    // 2. 타겟 닭 확인
    // 3. 타겟까지 이동
    // 4. 공격 범위 안이면 공격
    private IEnumerator CoLoop()
    {
        while (true)
        {
            // 공격 중에는 다른 행동을 하지 않음
            if (isAttacking)
            {
                yield return null;
                continue;
            }

            // 현재 타겟이 없거나 죽었으면 새로 찾기
            if (!IsTargetValid(targetChicken))
                targetChicken = FindNearChicken();

            // 현재 타겟이 너무 멀어졌으면 가까운 닭으로 다시 탐색
            if (IsTargetValid(targetChicken))
            {
                float distFromTarget = GetFlatDistance(transform.position, targetChicken.transform.position);

                if (distFromTarget > retargetDistance)
                    targetChicken = FindNearChicken();
            }

            // 최종적으로도 타겟이 없으면 대기
            if (!IsTargetValid(targetChicken))
            {
                SetWalk(false);
                yield return null;
                continue;
            }

            // 현재 타겟과의 거리 계산
            float dist = GetFlatDistance(transform.position, targetChicken.transform.position);

            // 공격 범위 밖이면 타겟 쪽으로 계속 이동
            if (dist > attackRange)
            {
                MoveTo(targetChicken.transform.position);
                yield return null;
                continue;
            }

            // 공격 범위 안에 들어오면 멈추고 공격
            SetWalk(false);
            yield return CoAttack(targetChicken);
        }
    }

    // 공격 코루틴
    // - 공격 시작 시 공격 대상을 고정
    // - 공격 전에 타겟 방향으로 부드럽게 회전
    // - 애니메이션 실행
    // - 실제 데미지는 애니메이션 이벤트에서 적용
    private IEnumerator CoAttack(ChickenAI chicken)
    {
        if (!IsTargetValid(chicken))
            yield break;

        // 공격 시작
        isAttacking = true;
        attackTarget = chicken;

        // 공격 전에 타겟 방향으로 살짝 부드럽게 회전
        yield return RotateToward(chicken.transform.position);

        // 공격 애니메이션 실행
        if (animator != null)
            animator.SetTrigger(attackHash);

        // 공격 딜레이만큼 대기 후 다음 공격 가능
        yield return new WaitForSeconds(attackDelay);

        isAttacking = false;
    }

    // 애니메이션 이벤트에서 호출
    // 실제 타격 타이밍에 호출해서 데미지를 넣음
    public void ApplyAttackDamage()
    {
        // 공격 대상이 없거나 이미 죽었으면 종료
        if (!IsTargetValid(attackTarget))
            return;

        int currentDamage = 0;

        // 기존 누적 데미지가 있으면 가져오기
        if (damageMap.ContainsKey(attackTarget))
            currentDamage = damageMap[attackTarget];

        // 이번 공격 데미지 추가
        currentDamage += damage;
        damageMap[attackTarget] = currentDamage;

        // 닭 체력 3 기준으로 누적 3 이상이면 처치
        if (currentDamage >= 3)
        {
            damageMap.Remove(attackTarget);

            // 프라이어가 있으면 생닭 1개 바로 전달
            if (fryer != null)
                fryer.AddRawDirect(1);

            // 닭은 플레이어 인벤토리 보상 없이 제거
            attackTarget.Hit(999, null);
        }
    }

    // 현재 씬 안에서 가장 가까운 활성 닭 찾기
    private ChickenAI FindNearChicken()
    {
        ChickenAI[] all = FindObjectsOfType<ChickenAI>();

        ChickenAI near = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < all.Length; i++)
        {
            ChickenAI chicken = all[i];

            // 죽었거나 비활성 닭은 제외
            if (!IsTargetValid(chicken))
                continue;

            float dist = (chicken.transform.position - transform.position).sqrMagnitude;

            if (dist < minDist)
            {
                minDist = dist;
                near = chicken;
            }
        }

        return near;
    }

    // 목표 위치로 부드럽게 이동
    private void MoveTo(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        // 거의 도착했으면 정지
        if (dir.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            SetWalk(false);
            return;
        }

        // 목표 방향으로 천천히 회전
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotateSpeed * Time.deltaTime
        );

        // 현재 바라보는 방향으로 앞으로 이동
        Vector3 moveDir = transform.forward;
        Vector3 move = moveDir * moveSpeed * Time.deltaTime;

        if (controller != null)
            controller.Move(move);
        else
            transform.position += move;

        SetWalk(true);
    }

    // 공격 전에 타겟 방향으로 잠깐 부드럽게 회전
    // 즉시 회전보다 덜 튀어 보이게 하기 위한 처리
    private IEnumerator RotateToward(Vector3 targetPos)
    {
        float timer = 0f;
        float maxTime = 0.15f;

        while (timer < maxTime)
        {
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                yield break;

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );

            timer += Time.deltaTime;
            yield return null;
        }
    }

    // 현재 타겟이 유효한지 검사
    // - null이면 false
    // - 비활성 상태면 false
    private bool IsTargetValid(ChickenAI chicken)
    {
        if (chicken == null)
            return false;

        if (!chicken.gameObject.activeInHierarchy)
            return false;

        return true;
    }

    // 걷기 애니메이션 on/off
    private void SetWalk(bool value)
    {
        if (animator == null)
            return;

        animator.SetBool(isWalkHash, value);
    }

    // y축을 무시한 평면 거리 계산
    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}