using System.Collections;
using UnityEngine;

// 자동 튀김기
// 입구에서 생닭 자동 투입
// 5초마다 1개씩 순서대로 튀김
// 출구에서 튀긴닭 수령
public class Fryer : MonoBehaviour
{
    [Header("최대 보유량")]
    [SerializeField] private int maxRawIn = 10;
    [SerializeField] private int maxFriedOut = 10;

    [Header("시간")]
    [SerializeField] private float inputDelay = 0.1f;
    [SerializeField] private float fryTime = 5f;

    [Header("현재 상태")]
    [SerializeField] private int rawIn;
    [SerializeField] private int friedOut;

    [Header("표시")]
    [SerializeField] private FryerStackView stackView;

    private Coroutine cookCo;

    public int RawIn => rawIn;
    public int FriedOut => friedOut;

    private void Awake()
    {
        if (stackView == null)
            stackView = GetComponentInChildren<FryerStackView>();
    }

    private void Start()
    {
        RefreshView();
    }

    public bool CanPut()
    {
        return rawIn < maxRawIn;
    }

    public bool CanTake()
    {
        return friedOut > 0;
    }

    // 입구에서 생닭 1개 넣기
    public bool PutOne(PlayerInventory inv)
    {
        if (inv == null)
            return false;

        if (!CanPut())
            return false;

        if (inv.Get(ResourceType.RawChicken) <= 0)
            return false;

        bool used = inv.Use(ResourceType.RawChicken, 1);

        if (!used)
            return false;

        rawIn += 1;
        RefreshView();

        if (cookCo == null)
            cookCo = StartCoroutine(CoCook());

        return true;
    }

    // 출구에서 튀긴닭 1개 가져가기
    public bool TakeOne(PlayerInventory inv)
    {
        if (inv == null)
            return false;

        if (friedOut <= 0)
            return false;

        if (inv.IsFriedFull())
            return false;

        bool added = inv.Add(ResourceType.FriedChicken, 1);

        if (!added)
            return false;

        friedOut -= 1;
        RefreshView();
        return true;
    }

    // 내부 생닭을 순서대로 1개씩 튀김
    private IEnumerator CoCook()
    {
        while (rawIn > 0)
        {
            yield return new WaitForSeconds(fryTime);

            if (rawIn <= 0)
                continue;

            // 완성품이 꽉 차 있으면 더 못 만듦
            if (friedOut >= maxFriedOut)
                continue;

            rawIn -= 1;
            friedOut += 1;
            RefreshView();
        }

        cookCo = null;
    }

    // 입구 트리거에서 자동 투입
    public IEnumerator CoPut(PlayerInventory inv)
    {
        while (true)
        {
            if (inv == null)
                yield break;

            if (!CanPut())
                yield break;

            if (inv.Get(ResourceType.RawChicken) <= 0)
                yield break;

            bool ok = PutOne(inv);

            if (!ok)
                yield break;

            yield return new WaitForSeconds(inputDelay);
        }
    }

    // 출구 트리거에서 자동 수령
    public IEnumerator CoTake(PlayerInventory inv)
    {
        while (true)
        {
            if (inv == null)
                yield break;

            if (friedOut <= 0)
                yield break;

            if (inv.IsFriedFull())
                yield break;

            bool ok = TakeOne(inv);

            if (!ok)
                yield break;

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void RefreshView()
    {
        if (stackView != null)
            stackView.SetCount(rawIn, friedOut);
    }
}