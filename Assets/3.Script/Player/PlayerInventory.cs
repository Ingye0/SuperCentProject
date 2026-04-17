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

    public int Money => money;
    public int Raw => raw;
    public int Fried => fried;
    public int RawMax => rawMax;
    public int FriedMax => friedMax;

    private void Awake()
    {
        if (stackView == null)
            stackView = GetComponentInChildren<BackStackView>();
    }

    private void Start()
    {
        RefreshView();
    }

    public int Get(ResourceType type)
    {
        if (type == ResourceType.Money)
            return money;

        if (type == ResourceType.RawChicken)
            return raw;

        if (type == ResourceType.FriedChicken)
            return fried;

        return 0;
    }

    public bool Add(ResourceType type, int amount)
    {
        if (amount <= 0)
            return false;

        if (type == ResourceType.Money)
        {
            money += amount;
            RefreshView();
            return true;
        }

        if (type == ResourceType.RawChicken)
        {
            if (raw >= rawMax)
                return false;

            int canAdd = rawMax - raw;
            if (amount > canAdd)
                amount = canAdd;

            raw += amount;
            RefreshView();
            return amount > 0;
        }

        if (type == ResourceType.FriedChicken)
        {
            if (fried >= friedMax)
                return false;

            int canAdd = friedMax - fried;
            if (amount > canAdd)
                amount = canAdd;

            fried += amount;
            RefreshView();
            return amount > 0;
        }

        return false;
    }

    public bool Use(ResourceType type, int amount)
    {
        if (amount <= 0)
            return false;

        if (type == ResourceType.Money)
        {
            if (money < amount)
                return false;

            money -= amount;
            RefreshView();
            return true;
        }

        if (type == ResourceType.RawChicken)
        {
            if (raw < amount)
                return false;

            raw -= amount;
            RefreshView();
            return true;
        }

        if (type == ResourceType.FriedChicken)
        {
            if (fried < amount)
                return false;

            fried -= amount;
            RefreshView();
            return true;
        }

        return false;
    }

    public bool IsRawFull()
    {
        return raw >= rawMax;
    }

    public bool IsFriedFull()
    {
        return fried >= friedMax;
    }

    private void RefreshView()
    {
        if (stackView != null)
            stackView.SetCount(raw, fried);
    }
}