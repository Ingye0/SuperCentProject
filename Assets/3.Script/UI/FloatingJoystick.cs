using UnityEngine;
using UnityEngine.EventSystems;

// 화면을 터치한 위치에 조이스틱을 보여주고,
// 드래그 방향을 입력값으로 만들어주는 스크립트
public class FloatingJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("조이스틱 UI")]
    [SerializeField] private RectTransform joystickRoot; // 조이스틱 전체 부모
    [SerializeField] private RectTransform background;   // 조이스틱 배경
    [SerializeField] private RectTransform handle;       // 조이스틱 손잡이
    [SerializeField] private Canvas canvas;              // 같은 캔버스
    [SerializeField] private CanvasGroup canvasGroup;    // 보이기/숨기기용

    [Header("설정")]
    [SerializeField] private float radius = 100f;        // 손잡이가 움직일 수 있는 최대 거리

    private Vector2 input;   // 최종 입력값 (-1 ~ 1)
    private Camera uiCamera;

    // 외부에서 이동 입력을 꺼내 쓸 수 있게 공개
    public float Horizontal => input.x;
    public float Vertical => input.y;

    private void Awake()
    {
        // Canvas가 비어있으면 부모에서 자동으로 찾음
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        // CanvasGroup이 비어있으면 joystickRoot에서 자동으로 찾음
        if (canvasGroup == null && joystickRoot != null)
            canvasGroup = joystickRoot.GetComponent<CanvasGroup>();

        // Overlay가 아닌 Canvas일 때만 카메라 사용
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        // 시작할 때는 조이스틱을 숨겨둠
        HideJoystick();
    }

    // 화면을 처음 터치했을 때 호출
    public void OnPointerDown(PointerEventData eventData)
    {
        ShowJoystick(eventData.position);
        UpdateJoystick(eventData.position);
    }

    // 터치한 상태로 끌 때 호출
    public void OnDrag(PointerEventData eventData)
    {
        UpdateJoystick(eventData.position);
    }

    // 손을 뗐을 때 호출
    public void OnPointerUp(PointerEventData eventData)
    {
        input = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;

        HideJoystick();
    }

    // 터치한 위치에 조이스틱을 표시
    private void ShowJoystick(Vector2 screenPosition)
    {
        if (joystickRoot == null || canvas == null)
            return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        Vector2 localPoint;

        // 화면 좌표를 Canvas 내부 좌표로 변환
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            uiCamera,
            out localPoint))
        {
            joystickRoot.anchoredPosition = localPoint;
        }

        // 조이스틱 표시
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    // 조이스틱 숨김
    private void HideJoystick()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        input = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
    }

    // 현재 터치 위치를 기준으로 입력값 계산
    private void UpdateJoystick(Vector2 screenPosition)
    {
        if (background == null || handle == null)
            return;

        // 배경 중심의 화면 좌표
        Vector2 bgScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, background.position);

        // 중심에서 현재 손가락 위치까지의 차이
        Vector2 delta = screenPosition - bgScreenPos;

        // 반지름 기준으로 -1 ~ 1 입력값 계산
        input = delta / radius;

        // 최대 길이를 1로 제한
        if (input.magnitude > 1f)
            input = input.normalized;

        // 손잡이 이동
        handle.anchoredPosition = input * radius;
    }
}