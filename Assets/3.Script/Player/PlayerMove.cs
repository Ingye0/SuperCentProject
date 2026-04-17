using UnityEngine;

// 조이스틱 입력으로 플레이어를 이동시키는 스크립트
public class PlayerMove : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;     // 이동 속도
    [SerializeField] private float rotateSpeed = 10f;  // 회전 속도
    [SerializeField] private float gravity = -20f;     // 중력

    [Header("참조")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private FloatingJoystick joystick;
    [SerializeField] private Transform cameraTransform;

    private Vector3 velocity; // y축 중력용

    private void Awake()
    {
        // CharacterController가 비어있으면 자동으로 가져옴
        if (controller == null)
            controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        if (controller == null || joystick == null || cameraTransform == null)
            return;

        // 조이스틱 입력값
        float x = joystick.Horizontal;
        float z = joystick.Vertical;

        // 카메라 기준 앞/오른쪽 방향
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // y축 제거해서 평면 이동만 하게 함
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        // 카메라 방향 기준 이동 벡터 계산
        Vector3 moveDir = forward * z + right * x;

        // 대각선 이동이 더 빨라지지 않게 보정
        if (moveDir.magnitude > 1f)
            moveDir = moveDir.normalized;

        // 이동
        controller.Move(moveDir * moveSpeed * Time.deltaTime);

        // 움직이고 있으면 이동 방향으로 회전
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
        }

        // 바닥에 있을 때 아래로 살짝 붙게 처리
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        // 중력 적용
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}