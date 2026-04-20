using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 손님 풀링 / 줄 서기 / 재사용 관리
// - 손님은 하나의 스폰 위치에서 등장
// - 앞 사람이 있으면 뒤에 줄을 섬
// - 맨 앞 손님만 판매대 앞으로 이동
// - 구매 완료 즉시 줄에서 빠짐
// - 뒤 손님은 바로 앞으로 당겨짐
public class CustomerManager : MonoBehaviour
{
    public static CustomerManager I;

    [Header("손님 프리팹")]
    [SerializeField] private CustomerAI customerPrefab;

    [Header("참조")]
    [SerializeField] private SalesCounter salesCounter;

    [Header("포인트")]
    [SerializeField] private Transform spawnPoint;         // 손님 생성 위치
    [SerializeField] private Transform counterStandPoint;  // 손님이 실제로 서는 위치
    [SerializeField] private Transform counterLookPoint;   // 손님이 바라볼 판매대 위치
    [SerializeField] private Transform exitPoint;          // 손님 퇴장 위치

    [Header("줄 설정")]
    [SerializeField] private int poolCount = 5;            // 최대 손님 수
    [SerializeField] private float lineSpacing = 1.2f;     // 줄 간격
    [SerializeField] private float spawnDelay = 2f;        // 손님 생성 간격

    private List<CustomerAI> pool = new List<CustomerAI>();
    private List<CustomerAI> line = new List<CustomerAI>();

    private bool isRunning;

    private void Awake()
    {
        I = this;
    }

    private void Start()
    {
        // 시작 시 손님 풀 생성
        InitPool();

        // 반복 생성 시작
        StartSpawnLoop();
    }

    // 손님 미리 생성
    private void InitPool()
    {
        if (customerPrefab == null)
        {
            Debug.LogWarning("[CustomerManager] customerPrefab이 비어있음");
            return;
        }

        for (int i = 0; i < poolCount; i++)
        {
            CustomerAI customer = Instantiate(customerPrefab, transform);
            customer.SetMgr(this);
            customer.gameObject.SetActive(false);
            pool.Add(customer);
        }
    }

    // 반복 생성 시작
    public void StartSpawnLoop()
    {
        if (isRunning)
            return;

        isRunning = true;
        StartCoroutine(CoSpawnLoop());
    }

    // 일정 시간마다 손님 생성 시도
    private IEnumerator CoSpawnLoop()
    {
        while (true)
        {
            TrySpawn();
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    // 손님 생성 시도
    private void TrySpawn()
    {
        if (salesCounter == null)
        {
            Debug.LogWarning("[CustomerManager] salesCounter가 비어있음");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("[CustomerManager] spawnPoint가 비어있음");
            return;
        }

        if (counterStandPoint == null)
        {
            Debug.LogWarning("[CustomerManager] counterStandPoint가 비어있음");
            return;
        }

        if (counterLookPoint == null)
        {
            Debug.LogWarning("[CustomerManager] counterLookPoint가 비어있음");
            return;
        }

        if (exitPoint == null)
        {
            Debug.LogWarning("[CustomerManager] exitPoint가 비어있음");
            return;
        }

        // 줄이 가득 찼으면 생성 안 함
        if (line.Count >= poolCount)
            return;

        CustomerAI customer = GetInactive();

        if (customer == null)
            return;

        // 손님 활성화
        customer.Activate(
            this,
            salesCounter,
            spawnPoint,
            counterLookPoint,
            exitPoint
        );

        // 줄에 추가
        line.Add(customer);

        // 전체 줄 재정렬
        RefreshLine();
    }

    // 비활성 손님 찾기
    private CustomerAI GetInactive()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeSelf)
                return pool[i];
        }

        return null;
    }

    // 현재 줄 상태에 맞게 손님 배치
    private void RefreshLine()
    {
        for (int i = 0; i < line.Count; i++)
        {
            CustomerAI customer = line[i];

            if (customer == null)
                continue;

            // 맨 앞 손님만 판매대 앞으로 이동
            if (i == 0)
            {
                customer.MoveToCounter(counterStandPoint.position);
            }
            else
            {
                // 뒤 손님들은 줄 위치로 이동
                Vector3 linePos = GetLinePos(i);
                customer.MoveToPoint(linePos);
            }
        }
    }

    // 줄 위치 계산
    private Vector3 GetLinePos(int index)
    {
        Vector3 pos = counterStandPoint.position;
        pos -= counterStandPoint.forward * lineSpacing * index;
        return pos;
    }

    // 구매 완료 즉시 줄에서 제거
    // 이 시점에 다음 손님이 바로 앞으로 땡겨져 옴
    public void RemoveFromLine(CustomerAI customer)
    {
        if (customer == null)
            return;

        line.Remove(customer);
        RefreshLine();
    }

    // 출구 도착 후 완전히 비활성화만 처리
    public void DespawnOnly(CustomerAI customer)
    {
        if (customer == null)
            return;

        customer.gameObject.SetActive(false);
    }
}