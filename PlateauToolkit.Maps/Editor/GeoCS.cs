using System;

namespace PlateauToolkit.Maps.Editor
{

    [Serializable]
    public class GeoCS
    {
        public string Name;
        public Datum Datum;
        public PrimeMeridian PrimeMeridian;
        public AngularUnit AngularUnit;
    }

    [Serializable]
    public class Datum
    {
        public string Name;
        public Spheroid Spheroid;
    }

    [Serializable]
    public class Spheroid
    {
        public string Name;
        public double SemiMajorAxis;
        public double InverseFlattening;
    }

    [Serializable]
    public class PrimeMeridian
    {
        public string Name;
        public double Longitude;
    }

    [Serializable]
    public class AngularUnit
    {
        public string Name;
        public double ConversionFactor;
    }
}