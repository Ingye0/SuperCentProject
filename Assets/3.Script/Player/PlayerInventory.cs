using System;
using UnityEngine;

// 플레이어 자원 인벤토리
public class PlayerInventory : MonoBehaviour
{
    [Header("최대 개수")]
    [SerializeField] private int rawMax = 10;
    [SerializeField] private int friedMax = 10;

    [Header("현재 자원")]
    [SerializeField] private int money;
    [SerializeField] private int raw;
    [SerializeField] private int fried;

    [Header("표시")]
    [SerializeField] private BackStackView stackView;

    // 자원이 바뀌면 UI에 알림
    public event Action OnInventoryChanged;

    public int Money => money;
    public int Raw => raw;
    public int Fried => fried;
    public int RawMax => rawMax;
    public int FriedMax => friedMax;

    private void Awake()
    {
        // 비어있으면 자식에서 자동으로 찾기
        if (stackView == null)
            stackView = GetComponentInChildren<BackStackView>();
    }

    private void Start()
    {
        // 시작할 때 표시 갱신
        RefreshView();
    }

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

    public bool Add(ResourceType type, int amount)
    {
        // 0 이하면 실패
        if (amount <= 0)
            return false;

        // 돈은 바로 추가
        if (type == ResourceType.Money)
        {
            money += amount;
            RefreshView();
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

            // 공간보다 많으면 잘라서 추가
            if (amount > canAdd)
                amount = canAdd;

            // 생닭 수 증가
            raw += amount;
            RefreshView();
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

            // 공간보다 많으면 잘라서 추가
            if (amount > canAdd)
                amount = canAdd;

            // 튀긴닭 수 증가
            fried += amount;
            RefreshView();
            return amount > 0;
        }

        return false;
    }

    public bool Use(ResourceType type, int amount)
    {
        // 0 이하면 실패
        if (amount <= 0)
            return false;

        // 돈 사용
        if (type == ResourceType.Money)
        {
            // 부족하면 실패
            if (money < amount)
                return false;

            // 돈 차감
            money -= amount;
            RefreshView();
            return true;
        }

        // 생닭 사용
        if (type == ResourceType.RawChicken)
        {
            // 부족하면 실패
            if (raw < amount)
                return false;

            // 생닭 차감
            raw -= amount;
            RefreshView();
            return true;
        }

        // 튀긴닭 사용
        if (type == ResourceType.FriedChicken)
        {
            // 부족하면 실패
            if (fried < amount)
                return false;

            // 튀긴닭 차감
            fried -= amount;
            RefreshView();
            return true;
        }

        return false;
    }

    public bool IsRawFull()
    {
        // 생닭이 가득 찼는지 확인
        return raw >= rawMax;
    }

    public bool IsFriedFull()
    {
        // 튀긴닭이 가득 찼는지 확인
        return fried >= friedMax;
    }

    private void RefreshView()
    {
        // 등 뒤 자원 표시 갱신
        if (stackView != null)
            stackView.SetCount(raw, fried);

        // UI 갱신 이벤트 호출
        OnInventoryChanged?.Invoke();
    }
}