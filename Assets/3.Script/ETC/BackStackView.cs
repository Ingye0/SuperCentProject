using System.Collections.Generic;
using UnityEngine;

// 플레이어 등 뒤에 생닭 / 튀긴닭 스택 표시
// - 둘 다 있으면 앞 1줄 + 뒤 1줄
// - 하나만 있으면 가운데 1줄
public class BackStackView : MonoBehaviour
{
    [Header("기준 위치")]
    [SerializeField] private Transform root;

    [Header("프리팹")]
    [SerializeField] private GameObject rawPrefab;
    [SerializeField] private GameObject friedPrefab;

    [Header("최대 개수")]
    [SerializeField] private int maxRaw = 10;
    [SerializeField] private int maxFried = 10;

    [Header("세로 간격")]
    [SerializeField] private float yStep = 0.25f;

    [Header("기본 오프셋")]
    [SerializeField] private Vector3 localOffset;

    [Header("2줄일 때 앞뒤 간격")]
    [SerializeField] private float backOffset = 0.2f;

    private List<GameObject> raws = new List<GameObject>();
    private List<GameObject> frieds = new List<GameObject>();

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (root == null)
            root = transform;

        if (rawPrefab == null || friedPrefab == null)
        {
            Debug.LogWarning("[BackStackView] rawPrefab 또는 friedPrefab이 비어있음");
            return;
        }

        for (int i = 0; i < maxRaw; i++)
        {
            GameObject obj = Instantiate(rawPrefab, root);
            obj.transform.localRotation = Quaternion.identity;
            obj.SetActive(false);
            raws.Add(obj);
        }

        for (int i = 0; i < maxFried; i++)
        {
            GameObject obj = Instantiate(friedPrefab, root);
            obj.transform.localRotation = Quaternion.identity;
            obj.SetActive(false);
            frieds.Add(obj);
        }
    }

    public void SetCount(int rawCount, int friedCount)
    {
        if (rawCount < 0)
            rawCount = 0;

        if (friedCount < 0)
            friedCount = 0;

        if (rawCount > raws.Count)
            rawCount = raws.Count;

        if (friedCount > frieds.Count)
            friedCount = frieds.Count;

        bool hasRaw = rawCount > 0;
        bool hasFried = friedCount > 0;
        bool twoLine = hasRaw && hasFried;

        // 생닭 배치
        for (int i = 0; i < raws.Count; i++)
        {
            bool show = i < rawCount;
            raws[i].SetActive(show);

            if (!show)
                continue;

            Vector3 pos = localOffset;

            // 둘 다 있으면 앞줄
            if (twoLine)
                pos.z += 0f;

            pos.y += yStep * i;
            raws[i].transform.localPosition = pos;
        }

        // 튀긴닭 배치
        for (int i = 0; i < frieds.Count; i++)
        {
            bool show = i < friedCount;
            frieds[i].SetActive(show);

            if (!show)
                continue;

            Vector3 pos = localOffset;

            // 둘 다 있으면 뒤줄
            if (twoLine)
                pos.z -= backOffset;

            pos.y += yStep * i;
            frieds[i].transform.localPosition = pos;
        }
    }
}