using UnityEngine;
using UnityEngine.UI;

// 닭 머리 위 체력바 UI
// - 월드 캔버스 기준으로 사용
// - fillImage의 fillAmount를 이용해서 체력 비율 표시
// - 필요하면 카메라를 바라보게 처리
public class ChickenHpBar : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Image fillImage;

    [Header("카메라 바라보기")]
    [SerializeField] private bool lookCamera = true;
    [SerializeField] private Camera cam;

    private void Awake()
    {
        // 캔버스가 비어있으면 자식에서 자동으로 찾기
        if (rootCanvas == null)
            rootCanvas = GetComponentInChildren<Canvas>(true);

        // 카메라가 비어있으면 메인카메라 사용
        if (cam == null)
            cam = Camera.main;
    }

    private void LateUpdate()
    {
        // 체력바가 카메라를 바라보게 처리
        if (!lookCamera)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        transform.forward = cam.transform.forward;
    }

    // 체력바 갱신
    public void SetHp(int currentHp, int maxHp)
    {
        if (maxHp <= 0)
            maxHp = 1;

        // 0 ~ 1 범위로 보정
        float ratio = (float)currentHp / maxHp;
        ratio = Mathf.Clamp01(ratio);

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        // 죽기 직전 0이면 숨기고 싶으면 여기서 처리 가능
        if (rootCanvas != null)
            rootCanvas.enabled = currentHp > 0;
    }

    // 강제로 보이기
    public void Show()
    {
        if (rootCanvas != null)
            rootCanvas.enabled = true;
    }

    // 강제로 숨기기
    public void Hide()
    {
        if (rootCanvas != null)
            rootCanvas.enabled = false;
    }
}