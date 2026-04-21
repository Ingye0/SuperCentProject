using System.Collections;
using UnityEngine;

// 판매대
// - 플레이어는 튀긴닭을 판매대에 올릴 수 있음
// - 손님은 판매대의 치킨을 1개씩 가져감
// - 손님 1명 판매 완료 시 돈 프리팹 1개 생성
// - 플레이어는 돈 프리팹을 가져가 실제 돈으로 환전함
public class SalesCounter : MonoBehaviour
{
    [Header("플레이어 트리거")]
    [SerializeField] private PlayerTriggerRelay putTrigger;   // 튀긴닭 올리는 트리거
    [SerializeField] private PlayerTriggerRelay takeTrigger;  // 돈 받는 트리거

    [Header("보관 설정")]
    [SerializeField] private int maxFried = 20;        // 판매대 최대 치킨 수
    [SerializeField] private int maxMoneyStack = 30;   // 판매대 최대 돈 프리팹 수

    [Header("손님 판매 설정")]
    [SerializeField] private int needFriedForCustomer = 4;   // 손님 1명이 사갈 치킨 수
    [SerializeField] private int rewardMoney = 10;           // 돈 프리팹 1개의 실제 가치
    [SerializeField] private float customerTakeDelay = 0.5f; // 손님이 1개씩 가져가는 간격

    [Header("플레이어 처리 속도")]
    [SerializeField] private float putDelay = 0.1f;   // 플레이어가 치킨 올리는 간격
    [SerializeField] private float takeDelay = 0.1f;  // 플레이어가 돈 받는 간격

    [Header("현재 상태")]
    [SerializeField] private int friedStored; // 판매대 위 치킨 수
    [SerializeField] private int moneyStored; // 판매대 위 돈 프리팹 수

    [Header("현재 손님 진행도")]
    [SerializeField] private int currentCustomerTaken; // 현재 손님이 지금까지 가져간 치킨 수

    [Header("표시")]
    [SerializeField] private SalesCounterView stackView; // 판매대 위 표시 갱신용

    private Coroutine putCo;
    private Coroutine takeCo;
    private Coroutine customerCo;

    private CustomerAI currentCustomer; // 현재 구매 중인 손님

    public int FriedStored => friedStored;
    public int MoneyStored => moneyStored;

    private void Start()
    {
        // 시작 시 표시 갱신
        RefreshView();
    }

    // 플레이어가 판매대에 치킨을 올릴 수 있는지 확인
    public bool CanPut(PlayerInventory inv)
    {
        if (friedStored >= maxFried)
            return false;

        return inv.Get(ResourceType.FriedChicken) > 0;
    }

    // 판매대에 치킨을 더 올릴 수 있는지 확인
    public bool CanPutFried()
    {
        return friedStored < maxFried;
    }

    // 돈 프리팹이 있는지 확인
    public bool CanTakeMoney()
    {
        return moneyStored > 0;
    }

    // 현재 손님이 더 가져가야 하는지 확인
    private bool NeedMoreForCurrentCustomer()
    {
        return currentCustomerTaken < needFriedForCustomer;
    }

    // 현재 손님이 앞으로 더 가져가야 할 수량 계산
    private int GetRemainForCurrentCustomer()
    {
        return needFriedForCustomer - currentCustomerTaken;
    }

    // 플레이어가 put 트리거에 들어오면 자동 올리기 시작
    public void StartPut()
    {
        PlayerInventory inv = putTrigger.CurrentPlayer;

        if (inv == null)
            return;

        if (putCo != null)
            StopCoroutine(putCo);

        putCo = StartCoroutine(CoPut(inv));
    }

    // 플레이어가 put 트리거에서 나가면 자동 올리기 중지
    public void StopPut()
    {
        if (putCo != null)
            StopCoroutine(putCo);

        putCo = null;
    }

    // 플레이어가 take 트리거에 들어오면 자동 수령 시작
    public void StartTake()
    {
        PlayerInventory inv = takeTrigger.CurrentPlayer;

        if (inv == null)
            return;

        if (takeCo != null)
            StopCoroutine(takeCo);

        takeCo = StartCoroutine(CoTake(inv));
    }

    // 플레이어가 take 트리거에서 나가면 자동 수령 중지
    public void StopTake()
    {
        if (takeCo != null)
            StopCoroutine(takeCo);

        takeCo = null;
    }

    // 플레이어가 튀긴닭 1개 올리기
    public bool PutOne(PlayerInventory inv)
    {
        if (!CanPut(inv))
            return false;

        // 플레이어 인벤토리에서 튀긴닭 1개 사용
        // 여기서 튀긴닭 빠지는 소리가 자동 재생됨
        bool used = inv.Use(ResourceType.FriedChicken, 1);

        if (!used)
            return false;

        // 판매대 치킨 증가
        friedStored += 1;
        RefreshView();
        return true;
    }

    // 외부에서 튀긴닭을 직접 판매대에 올리기
    // 배달 알바가 사용
    public int PutFriedDirect(int amount)
    {
        if (amount <= 0)
            return 0;

        int remainSpace = maxFried - friedStored;

        if (remainSpace <= 0)
            return 0;

        if (amount > remainSpace)
            amount = remainSpace;

        friedStored += amount;
        RefreshView();

        return amount;
    }

    // 플레이어가 돈 프리팹 1개 가져가기
    // 돈 프리팹 1개 = 실제 rewardMoney 만큼의 돈
    public bool TakeMoneyOne(PlayerInventory inv)
    {
        if (!CanTakeMoney())
            return false;

        // 플레이어 인벤토리에 실제 돈 추가
        // 여기서 돈 들어오는 소리가 자동 재생됨
        bool added = inv.Add(ResourceType.Money, rewardMoney);

        if (!added)
            return false;

        // 판매대 돈 프리팹 감소
        moneyStored -= 1;
        RefreshView();
        return true;
    }

    // 맨 앞 손님이 판매대에 도착했을 때 호출
    public void StartCustomerService(CustomerAI customer)
    {
        if (customer == null)
            return;

        // 이미 다른 손님이 이용 중이면 무시
        if (currentCustomer != null)
            return;

        currentCustomer = customer;
        currentCustomerTaken = 0;

        // 손님 머리 위 남은 수량 UI 갱신
        currentCustomer.SetRemainCount(GetRemainForCurrentCustomer());

        RefreshView();

        if (customerCo != null)
            StopCoroutine(customerCo);

        customerCo = StartCoroutine(CoCustomerBuy());
    }

    // 플레이어가 판매대에 치킨을 자동으로 올리는 루프
    private IEnumerator CoPut(PlayerInventory inv)
    {
        while (true)
        {
            if (putTrigger.CurrentPlayer != inv)
                break;

            if (!CanPut(inv))
            {
                yield return null;
                continue;
            }

            PutOne(inv);
            yield return new WaitForSeconds(putDelay);
        }

        putCo = null;
    }

    // 플레이어가 돈 프리팹을 자동으로 가져가는 루프
    private IEnumerator CoTake(PlayerInventory inv)
    {
        while (true)
        {
            if (takeTrigger.CurrentPlayer != inv)
                break;

            if (!CanTakeMoney())
            {
                yield return null;
                continue;
            }

            TakeMoneyOne(inv);
            yield return new WaitForSeconds(takeDelay);
        }

        takeCo = null;
    }

    // 손님 구매 처리
    private IEnumerator CoCustomerBuy()
    {
        while (NeedMoreForCurrentCustomer())
        {
            // 판매대에 치킨이 없으면 기다림
            if (friedStored <= 0)
            {
                yield return null;
                continue;
            }

            // 손님이 치킨 1개 가져감
            friedStored -= 1;
            currentCustomerTaken += 1;

            RefreshView();

            // 남은 수량 UI 갱신
            if (currentCustomer != null)
                currentCustomer.SetRemainCount(GetRemainForCurrentCustomer());

            yield return new WaitForSeconds(customerTakeDelay);
        }

        // 손님 1명 판매 완료 시 돈 프리팹 1개 생성
        if (moneyStored < maxMoneyStack)
            moneyStored += 1;

        RefreshView();

        // 줄에서 바로 제거해서 다음 손님이 앞으로 오게 함
        if (CustomerManager.I != null && currentCustomer != null)
            CustomerManager.I.RemoveFromLine(currentCustomer);

        // 현재 손님 퇴장 처리
        if (currentCustomer != null)
            currentCustomer.OnSaleFinished();

        currentCustomer = null;
        currentCustomerTaken = 0;
        customerCo = null;
    }

    // 판매대 표시 갱신
    private void RefreshView()
    {
        if (stackView != null)
            stackView.SetCount(friedStored, moneyStored);
    }
}