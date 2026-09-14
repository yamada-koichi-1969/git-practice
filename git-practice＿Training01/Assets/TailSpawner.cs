using System.Collections;
using UnityEngine;

public class TailSpawner : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameObject tailPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("生成設定")]
    [Min(0.1f)] [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private Vector2 fieldSize = new Vector2(10f, 10f);
    [SerializeField] private float spawnHeight = 0.5f;

    [Header("重複回避")]
    [Min(0f)] [SerializeField] private float minDistanceBetween = 1.5f;
    [SerializeField] private LayerMask overlapCheckLayers = ~0;
    [Min(1)] [SerializeField] private int maxTries = 20;

    [Header("同時存在数の上限（0で無制限）")]
    [Min(0)] [SerializeField] private int maxActiveTails = 10;

    private int activeTailCount = 0;

    void Start()
    {
        if (tailPrefab == null || spawnPoint == null)
        {
            Debug.LogError("[TailSpawner] tailPrefab または spawnPoint が未設定です。", this);
            enabled = false;
            return;
        }
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            yield return wait;

            if (maxActiveTails > 0 && activeTailCount >= maxActiveTails)
                continue;

            if (TryFindSpawnPosition(out Vector3 pos))
            {
                GameObject go = Instantiate(tailPrefab, pos, Quaternion.identity);
                activeTailCount++;

                var tail = go.GetComponent<Tail>();
                if (tail != null) tail.spawner = this;
            }
        }
    }

    bool TryFindSpawnPosition(out Vector3 result)
    {
        for (int i = 0; i < maxTries; i++)
        {
            Vector3 candidate = spawnPoint.position + new Vector3(
                Random.Range(-fieldSize.x / 2f, fieldSize.x / 2f),
                spawnHeight,
                Random.Range(-fieldSize.y / 2f, fieldSize.y / 2f));

            if (!Physics.CheckSphere(candidate, minDistanceBetween, overlapCheckLayers))
            {
                result = candidate;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

    public void OnTailAttached() => activeTailCount = Mathf.Max(0, activeTailCount - 1);

    void OnDrawGizmosSelected()
    {
        if (spawnPoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            spawnPoint.position + Vector3.up * spawnHeight,
            new Vector3(fieldSize.x, 0.1f, fieldSize.y));
    }
}