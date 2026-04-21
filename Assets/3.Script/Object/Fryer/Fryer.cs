using System.Collections;
using UnityEngine;

// 유니티 이벤트로 연결해서 쓰는 자동 튀김기
public class Fryer : MonoBehaviour
{
    [Header("트리거")]
    [SerializeField] private PlayerTriggerRelay inTrigger;   // 플레이어 생닭 투입 트리거
    [SerializeField] private PlayerTriggerRelay outTrigger;  // 플레이어 튀긴닭 수령 트리거

    [Header("최대 보유량")]
    [SerializeField] private int maxRawIn = 10;      // 내부 생닭 최대 개수
    [SerializeField] private int maxFriedOut = 10;   // 완성된 튀긴닭 최대 개수

    [Header("시간")]
    [SerializeField] private float putDelay = 0.1f;  // 플레이어가 생닭 넣는 속도
    [SerializeField] private float takeDelay = 0.1f; // 플레이어가 튀긴닭 가져가는 속도
    [SerializeField] private float fryTime = 5f;     // 생닭 1개 튀기는 시간

    [Header("현재 상태")]
    [SerializeField] private int rawIn;      // 현재 내부 생닭 개수
    [SerializeField] private int friedOut;   // 현재 완성된 튀긴닭 개수

    [Header("표시")]
    [SerializeField] private FryerStackView stackView; // 프라이어 위 스택 표시

    private Coroutine cookCo; // 튀김 코루틴
    private Coroutine putCo;  // 플레이어 생닭 투입 코루틴
    private Coroutine takeCo; // 플레이어 튀긴닭 수령 코루틴

    public int RawIn => rawIn;
    public int FriedOut => friedOut;

    private void Start()
    {
        // 시작할 때 표시 갱신
        RefreshView();
    }

    // 내부 생닭 공간이 남아있는지 확인
    public bool CanPut()
    {
        return rawIn < maxRawIn;
    }

    // 완성된 튀긴닭을 꺼낼 수 있는지 확인
    public bool CanTake()
    {
        return friedOut > 0;
    }

    // 플레이어 자동 투입 시작
    public void StartPut()
    {
        PlayerInventory inv = inTrigger.CurrentPlayer;

        if (inv == null)
            return;

        // 기존 코루틴 중지 후 다시 시작
        if (putCo != null)
            StopCoroutine(putCo);

        putCo = StartCoroutine(CoPut(inv));
    }

    // 플레이어 자동 투입 중지
    public void StopPut()
    {
        if (putCo == null)
            return;

        StopCoroutine(putCo);
        putCo = null;
    }

    // 플레이어 자동 수령 시작
    public void StartTake()
    {
        PlayerInventory inv = outTrigger.CurrentPlayer;

        if (inv == null)
            return;

        // 기존 코루틴 중지 후 다시 시작
        if (takeCo != null)
            StopCoroutine(takeCo);

        takeCo = StartCoroutine(CoTake(inv));
    }

    // 플레이어 자동 수령 중지
    public void StopTake()
    {
        if (takeCo == null)
            return;

        StopCoroutine(takeCo);
        takeCo = null;
    }

    // 플레이어가 생닭 1개 넣기
    public bool PutOne(PlayerInventory inv)
    {
        // 공간이 없으면 실패
        if (!CanPut())
            return false;

        // 플레이어 생닭이 없으면 실패
        if (inv.Get(ResourceType.RawChicken) <= 0)
            return false;

        // 플레이어 인벤토리에서 생닭 1개 차감
        bool used = inv.Use(ResourceType.RawChicken, 1);

        if (!used)
            return false;

        // 내부 생닭 1개 증가
        rawIn += 1;
        RefreshView();

        // 튀김 코루틴이 안 돌고 있으면 시작
        if (cookCo == null)
            cookCo = StartCoroutine(CoCook());

        return true;
    }

    // 플레이어가 튀긴닭 1개 가져가기
    public bool TakeOne(PlayerInventory inv)
    {
        // 완성된 닭이 없으면 실패
        if (friedOut <= 0)
            return false;

        // 플레이어 인벤토리가 꽉 찼으면 실패
        if (inv.IsFriedFull())
            return false;

        // 플레이어 인벤토리에 튀긴닭 1개 추가
        bool added = inv.Add(ResourceType.FriedChicken, 1);

        if (!added)
            return false;

        // 프라이어 완성품 1개 감소
        friedOut -= 1;
        RefreshView();
        return true;
    }

    // 외부에서 생닭을 바로 프라이어로 전달
    public bool AddRawDirect(int amount)
    {
        if (amount <= 0)
            return false;

        // 남은 공간 계산
        int canAdd = maxRawIn - rawIn;

        if (canAdd <= 0)
            return false;

        // 남은 공간보다 많으면 잘라서 추가
        if (amount > canAdd)
            amount = canAdd;

        rawIn += amount;
        RefreshView();

        // 튀김 코루틴이 안 돌고 있으면 시작
        if (cookCo == null)
            cookCo = StartCoroutine(CoCook());

        return amount > 0;
    }

    // 외부에서 튀긴닭을 직접 꺼내기
    // amount만큼 꺼내려고 시도하고 실제로 꺼낸 개수를 반환
    public int TakeFriedDirect(int amount)
    {
        // 0 이하이면 종료
        if (amount <= 0)
            return 0;

        // 꺼낼 닭이 없으면 종료
        if (friedOut <= 0)
            return 0;

        // 현재 있는 개수보다 많이 꺼내려 하면 있는 만큼만 꺼냄
        if (amount > friedOut)
            amount = friedOut;

        // 완성된 닭 감소
        friedOut -= amount;
        RefreshView();

        return amount;
    }

    // 내부 생닭이 있으면 계속 튀기기
    private IEnumerator CoCook()
    {
        while (rawIn > 0)
        {
            // 1개 튀기는 시간 대기
            yield return new WaitForSeconds(fryTime);

            // 완성품 공간이 꽉 차 있으면 이번 회차는 넘김
            if (friedOut >= maxFriedOut)
                continue;

            // 생닭 1개 감소, 튀긴닭 1개 증가
            rawIn -= 1;
            friedOut += 1;
            RefreshView();
        }

        // 더 튀길 게 없으면 코루틴 참조 비움
        cookCo = null;
    }

    // 플레이어가 입구 트리거 안에 있는 동안 생닭 자동 투입
    private IEnumerator CoPut(PlayerInventory inv)
    {
        while (true)
        {
            // 다른 플레이어로 바뀌었거나 밖으로 나가면 종료
            if (inTrigger.CurrentPlayer != inv)
                break;

            // 공간이 없거나 생닭이 없으면 잠깐 대기
            if (!CanPut() || inv.Get(ResourceType.RawChicken) <= 0)
            {
                yield return null;
                continue;
            }

            PutOne(inv);
            yield return new WaitForSeconds(putDelay);
        }

        putCo = null;
    }

    // 플레이어가 출구 트리거 안에 있는 동안 튀긴닭 자동 수령
    private IEnumerator CoTake(PlayerInventory inv)
    {
        while (true)
        {
            // 다른 플레이어로 바뀌었거나 밖으로 나가면 종료
            if (outTrigger.CurrentPlayer != inv)
                break;

            // 가져갈 닭이 없으면 대기
            if (friedOut <= 0)
            {
                yield return null;
                continue;
            }

            // 인벤토리가 꽉 찼으면 대기
            if (inv.IsFriedFull())
            {
                yield return null;
                continue;
            }

            TakeOne(inv);
            yield return new WaitForSeconds(takeDelay);
        }

        takeCo = null;
    }

    // 프라이어 표시 갱신
    private void RefreshView()
    {
        if (stackView != null)
            stackView.SetCount(rawIn, friedOut);
    }
}