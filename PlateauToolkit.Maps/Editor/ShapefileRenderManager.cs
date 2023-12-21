#if CESIUM_FOR_UNITY
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using TriangleNet.Geometry;
using JetBrains.Annotations;

namespace PlateauToolkit.Maps.Editor
{
    public class ShapefileRenderManager : IDisposable
    {
        List<IShape> m_ListOfShapes = new List<IShape>();
        public Action m_OnRender;

        CesiumGeoreference m_GeoRef;
        readonly string m_FolderPath;
        string m_DbfFilePath;
        [CanBeNull] DbfReader m_DbfReader;
        bool m_DbfIsAvailable;
        SupportedEncoding m_SupportedStringEncoding;
        int m_RenderMode;
        int m_ShapeType;
        GameObject m_PositionMarkerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        Material m_Clockwise;
        Material m_Counter;

        string m_CurrentRenderingObject;
        float m_RenderHeight;
        bool m_MergeMeshes;

        GameObject m_PointDataPrefab;

        public ShapefileRenderManager(string folderPath, int renderMode, float renderHeight, bool mergeMeshes, SupportedEncoding supportedEncoding, GameObject pointDataPrefab = null)
        {
            m_GeoRef = GameObject.FindObjectOfType<CesiumGeoreference>();
            m_FolderPath = folderPath;
            m_RenderMode = renderMode;
            m_RenderHeight = renderHeight;
            m_MergeMeshes = mergeMeshes;
            m_PointDataPrefab = pointDataPrefab;
            m_SupportedStringEncoding = supportedEncoding;
            m_DbfReader = null;
        }

        public bool Read(float lineWidth)
        {
            string[] filePaths = Directory.GetFiles(m_FolderPath, "*.shp");

            if (lineWidth == 0f)
            {
                lineWidth = 10f;
            }

            if (filePaths.Length == 0)
            {
                return false;
            }

            foreach (string filePath in filePaths)
            {
                ReadShapes(filePath);
                string dbfFileName = Path.GetFileNameWithoutExtension(filePath);
                m_CurrentRenderingObject = dbfFileName;
                if (File.Exists(m_FolderPath + "/" + dbfFileName + ".dbf"))
                {
                    m_DbfFilePath = m_FolderPath + "/" + dbfFileName + ".dbf";
                    m_DbfIsAvailable = true;
                    m_DbfReader = new DbfReader(m_DbfFilePath, m_SupportedStringEncoding);
                    m_DbfReader.ReadHeader();
                }
                else
                {
                    m_DbfFilePath = "";
                    m_DbfIsAvailable = false;
                }
                DrawShapes(m_CurrentRenderingObject, lineWidth);
            }
            return true;
        }

        public bool ReadShapes(string filePath)
        {
            if (m_GeoRef == null)
            {
                return false;
            }
#if UNITY_URP
            m_Clockwise = AssetDatabase.LoadAssetAtPath<Material>(PlateauToolkitMapsConstants.k_ClockwiseMaterial);
            m_Counter = AssetDatabase.LoadAssetAtPath<Material>(PlateauToolkitMapsConstants.k_CounterClockwiseMaterial);
#endif
#if UNITY_HDRP
            m_Clockwise = AssetDatabase.LoadAssetAtPath<Material>(PlateauToolkitMapsConstants.k_ClockwiseMaterialHdrp);
            m_Counter = AssetDatabase.LoadAssetAtPath<Material>(PlateauToolkitMapsConstants.k_CounterClockwiseMaterialHdrp);
#endif
            if (m_Counter == null || m_Clockwise == null)
            {
                return false;
            }

            using (ShapefileReader reader = new ShapefileReader(filePath))
            {
                m_ListOfShapes = reader.ReadShapes();
                m_ShapeType = reader.ShapeConstants;
            }
            m_PositionMarkerSphere.transform.SetParent(m_GeoRef.transform);
            m_PositionMarkerSphere.AddComponent<CesiumGlobeAnchor>();
            return true;
        }

        public void DrawShapes(string currentRenderingObjectName, float lineWidth)
        {
            if (m_GeoRef == null)
            {
                return;
            }

            if (m_ListOfShapes.Count > 0)
            {
                int index = 0;

                GameObject rootShpObject = new GameObject(currentRenderingObjectName + "_SHP");
                rootShpObject.transform.parent = m_GeoRef.transform;
                CesiumGlobeAnchor anchor = rootShpObject.AddComponent<CesiumGlobeAnchor>();
                rootShpObject.AddComponent<MeshFilter>();
                rootShpObject.AddComponent<MeshRenderer>();
#if UNITY_URP
                GameObject shpLineRendererObject = AssetDatabase.LoadAssetAtPath<GameObject>(PlateauToolkitMapsConstants.k_ShapeParentPrefab);
#endif
#if UNITY_HDRP
                GameObject shpLineRendererObject = AssetDatabase.LoadAssetAtPath<GameObject>(PlateauToolkitMapsConstants.k_ShapeParentHdrpPrefab);
#endif
                GameObject mesh = AssetDatabase.LoadAssetAtPath<GameObject>(PlateauToolkitMapsConstants.k_MeshObjectPrefab);

                foreach (IShape shape in m_ListOfShapes)
                {
                    DbfRecord record = new DbfRecord();

                    if (m_DbfIsAvailable)
                    {
                        record = m_DbfReader.ReadNextRecord();
                    }
                    if (shpLineRendererObject != null)
                    {
                        switch (m_ShapeType)
                        {
                            case 3:
                            case 5:
                                DrawPolygonOrPolyline(shape, index, lineWidth, shpLineRendererObject, rootShpObject, m_DbfIsAvailable, m_DbfReader, record, mesh);
                                break;
                            case 1:
                                DrawPoint(shape, rootShpObject, m_DbfIsAvailable, m_DbfReader, record);
                                break;
                        }

                        index++;
                    }
                    else
                    {
                        Debug.LogError("Failed to load shpLineRendererObject");
                    }
                }
                if (m_RenderMode == 0)
                {
                    double3 pos = anchor.longitudeLatitudeHeight;
                    pos.z = m_RenderHeight;
                    anchor.longitudeLatitudeHeight = pos;

                    if (rootShpObject.GetComponent<MeshFilter>() != null && rootShpObject.GetComponent<MeshRenderer>() != null && m_MergeMeshes)
                    {
                        rootShpObject.AddComponent<MeshCombiner>().CombineMeshes();
                        GameObject mergedMeshes = new GameObject(rootShpObject.name + "_merged");
                        mergedMeshes.AddComponent<MeshFilter>().mesh = rootShpObject.GetComponent<MeshCombiner>().CombinedMesh;
                        mergedMeshes.AddComponent<MeshRenderer>().material = m_Clockwise;
                        mergedMeshes.transform.parent = m_GeoRef.transform;
                        mergedMeshes.AddComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = anchor.longitudeLatitudeHeight;
                        GameObject.DestroyImmediate(rootShpObject);
                    }
                }
            }
            if(m_DbfReader != null)
            {
                m_DbfReader.Dispose();
            }
        }

        void DrawPoint(IShape shape, GameObject parentObject, bool dbfRead, DbfReader dbfReader, DbfRecord record)
        {
#if UNITY_URP
            GameObject markerObjectDefault = AssetDatabase.LoadAssetAtPath<GameObject>(PlateauToolkitMapsConstants.k_PointMarkerPrefab);
#endif
#if UNITY_HDRP
            GameObject markerObjectDefault = AssetDatabase.LoadAssetAtPath<GameObject>(PlateauToolkitMapsConstants.k_PointMarkerHdrpPrefab);
#endif

            foreach (Vector3 point in shape.Points)
            {
                double3 coordinates = new(point.x, point.z, m_RenderHeight);
                m_PositionMarkerSphere.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = coordinates;
                Vector3 pointPos = m_PositionMarkerSphere.transform.position;
                GameObject marker = m_PointDataPrefab == null ? (GameObject)PrefabUtility.InstantiatePrefab(markerObjectDefault) : GameObject.Instantiate(m_PointDataPrefab);
                marker.name = "point_data";
                marker.transform.parent = parentObject.transform;
                marker.AddComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = coordinates;
                if (!string.IsNullOrEmpty(m_DbfFilePath) && dbfRead && dbfReader.GetRecordLength() == m_ListOfShapes.Count)
                {
                    AttachMetadata(marker, record);
                }
            }
        }

        void DrawPolygonOrPolyline(IShape shape, int index, float lineWidth, GameObject shapeParent, GameObject parentObject, bool dbfRead, DbfReader dbfReader, DbfRecord record, GameObject mesh)
        {
            for (int i = 0; i < shape.Parts.Count - 1; i++)
            {
                int start = shape.Parts[i];
                int end = shape.Parts[i + 1];

                // get the points
                List<Vector3> partPoints = shape.Points.GetRange(start, end - start);
                List<Vector3> partPointsWorld = new List<Vector3>();

                foreach (Vector3 point in partPoints)
                {
                    double3 coordinates = new(point.x, point.z, m_RenderHeight);
                    m_PositionMarkerSphere.GetComponent<CesiumGlobeAnchor>().longitudeLatitudeHeight = coordinates;
                    Vector3 pointPos = m_PositionMarkerSphere.transform.position;
                    partPointsWorld.Add(pointPos);
                }

                if (m_RenderMode == 1)
                {
                    GameObject shpParentInstance = (GameObject)PrefabUtility.InstantiatePrefab(shapeParent);

                    shpParentInstance.transform.position = Vector3.zero;
                    shpParentInstance.name = "shpParent_" + index;

                    shpParentInstance.transform.parent = parentObject.transform;
                    LineRenderer lineRenderer = shpParentInstance.GetComponent<LineRenderer>();
                    lineRenderer.positionCount = partPointsWorld.Count;
                    lineRenderer.useWorldSpace = false;
                    lineRenderer.startWidth = lineWidth;  // Set the start width
                    lineRenderer.endWidth = lineWidth;
                    lineRenderer.SetPositions(partPointsWorld.ToArray());
                    lineRenderer.loop = true;
                    if (!string.IsNullOrEmpty(m_DbfFilePath) && dbfRead && dbfReader.GetRecordLength() == m_ListOfShapes.Count)
                    {
                        AttachMetadata(shpParentInstance, record);
                    }
                }
                else if (m_RenderMode == 0)
                {
                    GameObject meshObject = (GameObject)PrefabUtility.InstantiatePrefab(mesh);

                    meshObject.transform.position = Vector3.zero;
                    meshObject.transform.parent = parentObject.transform;
                    if (!string.IsNullOrEmpty(m_DbfFilePath) && dbfRead && dbfReader.GetRecordLength() == m_ListOfShapes.Count)
                    {
                        AttachMetadata(meshObject, record);
                    }
                    CreateMesh(false, partPointsWorld, meshObject.GetComponent<MeshFilter>(), meshObject.GetComponent<MeshRenderer>());
                }
                else
                {
                    Debug.LogError("Failed to instantiate shpLineRendererObject");
                }
            }
        }

        public void CreateMesh(bool isHole, List<Vector3> points, MeshFilter meshFilter, MeshRenderer meshRenderer)
        {
            var vertices = new List<Vertex>();
            // add your Vector3 points as Vertex to the points list here.

            for (int k = 0; k < points.Count; k++)
            {
                vertices.Add(new TriangleNet.Geometry.Vertex(points[k].x, points[k].z));
            }

            Polygon polygon = new Polygon();
            polygon.Add(new Contour(vertices), isHole);


            TriangleNet.Meshing.IMesh mesh = polygon.Triangulate();

            List<int> triangles = new List<int>();
            List<Vector3> unityVertices = new List<Vector3>();

            foreach (TriangleNet.Topology.Triangle triangle in mesh.Triangles)
            {
                unityVertices.Add(new Vector3((float)triangle.GetVertex(0).X, 0, (float)triangle.GetVertex(0).Y)); // Assume Y is up
                unityVertices.Add(new Vector3((float)triangle.GetVertex(1).X, 0, (float)triangle.GetVertex(1).Y));
                unityVertices.Add(new Vector3((float)triangle.GetVertex(2).X, 0, (float)triangle.GetVertex(2).Y));

                // Add the indices of the triangle vertices to the triangles list
                triangles.Add(unityVertices.Count - 1);
                triangles.Add(unityVertices.Count - 2);
                triangles.Add(unityVertices.Count - 3);
            }

            // Create a new Unity Mesh
            Mesh unityMesh = new Mesh();
            unityMesh.vertices = unityVertices.ToArray();
            unityMesh.triangles = triangles.ToArray();

            unityMesh.RecalculateNormals();
            meshFilter.sharedMesh = unityMesh;

            meshRenderer.sharedMaterial = isHole ? m_Counter : m_Clockwise;
        }

        public void AttachMetadata(GameObject gisObj, DbfRecord record)
        {
            DbfComponent dbfComponent = gisObj.AddComponent<DbfComponent>();
            foreach (string attr in record.Fields)
            {
                dbfComponent.Properties.Add($"{attr}");
            }
        }

        public void Dispose()
        {
            if (m_PositionMarkerSphere != null)
            {
                UnityEngine.Object.DestroyImmediate(m_PositionMarkerSphere);
                m_PositionMarkerSphere = null; // Prevent a second call to Dispose from trying to destroy it again.
            }
        }
    }
}
#endif