using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 손님 머리 위 주문 UI
// 현재 손님이 앞으로 몇 개의 치킨을 더 사가야 하는지 표시
public class CustomerOrderUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Canvas rootCanvas;      // 월드 캔버스
    [SerializeField] private Image bubbleImage;      // 말풍선 배경
    [SerializeField] private Image chickenIcon;      // 치킨 아이콘
    [SerializeField] private TMP_Text countText;     // 남은 개수 텍스트

    [Header("카메라 바라보기")]
    [SerializeField] private bool lookCamera = true; // 카메라를 계속 바라볼지
    [SerializeField] private Camera cam;

    private void Awake()
    {
        // 카메라가 비어있으면 메인카메라 사용
        if (cam == null)
            cam = Camera.main;

        // 시작할 때 캔버스가 비어있으면 자기 자신/자식에서 찾기
        if (rootCanvas == null)
            rootCanvas = GetComponentInChildren<Canvas>(true);
    }

    private void LateUpdate()
    {
        // 월드 UI가 카메라를 바라보게 처리
        if (!lookCamera)
            return;

        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            return;

        // 카메라와 같은 방향을 보게 맞춤
        // LookRotation보다 이 방식이 UI에서 덜 꼬임
        transform.forward = cam.transform.forward;
    }

    // 주문 UI 표시
    public void Show(int remainCount)
    {
        if (rootCanvas != null)
            rootCanvas.enabled = true;

        if (countText != null)
            countText.text = remainCount.ToString();
    }

    // 주문 UI 숨김
    public void Hide()
    {
        if (rootCanvas != null)
            rootCanvas.enabled = false;
    }
}