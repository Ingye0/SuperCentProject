using System.Collections.Generic;
using UnityEngine;

// 판매대 위 튀긴닭 / 돈 프리팹 표시
// - 튀긴닭은 위로 스택처럼 쌓아서 표시
// - 돈도 위로 스택처럼 쌓아서 표시
// - moneyCount는 실제 원 수가 아니라 돈 프리팹 개수
public class SalesCounterView : MonoBehaviour
{
    [Header("기준 위치")]
    [SerializeField] private Transform friedRoot;
    [SerializeField] private Transform moneyRoot;

    [Header("프리팹")]
    [SerializeField] private GameObject friedPrefab;
    [SerializeField] private GameObject moneyPrefab;

    [Header("최대 개수")]
    [SerializeField] private int maxFried = 10;   // 튀긴닭 최대 표시 개수
    [SerializeField] private int maxMoney = 30;   // 돈 프리팹 최대 표시 개수

    [Header("수직 간격")]
    [SerializeField] private float friedYStep = 0.12f; // 튀긴닭 위로 쌓이는 간격
    [SerializeField] private float moneyYStep = 0.05f; // 돈 위로 쌓이는 간격

    [Header("시작 위치")]
    [SerializeField] private Vector3 friedStartOffset; // 튀긴닭 시작 위치
    [SerializeField] private Vector3 moneyStartOffset; // 돈 시작 위치

    private List<GameObject> friedObjs = new List<GameObject>();
    private List<GameObject> moneyObjs = new List<GameObject>();

    private void Awake()
    {
        Init();
    }

    // 표시용 오브젝트를 미리 생성
    private void Init()
    {
        // 기준 위치가 없으면 종료
        if (friedRoot == null || moneyRoot == null)
        {
            Debug.LogWarning("[SalesCounterView] friedRoot 또는 moneyRoot가 비어있음");
            return;
        }

        // 프리팹이 없으면 종료
        if (friedPrefab == null || moneyPrefab == null)
        {
            Debug.LogWarning("[SalesCounterView] friedPrefab 또는 moneyPrefab이 비어있음");
            return;
        }

        // 튀긴닭 표시용 오브젝트 미리 생성
        for (int i = 0; i < maxFried; i++)
        {
            GameObject obj = Instantiate(friedPrefab, friedRoot);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localPosition = GetFriedPos(i);
            obj.SetActive(false);
            friedObjs.Add(obj);
        }

        // 돈 표시용 오브젝트 미리 생성
        for (int i = 0; i < maxMoney; i++)
        {
            GameObject obj = Instantiate(moneyPrefab, moneyRoot);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localPosition = GetMoneyPos(i);
            obj.SetActive(false);
            moneyObjs.Add(obj);
        }
    }

    // 현재 개수에 맞게 표시 on/off
    public void SetCount(int friedCount, int moneyCount)
    {
        // 음수 방지
        if (friedCount < 0)
            friedCount = 0;

        if (moneyCount < 0)
            moneyCount = 0;

        // 최대 표시 개수 제한
        if (friedCount > friedObjs.Count)
            friedCount = friedObjs.Count;

        if (moneyCount > moneyObjs.Count)
            moneyCount = moneyObjs.Count;

        // 튀긴닭 표시 on/off
        for (int i = 0; i < friedObjs.Count; i++)
        {
            friedObjs[i].SetActive(i < friedCount);
        }

        // 돈 표시 on/off
        for (int i = 0; i < moneyObjs.Count; i++)
        {
            moneyObjs[i].SetActive(i < moneyCount);
        }
    }

    // 튀긴닭 스택 위치 계산
    // index가 늘어날수록 위로 한 칸씩 쌓임
    private Vector3 GetFriedPos(int index)
    {
        Vector3 pos = friedStartOffset;
        pos.y += friedYStep * index;
        return pos;
    }

    // 돈 스택 위치 계산
    // index가 늘어날수록 위로 한 칸씩 쌓임
    private Vector3 GetMoneyPos(int index)
    {
        Vector3 pos = moneyStartOffset;
        pos.y += moneyYStep * index;
        return pos;
    }
}