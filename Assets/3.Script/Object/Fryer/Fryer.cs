using System.Collections;
using UnityEngine;

// 유니티 이벤트로 연결해서 쓰는 자동 튀김기
public class Fryer : MonoBehaviour
{
    [Header("트리거")]
    [SerializeField] private PlayerTriggerRelay inTrigger;
    [SerializeField] private PlayerTriggerRelay outTrigger;

    [Header("최대 보유량")]
    [SerializeField] private int maxRawIn = 10;
    [SerializeField] private int maxFriedOut = 10;

    [Header("시간")]
    [SerializeField] private float putDelay = 0.1f;
    [SerializeField] private float takeDelay = 0.1f;
    [SerializeField] private float fryTime = 5f;

    [Header("현재 상태")]
    [SerializeField] private int rawIn;
    [SerializeField] private int friedOut;

    [Header("표시")]
    [SerializeField] private FryerStackView stackView;

    private Coroutine cookCo;
    private Coroutine putCo;
    private Coroutine takeCo;

    public int RawIn => rawIn;
    public int FriedOut => friedOut;

    private void Start()
    {
        // 시작할 때 표시 갱신
        RefreshView();
    }

    public bool CanPut()
    {
        // 내부 생닭 공간이 남아있는지 확인
        return rawIn < maxRawIn;
    }

    public bool CanTake()
    {
        // 꺼낼 튀긴닭이 있는지 확인
        return friedOut > 0;
    }

    public void StartPut()
    {
        // 현재 입구 트리거 안의 플레이어 가져오기
        PlayerInventory inv = inTrigger.CurrentPlayer;

        // 플레이어가 없으면 종료
        if (inv == null)
            return;

        // 기존 투입 코루틴이 있으면 중지
        if (putCo != null)
            StopCoroutine(putCo);

        // 생닭 자동 투입 시작
        putCo = StartCoroutine(CoPut(inv));
    }

    public void StopPut()
    {
        // 돌고 있는 투입 코루틴이 없으면 종료
        if (putCo == null)
            return;

        // 생닭 자동 투입 중지
        StopCoroutine(putCo);
        putCo = null;
    }

    public void StartTake()
    {
        // 현재 출구 트리거 안의 플레이어 가져오기
        PlayerInventory inv = outTrigger.CurrentPlayer;

        // 플레이어가 없으면 종료
        if (inv == null)
            return;

        // 기존 수령 코루틴이 있으면 중지
        if (takeCo != null)
            StopCoroutine(takeCo);

        // 튀긴닭 자동 수령 시작
        takeCo = StartCoroutine(CoTake(inv));
    }

    public void StopTake()
    {
        // 돌고 있는 수령 코루틴이 없으면 종료
        if (takeCo == null)
            return;

        // 튀긴닭 자동 수령 중지
        StopCoroutine(takeCo);
        takeCo = null;
    }

    public bool PutOne(PlayerInventory inv)
    {
        // 더 넣을 수 없으면 실패
        if (!CanPut())
            return false;

        // 생닭이 없으면 실패
        if (inv.Get(ResourceType.RawChicken) <= 0)
            return false;

        // 플레이어 인벤토리에서 생닭 1개 사용
        bool used = inv.Use(ResourceType.RawChicken, 1);

        // 사용 실패면 종료
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

    public bool TakeOne(PlayerInventory inv)
    {
        // 완성된 튀긴닭이 없으면 실패
        if (friedOut <= 0)
            return false;

        // 플레이어 튀긴닭 공간이 꽉 찼으면 실패
        if (inv.IsFriedFull())
            return false;

        // 플레이어 인벤토리에 튀긴닭 1개 추가
        bool added = inv.Add(ResourceType.FriedChicken, 1);

        // 추가 실패면 종료
        if (!added)
            return false;

        // 완성된 튀긴닭 1개 감소
        friedOut -= 1;
        RefreshView();
        return true;
    }

    private IEnumerator CoCook()
    {
        while (rawIn > 0)
        {
            // 1개 튀기는 시간 대기
            yield return new WaitForSeconds(fryTime);

            // 완성품 공간이 꽉 차 있으면 이번 회차 넘김
            if (friedOut >= maxFriedOut)
                continue;

            // 생닭 1개를 튀긴닭 1개로 변경
            rawIn -= 1;
            friedOut += 1;
            RefreshView();
        }

        // 더 튀길 게 없으면 코루틴 참조 비우기
        cookCo = null;
    }

    private IEnumerator CoPut(PlayerInventory inv)
    {
        while (true)
        {
            // 트리거에서 나가면 종료
            if (inTrigger.CurrentPlayer != inv)
                break;

            // 더 넣을 수 없거나 생닭이 없으면 잠깐 대기
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

    private IEnumerator CoTake(PlayerInventory inv)
    {
        while (true)
        {
            // 트리거에서 나가면 종료
            if (outTrigger.CurrentPlayer != inv)
                break;

            // 가져갈 닭이 없으면 기다리기
            if (friedOut <= 0)
            {
                yield return null;
                continue;
            }

            // 인벤토리가 꽉 찼으면 기다리기
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

    private void RefreshView()
    {
        // 튀김기 표시 갱신
        stackView.SetCount(rawIn, friedOut);
    }
}