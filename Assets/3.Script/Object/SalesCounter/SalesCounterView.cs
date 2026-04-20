using System.Collections.Generic;
using UnityEngine;

// 판매대 위 튀긴닭 / 돈 표시
public class SalesCounterView : MonoBehaviour
{
    [Header("기준 위치")]
    [SerializeField] private Transform friedRoot;
    [SerializeField] private Transform moneyRoot;

    [Header("프리팹")]
    [SerializeField] private GameObject friedPrefab;
    [SerializeField] private GameObject moneyPrefab;

    [Header("최대 개수")]
    [SerializeField] private int maxFried = 20;
    [SerializeField] private int maxMoney = 30;

    [Header("수직 간격")]
    [SerializeField] private float friedYStep = 0.12f;
    [SerializeField] private float moneyYStep = 0.05f;

    [Header("시작 위치")]
    [SerializeField] private Vector3 friedStartOffset;
    [SerializeField] private Vector3 moneyStartOffset;

    private List<GameObject> friedObjs = new List<GameObject>();
    private List<GameObject> moneyObjs = new List<GameObject>();

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
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

    public void SetCount(int friedCount, int moneyCount)
    {
        // 음수 방지
        if (friedCount < 0)
            friedCount = 0;

        if (moneyCount < 0)
            moneyCount = 0;

        // 최대 개수 제한
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

    private Vector3 GetFriedPos(int index)
    {
        Vector3 pos = friedStartOffset;
        pos.y += friedYStep * index;
        return pos;
    }

    private Vector3 GetMoneyPos(int index)
    {
        Vector3 pos = moneyStartOffset;
        pos.y += moneyYStep * index;
        return pos;
    }
}