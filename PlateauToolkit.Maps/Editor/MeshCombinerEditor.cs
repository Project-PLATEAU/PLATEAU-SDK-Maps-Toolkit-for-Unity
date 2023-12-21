#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshCombiner))]
public class MeshCombinerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MeshCombiner meshCombiner = (MeshCombiner)target;
        if (GUILayout.Button("Combine and Save Mesh"))
        {
            meshCombiner.CombineMeshes();
            SaveMeshAsset(meshCombiner.CombinedMesh);
        }
    }

    void SaveMeshAsset(Mesh mesh)
    {
        string assetName = "CombinedMesh";
        string assetPath = "Assets/" + assetName + ".asset";
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
