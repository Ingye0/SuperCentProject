using System.Collections;
using UnityEngine;

// 판매대
public class SalesCounter : MonoBehaviour
{
    [Header("트리거")]
    [SerializeField] private PlayerTriggerRelay putTrigger;
    [SerializeField] private PlayerTriggerRelay takeTrigger;

    [Header("보관 설정")]
    [SerializeField] private int maxFried = 20;

    [Header("손님 판매 설정")]
    [SerializeField] private int needFriedForCustomer = 4;
    [SerializeField] private int rewardMoney = 10;
    [SerializeField] private float customerTakeDelay = 0.5f;

    [Header("플레이어 속도")]
    [SerializeField] private float putDelay = 0.1f;
    [SerializeField] private float takeDelay = 0.1f;

    [Header("현재 상태")]
    [SerializeField] private int friedStored;
    [SerializeField] private int moneyStored;

    [Header("표시")]
    [SerializeField] private SalesCounterView stackView;

    private Coroutine putCo;
    private Coroutine takeCo;
    private Coroutine customerCo;

    public int FriedStored => friedStored;
    public int MoneyStored => moneyStored;

    private void Start()
    {
        RefreshView();
    }

    public bool CanPut(PlayerInventory inv)
    {
        // 판매대가 가득 차 있으면 실패
        if (friedStored >= maxFried)
            return false;

        // 플레이어가 튀긴닭을 가지고 있는지 확인
        return inv.Get(ResourceType.FriedChicken) > 0;
    }

    public bool CanTakeMoney()
    {
        // 쌓인 돈이 있는지 확인
        return moneyStored > 0;
    }

    public bool CanCustomerBuy()
    {
        // 손님이 닭 4개를 가져갈 수 있는지 확인
        return friedStored >= needFriedForCustomer;
    }

    public void StartPut()
    {
        // 현재 넣기 트리거 안의 플레이어 가져오기
        PlayerInventory inv = putTrigger.CurrentPlayer;

        // 플레이어가 없으면 종료
        if (inv == null)
            return;

        // 기존 넣기 코루틴이 있으면 중지
        if (putCo != null)
            StopCoroutine(putCo);

        // 튀긴닭 넣기 시작
        putCo = StartCoroutine(CoPut(inv));
    }

    public void StopPut()
    {
        // 기존 넣기 코루틴이 있으면 중지
        if (putCo != null)
            StopCoroutine(putCo);

        putCo = null;
    }

    public void StartTake()
    {
        // 현재 받기 트리거 안의 플레이어 가져오기
        PlayerInventory inv = takeTrigger.CurrentPlayer;

        // 플레이어가 없으면 종료
        if (inv == null)
            return;

        // 기존 받기 코루틴이 있으면 중지
        if (takeCo != null)
            StopCoroutine(takeCo);

        // 돈 받기 시작
        takeCo = StartCoroutine(CoTake(inv));
    }

    public void StopTake()
    {
        // 기존 받기 코루틴이 있으면 중지
        if (takeCo != null)
            StopCoroutine(takeCo);

        takeCo = null;
    }

    public bool PutOne(PlayerInventory inv)
    {
        // 올릴 수 없으면 실패
        if (!CanPut(inv))
            return false;

        // 플레이어 인벤토리에서 튀긴닭 1개 사용
        bool used = inv.Use(ResourceType.FriedChicken, 1);

        // 사용 실패면 종료
        if (!used)
            return false;

        // 판매대 닭 1개 증가
        friedStored += 1;
        RefreshView();
        return true;
    }

    public bool TakeMoneyOne(PlayerInventory inv)
    {
        // 돈이 없으면 실패
        if (!CanTakeMoney())
            return false;

        // 플레이어 돈 1원 추가
        bool added = inv.Add(ResourceType.Money, 1);

        // 추가 실패면 종료
        if (!added)
            return false;

        // 판매대 돈 1 감소
        moneyStored -= 1;
        RefreshView();
        return true;
    }

    public void StartCustomerBuy()
    {
        // 이미 손님 코루틴이 돌고 있으면 중복 실행 방지
        if (customerCo != null)
            return;

        // 손님 구매 시작
        customerCo = StartCoroutine(CoCustomerBuy());
    }

    private IEnumerator CoPut(PlayerInventory inv)
    {
        while (true)
        {
            // 넣기 트리거에서 나가면 종료
            if (putTrigger.CurrentPlayer != inv)
                break;

            // 넣을 닭이 없으면 대기
            if (!CanPut(inv))
            {
                yield return null;
                continue;
            }

            // 닭 1개 넣기
            PutOne(inv);

            // 다음 입력까지 대기
            yield return new WaitForSeconds(putDelay);
        }

        putCo = null;
    }

    private IEnumerator CoTake(PlayerInventory inv)
    {
        while (true)
        {
            // 받기 트리거에서 나가면 종료
            if (takeTrigger.CurrentPlayer != inv)
                break;

            // 받을 돈이 없으면 대기
            if (!CanTakeMoney())
            {
                yield return null;
                continue;
            }

            // 돈 1원 가져가기
            TakeMoneyOne(inv);

            // 다음 수령까지 대기
            yield return new WaitForSeconds(takeDelay);
        }

        takeCo = null;
    }

    private IEnumerator CoCustomerBuy()
    {
        // 닭 4개가 모일 때까지 대기
        while (!CanCustomerBuy())
            yield return null;

        // 0.5초마다 1개씩 총 4개 가져가기
        for (int i = 0; i < needFriedForCustomer; i++)
        {
            friedStored -= 1;
            RefreshView();

            yield return new WaitForSeconds(customerTakeDelay);
        }

        // 4개 다 가져가면 돈 생성
        moneyStored += rewardMoney;
        RefreshView();

        customerCo = null;
    }

    private void RefreshView()
    {
        // 판매대 표시 갱신
        stackView.SetCount(friedStored, moneyStored);
    }
}