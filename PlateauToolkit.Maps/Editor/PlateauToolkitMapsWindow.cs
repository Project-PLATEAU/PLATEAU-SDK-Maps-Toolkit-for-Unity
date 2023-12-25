using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
#if CESIUM_FOR_UNITY
using CesiumForUnity;
#endif
using PLATEAU.CityInfo;
using System;
using System.IO;
using System.Collections;
using UnityEngine.Networking;
using System.Net;
using System.IO.Compression;
using System.Linq;
#if UNITY_EDITOR && !UNITY_EDITOR_WIN
    using System.Diagnostics;
#endif

namespace PlateauToolkit.Maps.Editor
{
    public enum PlateauMapsPositioningMode
    {
        PlateauModel,
        IfcModel
    }

    class PlateauToolkitMapsWindow : EditorWindow
    {
        public PlateauToolkitMapsWindow m_Window;
        const int k_TabButtonSize = 54;
        PlateauMapsPositioningMode m_PositionMode;

        // Plateau city model map placement
        List<PLATEAUInstancedCityModel> m_CityModelGroups = new();
        PLATEAUInstancedCityModel m_CityModel;
        double2 m_CityModelPosition;

        // IFC variables
        IfcFileLoader m_IfcFileLoader;
        TextAsset m_IfcMetaXml;
        GameObject m_IfcGameObject;
        double3 m_IfcWgsPosition;
#if CESIUM_FOR_UNITY
        CesiumGlobeAnchor m_IfcGlobeAnchor;
#endif
        IfcCoordinate m_IfcCoordinateInfo;
        float m_LongitudeInput;
        float m_LatitudeInput;
        float m_HeightInput;
        float m_RotationInput;
        float m_ScaleInput;
        string m_IfcFileName;

        // Shapefile/Geojson variables
#if CESIUM_FOR_UNITY
        ShapefileRenderManager m_ShapefileRenderManager;
#endif
        bool m_MergeMesh;
        float m_LineRendererWidth;
        float m_GisRenderHeight;
        SupportedEncoding m_SupportedEncoding;
        static readonly string[] k_SupportedEncodings = Enum.GetNames(typeof(SupportedEncoding));
        GameObject m_PointDataPrefab;

        string m_GisTargetFolderName;
        int m_SelectedShpRenderingIndex;
        int m_GisModeIndex;
        static readonly string[] k_ShpRenderingModes = { "Mesh", "Line" };
        static readonly string[] k_GisMode = { "SHP", "GeoJson" };

        Vector2 m_ScrollPosition;
        string m_SystemPathToProject;

        enum Tab
        {
            MapTools,
            IfcTools,
            GisTools
        }

        Tab m_CurrentTab;

        void OnEnable()
        {
            m_CityModelGroups.Clear();
            m_ScaleInput = 1f;
            m_SystemPathToProject = Directory.GetCurrentDirectory().ToString().Replace("\\", "/") + "/Assets/IfcConvert";
            if (m_IfcFileLoader == null)
            {
                m_IfcFileLoader = new IfcFileLoader();
            }
#if UNITY_EDITOR_WIN
            m_IfcFileLoader.SetIfcConvertPath(m_SystemPathToProject + "/IfcConvert.exe");
#else
            if (CheckMacChipType.GetMacChipType().Contains("M1"))
            {
                m_IfcFileLoader.SetIfcConvertPath(m_SystemPathToProject + "/IfcConvert-macos-64-M1");
            }
            else
            {
                m_IfcFileLoader.SetIfcConvertPath(m_SystemPathToProject + "/IfcConvert-macos-64");
            }
#endif
        }

        void HeaderLogo(float windowWidth)
        {
            EditorGUILayout.Space(6f);

            var logoTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(
                    PlateauMapsPaths.PlateauLogo, typeof(Texture2D));
            float width = Mathf.Min(windowWidth - 20, 260f);
            float height = (float)logoTexture.height / logoTexture.width * width;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                Rect logoRect = EditorGUILayout.GetControlRect(GUILayout.Width(width), GUILayout.Height(height));
                GUI.DrawTexture(logoRect, logoTexture);
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(10f);
        }

        void OnGUI()
        {
            #region Header
            m_Window ??= GetWindow<PlateauToolkitMapsWindow>();
            HeaderLogo(m_Window.position.width);
            if (m_IfcFileLoader == null)
            {
                m_IfcFileLoader = new IfcFileLoader();

            }
            #endregion

            bool TabButton(string iconPath, Tab tab)
            {
                UnityEngine.Color? buttonColor = tab == m_CurrentTab ? UnityEngine.Color.cyan : null;
                var imageButtonGUILayout = new PlateauMapsImageButtonGUI(k_TabButtonSize, k_TabButtonSize);
                if (imageButtonGUILayout.Button(iconPath, buttonColor))
                {
                    m_CurrentTab = tab;
                    return true;
                }

                return false;
            }

            PlateauMapsEditorGuiLayout.GridLayout(
               m_Window.position.width,
               k_TabButtonSize,
               k_TabButtonSize,
               new Action[]
               {
                    () => TabButton(PlateauToolkitMapsConstants.k_MapsToolsIcon, Tab.MapTools),
                    () => TabButton(PlateauToolkitMapsConstants.k_IfcToolsIcon, Tab.IfcTools),
                    () => TabButton(PlateauToolkitMapsConstants.k_ShpToolsIcon, Tab.GisTools)
               });

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            switch (m_CurrentTab)
            {
                case Tab.MapTools:
                    m_PositionMode = PlateauMapsPositioningMode.PlateauModel;
                    PlateauMapsEditorGuiLayout.Header("PLATEAUモデル位置合わせ");
#if CESIUM_FOR_UNITY
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(PlateauToolkitMapsConstants.s_MapsPositioning, MessageType.Info);

                    EditorGUILayout.Space(10f);

                    m_CityModel = (PLATEAUInstancedCityModel)EditorGUILayout.ObjectField("PLATEAUモデル:", m_CityModel, typeof(PLATEAUInstancedCityModel), true);

                    EditorGUILayout.Space(10f);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.BeginVertical();

                    if (GUILayout.Button("PLATEAUモデルの位置を合せる"))
                    {
                        if (m_CityModel != null
                            && m_CityModel.transform.parent != null
                            && m_CityModel.transform.GetComponent<CesiumGlobeAnchor>() != null
                            && m_CityModel.transform.parent.GetComponent<CesiumGeoreference>() != null)
                        {
                            m_CityModelPosition = PlateauMapsUtilities.GetOriginLongLatOfCityModel(m_CityModel);
                            string geoidRequestUri = PlateauToolkitMapsConstants.k_GeoidApiUrl + "&latitude=" + m_CityModelPosition.x + "&longitude=" + m_CityModelPosition.y;
                            PlateauMapsUtilities.RequestGeoidHeight(geoidRequestUri, MoveModel);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog(
                                "中心合わせが失敗しました",
                                "シーンにPlateauモデルが存在し、CesiumGlobeAnchorコンポネントが負荷されることかつCesiumGeoreferenceにParentされていることを確認してください。",
                                "OK"
                                );
                        }
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();
#else
                EditorGUILayout.HelpBox("この機能を利用するにはCesium For Unityが必要です", MessageType.Info);
#endif
                    break;
                case Tab.IfcTools:
                    m_PositionMode = PlateauMapsPositioningMode.IfcModel;
                    PlateauMapsEditorGuiLayout.Header("IFCモデルの読み込み");
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(PlateauToolkitMapsConstants.s_IfcTitle, MessageType.Info);
                    EditorGUILayout.Space();

                    if (GUILayout.Button("ローカルディスクからIFCファイルを読み込み"))
                    {
                        string ifcFilePath = EditorUtility.OpenFilePanel("Select IFC file", "", "ifc");
                        m_IfcFileName = Path.GetFileNameWithoutExtension(ifcFilePath);
                        if (!string.IsNullOrEmpty(ifcFilePath))
                        {
                            if (Path.GetExtension(ifcFilePath) != ".ifc")
                            {
                                EditorUtility.DisplayDialog(
                                "IFCファイルの読み込み失敗",
                                "選択されているファイルはIFCファイルであることを再度確認してください。",
                                "OK"
                                );
                            }
                            else
                            {
                                bool ifcLoadSuccess = m_IfcFileLoader.LoadIfcFile(ifcFilePath);
                                if (!ifcLoadSuccess)
                                {
                                    int option = EditorUtility.DisplayDialogComplex(
                                       "確認",
                                       "この機能を使用するにはIFCインポーターが必要になります。\r\nIFCインポーターを使用するに当たり、GPL3.0ライセンスへの承諾が必要になります。ライセンス内容を確認し、承諾後にダウンロードして、再度実行してください。",
                                       "承諾",
                                       "キャンセル",
                                       "ライセンス確認"
                                    );

                                    switch (option)
                                    {
                                        case 0:
                                            DownloadIfcExe();
                                            break;
                                        case 1:
                                            break;
                                        case 2:
                                            Application.OpenURL("https://github.com/opensource-jp/licenses/blob/main/GPL-3.0/GPL-3.0.md");
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    m_IfcGameObject = (GameObject)EditorGUILayout.ObjectField("IFC モデル：", m_IfcGameObject, typeof(GameObject), true);
                    EditorGUILayout.Space(10f);
                    m_IfcMetaXml = (TextAsset)EditorGUILayout.ObjectField("IFC属性情報：", m_IfcMetaXml, typeof(TextAsset), true);

                    if (GUILayout.Button("属性情報を付与"))
                    {
                        if (m_IfcGameObject != null && m_IfcMetaXml != null && m_IfcMetaXml.name == m_IfcFileName)
                        {
                            bool selection = EditorUtility.DisplayDialog(
                                       "属性情報を付与",
                                       "モデルサイズによっては時間がかかる場合があります。",
                                       "OK", "Cancel"
                                       );
                            if (selection)
                            {
                                DynamicXml.AttachMetadata(m_IfcMetaXml, m_IfcGameObject);
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog(
                                "IFC付属情報ファイルの読み込み失敗",
                                "選択されているIFCモデル、XMLファイルが正しいか再度確認してください。",
                                "OK"
                                );
                        }
                    }

                    PlateauMapsEditorGuiLayout.Header("IFCモデルをCesium 3D Tilesetsと位置合せ");
#if CESIUM_FOR_UNITY
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(PlateauToolkitMapsConstants.s_IfcPositioning, MessageType.Info);
                    EditorGUILayout.Space();

                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(PlateauToolkitMapsConstants.s_IfcManualPositioning, MessageType.Info);
                    EditorGUILayout.Space();

                    // Draw Text Fields
                    m_LatitudeInput = EditorGUILayout.FloatField("緯度", m_LatitudeInput);
                    m_LongitudeInput = EditorGUILayout.FloatField("経度", m_LongitudeInput);
                    m_HeightInput = EditorGUILayout.FloatField("標高", m_HeightInput);
                    m_RotationInput = EditorGUILayout.FloatField("回転角度", m_RotationInput);
                    m_ScaleInput = EditorGUILayout.FloatField("縮尺", m_ScaleInput);

                    if (GUILayout.Button("指定した位置に配置"))
                    {
                        if(m_IfcGameObject == null)
                        {
                            EditorUtility.DisplayDialog(
                                        "位置合わせ失敗",
                                        "IFCモデルが選択されていません。",
                                        "OK"
                                        );
                        }
                        else
                        {
                            m_IfcGlobeAnchor = m_IfcGameObject.GetComponent<CesiumGlobeAnchor>();
                            if (m_IfcGlobeAnchor == null || m_IfcGlobeAnchor.transform.parent == null || m_IfcGlobeAnchor.transform.parent.GetComponent<CesiumGeoreference>() == null)
                            {
                                EditorUtility.DisplayDialog(
                                            "位置合わせ失敗",
                                            "位置合わせを行うにはCesium Georeference とCesium Globe Anchor の設定が必要。",
                                            "OK"
                                            );
                            }
                            else
                            {
                                if (m_LongitudeInput < -180d || m_LongitudeInput > 180d || m_LatitudeInput < -90d || m_LatitudeInput > 90d)
                                {
                                    EditorUtility.DisplayDialog(
                                           "位置合わせ失敗",
                                           "緯度は‐90度から90度まで、経度は‐180度から180度までです。もう一度入力を確認してください。",
                                           "OK"
                                           );
                                }
                                else
                                {
                                    double3 ifcWgsPosition = new double3(m_LongitudeInput, m_LatitudeInput, m_HeightInput);
                                    m_IfcGlobeAnchor.transform.parent.GetComponent<CesiumGeoreference>().SetOriginLongitudeLatitudeHeight(m_LongitudeInput, m_LatitudeInput, m_HeightInput);

                                    m_IfcGlobeAnchor.longitudeLatitudeHeight = ifcWgsPosition;
                                    Quaternion rotation = Quaternion.Euler(0, m_RotationInput, 0);
                                    m_IfcGlobeAnchor.transform.rotation = rotation;
                                    Vector3 scale = new Vector3(m_ScaleInput, m_ScaleInput, m_ScaleInput);
                                    m_IfcGlobeAnchor.transform.localScale = scale;
                                }
                            }
                        }
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(PlateauToolkitMapsConstants.s_IfcAutoPositioning, MessageType.Info);
                    EditorGUILayout.Space();

                    if (GUILayout.Button("IFC属性情報から自動配置"))
                    {
                        if (m_IfcGameObject == null || m_IfcMetaXml == null)
                        {
                            EditorUtility.DisplayDialog(
                                        "位置合わせ失敗",
                                        "IFCモデルまたはXML ファイルが選択されていません。",
                                        "OK"
                                        );
                        }
                        else
                        {

                            m_IfcGlobeAnchor = m_IfcGameObject.GetComponent<CesiumGlobeAnchor>();
                            if (m_IfcGlobeAnchor == null || m_IfcGlobeAnchor.transform.parent == null || m_IfcGlobeAnchor.transform.parent.GetComponent<CesiumGeoreference>() == null)
                            {
                                EditorUtility.DisplayDialog(
                                            "位置合わせ失敗",
                                            "位置合わせを行うにはCesium Georeference とCesium Globe Anchor の設定が必要。",
                                            "OK"
                                            );
                            }
                            else
                            {
                                m_IfcCoordinateInfo = DynamicXml.GetCoordinateInformation(m_IfcMetaXml);

                                // if these values are -1 then the information either did not exist or the given xml file is not valid xml
                                if (m_IfcCoordinateInfo.Height == -1d || m_IfcCoordinateInfo.Eastings == -1d || m_IfcCoordinateInfo.Northings == -1d)
                                {
                                    EditorUtility.DisplayDialog(
                                            "位置合わせ失敗",
                                            "XMLファイルが有効でない可能性があります。",
                                            "OK"
                                            );
                                }
                                else
                                {
                                    if (m_IfcCoordinateInfo.Jgd2011Id == -1)
                                    {
                                        EditorUtility.DisplayDialog(
                                           "位置合わせ失敗",
                                           "EPSG情報がありません。",
                                           "OK"
                                           );
                                    }
                                    else
                                    {
                                        double3 inputPos = new double3(m_IfcCoordinateInfo.Northings, m_IfcCoordinateInfo.Eastings, m_IfcCoordinateInfo.Height);
                                        m_IfcWgsPosition = GeoCoordinateUtility.ConvertJGDToWGS(inputPos, m_IfcCoordinateInfo.Jgd2011Id - 1);

                                        if (m_IfcWgsPosition[0] < -180d || m_IfcWgsPosition[0] > 180d || m_IfcWgsPosition[1] < -90d || m_IfcWgsPosition[1] > 90d)
                                        {
                                            EditorUtility.DisplayDialog(
                                                   "位置合わせ失敗",
                                                   "緯度は‐90度から90度まで、経度は‐180度から180度までです。もう一度入力を確認してください。",
                                                   "OK"
                                                   );
                                        }
                                        else
                                        {
                                            string geoidRequestUri = PlateauToolkitMapsConstants.k_GeoidApiUrl + "&latitude=" + m_IfcWgsPosition.y + "&longitude=" + m_IfcWgsPosition.x;

                                            PlateauMapsUtilities.RequestGeoidHeight(geoidRequestUri, MoveModel);
                                        }
                                    }
                                }
                            }
                        }
                    }
#else
                    EditorGUILayout.HelpBox("この機能を利用するにはCesium For Unityが必要です", MessageType.Info);
#endif
                    EditorGUILayout.Space();
                    PlateauMapsEditorGuiLayout.Header(PlateauToolkitMapsConstants.s_IfcSettings);
                    EditorGUILayout.Space();

                    if (GUILayout.Button("IFCローダーのファイル指定"))
                    {
                        string selectedPath = EditorUtility.OpenFilePanel("Select IfcConvert", "", "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            m_IfcFileLoader.SetIfcConvertPath(selectedPath);
                        }
                    }
                    EditorGUILayout.TextField("IFC ローダーパス", m_IfcFileLoader.GetIfcExePath());

                    EditorGUILayout.Space();
                    if (GUILayout.Button("IFCローダーの出力フォルダ設定"))
                    {
                        string selectedPath = EditorUtility.OpenFolderPanel("Select IFC output folder", "", "");
                        if (!string.IsNullOrEmpty(selectedPath))
                        {
                            m_IfcFileLoader.SetOutputPath(selectedPath);
                            UnityEngine.Debug.Log("出力フォルダ " + selectedPath);
                        }
                    }
                    EditorGUILayout.TextField("IFC アウトプットパス", m_IfcFileLoader.GetOutputPath());

                    EditorGUILayout.Space();
                    if (GUILayout.Button("IFCローダパスと出力フォルダの自動設定"))
                    {
                        m_IfcFileLoader.SetPathsAutomatically();
                    }
                        break;
                case Tab.GisTools:
                    PlateauMapsEditorGuiLayout.Header("GISデータ読み込み");
#if CESIUM_FOR_UNITY
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(PlateauToolkitMapsConstants.s_GisOverview, MessageType.Info);
                    EditorGUILayout.Space();

                    m_GisModeIndex = EditorGUILayout.Popup("GIS type", m_GisModeIndex, k_GisMode);

                    EditorGUILayout.BeginHorizontal();
                    m_PointDataPrefab = (GameObject)EditorGUILayout.ObjectField("ポイントデータのマーカー", m_PointDataPrefab, typeof(GameObject), true);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    m_GisTargetFolderName = EditorGUILayout.TextField("フォルダパス", m_GisTargetFolderName);
                    if (GUILayout.Button("参照"))
                    {
                        // When the button is pressed, open the folder selection dialog
                        string path = EditorUtility.OpenFolderPanel("フォルダ選択", "", "");

                        // If the user selects a valid folder and hits the 'Select Folder' button in the dialog
                        if (!string.IsNullOrEmpty(path))
                        {
                            m_GisTargetFolderName = path;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();

                    m_SupportedEncoding = (SupportedEncoding)EditorGUILayout.Popup("DBFのエンコーディング", (int)m_SupportedEncoding, k_SupportedEncodings);

                    EditorGUILayout.EndHorizontal ();

                    m_GisRenderHeight = EditorGUILayout.FloatField("配置する高さ", m_GisRenderHeight);

                    if (m_GisModeIndex == 0) // SHP
                    {
                        m_SelectedShpRenderingIndex = EditorGUILayout.Popup("SHPのレンダー方法", m_SelectedShpRenderingIndex, k_ShpRenderingModes);
                        if (m_SelectedShpRenderingIndex == 0) //rendering mode is mesh
                        {
                            m_MergeMesh = EditorGUILayout.Toggle("メッシュを結合する", m_MergeMesh);
                        }
                    }
                    else // GeoJSON
                    {
                        EditorGUILayout.LabelField("描画タイプ", "ジオメトリから判断する");
                    }
                    if (m_SelectedShpRenderingIndex == 1)
                    {
                        m_LineRendererWidth = EditorGUILayout.FloatField("GISの線幅", m_LineRendererWidth);
                    }
                    if (GUILayout.Button("GISデータの読み込み"))
                    {
                        if (m_GisModeIndex == 0)
                        {
                            using (m_ShapefileRenderManager = new ShapefileRenderManager(m_GisTargetFolderName, m_SelectedShpRenderingIndex, m_GisRenderHeight, m_MergeMesh, m_SupportedEncoding, m_PointDataPrefab))
                            {
                                if (m_ShapefileRenderManager.Read(m_LineRendererWidth))
                                {
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog(
                                        "SHP読み込み失敗",
                                        "フォルダに有効なSHPとDBFファイルが含まれていることを再確認してください。また、PlateauToolkit.Maps/Editor/MaterialsフォルダにMaterialが入っていることも確認してください。",
                                        "OK"
                                        );
                                }
                            }
                        }
                        else
                        {
                            using (GeoJsonLoader geoJsonLoader = new GeoJsonLoader(m_PointDataPrefab))
                            {
                                if (geoJsonLoader.ReadAll(m_GisTargetFolderName, m_GisRenderHeight, m_LineRendererWidth))
                                {

                                }
                                else
                                {
                                    EditorUtility.DisplayDialog(
                                       "GeoJSON読み込み失敗",
                                       "フォルダに有効なGeoJSONファイルが含まれていることを再確認してください。また、PlateauToolkit.Maps/Editor/MaterialsフォルダにMaterialが入っていることも確認してください。",
                                       "OK"
                                       );
                                }
                            }
                        }
                    }
#else
                    EditorGUILayout.HelpBox("この機能を利用するにはCesium For Unityが必要です", MessageType.Info);
#endif
                    break;
            }
            EditorGUILayout.EndScrollView();
        }

        void DownloadIfcExe()
        {
            string downloadUrl;
            string savePath;
#if UNITY_EDITOR_WIN
            downloadUrl = "https://s3.amazonaws.com/ifcopenshell-builds/IfcConvert-v0.7.0-f0e03c7-win64.zip";
            savePath = "Assets/IfcConvert/IfcConvert-v0.7.0-f0e03c7-win64.zip";
#else
            if (CheckMacChipType.GetMacChipType().Contains("M1"))
            {
                downloadUrl = "https://s3.amazonaws.com/ifcopenshell-builds/IfcConvert-v0.7.0-f0e03c7-macosm164.zip";
                savePath = "Assets/IfcConvert/IfcConvert-v0.7.0-f0e03c7-macosm164.zip";
            }
            else
            {
                downloadUrl = "https://s3.amazonaws.com/ifcopenshell-builds/IfcConvert-v0.7.0-f0e03c7-macos64.zip";
                savePath = "Assets/IfcConvert/IfcConvert-v0.7.0-f0e03c7-macos64.zip";
            }
#endif

            FileDownloader.DownloadFile(downloadUrl, savePath, OnDownloadComplete);
        }

        private void OnDownloadComplete(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                UnityEngine.Debug.Log("Download completed: " + path);
            }
            Unzip(path);
        }

#if UNITY_EDITOR && !UNITY_EDITOR_WIN
        public void ExtractZipFile(string zipFilePath, string extractPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"unzip -o \\\"{zipFilePath}\\\" -d \\\"{extractPath}\\\"\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process proc = new Process { StartInfo = startInfo })
            {
                proc.Start();
                proc.WaitForExit();
            }
        }
#endif

        public void Unzip(string zipFilePath)
        {
            string extractPath = Path.GetDirectoryName(zipFilePath);

#if UNITY_EDITOR && UNITY_EDITOR_WIN
            if (!File.Exists(zipFilePath))
            {
                Debug.LogError("Zip file not found: " + zipFilePath);
                return;
            }
            try
            {
                ZipFile.ExtractToDirectory(zipFilePath, extractPath);
                Debug.Log("Unzip successful");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error unzipping file: " + e.Message);
            }
#elif UNITY_EDITOR && !UNITY_EDITOR_WIN
            ExtractZipFile(zipFilePath, extractPath);
#endif
            m_IfcFileLoader.SetPathsAutomatically();
        }

#if CESIUM_FOR_UNITY
        void MoveModel(float resultOfGeoidHeightQuery)
        {
            if (m_PositionMode == PlateauMapsPositioningMode.IfcModel && m_IfcGlobeAnchor == null)
            {
                return;
            }

            if (m_PositionMode == PlateauMapsPositioningMode.PlateauModel && (m_CityModel == null || m_CityModel.GetComponent<CesiumGlobeAnchor>() == null))
            {
                return;
            }

            // Plateau model
            if (m_PositionMode == PlateauMapsPositioningMode.PlateauModel)
            {
                double3 longLatHeight = new double3 { x = m_CityModelPosition.y, y = m_CityModelPosition.x, z = resultOfGeoidHeightQuery };
                m_CityModel.transform.parent.GetComponent<CesiumGeoreference>().SetOriginLongitudeLatitudeHeight(longLatHeight[0], longLatHeight[1], longLatHeight[2]);
                m_CityModel.transform.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = longLatHeight;
                m_CityModel.transform.rotation = Quaternion.identity;
            }
            else if (m_PositionMode == PlateauMapsPositioningMode.IfcModel)// IFC model
            {
                m_IfcGlobeAnchor.transform.parent.GetComponent<CesiumGeoreference>().SetOriginLongitudeLatitudeHeight(m_IfcWgsPosition[0], m_IfcWgsPosition[1], resultOfGeoidHeightQuery);
                m_IfcWgsPosition[2] /= 1000f;
                m_IfcWgsPosition[2] += resultOfGeoidHeightQuery ;
                m_IfcGlobeAnchor.longitudeLatitudeHeight = m_IfcWgsPosition;
                Quaternion rotation = Quaternion.Euler(0, 180f - Mathf.Atan2((float)m_IfcCoordinateInfo.XOrdinate, (float)m_IfcCoordinateInfo.XAbscissa), 0);
                m_IfcGlobeAnchor.transform.rotation = rotation;
                Vector3 scale = new Vector3((float)m_IfcCoordinateInfo.Scale, (float)m_IfcCoordinateInfo.Scale, (float)m_IfcCoordinateInfo.Scale);
                m_IfcGlobeAnchor.transform.localScale = scale;
            }

         

            if (resultOfGeoidHeightQuery == 0f)
            {
                EditorUtility.DisplayDialog(
                                "高さ合わせが失敗しました",
                                "インターネットに接続されていると確認した上で再度お試しください。高さはゼロに設定されました。",
                                "OK"
                                );
            }

        }
#endif
    }
}