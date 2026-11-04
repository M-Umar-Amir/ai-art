using DallasDash.Gameplay;
using UnityEditor;
using UnityEngine;

namespace DallasDash.EditorTools
{
    public static class WorldArtPass
    {
        public static void Prepare()
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!material || !material.HasProperty("_SurfaceStyle")) continue;
                material.SetFloat("_SurfaceStyle",1);
                material.SetFloat("_RimStrength",0);
                material.SetFloat("_OutlineWidth",0);
                material.SetShaderPassEnabled("Outline",false);
                EditorUtility.SetDirty(material); changed++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("WORLD_ART_PASS physical night surfaces=" + changed);
        }
    }
}
