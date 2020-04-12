using System;
using System.Collections.Generic;
using System.Globalization;

namespace wdWorker
{
    public class Calculator
    {
        public double Ra { get; set; }
        public double Dec { get; set; }
        public double Orientation { get; set; }
        public double Radius { get; set; }
        public int Parity { get; set; }
        public double Pixscale { get; set; }
        public int Imagew { get; set; }
        public int Imageh { get; set; }
        private Dictionary<string, double> Fh { get; set; }

        private double deg2rad(double a)
        {
            return a * Math.PI / 180.0;
        }
        private double rad2deg(double a)
        {
            return a * 180.0 / Math.PI;
        }
        private void calc_distortion(double u, double v, out double U, out double V)
        {
            // Do SIP distortion (in relative pixel coordinates)
            // See the sip_t struct definition in header file for details
            int p, q;
            double fuv = 0.0;
            double guv = 0.0;

            // avoid using pow() function
            double[] powu = new double[10];
            double[] powv = new double[10];
            powu[0] = 1.0;
            powu[1] = u;
            powv[0] = 1.0;
            powv[1] = v;
            for (p = 2; p <= (int)Math.Max(Fh["A_ORDER"], Fh["B_ORDER"]); p++)
            {
                powu[p] = powu[p - 1] * u;
                powv[p] = powv[p - 1] * v;
            }

            for (p = 0; p <= (int)Fh["A_ORDER"]; p++)
                for (q = 0; q <= (int)Fh["A_ORDER"]; q++)
                    // We include all terms, even the constant and linear ones; the standard
                    // isn't clear on whether these are allowed or not.
                    if (p + q <= (int)Fh["A_ORDER"])
                    {
                        double tm;
                        if (Fh.ContainsKey("A_" + p.ToString() + "_" + q.ToString())) tm = Fh["A_" + p.ToString() + "_" + q.ToString()]; else tm = 0.0;
                        fuv += tm * powu[p] * powv[q];
                    }
            for (p = 0; p <= (int)Fh["B_ORDER"]; p++)
                for (q = 0; q <= (int)Fh["B_ORDER"]; q++)
                    if (p + q <= (int)Fh["B_ORDER"])
                    {
                        double tm;
                        if (Fh.ContainsKey("B_" + p.ToString() + "_" + q.ToString())) tm = Fh["B_" + p.ToString() + "_" + q.ToString()]; else tm = 0.0;
                        guv += tm * powu[p] * powv[q];
                    }

            U = u + fuv;
            V = v + guv;

        }

        public bool Do(string fits)
        {
            Fh = new Dictionary<string, double>();
            CultureInfo culture = new CultureInfo("en-US");
            string[] lines = fits.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var l in lines)
            {
                if (l.StartsWith("HISTORY")) continue;
                if (l.StartsWith("COMMENT")) continue;
                if (!l.Contains('=')) continue;
                string[] i = l.Split('/');
                string[] a = i[0].Split('=');
                double d;
                try
                {
                    d = Convert.ToDouble(a[1].Trim(), culture);
                }
                catch (FormatException)
                {
                    continue;
                }
                Fh.Add(a[0].Trim(), d);
            }

            
            try
            {
                Imagew = Convert.ToInt32(Fh["IMAGEW"]);
                Imageh = Convert.ToInt32(Fh["IMAGEH"]);
            }
            catch (FormatException)
            {
                return false;
            }
            double px = (double)Imagew / 2.0 + 0.5;
            double py = (double)Imageh / 2.0 + 0.5;
            double U, V;
            calc_distortion(px, py, out U, out V);
            //U = px; V = py;
            double crpix0, crpix1;
            try
            {
                crpix0 = Convert.ToDouble(Fh["CRPIX1"]);
                crpix1 = Convert.ToDouble(Fh["CRPIX2"]);
            }
            catch (FormatException)
            {
                return false;
            }
            double u = U - crpix0;
            double v = V - crpix1;
            double cd00, cd01, cd10, cd11;
            try
            {
                cd00 = Convert.ToDouble(Fh["CD1_1"]);
                cd01 = Convert.ToDouble(Fh["CD1_2"]);
                cd10 = Convert.ToDouble(Fh["CD2_1"]);
                cd11 = Convert.ToDouble(Fh["CD2_2"]);
            }
            catch (FormatException)
            {
                return false;
            }
            double x = cd00 * u + cd01 * v;
            double y = cd10 * u + cd11 * v;


            x = deg2rad(x);
            y = deg2rad(y);
            double rx, ry, rz;
            double ix, iy, norm;
            double jx, jy, jz;

            double crval0, crval1;
            try
            {
                crval0 = Convert.ToDouble(Fh["CRVAL1"]);
                crval1 = Convert.ToDouble(Fh["CRVAL2"]);
            }
            catch (FormatException)
            {
                return false;
            }
            double cr0 = deg2rad(crval0);
            double cr1 = deg2rad(crval1);
            rx = Math.Cos(cr1) * Math.Cos(cr0);
            ry = Math.Cos(cr1) * Math.Sin(cr0);
            rz = Math.Sin(cr1);
            if (rz == 1.0)
            {
                // North pole
                ix = -1.0;
                iy = 0.0;
            }
            else if (rz == -1.0)
            {
                // South pole
                ix = -1.0;
                iy = 0.0;
            }
            else
            {
                // Form i = r cross north pole (0,0,1)
                ix = ry;
                iy = -rx;
                // iz = 0
                norm = Math.Sqrt(ix * ix + iy * iy);
                ix /= norm;
                iy /= norm;
            }
            jx = iy * rz;
            jy = -ix * rz;
            jz = ix * ry - iy * rx;

            double jnorm = Math.Sqrt(jx * jx + jy * jy + jz * jz);
            jx /= jnorm;
            jy /= jnorm;
            jz /= jnorm;

            double rfrac = Math.Sqrt(1.0 - (x * x + y * y));

            double tx = ix * x + jx * y + rx * rfrac;
            double ty = jy * x + jy * y + ry * rfrac;
            double tz = jz * y + rz * rfrac; // iz = 0
            // END TAN_IWC2XYZARR
            double ta = Math.Atan2(ty, tx);
            if (ta < 0) ta += 2.0 * Math.PI;
            Ra = rad2deg(ta);
            Dec = rad2deg(Math.Asin(tz));
          
            double det = cd00 * cd11 - cd01 * cd10;
            Parity = (det < 0) ? -1 : 1;
            Pixscale = 3600.0 * Math.Sqrt(Math.Abs(det));
            Radius = Pixscale * Math.Sqrt(Imagew * Imagew + Imageh * Imageh) / 2.0 / 3600.0;
            double T = Parity * cd00 + cd11;
            double A = Parity * cd10 - cd01;
            Orientation = -rad2deg(Math.Atan2(A, T));

            return true;
        }
    }
}
