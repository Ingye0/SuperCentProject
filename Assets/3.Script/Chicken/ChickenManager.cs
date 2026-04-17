using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 닭 생성 / 풀링 / 리스폰 관리
public class ChickenManager : MonoBehaviour
{
    public static ChickenManager I;

    [Header("설정")]
    [SerializeField] private ChickenAI prefab;
    [SerializeField] private int maxCount = 10;
    [SerializeField] private float respawnTime = 5f;
    [SerializeField] private Transform[] points;

    // 전체 풀
    private List<ChickenAI> pool = new List<ChickenAI>();

    private void Awake()
    {
        I = this;
    }

    private void Start()
    {
        Init();
    }

    // 처음에 최대 수만큼 미리 생성
    private void Init()
    {
        if (prefab == null)
        {
            Debug.LogWarning("[ChickenManager] prefab이 비어있음");
            return;
        }

        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("[ChickenManager] points가 비어있음");
            return;
        }

        for (int i = 0; i < maxCount; i++)
        {
            ChickenAI c = Instantiate(prefab, transform);
            c.SetMgr(this);
            c.gameObject.SetActive(false);
            pool.Add(c);
        }

        for (int i = 0; i < maxCount; i++)
        {
            Spawn();
        }
    }

    // 비활성 닭 하나 찾아서 활성화
    private void Spawn()
    {
        ChickenAI c = Get();

        if (c == null)
            return;

        Transform p = points[Random.Range(0, points.Length)];

        c.transform.position = p.position;
        c.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        c.gameObject.SetActive(true);
        c.InitMove();
    }

    // 풀에서 비활성 오브젝트 찾기
    private ChickenAI Get()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeSelf)
                return pool[i];
        }

        return null;
    }

    // 닭이 잡혔을 때 호출
    public void Despawn(ChickenAI c)
    {
        c.gameObject.SetActive(false);
        StartCoroutine(CoRespawn());
    }

    // 5초 뒤 다시 스폰
    private IEnumerator CoRespawn()
    {
        yield return new WaitForSeconds(respawnTime);
        Spawn();
    }
}