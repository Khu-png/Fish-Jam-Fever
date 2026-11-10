using System.Collections;
using UnityEngine;

public class GameUnit : MonoBehaviour
{
    private Transform tf;
    public Transform TF
    {
        get
        {
            if (tf == null)
            {
                tf = transform;
            }
            return tf;
        }
    }

    public PoolType poolType;

    public void OnDespawn(float delay)
    {
        StartCoroutine(DespawnAfterDelay(delay));
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HBPool.Despawn(this);
    }
}
