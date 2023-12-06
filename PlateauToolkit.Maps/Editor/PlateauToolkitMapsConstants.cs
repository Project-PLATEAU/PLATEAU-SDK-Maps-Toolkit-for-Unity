using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlateauToolkit.Maps.Editor
{
    public static class PlateauToolkitMapsConstants
    {
        #region icons, sprites
        const string k_EditorRootFolder = "Packages/com.unity.plateautoolkit.maps/PlateauToolkit.Maps/Editor";
        const string k_RuntimeRootFolder = "Packages/com.unity.plateautoolkit.maps/PlateauToolkit.Maps/Runtime";
        const string k_SpritesFolder = "Packages/com.unity.plateautoolkit.maps/PlateauToolkit.Maps/Editor/Sprites";

        public static readonly string k_MapsToolsIcon = $"{k_SpritesFolder}/PlateauToolkitRendering_Map.png";
        public static readonly string k_IfcToolsIcon = $"{k_SpritesFolder}/PlateauToolkitRendering_IFCLoad.png";
        public static readonly string k_ShpToolsIcon = $"{k_SpritesFolder}/PlateauToolkitRendering_GISLoad.png";

        public static readonly string k_ClockwiseMaterial = $"{k_EditorRootFolder}/Materials/clockwise.mat";
        public static readonly string k_CounterClockwiseMaterial = $"{k_EditorRootFolder}/Materials/counterClockwise.mat";
        public static readonly string k_ClockwiseMaterialHdrp = $"{k_EditorRootFolder}/Materials/clockwiseHDRP.mat";
        public static readonly string k_CounterClockwiseMaterialHdrp = $"{k_EditorRootFolder}/Materials/counterClockwiseHDRP.mat";

        public static readonly string k_ShapeParentPrefab = $"{k_RuntimeRootFolder}/Prefabs/ShapeParent.prefab";
        public static readonly string k_ShapeParentHdrpPrefab = $"{k_RuntimeRootFolder}/Prefabs/ShapeParentHdrp.prefab";
        public static readonly string k_MeshObjectPrefab = $"{k_RuntimeRootFolder}/Prefabs/MeshObject.prefab";

        public static readonly string k_PointMarkerPrefab = $"{k_RuntimeRootFolder}/Prefabs/marker.prefab";
        public static readonly string k_PointMarkerHdrpPrefab = $"{k_RuntimeRootFolder}/Prefabs/markerHDRP.prefab";

        public static float LARGE_BUTTON_WIDTH = 68;
        public static float LARGE_BUTTON_HEIGHT = 64;
        public static float MEDIUM_BUTTON_WIDTH = 53;
        public static float MEDIUM_BUTTON_HEIGHT = 53;
        public static float SMALL_BUTTON_WIDTH = 30;
        public static float SMALL_BUTTON_HEIGHT = 16;

        #endregion

        #region Geographic Coordinate systems
        public static string GEOGCS = "GEOGCS";

        #endregion

        #region Coordinate indexes

        public static string[] k_CoordinateIndex = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        public static List<double> k_JaLatOrigins = new List<double>
    {
        33.00000000,
        33.00000000,
        36.00000000,
        33.00000000,
        36.00000000,
        36.00000000,
        36.00000000,
        36.00000000,
        36.00000000,
        40.00000000,
        44.00000000,
        44.00000000,
        44.00000000,
        26.00000000,
        26.00000000,
        26.00000000,
        26.00000000,
        20.00000000,
        26.00000000,
    };
        public static List<double> k_JaLonOrigins = new List<double>
    {
        129 + 30d / 60d,
        131 + 0d / 60d,
        132 + 10d / 60d,
        133 + 30d / 60d,
        134 + 20d / 60d,
        136 + 0d / 60d,
        137 + 10d / 60d,
        138 + 30d / 60d,
        139 + 50d / 60d,
        140 + 50d / 60d,
        140 + 15d / 60d,
        142 + 15d / 60d,
        144 + 15d / 60d,
        142 + 15d / 60d,
        127 + 30d / 60d,
        124 + 0d / 60d,
        131 + 0d / 60d,
        136 + 0d / 60d,
        154 + 0d / 60d,
    };

        #endregion

        #region API urls
        public static string k_GeoidApiUrl = "http://vldb.gsi.go.jp/sokuchi/surveycalc/geoid/calcgh/cgi/geoidcalc.pl?outputType=json";
        #endregion

        #region Help text

        public static string s_MapsPositioning = "PLATEAU SDKで読み込んだモデルとCesium SDKで読み込んだ 3D Tilesが重なるように位置合せします。\r\nCesium Globe Anchor (Script)を使用してPLATEAUモデルの配置について緯度経度座標（Latitude,Longitude）とUnity座標を連携させます。";
        public static string s_IfcTitle = "IFCファイルを読み込むとProject内のAssets＞MeshesフォルダにglTFファイル（形状）とxmlファイル（属性）として保存されます。glTFはシーンに配置することができます。";
        public static string s_IfcPositioning = "読み込んだIFCモデルとCesium SDKで読み込んだ3D Tilesが重なるように位置合せします。\r\nCesium Globe Anchor (Script) を使用してIFCモデルの配置について緯度経度座標(Latitude,Longitude)とUnity座標を連携させます。";
        public static string s_IfcManualPositioning = "配置する緯度経度高さを指定する場合は、値を入力し指定した位置に配置します。";
        public static string s_IfcAutoPositioning = "IFCモデルに位置情報が保存されている場合は、IFC属性情報から自動配置を実行すると保存された位置に配置されます。";
        public static string s_GisOverview = "GISデータとしてシェープファイル(SHP)またはGeoJSONファイルを読み込むことができます。\r\n緯度経度の座標を持つファイルを描画するために、事前にCesium SDK でCesium Georeferenceオブジェクトを追加しておく必要があります。\r\nファイルタイプがシェープファイルの場合は描画タイプを選択しますがGeoJSONタイプの場合はジオメトリタイプに応じて自動選択されます。";
        public static string s_IfcSettings = "環境設定";

        #endregion

    }

    public static class PlateauMapsPaths
    {
        const string k_SpritesFolder = "Packages/com.unity.plateautoolkit.maps/PlateauToolkit.Maps/Editor/Sprites";
        public static string PlateauLogo { get; } = $"{k_SpritesFolder}/PlateauLogo.png";

    }
}