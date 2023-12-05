// Original implementation : https://gist.github.com/yubeneko/cbde0e3bae30eac4fab5457dc5b1f0a2#file-geocoordinateconverter-cs

using System;
using Unity.Mathematics;
using UnityEngine;

namespace PlateauToolkit.Maps.Editor
{
    public static class GeoCoordinateUtility
    {
        /// <summary>
        /// Earth radius
        /// </summary>
        const double k_A = 6378137d;
        /// <summary>
        /// F value
        /// </summary>
        const double k_F = 298.257222101;
        /// <summary>
        /// m0 parameter
        /// </summary>
        const double k_M0 = 0.9999;

        /// <summary>
        /// Parameters and equations follows this page
        /// https://vldb.gsi.go.jp/sokuchi/surveycalc/surveycalc/algorithm/xy2bl/xy2bl.htm
        /// </summary>
        /// <param name="x0">x0</param>
        /// <param name="y0">y0</param>
        /// <param name="latOrigin_deg">Origin latitude</param>
        /// <param name="lonOrigin_deg">Origin longitude</param>
        /// <returns>GeoPoint holding Latitude and Longitude</returns>
        public static GeoPoint Coordinate2LatLon(double x0, double y0, double latOrigin_deg, double lonOrigin_deg)
        {
            double phi0_rad = Degree2Radian(latOrigin_deg);
            double lambda0_rad = Degree2Radian(lonOrigin_deg);

            // n
            double n = 1 / (2 * k_F - 1);

            // A0~A5
            double[] a = new double[6];
            a[0] = 1d + Math.Pow(n, 2) / 4 + Math.Pow(n, 4) / 64;
            a[1] = -3d / 2d * (n - Math.Pow(n, 3) / 8 - Math.Pow(n, 5) / 64);
            a[2] = 15d / 16d * (Math.Pow(n, 2) - Math.Pow(n, 4) / 4);
            a[3] = -35d / 48d * (Math.Pow(n, 3) - 5d / 16d * Math.Pow(n, 5));
            a[4] = 315d / 512d * Math.Pow(n, 4);
            a[5] = -693d / 1280d * Math.Pow(n, 5);

            double[] beta = new double[6];
            beta[1] = 1d / 2d * n - 2d / 3d * Math.Pow(n, 2) + 37d / 96d * Math.Pow(n, 3) - 1d / 360d * Math.Pow(n, 4) - 81d / 512d * Math.Pow(n, 5);
            beta[2] = 1d / 48d * Math.Pow(n, 2) + 1d / 15d * Math.Pow(n, 3) - 437d / 1440d * Math.Pow(n, 4) + 46d / 105d * Math.Pow(n, 5);
            beta[3] = 17d / 480d * Math.Pow(n, 3) - 37d / 840d * Math.Pow(n, 4) - 209d / 4480d * Math.Pow(n, 5);
            beta[4] = 4397d / 161280d * Math.Pow(n, 4) - 11d / 504d * Math.Pow(n, 5);
            beta[5] = 4583d / 161280d * Math.Pow(n, 5);

            double[] delta = new double[7];
            delta[1] = 2 * n - 2d / 3d * Math.Pow(n, 2) - 2 * Math.Pow(n, 3) + 116d / 45d * Math.Pow(n, 4) + 26d / 45d * Math.Pow(n, 5) - 2854d / 675d * Math.Pow(n, 6);
            delta[2] = 7d / 3d * Math.Pow(n, 2) - 8d / 5d * Math.Pow(n, 3) - 227d / 45d * Math.Pow(n, 4) + 2704d / 315d * Math.Pow(n, 5) + 2323d / 945d * Math.Pow(n, 6);
            delta[3] = 56d / 15d * Math.Pow(n, 3) - 136d / 35d * Math.Pow(n, 4) - 1262d / 105d * Math.Pow(n, 5) + 73814d / 2835d * Math.Pow(n, 6);
            delta[4] = 4279d / 630d * Math.Pow(n, 4) - 332d / 35d * Math.Pow(n, 5) - 399572d / 14175d * Math.Pow(n, 6);
            delta[5] = 4174d / 315d * Math.Pow(n, 5) - 144838d / 6237d * Math.Pow(n, 6);
            delta[6] = 601676d / 22275d * Math.Pow(n, 6);

            double a_ = k_M0 * k_A / (1 + n) * a[0];
            double phi0 = 0;
            for (int j = 1; j <= 5; j++)
            {
                phi0 += a[j] * Math.Sin(2 * j * phi0_rad);
            }
            phi0 = k_M0 * k_A / (1 + n) * (a[0] * phi0_rad + phi0);

            double xi = (x0 + phi0) / a_;
            double eta = y0 / a_;

            double xiD = 0;
            for (int j = 1; j <= 5; j++)
            {
                xiD += beta[j] * Math.Sin(2 * j * xi) * Math.Cosh(2 * j * eta);
            }
            xiD = xi - xiD;
            double etaD = 0;
            for (int j = 1; j <= 5; j++)
            {
                etaD += beta[j] * Math.Cos(2 * j * xi) * Math.Sinh(2 * j * eta);
            }
            etaD = eta - etaD;

            double chi = Math.Asin(Math.Sin(xiD) / Math.Cosh(etaD));

            double phi_rad = 0;
            for (int j = 1; j <= 6; j++)
            {
                phi_rad += delta[j] * Math.Sin(2 * j * chi);
            }
            phi_rad += chi;
            double lambda_rad = lambda0_rad + Math.Atan(Math.Sinh(etaD) / Math.Cos(xiD));

            var latLon = new GeoPoint()
            {
                // Latitude
                m_X = Radian2Degree(phi_rad),
                // Longitude
                m_Y = Radian2Degree(lambda_rad)
            };
            return latLon;
        }

        /// <summary> 
        /// based on the following link
        /// https://vldb.gsi.go.jp/sokuchi/surveycalc/surveycalc/algorithm/bl2xy/bl2xy.htm
        /// </summary>
        /// <param name="lat0_deg">•ÏŠ·Œ³‚ÌˆÜ“x(“x’PˆÊ10i)</param>
        /// <param name="lon0_deg">•ÏŠ·Œ³‚ÌŒo“x(“x’PˆÊ10i)</param>
        /// <param name="latOrigin_deg">•½–Ê’¼ŒðÀ•WŒnŒ´“_‚ÌˆÜ“x(“x’PˆÊ10i)</param>
        /// <param name="lonOrigin_deg">•½–Ê’¼ŒðÀ•WŒnŒ´“_‚ÌŒo“x(“x’PˆÊ10i)</param>
        /// <returns> GeoPointŒ^BX:•ÏŠ·Œã‚ÌXÀ•W, Y:•ÏŠ·Œã‚ÌYÀ•WB‚Æ‚à‚Éƒ[ƒgƒ‹’PˆÊB</returns>
        public static GeoPoint LatLon2Coordinate(double lat0_deg, double lon0_deg, double latOrigin_deg, double lonOrigin_deg)
        {
            double phi0_rad = Degree2Radian(latOrigin_deg);
            double lambda0_rad = Degree2Radian(lonOrigin_deg);

            double phi_rad = Degree2Radian(lat0_deg);
            double lambda_rad = Degree2Radian(lon0_deg);

            double n = 1 / (2 * k_F - 1);

            double[] a = new double[6];
            a[0] = 1 + Math.Pow(n, 2) / 4 + Math.Pow(n, 4) / 64;
            a[1] = -3d / 2d * (n - Math.Pow(n, 3) / 8 - Math.Pow(n, 5) / 64);
            a[2] = 15d / 16d * (Math.Pow(n, 2) - Math.Pow(n, 4) / 4);
            a[3] = -35d / 48d * (Math.Pow(n, 3) - 5d / 16d * Math.Pow(n, 5));
            a[4] = 315d / 512d * Math.Pow(n, 4);
            a[5] = -693d / 1280d * Math.Pow(n, 5);

            double[] alpha = new double[6];
            alpha[1] = 1d / 2d * n - 2d / 3d * Math.Pow(n, 2) + 5d / 16d * Math.Pow(n, 3) + 41d / 180d * Math.Pow(n, 4) - 127d / 288d * Math.Pow(n, 5);
            alpha[2] = 13d / 48d * Math.Pow(n, 2) - 3d / 5d * Math.Pow(n, 3) + 557d / 1440d * Math.Pow(n, 4) + 281d / 630d * Math.Pow(n, 5);
            alpha[3] = 61d / 240d * Math.Pow(n, 3) - 103d / 140d * Math.Pow(n, 4) + 15061d / 26880d * Math.Pow(n, 5);
            alpha[4] = 49561d / 161280d * Math.Pow(n, 4) - 179d / 168d * Math.Pow(n, 5);
            alpha[5] = 34729d / 80640d * Math.Pow(n, 5);

            double phi0 = 0;
            for (int j = 1; j <= 5; j++)
            {
                phi0 += a[j] * Math.Sin(2 * j * phi0_rad);
            }
            phi0 = k_M0 * k_A / (1 + n) * (a[0] * phi0_rad + phi0);
            double a_ = k_M0 * k_A / (1 + n) * a[0];

            double lambda_c = Math.Cos(lambda_rad - lambda0_rad);
            double lambda_s = Math.Sin(lambda_rad - lambda0_rad);

            double t = Math.Sinh(Atanh(Math.Sin(phi_rad)) - 2 * Math.Sqrt(n) / (1 + n) * Atanh(2 * Math.Sqrt(n) / (1 + n) * Math.Sin(phi_rad)));
            double t_ = Math.Sqrt(1 + Math.Pow(t, 2));

            double xiD = Math.Atan(t / lambda_c);
            double etaD = Atanh(lambda_s / t_);

            double x = 0;
            double y = 0;
            for (int j = 1; j <= 5; j++)
            {
                x += alpha[j] * Math.Sin(2 * j * xiD) * Math.Cosh(2 * j * etaD);
                y += alpha[j] * Math.Cos(2 * j * xiD) * Math.Sinh(2 * j * etaD);
            }
            x = a_ * (xiD + x) - phi0;
            y = a_ * (etaD + y);

            return new GeoPoint()
            {
                m_X = x,
                m_Y = y,
            };
        }

        static double Degree2Radian(double degree) => degree * Math.PI / 180;
        static double Radian2Degree(double radian) => radian * 180 / Math.PI;
        static double Atanh(double x) => Math.Log((1 + x) / (1 - x)) / 2;

        public static double3 ConvertJGDToWGS(double3 coord, int geoOriginIndex)
        {
            // coord is JGD coordinates, in x, y, z
            GeoPoint geoPoint = GeoCoordinateUtility.Coordinate2LatLon(
                x0: coord.x,
                y0: coord.y,
                latOrigin_deg: PlateauToolkitMapsConstants.k_JaLatOrigins[geoOriginIndex],
                lonOrigin_deg: PlateauToolkitMapsConstants.k_JaLonOrigins[geoOriginIndex]
            );
            double lat = geoPoint.m_X;
            double lon = geoPoint.m_Y;

            double3 result = new double3(lon, lat, coord[2]);

            return result;
        }

        public static GeoPoint ConvertWGSToJGD(double3 longlatheight,  int geoOriginIndex)
        {
            Debug.Log("long: " + longlatheight.x + ", lat: " + longlatheight.y + ",index: " + geoOriginIndex);
            return GeoCoordinateUtility.LatLon2Coordinate(
                lat0_deg: longlatheight.y, // we ignore the height (z).
                lon0_deg: longlatheight.x,
                latOrigin_deg: PlateauToolkitMapsConstants.k_JaLatOrigins[geoOriginIndex],
                lonOrigin_deg: PlateauToolkitMapsConstants.k_JaLonOrigins[geoOriginIndex]
            );
        }
    }

    /// <summary>
    /// The struct to define the JGD planar coordinates
    /// </summary>
    public struct GeoPoint
    {
        public double m_X;
        public double m_Y;

        public GeoPoint(double x, double y)
        {
            m_X = x;
            m_Y = y;
        }
    }
}