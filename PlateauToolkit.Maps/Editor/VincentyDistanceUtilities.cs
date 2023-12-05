using System;

namespace PlateauToolkit.Maps
{
    public static class VincentyDistanceUtilities
    {
        const double a = 6378137;
        const double b = 6356752.314245;
        const double f = 1 / 298.257223563;

        public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2, double threshold)
        {
            double L = ToRad(lon2 - lon1);
            double U1 = Math.Atan((1 - f) * Math.Tan(ToRad(lat1)));
            double U2 = Math.Atan((1 - f) * Math.Tan(ToRad(lat2)));

            double sinU1 = Math.Sin(U1), cosU1 = Math.Cos(U1);
            double sinU2 = Math.Sin(U2), cosU2 = Math.Cos(U2);

            double lambda = L;
            double lambdaP, iterLimit = 100;

            double sinLambda, cosLambda, sinSigma, cosSigma, sigma, sinAlpha, cosSqAlpha, cos2SigmaM;
            do
            {
                sinLambda = Math.Sin(lambda);
                cosLambda = Math.Cos(lambda);
                sinSigma = Math.Sqrt(cosU2 * sinLambda * (cosU2 * sinLambda) +
                    (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda) * (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda));

                if (sinSigma == 0)
                {
                    return 0;
                }

                cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
                sigma = Math.Atan2(sinSigma, cosSigma);
                sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
                cosSqAlpha = 1 - sinAlpha * sinAlpha;
                cos2SigmaM = cosSigma - 2 * sinU1 * sinU2 / cosSqAlpha;

                if (double.IsNaN(cos2SigmaM))
                {
                    cos2SigmaM = 0;
                }

                double C = f / 16 * cosSqAlpha * (4 + f * (4 - 3 * cosSqAlpha));
                lambdaP = lambda;
                lambda = L + (1 - C) * f * sinAlpha *
                    (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));
            }
            while (Math.Abs(lambda - lambdaP) > threshold && --iterLimit > 0);

            if (iterLimit == 0)
            {
                return double.NaN;
            }

            double uSq = cosSqAlpha * (a * a - b * b) / (b * b);
            double A = 1 + uSq / 16384 * (4096 + uSq * (-768 + uSq * (320 - 175 * uSq)));
            double B = uSq / 1024 * (256 + uSq * (-128 + uSq * (74 - 47 * uSq)));
            double deltaSigma = B * sinSigma * (cos2SigmaM + B / 4 * (cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM) -
                B / 6 * cos2SigmaM * (-3 + 4 * sinSigma * sinSigma) * (-3 + 4 * cos2SigmaM * cos2SigmaM)));
            double s = b * A * (sigma - deltaSigma);

            return s;
        }

        public static double ToRad(double angle)
        {
            return Math.PI * angle / 180.0;
        }
    }
}