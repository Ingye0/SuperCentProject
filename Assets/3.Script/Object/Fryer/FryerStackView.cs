using System.Collections.Generic;
using UnityEngine;

// 프라이어 위에 생닭 / 튀긴닭 보유량 표시
// 각각 최대 10개, 5개씩 2줄로 배치
public class FryerStackView : MonoBehaviour
{
    [Header("기준 위치")]
    [SerializeField] private Transform rawRoot;
    [SerializeField] private Transform friedRoot;

    [Header("프리팹")]
    [SerializeField] private GameObject rawPrefab;
    [SerializeField] private GameObject friedPrefab;

    [Header("최대 개수")]
    [SerializeField] private int maxRaw = 10;
    [SerializeField] private int maxFried = 10;

    [Header("격자 간격")]
    [SerializeField] private float xStep = 0.2f;
    [SerializeField] private float zStep = 0.2f;

    [Header("로컬 시작 위치")]
    [SerializeField] private Vector3 startOffset;

    private List<GameObject> rawObjs = new List<GameObject>();
    private List<GameObject> friedObjs = new List<GameObject>();

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (rawRoot == null || friedRoot == null)
        {
            Debug.LogWarning("[FryerStackView] rawRoot 또는 friedRoot가 비어있음");
            return;
        }

        if (rawPrefab == null || friedPrefab == null)
        {
            Debug.LogWarning("[FryerStackView] rawPrefab 또는 friedPrefab이 비어있음");
            return;
        }

        for (int i = 0; i < maxRaw; i++)
        {
            GameObject obj = Instantiate(rawPrefab, rawRoot);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localPosition = GetPos(i);
            obj.SetActive(false);
            rawObjs.Add(obj);
        }

        for (int i = 0; i < maxFried; i++)
        {
            GameObject obj = Instantiate(friedPrefab, friedRoot);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localPosition = GetPos(i);
            obj.SetActive(false);
            friedObjs.Add(obj);
        }
    }

    public void SetCount(int rawCount, int friedCount)
    {
        if (rawCount < 0)
            rawCount = 0;

        if (friedCount < 0)
            friedCount = 0;

        if (rawCount > rawObjs.Count)
            rawCount = rawObjs.Count;

        if (friedCount > friedObjs.Count)
            friedCount = friedObjs.Count;

        for (int i = 0; i < rawObjs.Count; i++)
        {
            rawObjs[i].SetActive(i < rawCount);
        }

        for (int i = 0; i < friedObjs.Count; i++)
        {
            friedObjs[i].SetActive(i < friedCount);
        }
    }

    // 5개씩 2줄 배치
    private Vector3 GetPos(int index)
    {
        int col = index % 5;
        int row = index / 5;

        Vector3 pos = startOffset;
        pos.x += xStep * col;
        pos.z += zStep * row;

        return pos;
    }
}