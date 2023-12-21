using System;

namespace PlateauToolkit.Maps.Editor
{
    [Serializable]
    public class ProjectedCoordinateSystem
    {
        public string Name;
        public GeoCS GeographicCoordinateSystem;
        public Projection Projection;
        public LinearUnit LinearUnit;
    }

    [Serializable]
    public class Projection
    {
        public string Name;
        public double FalseEasting;
        public double FalseNorthing;
        public double CentralMeridian;
        public double ScaleFactor;
        public double LatitudeOfOrigin;
    }

    [Serializable]
    public class LinearUnit
    {
        public string Name;
        public double ConversionFactor;
    }
}