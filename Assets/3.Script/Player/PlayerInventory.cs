using System;
using UnityEngine;

// 플레이어 자원 인벤토리
// - 돈 / 생닭 / 튀긴닭을 보관
// - 자원이 실제로 들어오거나 빠질 때 소리 재생
// - 자원 수량이 바뀌면 등 뒤 표시와 UI도 갱신
public class PlayerInventory : MonoBehaviour
{
    [Header("최대 개수")]
    [SerializeField] private int rawMax = 10;   // 플레이어가 들 수 있는 생닭 최대 개수
    [SerializeField] private int friedMax = 10; // 플레이어가 들 수 있는 튀긴닭 최대 개수

    [Header("현재 자원")]
    [SerializeField] private int money; // 현재 돈
    [SerializeField] private int raw;   // 현재 생닭 수
    [SerializeField] private int fried; // 현재 튀긴닭 수

    [Header("표시")]
    [SerializeField] private BackStackView stackView; // 등 뒤 자원 표시용

    // 자원 수량이 바뀌면 외부 UI에 알려주기 위한 이벤트
    public event Action OnInventoryChanged;

    // 외부에서 읽기 전용으로 접근할 수 있게 프로퍼티 제공
    public int Money => money;
    public int Raw => raw;
    public int Fried => fried;
    public int RawMax => rawMax;
    public int FriedMax => friedMax;

    private void Awake()
    {
        // 비어 있으면 자식에서 자동으로 찾기
        if (stackView == null)
            stackView = GetComponentInChildren<BackStackView>();
    }

    private void Start()
    {
        // 시작 시 현재 수량 기준으로 표시 갱신
        RefreshView();
    }

    // 자원 종류에 따라 현재 수량 반환
    public int Get(ResourceType type)
    {
        // 돈 반환
        if (type == ResourceType.Money)
            return money;

        // 생닭 반환
        if (type == ResourceType.RawChicken)
            return raw;

        // 튀긴닭 반환
        if (type == ResourceType.FriedChicken)
            return fried;

        return 0;
    }

    // 자원 추가
    // 실제로 수량이 증가했을 때만 true 반환
    public bool Add(ResourceType type, int amount)
    {
        // 0 이하 값은 무시
        if (amount <= 0)
            return false;

        // 돈은 최대 제한 없이 바로 추가
        if (type == ResourceType.Money)
        {
            money += amount;

            // 표시/UI 갱신
            RefreshView();

            // 실제로 플레이어 인벤토리에 돈이 들어왔으므로 소리 재생
            PlayInventorySound();

            return true;
        }

        // 생닭 추가
        if (type == ResourceType.RawChicken)
        {
            // 이미 가득 찼으면 실패
            if (raw >= rawMax)
                return false;

            // 남은 공간 계산
            int canAdd = rawMax - raw;

            // 남은 공간보다 더 많이 들어오면 잘라서 추가
            if (amount > canAdd)
                amount = canAdd;

            // 실제 수량 증가
            raw += amount;

            // 표시/UI 갱신
            RefreshView();

            // 실제로 1개 이상 들어왔다면 소리 재생
            if (amount > 0)
                PlayInventorySound();

            return amount > 0;
        }

        // 튀긴닭 추가
        if (type == ResourceType.FriedChicken)
        {
            // 이미 가득 찼으면 실패
            if (fried >= friedMax)
                return false;

            // 남은 공간 계산
            int canAdd = friedMax - fried;

            // 남은 공간보다 더 많이 들어오면 잘라서 추가
            if (amount > canAdd)
                amount = canAdd;

            // 실제 수량 증가
            fried += amount;

            // 표시/UI 갱신
            RefreshView();

            // 실제로 1개 이상 들어왔다면 소리 재생
            if (amount > 0)
                PlayInventorySound();

            return amount > 0;
        }

        return false;
    }

    // 자원 사용
    // 실제로 수량이 줄었을 때만 true 반환
    public bool Use(ResourceType type, int amount)
    {
        // 0 이하 값은 무시
        if (amount <= 0)
            return false;

        // 돈 사용
        if (type == ResourceType.Money)
        {
            // 돈이 부족하면 실패
            if (money < amount)
                return false;

            // 실제 수량 감소
            money -= amount;

            // 표시/UI 갱신
            RefreshView();

            // 실제로 플레이어 인벤토리에서 돈이 빠졌으므로 소리 재생
            PlayInventorySound();

            return true;
        }

        // 생닭 사용
        if (type == ResourceType.RawChicken)
        {
            // 생닭이 부족하면 실패
            if (raw < amount)
                return false;

            // 실제 수량 감소
            raw -= amount;

            // 표시/UI 갱신
            RefreshView();

            // 실제로 생닭이 빠졌으므로 소리 재생
            PlayInventorySound();

            return true;
        }

        // 튀긴닭 사용
        if (type == ResourceType.FriedChicken)
        {
            // 튀긴닭이 부족하면 실패
            if (fried < amount)
                return false;

            // 실제 수량 감소
            fried -= amount;

            // 표시/UI 갱신
            RefreshView();

            // 실제로 튀긴닭이 빠졌으므로 소리 재생
            PlayInventorySound();

            return true;
        }

        return false;
    }

    // 생닭이 가득 찼는지 확인
    public bool IsRawFull()
    {
        return raw >= rawMax;
    }

    // 튀긴닭이 가득 찼는지 확인
    public bool IsFriedFull()
    {
        return fried >= friedMax;
    }

    // 플레이어 인벤토리 입출력 공용 소리 재생
    private void PlayInventorySound()
    {
        if (AudioManager.I != null)
            AudioManager.I.PlayPop();
    }

    // 등 뒤 자원 표시 + UI 이벤트 갱신
    private void RefreshView()
    {
        // 등 뒤 자원 표시 갱신
        if (stackView != null)
            stackView.SetCount(raw, fried);

        // 돈 UI 같은 외부 UI 갱신 이벤트 호출
        OnInventoryChanged?.Invoke();
    }
}