using System.Collections;
using UnityEngine;

// 닭 이동 AI
// Idle -> 랜덤 방향 회전 -> 앞으로 이동 반복
// 체력 3, 맞으면 감소
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
    [SerializeField] private int maxHp = 3;

    [Header("UI")]
    [SerializeField] private ChickenHpBar hpBar;

    private ChickenManager mgr; // 닭 매니저 참조
    private Coroutine co;       // 이동 코루틴
    private int hp;             // 현재 체력

    private readonly int isWalkHash = Animator.StringToHash("IsWalk");

    // 매니저 연결
    public void SetMgr(ChickenManager m)
    {
        mgr = m;
    }

    // 스폰될 때마다 상태 초기화
    public void InitMove()
    {
        // Animator가 비어 있으면 자식에서 자동으로 찾기
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // 체력바가 비어 있으면 자식에서 자동으로 찾기
        if (hpBar == null)
            hpBar = GetComponentInChildren<ChickenHpBar>(true);

        // 이전 이동 코루틴이 있으면 중지
        if (co != null)
            StopCoroutine(co);

        // 체력 초기화
        hp = maxHp;

        // 체력바 갱신
        RefreshHpBar();

        // 시작은 걷지 않는 상태
        SetWalk(false);

        // 이동 루프 시작
        co = StartCoroutine(CoMove());
    }

    // Idle -> 회전 -> 이동 반복
    private IEnumerator CoMove()
    {
        while (true)
        {
            // 잠시 가만히 있음
            SetWalk(false);

            float idleTime = Random.Range(idleMin, idleMax);
            yield return new WaitForSeconds(idleTime);

            // 랜덤 방향 하나 선택
            float y = Random.Range(0f, 360f);
            Quaternion targetRot = Quaternion.Euler(0f, y, 0f);

            // 목표 방향까지 천천히 회전
            while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    turnSpeed * Time.deltaTime
                );

                yield return null;
            }

            // 앞으로 이동 시작
            SetWalk(true);

            float moveTime = Random.Range(moveMin, moveMax);
            float t = 0f;

            while (t < moveTime)
            {
                t += Time.deltaTime;
                transform.position += transform.forward * moveSpeed * Time.deltaTime;
                yield return null;
            }

            // 이동 끝
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

    // 플레이어 공격을 맞았을 때 호출
    public void Hit(int damage, PlayerInventory attacker)
    {
        hp -= damage;

        // 음수 방지
        if (hp < 0)
            hp = 0;

        // 체력바 갱신
        RefreshHpBar();

        // 체력이 0 이하이면 죽음 처리
        if (hp <= 0)
            Die(attacker);
    }

    // 체력바 갱신
    private void RefreshHpBar()
    {
        if (hpBar == null)
            return;

        hpBar.SetHp(hp, maxHp);
    }

    // 닭 사망 처리
    private void Die(PlayerInventory attacker)
    {
        // 이동 중지
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }

        SetWalk(false);

        // 닭 죽는 소리 재생
        if (AudioManager.I != null)
            AudioManager.I.PlayChickenDead();

        // 공격한 플레이어가 있으면 생닭 1개 지급
        // 이때 생닭 획득 소리는 PlayerInventory.Add 안에서 자동 재생됨
        if (attacker != null)
            attacker.Add(ResourceType.RawChicken, 1);

        // 매니저가 있으면 풀로 반환
        if (mgr != null)
            mgr.Despawn(this);
        else
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        // 비활성화 시 코루틴 정리
        if (co != null)
        {
            StopCoroutine(co);
            co = null;
        }

        SetWalk(false);
    }
}