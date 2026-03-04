using UnityEditor;
using UnityEditor.PackageManager;

namespace PlateauToolkit.Maps.Editor
{
    /// <summary>
    /// Packagesフォルダに入っている PLATEAU SDK Maps Toolkit for Unity を tarball 形式で出力します。
    /// デプロイで利用します。
    /// </summary>
    internal static class PackagePacker
    {
        [MenuItem("PLATEAU/Debug/Pack PLATEAU Maps Toolkit Package to tarball")]
        public static void Pack()
        {
            string destDir = EditorUtility.SaveFolderPanel("出力先", "", "");
            Client.Pack("Packages/com.unity.plateautoolkit.maps", destDir);
        }
    }
}
