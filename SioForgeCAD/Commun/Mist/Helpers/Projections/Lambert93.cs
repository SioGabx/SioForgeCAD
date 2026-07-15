using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SioForgeCAD.Commun.Mist.Helpers.Projections
{

    internal class Lambert93
    {
        private static readonly Dictionary<int, Lambert93> projections = new Dictionary<int, Lambert93>()
        {
            {2154, new Lambert93(44, 49, 46.5, 3, 700000, 6600000)}, //Lambert 93
            {3942, new Lambert93(41.25, 42.75, 42, 3, 1700000, 1200000)},//CC42 EPSG:3942
            {3943, new Lambert93( 42.25, 43.75, 43, 3, 1700000, 2200000) }, //CC43 EPSG:3943
            {3944, new Lambert93(43.25, 44.75, 44, 3, 1700000, 3200000) }, //CC44 EPSG:3944
            {3945, new Lambert93(44.25, 45.75, 45, 3, 1700000, 4200000) }, //CC45 EPSG:3945
            {3946, new Lambert93(45.25, 46.75, 46, 3, 1700000, 5200000) }, //CC46 EPSG:3946
            {3947, new Lambert93(46.25, 47.75, 47, 3, 1700000, 6200000) }, //CC47 EPSG:3947
            {3948, new Lambert93(47.25, 48.75, 48, 3, 1700000, 7200000) }, //CC48 EPSG:3948
            {3949, new Lambert93(48.25, 49.75, 49, 3, 1700000, 8200000) }, //CC49 EPSG:3949
            {3950, new Lambert93(49.25, 50.75, 50, 3, 1700000, 9200000) } //CC50 EPSG:3950
        };


        public static Lambert93 Get(int epsg)
        {
            if (!projections.TryGetValue(epsg, out var projection))
            {
                throw new ArgumentException($"Projection EPSG:{epsg} non supportée");
            }

            return projection;
        }

        // GRS80
        private const double a = 6378137.0;
        private const double e = 0.0818191910428158;

        private readonly double n;
        private readonly double C;
        private readonly double Xs;
        private readonly double Ys;
        private readonly double lon0;

        public Lambert93(double lat1, double lat2, double lat0, double lon0, double falseEasting, double falseNorthing)
        {
            double phi1 = DegToRad(lat1);
            double phi2 = DegToRad(lat2);
            double phi0 = DegToRad(lat0);
            this.lon0 = DegToRad(lon0);
            double m1 = Math.Cos(phi1) / Math.Sqrt(1 - (e * e * Math.Pow(Math.Sin(phi1), 2)));
            double m2 = Math.Cos(phi2) / Math.Sqrt(1 - (e * e * Math.Pow(Math.Sin(phi2), 2)));
            double t1 = LatitudeToIso(phi1);
            double t2 = LatitudeToIso(phi2);
            double t0 = LatitudeToIso(phi0);
            n = Math.Log(m1 / m2) / Math.Log(t1 / t2);
            C = m1 / (n * Math.Pow(t1, n));
            double rho0 = a * C * Math.Pow(t0, n);
            Xs = falseEasting;
            Ys = falseNorthing + rho0;
        }

        public (double X, double Y) Forward(double lonDeg, double latDeg)
        {
            double lon = DegToRad(lonDeg);
            double lat = DegToRad(latDeg);
            double t = LatitudeToIso(lat);
            double rho = a * C * Math.Pow(t, n);
            double theta = n * (lon - lon0);
            double X = Xs + (rho * Math.Sin(theta));
            double Y = Ys - (rho * Math.Cos(theta));
            return (X, Y);
        }

        public (double Lon, double Lat) Inverse(double X, double Y)
        {
            double dx = X - Xs;
            double dy = Ys - Y;
            double rho = Math.Sign(n) * Math.Sqrt((dx * dx) + (dy * dy));
            double theta = Math.Atan2(dx, dy);

            if (n < 0)
            {
                theta -= Math.PI;
            }

            double t = Math.Pow(rho / (a * C), 1.0 / n);
            double lon = lon0 + (theta / n);
            double lat = IsoToLatitude(t);

            return (NormalizeLongitude(RadToDeg(lon)), RadToDeg(lat));
        }

        private static double LatitudeToIso(double phi)
        {
            return Math.Tan((Math.PI / 4) - (phi / 2)) / Math.Pow((1 - (e * Math.Sin(phi))) / (1 + (e * Math.Sin(phi))), e / 2);
        }

        private static double IsoToLatitude(double t)
        {
            double phi = (Math.PI / 2) - (2 * Math.Atan(t));

            for (int i = 0; i < 20; i++)
            {
                double es = e * Math.Sin(phi);
                double next = (Math.PI / 2) - (2 * Math.Atan(t * Math.Pow((1 - es) / (1 + es), e / 2)));

                if (Math.Abs(next - phi) < 1e-12)
                {
                    break;
                }

                phi = next;
            }

            return phi;
        }

        private static double NormalizeLongitude(double lon)
        {
            while (lon < -180)
            {
                lon += 360;
            }

            while (lon > 180)
            {
                lon -= 360;
            }

            return lon;
        }

        static double DegToRad(double d) => d * Math.PI / 180.0;
        static double RadToDeg(double r) => r * 180.0 / Math.PI;
    }


    internal static class LambertProjectionTest
    {
        public static void Run()
        {
            TestAllCCRoundTrip();
            TestAllCCForwardReference();
        }


        private static void TestAllCCRoundTrip()
        {
            double lon = 6.1844;
            double lat = 48.6921;
            int[] epsgs =
        {
                3942,
                3943,
                3944,
                3945,
                3946,
                3947,
                3948,
                3949,
                3950
            };

            foreach (var epsg in epsgs)
            {
                var cc = Lambert93.Get(epsg);

                var xy = cc.Forward(lon, lat);
                var geo = cc.Inverse(xy.X, xy.Y);

                double errLon = Math.Abs(lon - geo.Lon);
                double errLat = Math.Abs(lat - geo.Lat);

                Debug.WriteLine(
                    $"EPSG:{epsg} " +
                    $"LonErr={errLon:E} " +
                    $"LatErr={errLat:E}");
            }
        }

        private static void TestAllCCForwardReference()
        {
            Debug.WriteLine("=== TEST FORWARD CC42-CC50 REFERENCES ===");

            var tests = new[]
            {
                new { Epsg = 3942, Lon = 5.3698, Lat = 43.2965, X = 1892312.362, Y = 1346684.277 },
                new { Epsg = 3943, Lon = 4.8357, Lat = 45.7640, X = 1842947.291, Y = 2508790.466 },
                new { Epsg = 3944, Lon = 3.0573, Lat = 45.7800, X = 1704457.987, Y = 3397827.275 },
                new { Epsg = 3945, Lon = 2.3522, Lat = 46.6034, X = 1650352.786, Y = 4378420.339 },
                new { Epsg = 3946, Lon = 3.8772, Lat = 46.5640, X = 1767253.233, Y = 5263058.447 },
                new { Epsg = 3947, Lon = 2.6500, Lat = 47.4784, X = 1673620.366, Y = 6253241.358 },
                new { Epsg = 3948, Lon = 1.9093, Lat = 48.6921, X = 1619704.448, Y = 7277522.747 },
                new { Epsg = 3949, Lon = 6.1844, Lat = 48.6921, X = 1934355.775, Y = 8170678.140 },
                new { Epsg = 3950, Lon = 2.3339, Lat = 50.9726, X = 1653213.553, Y = 9308404.854 }
            };


            foreach (var t in tests)
            {
                var projection = Lambert93.Get(t.Epsg);
                var result = projection.Forward(t.Lon, t.Lat);
                double errX = result.X - t.X;
                double errY = result.Y - t.Y;
                double distance = Math.Sqrt((errX * errX) + (errY * errY));
                Debug.WriteLine($"EPSG:{t.Epsg}");
                Debug.WriteLine($"Calcul  X={result.X:0.000}  Y={result.Y:0.000}");
                Debug.WriteLine($"Réf     X={t.X:0.000}  Y={t.Y:0.000}");
                Debug.WriteLine($"Erreur X={errX:0.000000} m");
                Debug.WriteLine($"Erreur Y={errY:0.000000} m");
                Debug.WriteLine($"Erreur planimétrique={distance:0.000000} m");

                if (distance < 0.001)
                {
                    Debug.WriteLine("RESULTAT : OK");
                }
                else
                {
                    Debug.WriteLine("RESULTAT : ECHEC");
                }
            }
        }
    }
}