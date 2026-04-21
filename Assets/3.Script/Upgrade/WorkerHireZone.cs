using System.Collections;
using TMPro;
using UnityEngine;

// 고용 가능한 알바 종류
public enum WorkerType
{
    Hunter,
    Delivery
}

// 공용 알바 고용 존
// - 트리거 안에 있는 동안 돈을 자동으로 적립
// - 목표 금액이 다 차면 알바 생성
// - 남은 골드를 숫자로만 표시
// - 고용 완료되면 숫자와 돈 이미지를 숨김
public class WorkerHireZone : MonoBehaviour
{
    [Header("트리거")]
    [SerializeField] private PlayerTriggerRelay triggerRelay; // 플레이어 진입/이탈 감지용

    [Header("알바 종류")]
    [SerializeField] private WorkerType workerType; // 어떤 알바를 생성할지 선택

    [Header("공통 생성")]
    [SerializeField] private Transform spawnPoint; // 알바 생성 위치
    [SerializeField] private int hireCost = 50;    // 총 고용 비용
    [SerializeField] private bool isHired;         // 이미 고용했는지 여부

    [Header("적립 설정")]
    [SerializeField] private int depositPerTick = 10;       // 한 번에 넣을 금액
    [SerializeField] private float depositInterval = 0.1f;  // 적립 간격

    [Header("현재 누적 금액")]
    [SerializeField] private int paidAmount; // 지금까지 넣은 금액

    [Header("사냥 알바 프리팹")]
    [SerializeField] private WorkerHunterAI hunterPrefab; // 사냥 알바 프리팹

    [Header("배달 알바 프리팹")]
    [SerializeField] private DeliveryWorkerAI deliveryPrefab; // 배달 알바 프리팹

    [Header("공용 참조")]
    [SerializeField] private Fryer fryer; // 프라이어 참조

    [Header("배달 알바 전용 참조")]
    [SerializeField] private SalesCounter salesCounter;     // 판매대 참조
    [SerializeField] private Transform fryerPoint;          // 프라이어 이동 포인트
    [SerializeField] private Transform salesCounterPoint;   // 판매대 이동 포인트

    [Header("UI")]
    [SerializeField] private TMP_Text remainGoldText; // 남은 골드 숫자 표시 텍스트
    [SerializeField] private GameObject workerImage;   // 알바 이미지 오브젝트
    [SerializeField] private GameObject squareImage;   // 네모 이미지 오브젝트

    private Coroutine depositCo; // 적립 반복 코루틴

    public bool IsHired => isHired;
    public int PaidAmount => paidAmount;

    private void Awake()
    {
        // 비어있으면 같은 오브젝트에서 자동으로 찾기
        if (triggerRelay == null)
            triggerRelay = GetComponent<PlayerTriggerRelay>();
    }

    private void Start()
    {
        // 시작 시 UI 갱신
        RefreshText();
    }

    // 플레이어가 존 안에 들어오면 적립 시작
    public void StartDeposit()
    {
        // 이미 고용했으면 더 이상 적립 안 함
        if (isHired)
            return;

        // 기존 코루틴이 있으면 중지 후 다시 시작
        if (depositCo != null)
            StopCoroutine(depositCo);

        depositCo = StartCoroutine(CoDeposit());

        // UI 갱신
        RefreshText();
    }

    // 플레이어가 존 밖으로 나가면 적립 중지
    public void StopDeposit()
    {
        if (depositCo == null)
            return;

        StopCoroutine(depositCo);
        depositCo = null;

        // UI 갱신
        RefreshText();
    }

    // 트리거 안에 있는 동안 일정 시간마다 반복 적립
    private IEnumerator CoDeposit()
    {
        while (true)
        {
            TryDepositOnce();
            yield return new WaitForSeconds(depositInterval);
        }
    }

    // 1회 적립 시도
    private void TryDepositOnce()
    {
        // 이미 고용했다면 적립 종료
        if (isHired)
        {
            StopDeposit();
            RefreshText();
            return;
        }

        if (triggerRelay == null)
            return;

        // 현재 트리거 안에 있는 플레이어 인벤토리 가져오기
        PlayerInventory playerInv = triggerRelay.CurrentPlayer;

        if (playerInv == null)
            return;

        // 생성 위치가 비어있으면 종료
        if (spawnPoint == null)
        {
            Debug.LogWarning("[WorkerHireZone] spawnPoint가 비어있음");
            return;
        }

        // 남은 금액 계산
        int remain = hireCost - paidAmount;

        // 이미 다 모였으면 바로 고용
        if (remain <= 0)
        {
            HireWorker();
            return;
        }

        // 이번 틱에 넣을 금액 계산
        int pay = depositPerTick;

        if (pay > remain)
            pay = remain;

        // 플레이어 돈이 부족하면 이번 틱은 대기
        if (playerInv.Get(ResourceType.Money) < pay)
        {
            RefreshText();
            return;
        }

        // 플레이어 돈 차감
        bool used = playerInv.Use(ResourceType.Money, pay);

        if (!used)
        {
            RefreshText();
            return;
        }

        // 누적 금액 증가
        paidAmount += pay;

        // UI 갱신
        RefreshText();

        // 목표 금액 도달 시 알바 생성
        if (paidAmount >= hireCost)
            HireWorker();
    }

    // 실제 알바 생성 처리
    private void HireWorker()
    {
        // 이미 고용했으면 중복 생성 방지
        if (isHired)
            return;

        // 선택한 알바 종류에 따라 생성
        if (workerType == WorkerType.Hunter)
        {
            HireHunter();
            return;
        }

        if (workerType == WorkerType.Delivery)
        {
            HireDelivery();
            return;
        }
    }

    // 사냥 알바 생성
    private void HireHunter()
    {
        if (hunterPrefab == null)
        {
            Debug.LogWarning("[WorkerHireZone] hunterPrefab이 비어있음");
            return;
        }

        WorkerHunterAI worker = Instantiate(
            hunterPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // 생성 직후 프라이어 참조 전달
        worker.SetFryer(fryer);

        isHired = true;
        StopDeposit();

        Debug.Log("[WorkerHireZone] 사냥 알바 고용 완료");

        // 완료 상태 UI 갱신
        RefreshText();
    }

    // 배달 알바 생성
    private void HireDelivery()
    {
        if (deliveryPrefab == null)
        {
            Debug.LogWarning("[WorkerHireZone] deliveryPrefab이 비어있음");
            return;
        }

        DeliveryWorkerAI worker = Instantiate(
            deliveryPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // 생성 직후 필요한 참조 전달
        worker.Init(fryer, salesCounter, fryerPoint, salesCounterPoint);

        isHired = true;
        StopDeposit();

        Debug.Log("[WorkerHireZone] 배달 알바 고용 완료");

        // 완료 상태 UI 갱신
        RefreshText();
    }

    // 현재 남은 골드 텍스트와 돈 이미지 표시 갱신
    private void RefreshText()
    {
        // 이미 고용 완료면 텍스트와 돈 이미지 숨김
        if (isHired)
        {
            if (remainGoldText != null)
                remainGoldText.gameObject.SetActive(false);

            if (workerImage != null)
                workerImage.SetActive(false);

            if (squareImage != null)
                squareImage.SetActive(false);

            return;
        }

        // 아직 고용 전이면 둘 다 표시
        if (remainGoldText != null)
            remainGoldText.gameObject.SetActive(true);

        if (workerImage != null)
            workerImage.SetActive(true);

        if (squareImage != null)
            squareImage.SetActive(true);

        // 텍스트가 없으면 종료
        if (remainGoldText == null)
            return;

        // 남은 금액 계산
        int remain = hireCost - paidAmount;

        if (remain < 0)
            remain = 0;

        // 숫자만 표시
        remainGoldText.text = remain.ToString();
    }
}