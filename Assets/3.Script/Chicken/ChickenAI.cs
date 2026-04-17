using System.Collections;
using UnityEngine;

// 닭 이동 AI
// Idle -> 랜덤 방향 회전 -> 앞으로 이동 반복
// 체력 2, 두 번 맞으면 죽음
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

    [Header("체력")]
    [SerializeField] private int maxHp = 2;

    private ChickenManager mgr;
    private Coroutine co;
    private int hp;

    private readonly int isWalkHash = Animator.StringToHash("IsWalk");

    public void SetMgr(ChickenManager m)
    {
        mgr = m;
    }

    // 스폰될 때마다 초기화
    public void InitMove()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (co != null)
            StopCoroutine(co);

        hp = maxHp;
        SetWalk(false);
        co = StartCoroutine(CoMove());
    }

    private IEnumerator CoMove()
    {
        while (true)
        {
            // Idle
            SetWalk(false);

            float idleTime = Random.Range(idleMin, idleMax);
            yield return new WaitForSeconds(idleTime);

            // 랜덤 회전
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

            // 이동
            SetWalk(true);

            float moveTime = Random.Range(moveMin, moveMax);
            float t = 0f;

            while (t < moveTime)
            {
                t += Time.deltaTime;
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
                yield return null;
            }

            SetWalk(false);
        }
    }

    private void SetWalk(bool value)
    {
        if (animator == null)
            return;

        animator.SetBool(isWalkHash, value);
    }

    // 플레이어 공격 맞았을 때
    public void Hit(int damage)
    {
        hp -= damage;

        if (hp <= 0)
            Die();
    }

    private void Die()
    {
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }

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