using System.Collections;
using UnityEngine;

// 손님 1명 동작 담당
// 1. 스폰 포인트에서 시작
// 2. 자기 줄 위치로 이동
// 3. 맨 앞 차례면 판매대 앞으로 이동
// 4. 판매대 앞 도착 후 판매 시작 요청
// 5. 구매 완료 시 출구로 이동
// 6. 출구 도착 후 비활성화
public class CustomerAI : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 2f;           // 이동 속도
    [SerializeField] private float rotateSpeed = 720f;       // 회전 속도
    [SerializeField] private float arriveDistance = 0.2f;    // 도착 판정 거리

    [Header("애니메이션")]
    [SerializeField] private Animator animator;

    [Header("UI")]
    [SerializeField] private CustomerOrderUI orderUI;        // 손님 머리 위 주문 UI

    private CustomerManager mgr;         // 손님 매니저
    private SalesCounter counter;        // 판매대
    private Transform exitPoint;         // 출구 위치
    private Transform counterLookPoint;  // 판매대 바라볼 위치
    private Coroutine moveCo;            // 현재 이동 코루틴

    // Animator 파라미터 해시
    private readonly int isWalkHash = Animator.StringToHash("IsWalk");

    public void SetMgr(CustomerManager manager)
    {
        mgr = manager;
    }

    private void Awake()
    {
        // Animator가 비어있으면 자식에서 자동으로 찾기
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // 주문 UI가 비어있으면 자식에서 자동으로 찾기
        if (orderUI == null)
            orderUI = GetComponentInChildren<CustomerOrderUI>(true);
    }

    // 손님 활성화
    // 생성 시 필요한 참조와 위치를 초기화
    public void Activate(
        CustomerManager manager,
        SalesCounter salesCounter,
        Transform spawnPoint,
        Transform lookPoint,
        Transform exit)
    {
        mgr = manager;
        counter = salesCounter;
        counterLookPoint = lookPoint;
        exitPoint = exit;

        // 시작 위치와 방향 설정
        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        // 손님 활성화
        gameObject.SetActive(true);

        // 시작 시에는 Idle 상태
        SetWalk(false);

        // 줄 서는 동안 주문 UI는 숨김
        if (orderUI != null)
            orderUI.Hide();
    }

    // 줄 위치로 이동
    // 뒤에 서 있는 손님들이 자기 자리로 갈 때 사용
    public void MoveToPoint(Vector3 targetPos)
    {
        // 이전 이동이 있으면 중지
        if (moveCo != null)
            StopCoroutine(moveCo);

        moveCo = StartCoroutine(CoMoveTo(targetPos, false));
    }

    // 판매대 앞으로 이동
    // 맨 앞 차례 손님이 호출
    public void MoveToCounter(Vector3 targetPos)
    {
        // 이전 이동이 있으면 중지
        if (moveCo != null)
            StopCoroutine(moveCo);

        moveCo = StartCoroutine(CoMoveTo(targetPos, true));
    }

    // 현재 손님 머리 위 남은 치킨 수 UI 갱신
    public void SetRemainCount(int remain)
    {
        // 주문 UI가 없으면 종료
        if (orderUI == null)
            return;

        // 남은 수량이 0 이하이면 숨김
        if (remain <= 0)
        {
            orderUI.Hide();
            return;
        }

        // 남은 수량 표시
        orderUI.Show(remain);
    }

    // 구매 완료 후 출구 이동
    public void OnSaleFinished()
    {
        // 기존 이동이 있으면 중지
        if (moveCo != null)
            StopCoroutine(moveCo);

        // 구매 끝났으니 주문 UI 숨김
        if (orderUI != null)
            orderUI.Hide();

        // 출구 이동 시작
        moveCo = StartCoroutine(CoExit());
    }

    // 공용 이동 코루틴
    // toCounter가 true면 판매대 도착 후 판매 시작 요청
    private IEnumerator CoMoveTo(Vector3 targetPos, bool toCounter)
    {
        // 이동 시작 시 걷기 애니메이션 켜기
        SetWalk(true);

        while (true)
        {
            // 현재 위치에서 목표 위치까지 방향 계산
            Vector3 dir = targetPos - transform.position;
            dir.y = 0f;

            // 목표 지점에 충분히 가까워졌으면 도착 처리
            if (dir.sqrMagnitude <= arriveDistance * arriveDistance)
                break;

            // 방향이 유효하면 그 방향으로 회전
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    rotateSpeed * Time.deltaTime
                );
            }

            // 현재 바라보는 방향으로 앞으로 이동
            transform.position += transform.forward * moveSpeed * Time.deltaTime;

            yield return null;
        }

        // 마지막 위치를 목표 위치에 정확히 맞춤
        transform.position = new Vector3(
            targetPos.x,
            transform.position.y,
            targetPos.z
        );

        // 도착 후 Idle 상태로 변경
        SetWalk(false);

        // 자기 차례 손님이면 판매대 도착 처리
        if (toCounter)
        {
            // 판매대 방향으로 정렬
            LookAtCounter();

            // 판매대에 도착했으니 판매 시작 요청
            if (counter != null)
                counter.StartCustomerService(this);
        }
    }

    // 판매대 방향 바라보기
    private void LookAtCounter()
    {
        // 바라볼 위치가 없으면 종료
        if (counterLookPoint == null)
            return;

        Vector3 dir = counterLookPoint.position - transform.position;
        dir.y = 0f;

        // 방향이 너무 짧으면 회전하지 않음
        if (dir.sqrMagnitude < 0.0001f)
            return;

        // 판매대 방향으로 즉시 회전
        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    // 출구까지 이동하는 코루틴
    private IEnumerator CoExit()
    {
        // 출구 위치가 없으면 종료
        if (exitPoint == null)
            yield break;

        // 출구로 걸어가므로 걷기 애니메이션 켜기
        SetWalk(true);

        while (true)
        {
            // 현재 위치에서 출구까지 방향 계산
            Vector3 dir = exitPoint.position - transform.position;
            dir.y = 0f;

            // 충분히 가까워지면 도착 처리
            if (dir.sqrMagnitude <= arriveDistance * arriveDistance)
                break;

            // 방향이 유효하면 회전
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    rotateSpeed * Time.deltaTime
                );
            }

            // 앞으로 이동
            transform.position += transform.forward * moveSpeed * Time.deltaTime;

            yield return null;
        }

        // 출구 도착 후 Idle 상태
        SetWalk(false);

        // 매니저가 있으면 매니저를 통해 비활성화
        if (mgr != null)
            mgr.DespawnOnly(this);
        else
            gameObject.SetActive(false);
    }

    // 걷기 애니메이션 on/off
    private void SetWalk(bool value)
    {
        if (animator == null)
            return;

        animator.SetBool(isWalkHash, value);
    }

    private void OnDisable()
    {
        // 비활성화될 때 남아있는 이동 코루틴 정리
        if (moveCo != null)
        {
            StopCoroutine(moveCo);
            moveCo = null;
        }

        // 걷기 애니메이션 종료
        SetWalk(false);

        // 주문 UI도 숨김
        if (orderUI != null)
            orderUI.Hide();
    }
}