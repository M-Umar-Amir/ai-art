using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    public class DashWardrobe : MonoBehaviour
    {
        public int character;
        Renderer[] renderers;
        MaterialPropertyBlock properties;
        int last = -1;
        void Start() { renderers = GetComponentsInChildren<Renderer>(); properties = new MaterialPropertyBlock(); Apply(); }
        void Update() { if (DashInventory.Outfit(character) != last) Apply(); }
        void Apply()
        {
            last = DashInventory.Outfit(character);
            if (renderers == null) return;
            foreach (var renderer in renderers)
            {
                if (!renderer) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = filter ? filter.sharedMesh : renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : null;
                if (!mesh) continue;
                var b = mesh.bounds;
                renderer.GetPropertyBlock(properties);
                properties.SetFloat("_Outfit", last); properties.SetFloat("_CrewIndex", character);
                properties.SetFloat("_WardrobeMin", b.min.y); properties.SetFloat("_WardrobeHeight", b.size.y);
                renderer.SetPropertyBlock(properties);
            }
        }
    }
}
