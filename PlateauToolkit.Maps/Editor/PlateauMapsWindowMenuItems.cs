using UnityEditor;

namespace PlateauToolkit.Maps.Editor
{
    static class PlateauMapsWindowMenuItems
    {
        [MenuItem("PLATEAU/PLATEAU Toolkit/Maps Toolkit", priority = 0)]
        static void ShowMapsWindow()
        {
            EditorWindow.GetWindow(typeof(PlateauToolkitMapsWindow), false, "PLATEAU Maps Toolkit");
        }
    }
}
