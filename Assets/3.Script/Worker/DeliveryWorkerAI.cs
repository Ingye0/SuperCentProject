using System.Collections;
using UnityEngine;

// 자동 배달 알바
// - 프라이어에서 튀긴닭을 최대 3개까지 가져감
// - 0.2초마다 1개씩 가져오고 1개씩 내려놓음
// - 판매대에 가져다 놓은 뒤 다시 프라이어로 돌아감
// - PlayerInventory 없이 carryCount만으로 처리
// - 등 뒤에는 BackStackView로 튀긴닭 수량을 표시
// - 가져갈 때 / 내려놓을 때 알바 액션 소리 재생
public class DeliveryWorkerAI : MonoBehaviour
{
    private enum State
    {
        WaitAtFryer,
        MoveToFryer,
        TakeFromFryer,
        MoveToCounter,
        PutToCounter
    }

    [Header("이동")]
    [SerializeField] private float moveSpeed = 2.5f;        // 이동 속도
    [SerializeField] private float rotateSpeed = 360f;      // 회전 속도
    [SerializeField] private float arriveDistance = 0.15f;  // 도착 판정 거리

    [Header("운반")]
    [SerializeField] private int maxCarry = 3;       // 최대 3개까지 운반
    [SerializeField] private float takeDelay = 0.2f; // 가져가는 간격
    [SerializeField] private float putDelay = 0.2f;  // 내려놓는 간격

    [Header("참조")]
    [SerializeField] private Fryer fryer;                      // 프라이어 참조
    [SerializeField] private SalesCounter salesCounter;        // 판매대 참조
    [SerializeField] private Transform fryerPoint;             // 프라이어 이동 포인트
    [SerializeField] private Transform salesCounterPoint;      // 판매대 이동 포인트
    [SerializeField] private Animator animator;                // 걷기 애니메이터
    [SerializeField] private CharacterController controller;   // 이동용 컨트롤러
    [SerializeField] private BackStackView stackView;          // 등 뒤 스택 표시

    [Header("현재 상태")]
    [SerializeField] private State state = State.WaitAtFryer;  // 현재 행동 상태
    [SerializeField] private int carryCount;                   // 현재 들고 있는 튀긴닭 개수

    private Coroutine loopCo;

    private readonly int isWalkHash = Animator.StringToHash("IsWalk");

    public int CarryCount => carryCount;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (stackView == null)
            stackView = GetComponentInChildren<BackStackView>();

        RefreshView();
    }

    private void OnEnable()
    {
        if (loopCo != null)
            StopCoroutine(loopCo);

        loopCo = StartCoroutine(CoLoop());
    }

    private void OnDisable()
    {
        if (loopCo != null)
        {
            StopCoroutine(loopCo);
            loopCo = null;
        }

        SetWalk(false);
    }

    // 생성 직후 외부에서 참조 연결
    public void Init(Fryer newFryer, SalesCounter newSalesCounter, Transform newFryerPoint, Transform newSalesCounterPoint)
    {
        fryer = newFryer;
        salesCounter = newSalesCounter;
        fryerPoint = newFryerPoint;
        salesCounterPoint = newSalesCounterPoint;
    }

    // 메인 루프
    private IEnumerator CoLoop()
    {
        while (true)
        {
            // 손에 닭이 없으면 프라이어로 감
            if (carryCount <= 0)
            {
                state = State.MoveToFryer;
                yield return CoMoveToPoint(fryerPoint);

                state = State.TakeFromFryer;
                yield return CoTakeFromFryer();

                // 못 들었으면 프라이어 앞에서 대기
                if (carryCount <= 0)
                {
                    state = State.WaitAtFryer;
                    yield return null;
                    continue;
                }
            }

            // 닭을 들고 있으면 판매대로 이동
            state = State.MoveToCounter;
            yield return CoMoveToPoint(salesCounterPoint);

            // 판매대에 내려놓기
            state = State.PutToCounter;
            yield return CoPutToCounter();

            yield return null;
        }
    }

    // 특정 포인트까지 이동
    private IEnumerator CoMoveToPoint(Transform targetPoint)
    {
        if (targetPoint == null)
            yield break;

        while (true)
        {
            Vector3 targetPos = targetPoint.position;
            Vector3 flatCurrent = transform.position;
            Vector3 flatTarget = targetPos;

            flatCurrent.y = 0f;
            flatTarget.y = 0f;

            float dist = Vector3.Distance(flatCurrent, flatTarget);

            if (dist <= arriveDistance)
            {
                SetWalk(false);
                yield break;
            }

            MoveTo(targetPos);
            yield return null;
        }
    }

    // 프라이어에서 튀긴닭 수령
    private IEnumerator CoTakeFromFryer()
    {
        if (fryer == null)
            yield break;

        while (carryCount < maxCarry)
        {
            // 더 가져갈 닭이 없으면 종료
            if (!fryer.CanTake())
                yield break;

            // 프라이어에서 1개 직접 꺼내기
            int got = fryer.TakeFriedDirect(1);

            if (got <= 0)
                yield break;

            // 현재 들고 있는 개수 증가
            carryCount += got;
            RefreshView();

            // 알바가 가져갈 때 소리
            if (AudioManager.I != null)
                AudioManager.I.PlayPop();

            yield return new WaitForSeconds(takeDelay);
        }
    }

    // 판매대에 튀긴닭 내려놓기
    private IEnumerator CoPutToCounter()
    {
        if (salesCounter == null)
            yield break;

        while (carryCount > 0)
        {
            // 판매대가 꽉 차 있으면 잠깐 대기
            if (!salesCounter.CanPutFried())
            {
                yield return null;
                continue;
            }

            // 판매대에 1개 직접 올리기
            int put = salesCounter.PutFriedDirect(1);

            if (put <= 0)
            {
                yield return null;
                continue;
            }

            // 들고 있는 수 감소
            carryCount -= put;
            RefreshView();

            // 알바가 내려놓을 때 소리
            if (AudioManager.I != null)
                AudioManager.I.PlayPop();

            yield return new WaitForSeconds(putDelay);
        }
    }

    // 목표 위치로 이동
    private void MoveTo(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
        {
            SetWalk(false);
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            rotateSpeed * Time.deltaTime
        );

        Vector3 move = transform.forward * moveSpeed * Time.deltaTime;

        if (controller != null)
            controller.Move(move);
        else
            transform.position += move;

        SetWalk(true);
    }

    // 등 뒤 스택 표시 갱신
    private void RefreshView()
    {
        if (stackView != null)
            stackView.SetCount(0, carryCount);
    }

    // 걷기 애니메이션 on/off
    private void SetWalk(bool value)
    {
        if (animator == null)
            return;

        animator.SetBool(isWalkHash, value);
    }
}