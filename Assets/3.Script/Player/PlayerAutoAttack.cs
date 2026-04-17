using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 범위 안 치킨들 중 가장 가까운 치킨을 자동 공격
// 공격 가능한 대상이 있으면 계속 그 치킨을 바라봄
public class PlayerAutoAttack : MonoBehaviour
{
    [Header("공격")]
    [SerializeField] private float attackDelay = 0.8f;
    [SerializeField] private int damage = 1;

    [Header("바라보기")]
    [SerializeField] private bool lookTarget = true;
    [SerializeField] private float turnSpeed = 720f;

    [Header("참조")]
    [SerializeField] private PlayerInventory inventory;

    private List<ChickenAI> chickens = new List<ChickenAI>();
    private Coroutine attackCo;
    private ChickenAI nowTarget;

    private void Awake()
    {
        if (inventory == null)
            inventory = GetComponent<PlayerInventory>();
    }

    private void OnEnable()
    {
        if (attackCo != null)
            StopCoroutine(attackCo);

        attackCo = StartCoroutine(CoAttack());
    }

    private void Update()
    {
        if (!lookTarget)
            return;

        nowTarget = GetNear();

        if (nowTarget == null)
            return;

        if (!nowTarget.gameObject.activeInHierarchy)
            return;

        Look(nowTarget.transform);
    }

    private void OnDisable()
    {
        if (attackCo != null)
        {
            StopCoroutine(attackCo);
            attackCo = null;
        }

        chickens.Clear();
        nowTarget = null;
    }

    public void AddChicken(ChickenAI chicken)
    {
        if (chicken == null)
            return;

        if (!chickens.Contains(chicken))
            chickens.Add(chicken);
    }

    public void RemoveChicken(ChickenAI chicken)
    {
        if (chicken == null)
            return;

        chickens.Remove(chicken);

        if (nowTarget == chicken)
            nowTarget = null;
    }

    private IEnumerator CoAttack()
    {
        while (true)
        {
            nowTarget = GetNear();

            if (nowTarget != null && nowTarget.gameObject.activeInHierarchy)
            {
                nowTarget.Hit(damage, inventory);
                Clean();

                yield return new WaitForSeconds(attackDelay);
            }
            else
            {
                yield return null;
            }
        }
    }

    private ChickenAI GetNear()
    {
        Clean();

        ChickenAI target = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < chickens.Count; i++)
        {
            ChickenAI c = chickens[i];

            if (c == null)
                continue;

            if (!c.gameObject.activeInHierarchy)
                continue;

            float dist = (c.transform.position - transform.position).sqrMagnitude;

            if (dist < minDist)
            {
                minDist = dist;
                target = c;
            }
        }

        return target;
    }

    private void Look(Transform target)
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRot,
            turnSpeed * Time.deltaTime
        );
    }

    private void Clean()
    {
        for (int i = chickens.Count - 1; i >= 0; i--)
        {
            if (chickens[i] == null || !chickens[i].gameObject.activeInHierarchy)
                chickens.RemoveAt(i);
        }
    }
}