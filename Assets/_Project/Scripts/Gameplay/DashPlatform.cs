using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    public class DashPlatform : MonoBehaviour
    {
        public Transform platform;
        /// False for the fixed ledges/rooftops of the Skypark parkour route; true for the docking
        /// platforms that rise and fall inside a roof gap.
        public bool moving = true;
        /// Explicit world-space support box. Static route decks are built as one batched mesh per zone,
        /// so a renderer's bounds can no longer be trusted to describe a single platform.
        public bool useBounds;
        public Bounds bounds;
        public bool Safe => platform && platform.position.y >= -0.18f;
        public bool Support(float x, float z, out float height)
        {
            height = 0f;
            if (useBounds)
            {
                if (x < bounds.min.x || x > bounds.max.x || z < bounds.min.z || z > bounds.max.z) return false;
                height = bounds.max.y;
                return true;
            }
            if (!platform || !platform.TryGetComponent<Renderer>(out var renderer)) return false;
            var b = renderer.bounds;
            if (x < b.min.x || x > b.max.x || z < b.min.z || z > b.max.z) return false;
            height = b.max.y;
            return true;
        }
        void Update()
        {
            if (!moving || !platform || !GameManager.Instance || GameManager.Instance.Paused) return;
            // A slow docking cycle is readable from the approach. Jumping always bypasses it.
            float y = -0.42f + Mathf.Sin(GameManager.Instance.RunTime * 1.3f + transform.position.z * 0.01f) * 0.38f;
            var p = platform.localPosition; p.y = y; platform.localPosition = p;
        }
    }
}
