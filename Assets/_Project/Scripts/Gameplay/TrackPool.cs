using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace DallasDash.Gameplay
{
    // Pools belong to one spawner/scene, not a static cache surviving scene loads.
    // Streamed items remain in the live list until culled, even after being collected.
    public sealed class TrackPool : IDisposable
    {
        readonly Dictionary<string, ObjectPool<GameObject>> pools = new();
        readonly Transform parent;
        public int Created { get; private set; }
        public int Reused { get; private set; }
        public TrackPool(Transform parent) => this.parent = parent;

        public GameObject Rent(string key, Func<GameObject> create, Vector3 position, Quaternion rotation)
        {
            if (!pools.TryGetValue(key, out var pool))
            {
                ObjectPool<GameObject> owner = null;
                owner = new ObjectPool<GameObject>(() =>
                {
                    var go = create();
                    go.SetActive(false);
                    var item = go.AddComponent<PooledTrackItem>();
                    item.pool = owner; item.scale = go.transform.localScale;
                    Created++;
                    return go;
                }, null, go =>
                {
                    go.SetActive(false);
                    if (go.TryGetComponent<Topple>(out var topple)) topple.enabled = false;
                    if (go.TryGetComponent<Knockback>(out var knockback)) knockback.enabled = false;
                }, go => UnityEngine.Object.Destroy(go), true, 32, 256);
                pools.Add(key, owner); pool = owner;
            }
            bool reuse = pool.CountInactive > 0;
            var result = pool.Get();
            var marker = result.GetComponent<PooledTrackItem>(); marker.rented = true;
            result.transform.SetParent(parent, false);
            result.transform.localScale = marker.scale;
            result.transform.SetPositionAndRotation(position, rotation);
            if (reuse) Reused++;
            result.SetActive(true);
            return result;
        }
        public void Release(GameObject go)
        {
            if (!go) return;
            if (!go.TryGetComponent<PooledTrackItem>(out var marker)) { UnityEngine.Object.Destroy(go); return; }
            if (!marker.rented) return;
            marker.rented = false; marker.pool.Release(go);
        }
        public void Dispose() { foreach (var pool in pools.Values) pool.Clear(); pools.Clear(); }
    }
    public sealed class PooledTrackItem : MonoBehaviour
    {
        internal ObjectPool<GameObject> pool;
        internal Vector3 scale;
        internal bool rented;
    }
}
