using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FryerTriggerType
{
    In,
    Out
}

// ∆¢±Ë±‚ ¿‘±∏ / √‚±∏ ∆Æ∏Æ∞≈
public class FryerTrigger : MonoBehaviour
{
    [SerializeField] private Fryer fryer;
    [SerializeField] private FryerTriggerType type;

    private Dictionary<PlayerInventory, Coroutine> runMap = new Dictionary<PlayerInventory, Coroutine>();

    private void Awake()
    {
        if (fryer == null)
            fryer = GetComponentInParent<Fryer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInventory inv = FindInv(other);

        if (inv == null)
            return;

        if (runMap.ContainsKey(inv))
            return;

        Coroutine co = null;

        if (type == FryerTriggerType.In)
            co = StartCoroutine(RunPut(inv));
        else
            co = StartCoroutine(RunTake(inv));

        runMap.Add(inv, co);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInventory inv = FindInv(other);

        if (inv == null)
            return;

        StopRun(inv);
    }

    private IEnumerator RunPut(PlayerInventory inv)
    {
        while (true)
        {
            if (inv == null)
                break;

            yield return fryer.CoPut(inv);
            yield return null;
        }

        if (inv != null)
            runMap.Remove(inv);
    }

    private IEnumerator RunTake(PlayerInventory inv)
    {
        while (true)
        {
            if (inv == null)
                break;

            yield return fryer.CoTake(inv);
            yield return null;
        }

        if (inv != null)
            runMap.Remove(inv);
    }

    private void StopRun(PlayerInventory inv)
    {
        if (!runMap.ContainsKey(inv))
            return;

        Coroutine co = runMap[inv];

        if (co != null)
            StopCoroutine(co);

        runMap.Remove(inv);
    }

    private PlayerInventory FindInv(Collider other)
    {
        PlayerInventory inv = other.GetComponent<PlayerInventory>();

        if (inv == null)
            inv = other.GetComponentInParent<PlayerInventory>();

        if (inv == null)
            inv = other.GetComponentInChildren<PlayerInventory>();

        return inv;
    }
}