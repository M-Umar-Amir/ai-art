using DallasDash.Core;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// The earned outfit recolours the crew's jacket. The model carries two jacket materials, so the
    /// swap is instant and does not need a shader mask.
    public class CrewOutfit : MonoBehaviour
    {
        public int character;
        public Material baseJacket;
        public Material altJacket;

        Renderer[] jackets;
        int last = -1;

        void Awake()
        {
            var all = GetComponentsInChildren<Renderer>(true);
            var list = new System.Collections.Generic.List<Renderer>(all.Length);
            foreach (var r in all)
                if (r) foreach (var material in r.sharedMaterials)
                    if (material == baseJacket) { list.Add(r); break; }
            jackets = list.ToArray();
            Apply();
        }

        void Update()
        {
            if (DashInventory.Outfit(character) != last) Apply();
        }

        void Apply()
        {
            last = DashInventory.Outfit(character);
            var m = last == 1 && altJacket ? altJacket : baseJacket;
            if (jackets == null) return;
            foreach (var r in jackets) if (r)
            {
                var slots = r.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) if (slots[i] == baseJacket || slots[i] == altJacket) slots[i] = m;
                r.sharedMaterials = slots;
            }
        }
    }
}
