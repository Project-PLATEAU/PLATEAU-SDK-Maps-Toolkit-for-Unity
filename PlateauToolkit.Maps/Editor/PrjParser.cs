using System.IO;
using System.Text.RegularExpressions;

namespace PlateauToolkit.Maps.Editor
{
    public static class PrjParser
    {
        public static int GeogOrProj(string textToRead)
        {
            // We are assuming there are only two types, GEOGCS or PROJCS.
            return textToRead.Contains("GEOGCS") ? 0 : 1;
        }

        public static GeoCS ParseStringToGeog(string textToRead)
        {
            GeoCS gcs = new GeoCS
            {
                Name = ExtractParameter("GEOGCS", textToRead),
                Datum = new Datum
                {
                    Name = ExtractParameter("DATUM", textToRead),
                    Spheroid = new Spheroid
                    {
                        Name = ExtractParameter("SPHEROID", textToRead),
                        SemiMajorAxis = ParseDouble(Regex.Match(ExtractParameter("SPHEROID", textToRead), @"(\d+(\.\d+)?)").Value),
                        InverseFlattening = ParseDouble(Regex.Match(ExtractParameter("SPHEROID", textToRead), @"(\d+(\.\d+)?)(?!.*\d)").Value)
                    }
                },
                PrimeMeridian = new PrimeMeridian
                {
                    Name = ExtractParameter("PRIMEM", textToRead),
                    Longitude = ParseDouble(ExtractParameter("PRIMEM", textToRead))
                },
                AngularUnit = new AngularUnit
                {
                    Name = ExtractParameter("UNIT", textToRead),
                    ConversionFactor = ParseDouble(ExtractParameter("UNIT", textToRead))
                }
            };

            return gcs;
        }

        public static ProjectedCoordinateSystem ParseStringToProj(string textToRead)
        {
            // Parse Projected Coordinate System
            ProjectedCoordinateSystem pcs = new ProjectedCoordinateSystem
            {
                Name = ExtractParameter("PROJCS", textToRead),
                GeographicCoordinateSystem = ParseStringToGeog(ExtractParameter("GEOGCS", textToRead)) as GeoCS,
                Projection = new Projection
                {
                    Name = ExtractParameter("PROJECTION", textToRead),
                    FalseEasting = ParseDouble(ExtractParameter("False_Easting", textToRead)),
                    FalseNorthing = ParseDouble(ExtractParameter("False_Northing", textToRead)),
                    CentralMeridian = ParseDouble(ExtractParameter("Central_Meridian", textToRead)),
                    ScaleFactor = ParseDouble(ExtractParameter("Scale_Factor", textToRead)),
                    LatitudeOfOrigin = ParseDouble(ExtractParameter("Latitude_Of_Origin", textToRead)),
                },
                LinearUnit = new LinearUnit
                {
                    Name = ExtractParameter("UNIT", textToRead),
                    ConversionFactor = ParseDouble(ExtractParameter("UNIT", textToRead))
                }
            };
            return pcs;
        }

        static string ExtractParameter(string parameter, string wkt)
        {
            int start = wkt.IndexOf(parameter) + parameter.Length + 1;
            int end = wkt.IndexOf("]", start);
            return wkt.Substring(start, end - start);
        }

        static double ParseDouble(string textToRead)
        {
            return double.TryParse(textToRead, out double result) ? result : 0;

        }
    }
}