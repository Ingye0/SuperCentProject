using System.Collections;
using UnityEngine;

// 닭 이동 AI
// Idle -> 랜덤 방향 회전 -> 앞으로 이동 반복
// 애니메이션은 Idle / Walk 두 가지만 사용
public class ChickenAI : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float turnSpeed = 360f;

    [Header("시간")]
    [SerializeField] private float idleMin = 1f;
    [SerializeField] private float idleMax = 2f;
    [SerializeField] private float moveMin = 1.5f;
    [SerializeField] private float moveMax = 3f;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;

    private ChickenManager mgr;
    private Coroutine co;

    // Animator bool 이름
    private readonly int isWalkHash = Animator.StringToHash("IsWalk");

    public void SetMgr(ChickenManager m)
    {
        mgr = m;
    }

    // 스폰될 때마다 행동 다시 시작
    public void InitMove()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (co != null)
            StopCoroutine(co);

        SetWalk(false);
        co = StartCoroutine(CoMove());
    }

    private IEnumerator CoMove()
    {
        while (true)
        {
            // 1. Idle
            SetWalk(false);

            float idleTime = Random.Range(idleMin, idleMax);
            yield return new WaitForSeconds(idleTime);

            // 2. 랜덤 방향으로 회전
            float y = Random.Range(0f, 360f);
            Quaternion targetRot = Quaternion.Euler(0f, y, 0f);

            while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    turnSpeed * Time.deltaTime
                );

                yield return null;
            }

            // 3. 앞으로 이동
            SetWalk(true);

            float moveTime = Random.Range(moveMin, moveMax);
            float t = 0f;

            while (t < moveTime)
            {
                t += Time.deltaTime;
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
                yield return null;
            }

            // 이동 끝나면 다시 Idle로 돌아가게 false
            SetWalk(false);
        }
    }

    // 걷기 애니메이션 on/off
    private void SetWalk(bool value)
    {
        if (animator == null)
            return;

        animator.SetBool(isWalkHash, value);
    }

    // 잡혔을 때 호출
    public void Catch()
    {
        if (co != null)
            StopCoroutine(co);

        SetWalk(false);

        if (mgr != null)
            mgr.Despawn(this);
        else
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }

        SetWalk(false);
    }
}