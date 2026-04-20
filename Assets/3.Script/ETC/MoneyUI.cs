using TMPro;
using UnityEngine;

// 플레이어 돈 UI 표시
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
        // 이벤트 연결 후 텍스트 갱신
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
        // 기존 이벤트 연결 해제
        Unbind();

        // 새 인벤토리로 교체
        inventory = newInventory;

        // 새 이벤트 연결
        Bind();

        // 텍스트 즉시 갱신
        RefreshText();
    }

    private void Bind()
    {
        // 인벤토리가 없으면 종료
        if (inventory == null)
            return;

        // 중복 연결 방지 후 다시 연결
        inventory.OnInventoryChanged -= RefreshText;
        inventory.OnInventoryChanged += RefreshText;
    }

    private void Unbind()
    {
        // 인벤토리가 없으면 종료
        if (inventory == null)
            return;

        // 이벤트 연결 해제
        inventory.OnInventoryChanged -= RefreshText;
    }

    private void RefreshText()
    {
        // 텍스트가 없으면 종료
        if (moneyText == null)
            return;

        // 인벤토리가 없으면 0 표시
        if (inventory == null)
        {
            moneyText.text = prefix + "0";
            return;
        }

        // 현재 돈 표시
        moneyText.text = prefix + inventory.Money.ToString();
    }
}