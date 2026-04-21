using System.Collections;
using UnityEngine;

// 유니티 이벤트로 연결해서 쓰는 자동 튀김기
// - 플레이어가 생닭을 넣을 수 있음
// - 플레이어가 튀긴닭을 가져갈 수 있음
// - 생닭이 있으면 내부에서 자동으로 튀김 진행
// - 실제로 튀기는 중일 때만 지글지글 루프 소리 재생
public class Fryer : MonoBehaviour
{
    [Header("트리거")]
    [SerializeField] private PlayerTriggerRelay inTrigger;   // 생닭 넣는 쪽 트리거
    [SerializeField] private PlayerTriggerRelay outTrigger;  // 튀긴닭 받는 쪽 트리거

    [Header("최대 보유량")]
    [SerializeField] private int maxRawIn = 10;      // 내부 생닭 최대 개수
    [SerializeField] private int maxFriedOut = 10;   // 완성된 튀긴닭 최대 개수

    [Header("시간")]
    [SerializeField] private float putDelay = 0.1f;  // 자동으로 넣는 속도
    [SerializeField] private float takeDelay = 0.1f; // 자동으로 가져가는 속도
    [SerializeField] private float fryTime = 5f;     // 생닭 1개 튀기는 시간

    [Header("현재 상태")]
    [SerializeField] private int rawIn;      // 현재 내부 생닭 수
    [SerializeField] private int friedOut;   // 현재 완성된 튀긴닭 수

    [Header("표시")]
    [SerializeField] private FryerStackView stackView; // 프라이어 위 표시용

    private Coroutine cookCo; // 내부 튀김 코루틴
    private Coroutine putCo;  // 플레이어 생닭 투입 코루틴
    private Coroutine takeCo; // 플레이어 튀긴닭 수령 코루틴

    public int RawIn => rawIn;
    public int FriedOut => friedOut;

    private void Start()
    {
        // 시작 시 표시 갱신
        RefreshView();

        // 시작 시 루프 소리 상태도 맞춰줌
        RefreshFryLoopSound();
    }

    // 생닭을 더 넣을 수 있는지 확인
    public bool CanPut()
    {
        return rawIn < maxRawIn;
    }

    // 완성된 튀긴닭을 꺼낼 수 있는지 확인
    public bool CanTake()
    {
        return friedOut > 0;
    }

    // 플레이어가 입구 트리거에 들어오면 자동 투입 시작
    public void StartPut()
    {
        PlayerInventory inv = inTrigger.CurrentPlayer;

        if (inv == null)
            return;

        if (putCo != null)
            StopCoroutine(putCo);

        putCo = StartCoroutine(CoPut(inv));
    }

    // 플레이어가 입구 트리거에서 나가면 자동 투입 중지
    public void StopPut()
    {
        if (putCo == null)
            return;

        StopCoroutine(putCo);
        putCo = null;
    }

    // 플레이어가 출구 트리거에 들어오면 자동 수령 시작
    public void StartTake()
    {
        PlayerInventory inv = outTrigger.CurrentPlayer;

        if (inv == null)
            return;

        if (takeCo != null)
            StopCoroutine(takeCo);

        takeCo = StartCoroutine(CoTake(inv));
    }

    // 플레이어가 출구 트리거에서 나가면 자동 수령 중지
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

        // 플레이어가 생닭을 안 들고 있으면 실패
        if (inv.Get(ResourceType.RawChicken) <= 0)
            return false;

        // 플레이어 인벤토리에서 생닭 1개 차감
        // 여기서 생닭 빠지는 소리는 PlayerInventory에서 자동 재생
        bool used = inv.Use(ResourceType.RawChicken, 1);

        if (!used)
            return false;

        // 프라이어 내부 생닭 증가
        rawIn += 1;
        RefreshView();

        // 아직 튀김 코루틴이 안 돌고 있으면 시작
        if (cookCo == null)
            cookCo = StartCoroutine(CoCook());

        // 상태가 바뀌었으니 루프 소리 갱신
        RefreshFryLoopSound();

        return true;
    }

    // 플레이어가 튀긴닭 1개 가져가기
    public bool TakeOne(PlayerInventory inv)
    {
        // 완성된 닭이 없으면 실패
        if (friedOut <= 0)
            return false;

        // 플레이어 튀긴닭 칸이 꽉 찼으면 실패
        if (inv.IsFriedFull())
            return false;

        // 플레이어 인벤토리에 튀긴닭 1개 추가
        // 여기서 튀긴닭 들어오는 소리는 PlayerInventory에서 자동 재생
        bool added = inv.Add(ResourceType.FriedChicken, 1);

        if (!added)
            return false;

        // 프라이어 완성품 감소
        friedOut -= 1;
        RefreshView();

        // 상태가 바뀌었으니 루프 소리 갱신
        RefreshFryLoopSound();

        return true;
    }

    // 외부에서 생닭을 바로 프라이어에 넣기
    // 사냥 알바가 사용할 수 있음
    public bool AddRawDirect(int amount)
    {
        if (amount <= 0)
            return false;

        int canAdd = maxRawIn - rawIn;

        if (canAdd <= 0)
            return false;

        if (amount > canAdd)
            amount = canAdd;

        rawIn += amount;
        RefreshView();

        if (cookCo == null)
            cookCo = StartCoroutine(CoCook());

        // 상태가 바뀌었으니 루프 소리 갱신
        RefreshFryLoopSound();

        return amount > 0;
    }

    // 외부에서 튀긴닭을 직접 꺼내기
    // 배달 알바가 사용할 수 있음
    public int TakeFriedDirect(int amount)
    {
        if (amount <= 0)
            return 0;

        if (friedOut <= 0)
            return 0;

        if (amount > friedOut)
            amount = friedOut;

        friedOut -= amount;
        RefreshView();

        // 상태가 바뀌었으니 루프 소리 갱신
        RefreshFryLoopSound();

        return amount;
    }

    // 내부 생닭이 있으면 계속 튀기기
    private IEnumerator CoCook()
    {
        // 코루틴이 실제 시작되면 루프 소리 상태 갱신
        RefreshFryLoopSound();

        while (rawIn > 0)
        {
            // 생닭 1개 튀기는 시간 대기
            yield return new WaitForSeconds(fryTime);

            // 완성품 칸이 꽉 찼으면 이번 회차는 넘김
            if (friedOut >= maxFriedOut)
            {
                RefreshFryLoopSound();
                continue;
            }

            // 생닭 1개 감소 / 튀긴닭 1개 증가
            rawIn -= 1;
            friedOut += 1;
            RefreshView();

            // 한 번 처리 후 상태 다시 확인
            RefreshFryLoopSound();
        }

        // 더 튀길 게 없으면 코루틴 참조 비움
        cookCo = null;

        // 종료 시 루프 소리 정지
        RefreshFryLoopSound();
    }

    // 플레이어가 입구 트리거 안에 있는 동안 생닭 자동 투입
    private IEnumerator CoPut(PlayerInventory inv)
    {
        while (true)
        {
            // 다른 플레이어로 바뀌었거나 나갔으면 종료
            if (inTrigger.CurrentPlayer != inv)
                break;

            // 넣을 수 없거나 생닭이 없으면 잠시 대기
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
            // 다른 플레이어로 바뀌었거나 나갔으면 종료
            if (outTrigger.CurrentPlayer != inv)
                break;

            // 가져갈 닭이 없으면 대기
            if (friedOut <= 0)
            {
                yield return null;
                continue;
            }

            // 플레이어 인벤토리가 꽉 찼으면 대기
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

    // 현재 프라이어가 실제로 튀기고 있는지 검사해서
    // 지글지글 루프 소리를 켜거나 끔
    private void RefreshFryLoopSound()
    {
        if (AudioManager.I == null)
            return;

        // 실제 튀김 중인 상태
        // - 코루틴이 돌고 있고
        // - 아직 생닭이 남아 있어야 함
        bool isCooking = cookCo != null && rawIn > 0;

        if (isCooking)
            AudioManager.I.StartFryerLoop();
        else
            AudioManager.I.StopFryerLoop();
    }

    // 프라이어 위 표시 갱신
    private void RefreshView()
    {
        if (stackView != null)
            stackView.SetCount(rawIn, friedOut);
    }

    private void OnDisable()
    {
        // 프라이어가 꺼질 때 남아 있는 코루틴 정리
        if (putCo != null)
        {
            StopCoroutine(putCo);
            putCo = null;
        }

        if (takeCo != null)
        {
            StopCoroutine(takeCo);
            takeCo = null;
        }

        if (cookCo != null)
        {
            StopCoroutine(cookCo);
            cookCo = null;
        }

        // 프라이어가 비활성화되면 루프 소리도 같이 끔
        if (AudioManager.I != null)
            AudioManager.I.StopFryerLoop();
    }
}