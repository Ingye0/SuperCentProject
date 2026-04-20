using TMPro;
using UnityEngine;

// 플레이어 현재 돈 UI 표시
public class MoneyUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private TMP_Text moneyText;

    [Header("표시 형식")]
    [SerializeField] private string prefix = "Money : ";

    private void Awake()
    {
        // 비어있으면 같은 오브젝트에서 TMP_Text 찾기
        if (moneyText == null)
            moneyText = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        // 이벤트 연결 후 즉시 갱신
        Bind();
        RefreshText();
    }

    private void OnDisable()
    {
        // 비활성화 시 이벤트 해제
        Unbind();
    }

    public void SetInventory(PlayerInventory newInventory)
    {
        // 기존 연결 해제
        Unbind();

        // 새 인벤토리로 교체
        inventory = newInventory;

        // 새 연결
        Bind();

        // 즉시 갱신
        RefreshText();
    }

    private void Bind()
    {
        if (inventory == null)
            return;

        inventory.OnInventoryChanged -= RefreshText;
        inventory.OnInventoryChanged += RefreshText;
    }

    private void Unbind()
    {
        if (inventory == null)
            return;

        inventory.OnInventoryChanged -= RefreshText;
    }

    private void RefreshText()
    {
        if (moneyText == null)
            return;

        if (inventory == null)
        {
            moneyText.text = prefix + "0";
            return;
        }

        moneyText.text = prefix + inventory.Money.ToString();
    }
}