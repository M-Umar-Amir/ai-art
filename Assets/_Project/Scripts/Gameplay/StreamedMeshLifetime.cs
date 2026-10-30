using System.Collections.Generic;
using UnityEngine;

namespace DallasDash.Gameplay
{
    // Runtime combined meshes are owned by their streamed block and released with it.
    public class StreamedMeshLifetime : MonoBehaviour
    {
        public readonly List<Mesh> meshes = new();
        void OnDestroy() { foreach (var mesh in meshes) if (mesh) Destroy(mesh); }
    }
}
