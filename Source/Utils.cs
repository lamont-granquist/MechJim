using System;

namespace MechJim {
  public static class Utils {
    public static double Clamp(double x, double min, double max) {
      return Math.Min(Math.Max(x, min), max);
    }

    //keeps angles in the range 0 to 360
    public static double ClampDegrees360(double angle)
    {
      angle = angle % 360.0;
      if (angle < 0) return angle + 360.0;
      else return angle;
    }

    //keeps angles in the range -180 to 180
    public static double ClampDegrees180(double angle)
    {
      angle = ClampDegrees360(angle);
      if (angle > 180) angle -= 360;
      return angle;
    }

    public static double ClampRadiansTwoPi(double angle)
    {
      angle = angle % (2 * Math.PI);
      if (angle < 0) return angle + 2 * Math.PI;
      else return angle;
    }

    public static double ClampRadiansPi(double angle)
    {
      angle = ClampRadiansTwoPi(angle);
      if (angle > Math.PI) angle -= 2 * Math.PI;
      return angle;
    }

    public static double Asinh(double x) {
      return Math.Log(x + Math.Sqrt(x * x + 1));
    }

    public static double Acosh(double x) {
      return Math.Log(x + Math.Sqrt(x * x - 1));
    }

    public static double Atanh(double x) {
      return 0.5 * (Math.Log(1 + x) - Math.Log(1 - x));
    }

    public static double BinarySearch(Func<double,double> f, Func<double,double,bool> cmp, double lo, double hi, double target, double eta, int maxsteps, out int steps) {
      double f_lo = f(lo);
      double f_hi = f(hi);

      if (cmp(f_lo,f_hi))
        return BinarySearch(f, cmp, hi, lo, target, eta, maxsteps, out steps);

      for(int i = 1; i <= maxsteps; i++) {
        double x = ( lo + hi ) / 2.0;
        double f_x = f(x);

        if (Math.Abs(f_x - target) < eta) {
          steps = i;
          return x;
        }

        if (cmp(target, f_x)) {
          lo = x;
          f_lo = f_x;
        } else {
          hi = x;
          f_hi = f_x;
        }
      }

      throw new Exception("MechJim.Utils.BinarySearch: failed to converge");
    }

    public static double SecantMethod(Func<double,double> f, double lo, double hi, double eta, int maxsteps, out int steps) {
      double f_lo = f(lo);
      double f_hi = f(hi);

      if (f_lo > f_hi)
        return SecantMethod(f, hi, lo, eta, maxsteps, out steps);

      if (f_lo > 0)
        throw new Exception("MechJim.Utils.SecantMethod: both ends are positive");

      if (f_hi < 0)
        throw new Exception("MechJim.Utils.SecantMethod: both ends are negative");

      for(int i = 1; i <= maxsteps; i++) {
        double x = lo - f_lo * ( lo - hi ) / ( f_lo - f_hi );
        double f_x = f(x);

        if (Math.Abs(f_x) < eta) {
          steps = i;
          return x;
        }

        if (f_x < 0) {
          lo = x;
          f_lo = f_x;
        } else {
          hi = x;
          f_hi = f_x;
        }
      }

      throw new Exception("MechJim.Utils.SecantMethod: failed to converge");
    }

    public static double HeadingForInclination(double inclinationDegrees, double latitudeDegrees) {
      double cosDesiredSurfaceAngle = Math.Cos(inclinationDegrees * Math.PI / 180) / Math.Cos(latitudeDegrees * Math.PI / 180);
      if (Math.Abs(cosDesiredSurfaceAngle) > 1.0) {
        //If inclination < latitude, we get this case: the desired inclination is impossible
        if (Math.Abs(Utils.ClampDegrees180(inclinationDegrees)) < 90)
          return 90;
        else
          return 270;
      } else {
        double angleFromEast = (180 / Math.PI) * Math.Acos(cosDesiredSurfaceAngle); //an angle between 0 and 180
        if (inclinationDegrees < 0)
          angleFromEast *= -1;
        //now angleFromEast is between -180 and 180

        return ClampDegrees360(90 - angleFromEast);
      }
    }
  }
}
