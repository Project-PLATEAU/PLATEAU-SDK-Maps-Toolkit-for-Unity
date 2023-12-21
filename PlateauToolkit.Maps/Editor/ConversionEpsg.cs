using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.CoordinateSystems;
using ProjNet.Converters.WellKnownText;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System;
using System.Text;
using Unity.Mathematics;

namespace PlateauToolkit.Maps.Editor
{
    public static class ConversionEpsg
    {
        public static double3 ConvertEpsgToWgs(IfcCoordinate ifcCoo)
        {
            // Provided IFC coordinates
            double[] ifcCoords = ifcCoo.Coordinates;  // The origin of the IFC file
            double eastings = ifcCoo.Eastings;
            double northings = ifcCoo.Northings;
            double height = ifcCoo.Height;
            double xAbcessa = ifcCoo.XAbscissa;
            double yOrdinate = ifcCoo.XOrdinate;
            double scale = ifcCoo.Scale;

            // Convert to EPSG:6677
            double[] epsgCoords = ConvertToEpsg(ifcCoords, eastings, northings, height, xAbcessa, yOrdinate, scale);

            // Define the coordinate systems
            string jgd2011Wkt = "PROJCS[\"JGD2011 / Japan Plane Rectangular CS IX\",GEOGCS[\"JGD2011\",DATUM[\"Japanese_Geodetic_Datum_2011\",SPHEROID[\"GRS 1980\",6378137,298.257222101],TOWGS84[0,0,0,0,0,0,0]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"6668\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",36],PARAMETER[\"central_meridian\",139.833333333333],PARAMETER[\"scale_factor\",0.9999],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AUTHORITY[\"EPSG\",\"6677\"]]";

            ICoordinateSystem jgd2011 = (ICoordinateSystem)CoordinateSystemWktReader.Parse(jgd2011Wkt, Encoding.UTF8);
            ICoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;

            // Define a coordinate transformation factory
            CoordinateTransformationFactory ctFactory = new CoordinateTransformationFactory();

            // Create a coordinate transformation from EPSG:6677 to WGS84
            ICoordinateTransformation transformer = ctFactory.CreateFromCoordinateSystems(jgd2011, wgs84);
            // Transform the point to WGS84
            double[] res = transformer.MathTransform.Transform(epsgCoords);
            double3 final = new double3(res[0], res[1], res[2] / 100);
            return final;
        }

        static double[] ConvertToEpsg(double[] ifcCoords, double eastings, double northings, double height, double xAbcessa, double yOrdinate, double scale)
        {
            // Rotate and scale
            double angle = Math.Atan2(yOrdinate, xAbcessa);
            double rotatedX = scale * (ifcCoords[0] * Math.Cos(angle) - ifcCoords[1] * Math.Sin(angle));
            double rotatedY = scale * (ifcCoords[0] * Math.Sin(angle) + ifcCoords[1] * Math.Cos(angle));
            double scaledZ = scale * ifcCoords[2];

            // Translate
            double[] epsgCoords = new double[] { rotatedX + eastings, rotatedY + northings, scaledZ + height };

            return epsgCoords;
        }
    }
}