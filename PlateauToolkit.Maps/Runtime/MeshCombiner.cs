using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombiner : MonoBehaviour
{
    public Mesh CombinedMesh { get; private set; }

    public void CombineMeshes()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];

        for (int i = 0; i < meshFilters.Length; i++)
        {
            if (meshFilters[i].sharedMesh == null)
                continue;
            combineInstances[i].mesh = meshFilters[i].sharedMesh;
            combineInstances[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        CombinedMesh = new Mesh();
        CombinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        CombinedMesh.CombineMeshes(combineInstances);

        GetComponent<MeshFilter>().sharedMesh = CombinedMesh;
        GetComponent<MeshRenderer>().sharedMaterials = meshFilters[0].GetComponent<MeshRenderer>().sharedMaterials;
    }
}
