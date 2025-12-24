using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace PlateauToolkit.Maps
{
    public class XmlElementComponent : MonoBehaviour
    {
        public string ID;
        public List<string> Properties = new List<string>();
        public List<string> ChildElements = new List<string>();
    }

    [CanBeNull]
    public class IfcCoordinate
    {
        public double[] Coordinates { get; set; }
        public double Eastings { get; set; }
        public double Northings { get; set; }
        public double Height { get; set; }
        public double XAbscissa { get; set; }
        public double XOrdinate { get; set; }
        public double Scale { get; set; }
        public int EpsgCode { get; set; }
        public int Jgd2011Id { get; set; }
        
        // WGS84座標（IfcSiteから取得した場合）
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsWgs84 { get; set; } // WGS84座標かどうかのフラグ

        // Default constructor
        public IfcCoordinate()
        {
            // Set default values for each property
            Coordinates = new double[] { 0.0, 0.0, 0.0 }; // Example default value
            Eastings = -1.0;
            Northings = -1.0;
            Height = -1.0;
            XAbscissa = -1.0;
            XOrdinate = -1.0;
            Scale = 1.0; // Assuming a default scale of 1. Adjust as needed.
            Latitude = -1.0;
            Longitude = -1.0;
            IsWgs84 = false;
        }
    }
}