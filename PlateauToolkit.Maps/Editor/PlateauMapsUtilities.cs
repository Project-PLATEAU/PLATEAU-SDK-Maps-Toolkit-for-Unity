using UnityEditor;
using UnityEngine;
using PLATEAU.CityInfo;
using Unity.Mathematics;
using PLATEAU.Native;

namespace PlateauToolkit.Maps.Editor
{
    static class PlateauMapsUtilities
    {

        #region Move model to new origin location
        public static int GetGeoIndexOfCityModel(PLATEAUInstancedCityModel plateauGroup)
        {
            return plateauGroup.GeoReference.ZoneID - 1;
        }

        public static double2 GetOriginLongLatOfCityModel(PLATEAUInstancedCityModel plateauGroup)
        {
            PLATEAU.Geometries.GeoReference geoRef = plateauGroup.GeoReference;
            GeoCoordinate geoCoord = geoRef.Unproject(new PlateauVector3d(0, 0, 0));
            return new double2 { x = geoCoord.Latitude, y = geoCoord.Longitude };

        }
        #endregion

        #region Adjust height

        public static float GetLowestVertex(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            float minY = Mathf.Infinity; // initialize minY to a large value
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += normals[i] * Mathf.Sin(Time.time);
                if (vertices[i].y < minY)
                { // update minY if a lower value is found
                    minY = vertices[i].y;
                }
            }
            mesh.vertices = vertices;
            return minY; // return the lowest Y value
        }

        #endregion

        #region Request geoid height

        // taken from https://vldb.gsi.go.jp/sokuchi/surveycalc/api_help.html
        public static void RequestGeoidHeight(string uri, PlateauMapsHeightClient.GeoidDataCallback callback)
        {
            PlateauMapsHeightClient.RequestGeoidHeight(uri, callback);
        }

        #endregion

    }

}