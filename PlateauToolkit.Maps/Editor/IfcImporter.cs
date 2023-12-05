#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlateauToolkit.Maps.Editor
{
    class IfcImporter : AssetPostprocessor
    {
        public void OnPreprocessModel()
        {
            string assetPath = assetImporter.assetPath;

            if (Path.GetExtension(assetPath) == ".ifc")
            {
                Debug.Log($"IFC file imported at {assetPath}");
            }
        }
    }
}
#endif