using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    // Lightweight vertex posing for the generated parkour mesh; no skeletal rig is required.
    public class DashMeshMotion : MonoBehaviour
    {
        Renderer[] renderers;
        MaterialPropertyBlock properties;
        float phase;
        RiderController rider;
        void Start()
        {
            renderers = GetComponentsInChildren<Renderer>(); properties = new MaterialPropertyBlock();
            rider = GetComponentInParent<RiderController>();
            foreach (var renderer in renderers)
            {
                var filter = renderer.GetComponent<MeshFilter>(); if (!filter || !filter.sharedMesh) continue;
                var b = filter.sharedMesh.bounds;
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_GaitMin", b.min.y); properties.SetFloat("_GaitHeight", b.size.y);
                properties.SetFloat("_GaitCenter", b.center.x); renderer.SetPropertyBlock(properties);
            }
        }
        void Update()
        {
            if (renderers == null) return;
            var gm = GameManager.Instance;
            bool moving = gm && gm.State == RunState.Running && !gm.Paused && PlayerProfile.Character == 1
                && !(DashRun.Instance && DashRun.Instance.Board) && !(rider && rider.Airborne);
            phase += moving ? Time.deltaTime * gm.Speed * 0.65f : 0f;
            foreach (var renderer in renderers)
            {
                if (!renderer) continue;
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_GaitPhase", phase); properties.SetFloat("_GaitAmount", moving ? 1f : 0f);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
