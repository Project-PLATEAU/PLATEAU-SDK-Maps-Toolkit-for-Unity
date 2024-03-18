#if CESIUM_FOR_UNITY
using CesiumForUnity;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Mathematics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.IO;

namespace PlateauToolkit.Maps.Editor
{
    public class GeoJsonLoader : IDisposable
    {
        CesiumGeoreference m_GeoRef;
        GameObject m_PositionMarkerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        GameObject m_PointDataPrefab;
        GameObject m_PointMarkerDefaultPrefab;
        bool m_LoopLineRenderer;

        public GeoJsonLoader(bool closeLineRenderer, GameObject pointDataPrefab = null)
        {
            m_GeoRef = GameObject.FindObjectOfType<CesiumGeoreference>();
            m_PositionMarkerSphere.transform.SetParent(m_GeoRef.transform);
            m_PositionMarkerSphere.AddComponent<CesiumGlobeAnchor>();
            m_PointDataPrefab = pointDataPrefab;
            m_LoopLineRenderer = closeLineRenderer;
        }

        public bool ReadAll(string pathToFolder, float height, float lineWidth)
        {
            if (lineWidth == 0f)
            {
                lineWidth = 10f;
            }

            if (m_GeoRef == null)
            {
                return false;
            }

            string[] filePaths = Directory.GetFiles(pathToFolder, "*.geojson");

            if (filePaths.Length == 0)
            {
                return false;
            }

            foreach (string filePath in filePaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                ReadGeoJsonData(filePath, height, lineWidth, fileName);
            }
            return true;
        }

        void ReadGeoJsonData(string filePath, float height, float lineWidth, string currentRenderingObjectName)
        {
            string jsonString = File.ReadAllText(filePath);
            GeoJson geoJson = JsonConvert.DeserializeObject<GeoJson>(jsonString);

            GameObject parentObject = new GameObject(currentRenderingObjectName + "_GeoJSON");
            parentObject.transform.parent = m_GeoRef.transform;
            parentObject.AddComponent<CesiumGlobeAnchor>();

#if UNITY_URP
            GameObject shapeParent = AssetDatabase.LoadAssetAtPath<GameObject>(PlateauToolkitMapsConstants.k_ShapeParentPrefab);
            m_PointMarkerDefaultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlateauToolkitMapsConstants.k_PointMarkerPrefab);
#endif
#if UNITY_HDRP
            GameObject shapeParent = AssetDatabase.LoadAssetAtPath<GameObject>(PlateauToolkitMapsConstants.k_ShapeParentHdrpPrefab);
            m_PointMarkerDefaultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlateauToolkitMapsConstants.k_PointMarkerHdrpPrefab);
#endif
            GameObject mesh = AssetDatabase.LoadAssetAtPath<GameObject>(PlateauToolkitMapsConstants.k_MeshObjectPrefab);

            ProcessFeatures(geoJson.features, parentObject, shapeParent, height, lineWidth);
        }

        void ProcessFeatures(IEnumerable<Feature> features, GameObject parentObject, GameObject shapeParent, float height, float lineWidth)
        {
            foreach (Feature feature in features)
            {
                string type = feature.geometry.type;
                Dictionary<string, object> properties = feature.properties;
                var coordinates = (JArray)feature.geometry.coordinates;

                switch (type)
                {
                    case "Point":
                        ProcessPoint(coordinates, parentObject, height, properties);
                        break;
                    case "MultiPolygon":
                        ProcessMultiPolygon(coordinates, parentObject, shapeParent, height, lineWidth, properties);
                        break;
                    case "Polygon":
                        ProcessPolygon(coordinates, parentObject, shapeParent, height, lineWidth, properties);
                        break;
                    case "LineString":
                        ProcessLineString(coordinates, parentObject, shapeParent, height, lineWidth, properties);
                        break;
                    default:
                        Debug.LogError($"Unsupported geometry type: {type}");
                        break;
                }
            }
        }

        void ProcessPoint(JArray coordinates, GameObject parentObject, float height, Dictionary<string, object> properties)
        {
            List<double> pointCoords = coordinates.ToObject<List<double>>();
            CreateAndPlaceMarker(pointCoords, parentObject, height, "Point", properties);
        }

        void ProcessPolygon(JArray coordinates, GameObject parentObject, GameObject shapeParent, float height, float lineWidth, Dictionary<string, object> properties)
        {
            double[][][] polygonCoordinates = coordinates.ToObject<double[][][]>();
            CreateAndRenderShape(polygonCoordinates, parentObject, shapeParent, height, lineWidth, "Polygon", properties);
        }

        void ProcessMultiPolygon(JArray coordinates, GameObject parentObject, GameObject shapeParent, float height, float lineWidth, Dictionary<string, object> properties)
        {
            double[][][][] multiPolygonCoordinates = coordinates.ToObject<double[][][][]>();
            foreach (double[][][] polygonCoords in multiPolygonCoordinates)
            {
                CreateAndRenderShape(polygonCoords, parentObject, shapeParent, height, lineWidth, "MultiPolygon", properties);
            }
        }

        void ProcessLineString(JArray coordinates, GameObject parentObject, GameObject shapeParent, float height, float lineWidth, Dictionary<string, object> properties)
        {
            double[][] lineCoordinates = coordinates.ToObject<double[][]>();
            CreateAndRenderLine(lineCoordinates, parentObject, shapeParent, height, lineWidth, properties);
        }

        void CreateAndPlaceMarker(List<double> coords, GameObject parentObject, float height, string prefix, Dictionary<string, object> properties)
        {
            // Assuming m_PointDataPrefab and m_PointMarkerDefaultPrefab are predefined in the context.
            for (int i = 0; i < coords.Count; i += 2)
            {
                double3 position = new double3(coords[i], coords[i + 1], height);
                // Instantiate and set up the marker GameObject
                GameObject marker = InstantiateMarker(m_PointDataPrefab, m_PointMarkerDefaultPrefab, parentObject, position, prefix);
                // Additional properties can be attached to the marker here if needed
                AttachProperties(properties, marker);
            }
        }

        void CreateAndRenderShape(double[][][] coordinates, GameObject parentObject, GameObject shapeParent, float height, float lineWidth, string prefix, Dictionary<string, object> properties)
        {
            for (int i = 0; i < coordinates.Length; i++)
            {
                // Create line renderer object and set properties
                GameObject shapeInstance = InstantiateShape(shapeParent, parentObject, $"{prefix}_{i}", lineWidth, properties);
                IReadOnlyList<Vector3> shapeWorldCoords = ConvertCoordinatesToWorldSpace(coordinates[i], height);
                RenderShape(shapeInstance, shapeWorldCoords);
            }
        }

        void CreateAndRenderLine(double[][] coordinates, GameObject parentObject, GameObject shapeParent, float height, float lineWidth, Dictionary<string, object> properties)
        {
            // Create line renderer object and set properties
            GameObject lineInstance = InstantiateShape(shapeParent, parentObject, "LineString", lineWidth, properties);
            IReadOnlyList<Vector3> lineWorldCoords = ConvertCoordinatesToWorldSpace(coordinates, height);
            RenderShape(lineInstance, lineWorldCoords);
        }

        IReadOnlyList<Vector3> ConvertCoordinatesToWorldSpace(double[][] coords, float height)
        {
            List<Vector3> worldCoords = new List<Vector3>();
            foreach (double[] coord in coords)
            {
                double3 position = new double3(coord[0], coord[1], height);
                CesiumGlobeAnchor cesiumGlobeAnchor = m_PositionMarkerSphere.GetComponent<CesiumGlobeAnchor>();
                Debug.Assert(cesiumGlobeAnchor != null, "CesiumGlobeAnchor component is not attached to the object.");

                if (cesiumGlobeAnchor != null)
                {
                    m_PositionMarkerSphere.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = position;
                }
                worldCoords.Add(m_PositionMarkerSphere.transform.position);
            }
            return worldCoords;
        }

        GameObject InstantiateMarker(GameObject prefab, GameObject defaultPrefab, GameObject parentObject, double3 position, string prefix)
        {
            GameObject marker = m_PointDataPrefab == null ? (GameObject)PrefabUtility.InstantiatePrefab(defaultPrefab) : GameObject.Instantiate(prefab);
            marker.name = $"{prefix}_marker";
            marker.transform.parent = parentObject.transform;
            marker.AddComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = position;
            return marker;
        }

        GameObject InstantiateShape(GameObject shapeParent, GameObject parentObject, string name, float lineWidth, Dictionary<string, object> properties)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(shapeParent);
            instance.transform.position = Vector3.zero;
            instance.name = $"geojsonParent_{name}";
            instance.transform.parent = parentObject.transform;

            LineRenderer lineRenderer = instance.GetComponent<LineRenderer>();
            if (m_LoopLineRenderer)
            {
                lineRenderer.loop = true;
            }
            else
            {
                lineRenderer.loop = false;
            }
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            AttachProperties(properties, instance);

            return instance;
        }

        void RenderShape(GameObject shapeInstance, IReadOnlyList<Vector3> worldCoords)
        {
            LineRenderer lineRenderer = shapeInstance.GetComponent<LineRenderer>();
            lineRenderer.positionCount = worldCoords.Count;
            lineRenderer.useWorldSpace = false;
            lineRenderer.SetPositions(worldCoords.ToArray());
        }

        void AttachProperties(Dictionary<string, object> properties, GameObject renderObject)
        {
            GeoJsonProperties propertiesComponent = renderObject.AddComponent<GeoJsonProperties>();
            propertiesComponent.SetProperties(properties);
        }

        public void Dispose()
        {
            if (m_PositionMarkerSphere != null)
            {
                UnityEngine.Object.DestroyImmediate(m_PositionMarkerSphere);
            }
        }
    }
}
#endif