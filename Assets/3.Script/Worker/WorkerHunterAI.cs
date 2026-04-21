using System.Collections;
using UnityEngine;

// 자동 사냥 알바
// - 가장 가까운 닭을 찾아 이동
// - 공격 범위 안에 들어오면 공격
// - 실제 타격 시점마다 ChickenAI.Hit()를 호출해서
//   닭의 실제 HP를 깎고 체력바도 같이 갱신되게 함
// - 닭이 죽으면 생닭 1개를 프라이어에 바로 넣어줌
// - 공격할 때 무기 휘두르는 소리 재생
// - 프라이어에 생닭을 넣을 때 알바 액션 소리 재생
public class WorkerHunterAI : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 2.5f;      // 이동 속도
    [SerializeField] private float rotateSpeed = 360f;    // 회전 속도
    [SerializeField] private float arriveDistance = 0.1f; // 거의 도착했다고 보는 거리

    [Header("타겟 유지")]
    [SerializeField] private float retargetDistance = 6f; // 현재 타겟이 너무 멀어지면 다시 가까운 닭 탐색

    [Header("공격")]
    [SerializeField] private float attackRange = 1.5f;    // 공격 가능한 거리
    [SerializeField] private float attackDelay = 0.8f;    // 한 번 공격 후 다음 공격까지 대기 시간
    [SerializeField] private int damage = 1;              // 알바 공격력

    [Header("참조")]
    [SerializeField] private Fryer fryer;                    // 죽인 닭을 생닭 1개로 프라이어에 전달
    [SerializeField] private Animator animator;              // 걷기/공격 애니메이션
    [SerializeField] private CharacterController controller; // 이동용 컨트롤러

    private Coroutine loopCo;        // 메인 행동 루프 코루틴
    private bool isAttacking;        // 현재 공격 중인지
    private ChickenAI targetChicken; // 현재 추적 중인 닭
    private ChickenAI attackTarget;  // 이번 공격에서 실제로 때릴 닭

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
        // 중복 실행 방지
        if (loopCo != null)
            StopCoroutine(loopCo);

        // 행동 루프 시작
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

        // 걷기 애니메이션 끄기
        SetWalk(false);
    }

    // 외부에서 프라이어 연결
    public void SetFryer(Fryer newFryer)
    {
        fryer = newFryer;
    }

    // 알바 메인 루프
    private IEnumerator CoLoop()
    {
        while (true)
        {
            // 공격 중에는 다른 행동 안 함
            if (isAttacking)
            {
                yield return null;
                continue;
            }

            // 타겟이 없거나 비활성이면 새로 찾기
            if (!IsTargetValid(targetChicken))
                targetChicken = FindNearChicken();

            // 타겟이 너무 멀어졌으면 다시 가까운 닭 찾기
            if (IsTargetValid(targetChicken))
            {
                float distFromTarget = GetFlatDistance(transform.position, targetChicken.transform.position);

                if (distFromTarget > retargetDistance)
                    targetChicken = FindNearChicken();
            }

            // 그래도 타겟이 없으면 대기
            if (!IsTargetValid(targetChicken))
            {
                SetWalk(false);
                yield return null;
                continue;
            }

            // 현재 타겟과 거리 계산
            float dist = GetFlatDistance(transform.position, targetChicken.transform.position);

            // 공격 거리 밖이면 이동
            if (dist > attackRange)
            {
                MoveTo(targetChicken.transform.position);
                yield return null;
                continue;
            }

            // 공격 거리 안이면 공격 시작
            SetWalk(false);
            yield return CoAttack(targetChicken);
        }
    }

    // 공격 처리
    private IEnumerator CoAttack(ChickenAI chicken)
    {
        // 공격 대상이 유효하지 않으면 종료
        if (!IsTargetValid(chicken))
            yield break;

        // 공격 시작
        isAttacking = true;

        // 이번 공격 타겟 고정
        attackTarget = chicken;

        // 공격 전 타겟 방향 회전
        yield return RotateToward(chicken.transform.position);

        // 공격 애니메이션 실행
        if (animator != null)
            animator.SetTrigger(attackHash);

        // 다음 공격까지 대기
        yield return new WaitForSeconds(attackDelay);

        // 공격 종료
        isAttacking = false;
    }

    // 애니메이션 이벤트에서 호출
    public void ApplyAttackDamage()
    {
        // 공격 대상이 없거나 이미 사라졌으면 종료
        if (!IsTargetValid(attackTarget))
            return;

        // 사냥 알바 공격 소리
        if (AudioManager.I != null)
            AudioManager.I.PlayWeaponSwing();

        // Hit 호출 후 바로 비활성화될 수 있으므로 지역변수로 저장
        ChickenAI hitChicken = attackTarget;

        // 실제 데미지 적용
        hitChicken.Hit(damage, null);

        // 닭이 죽었으면 프라이어에 생닭 1개 전달
        if (!hitChicken.gameObject.activeInHierarchy)
        {
            if (fryer != null)
            {
                bool added = fryer.AddRawDirect(1);

                // 실제로 프라이어에 들어갔을 때만 알바 액션 소리
                if (added)
                {
                    if (AudioManager.I != null)
                        AudioManager.I.PlayPop();
                }
            }
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

    // 목표 위치까지 이동
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

        // 목표 방향 회전
        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotateSpeed * Time.deltaTime
        );

        // 앞으로 이동
        Vector3 move = transform.forward * moveSpeed * Time.deltaTime;

        if (controller != null)
            controller.Move(move);
        else
            transform.position += move;

        SetWalk(true);
    }

    // 공격 전에 타겟 방향을 바라보게 회전
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

    // 타겟 유효성 검사
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

    // y축 무시 평면 거리 계산
    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}