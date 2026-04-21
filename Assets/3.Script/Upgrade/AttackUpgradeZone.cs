using System.Collections;
using TMPro;
using UnityEngine;

// 공격 업그레이드 존
// - 트리거 안에 있는 동안 돈을 자동으로 적립
// - 단계별 가격만큼 누적되면 업그레이드 적용
// - 남은 골드를 숫자로만 표시
// - 전부 업그레이드 완료되면 숫자와 돈 이미지를 숨김
public class AttackUpgradeZone : MonoBehaviour
{
    [Header("트리거")]
    [SerializeField] private PlayerTriggerRelay triggerRelay; // 플레이어 진입/이탈 감지용

    [Header("가격")]
    [SerializeField] private int level1Cost = 30;  // 1단계 총 가격
    [SerializeField] private int level2Cost = 100; // 2단계 총 가격

    [Header("적립 설정")]
    [SerializeField] private int depositPerTick = 10;       // 한 번에 넣을 금액
    [SerializeField] private float depositInterval = 0.1f;  // 돈을 넣는 간격

    [Header("현재 단계")]
    [SerializeField] private int currentLevel; // 0 = 기본, 1 = 1단계 완료, 2 = 2단계 완료

    [Header("누적 금액")]
    [SerializeField] private int level1Paid; // 1단계에 지금까지 넣은 금액
    [SerializeField] private int level2Paid; // 2단계에 지금까지 넣은 금액

    [Header("UI")]
    [SerializeField] private TMP_Text remainGoldText; // 남은 골드 숫자 표시 텍스트
    [SerializeField] private GameObject knifeImage;   // 칼 이미지 오브젝트
    [SerializeField] private GameObject squareImage;   // 네모 이미지 오브젝트


    private Coroutine depositCo; // 적립 반복 코루틴

    public int CurrentLevel => currentLevel;

    private void Awake()
    {
        // 비어있으면 같은 오브젝트에서 자동으로 찾기
        if (triggerRelay == null)
            triggerRelay = GetComponent<PlayerTriggerRelay>();
    }

    private void Start()
    {
        // 시작할 때 UI 먼저 갱신
        RefreshText();
    }

    // 플레이어가 존 안에 들어오면 적립 시작
    public void StartDeposit()
    {
        // 이미 돌고 있던 코루틴이 있으면 중지
        if (depositCo != null)
            StopCoroutine(depositCo);

        depositCo = StartCoroutine(CoDeposit());

        // UI 즉시 갱신
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

    // 일정 간격마다 반복 적립
    private IEnumerator CoDeposit()
    {
        while (true)
        {
            TryDepositOnce();
            yield return new WaitForSeconds(depositInterval);
        }
    }

    // 현재 단계에 맞는 업그레이드 적립 1회 시도
    private void TryDepositOnce()
    {
        if (triggerRelay == null)
            return;

        // 현재 트리거 안에 있는 플레이어 가져오기
        PlayerInventory inv = triggerRelay.CurrentPlayer;

        if (inv == null)
            return;

        // 플레이어 공격 스크립트 찾기
        PlayerAutoAttack attack = inv.GetComponent<PlayerAutoAttack>();

        if (attack == null)
            return;

        // 이미 끝까지 업그레이드했으면 적립 종료
        if (currentLevel >= 2)
        {
            StopDeposit();
            RefreshText();
            return;
        }

        // 현재 단계에 따라 적립 처리
        if (currentLevel == 0)
        {
            DepositToLevel1(inv, attack);
            return;
        }

        if (currentLevel == 1)
        {
            DepositToLevel2(inv, attack);
            return;
        }
    }

    // 1단계 금액 적립
    private void DepositToLevel1(PlayerInventory inv, PlayerAutoAttack attack)
    {
        // 남은 금액 계산
        int remain = level1Cost - level1Paid;

        // 이미 다 찼으면 바로 적용
        if (remain <= 0)
        {
            ApplyLevel1(attack);
            return;
        }

        // 이번 틱에 넣을 금액 결정
        int pay = depositPerTick;

        if (pay > remain)
            pay = remain;

        // 플레이어 돈이 부족하면 이번 틱은 대기
        if (inv.Get(ResourceType.Money) < pay)
        {
            RefreshText();
            return;
        }

        // 플레이어 돈 차감
        bool used = inv.Use(ResourceType.Money, pay);

        if (!used)
        {
            RefreshText();
            return;
        }

        // 누적 금액 증가
        level1Paid += pay;

        // UI 갱신
        RefreshText();

        // 목표 금액 도달 시 업그레이드 적용
        if (level1Paid >= level1Cost)
            ApplyLevel1(attack);
    }

    // 2단계 금액 적립
    private void DepositToLevel2(PlayerInventory inv, PlayerAutoAttack attack)
    {
        // 남은 금액 계산
        int remain = level2Cost - level2Paid;

        // 이미 다 찼으면 바로 적용
        if (remain <= 0)
        {
            ApplyLevel2(attack);
            return;
        }

        // 이번 틱에 넣을 금액 결정
        int pay = depositPerTick;

        if (pay > remain)
            pay = remain;

        // 플레이어 돈이 부족하면 이번 틱은 대기
        if (inv.Get(ResourceType.Money) < pay)
        {
            RefreshText();
            return;
        }

        // 플레이어 돈 차감
        bool used = inv.Use(ResourceType.Money, pay);

        if (!used)
        {
            RefreshText();
            return;
        }

        // 누적 금액 증가
        level2Paid += pay;

        // UI 갱신
        RefreshText();

        // 목표 금액 도달 시 업그레이드 적용
        if (level2Paid >= level2Cost)
            ApplyLevel2(attack);
    }

    // 1단계 업그레이드 적용
    private void ApplyLevel1(PlayerAutoAttack attack)
    {
        attack.UpgradeDamageLevel1();
        currentLevel = 1;

        Debug.Log("[AttackUpgradeZone] 1단계 업그레이드 완료 / 공격력 2");

        // 다음 단계 남은 금액으로 UI 갱신
        RefreshText();
    }

    // 2단계 업그레이드 적용
    private void ApplyLevel2(PlayerAutoAttack attack)
    {
        attack.UpgradeDamageLevel2();
        currentLevel = 2;

        Debug.Log("[AttackUpgradeZone] 2단계 업그레이드 완료 / 공격력 3 + 공격 거리 2배");

        // 더 이상 적립할 필요 없으므로 종료
        StopDeposit();

        // 완료 상태 UI 갱신
        RefreshText();
    }

    // 현재 남은 골드 텍스트와 돈 이미지 표시 갱신
    private void RefreshText()
    {
        // 최종 완료면 텍스트와 돈 이미지 숨김
        if (currentLevel >= 2)
        {
            if (remainGoldText != null)
                remainGoldText.gameObject.SetActive(false);

            if (knifeImage != null)
                knifeImage.SetActive(false);

            if (squareImage != null)
                squareImage.SetActive(false);

            return;
        }

        // 아직 완료가 아니면 둘 다 표시
        if (remainGoldText != null)
            remainGoldText.gameObject.SetActive(true);

        if (knifeImage != null)
            knifeImage.SetActive(true);

        if (squareImage != null)
            squareImage.SetActive(true);

        // 텍스트가 없으면 여기서 종료
        if (remainGoldText == null)
            return;

        // 현재 단계에 맞는 남은 금액 계산
        if (currentLevel == 0)
        {
            int remain = level1Cost - level1Paid;

            if (remain < 0)
                remain = 0;

            // 숫자만 표시
            remainGoldText.text = remain.ToString();
            return;
        }

        if (currentLevel == 1)
        {
            int remain = level2Cost - level2Paid;

            if (remain < 0)
                remain = 0;

            // 숫자만 표시
            remainGoldText.text = remain.ToString();
            return;
        }
    }
}