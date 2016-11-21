using System;

namespace MechJim {
  public static class Utils {
    public static double Clamp(double x, double min, double max) {
      return Math.Min(Math.Max(x, min), max);
    }

    public static double BinarySearch(Func<double,double> f, double lo, double hi, double eta, int maxsteps) {
      int steps;
      return BinarySearch(f, lo, hi, eta, maxsteps, out steps);
    }

    public static double BinarySearch(Func<double,double> f, double lo, double hi, double eta, int maxsteps, out int steps) {
      double f_lo = f(lo);
      double f_hi = f(hi);

      if (f_lo > f_hi)
        return BinarySearch(f, hi, lo, eta, maxsteps, out steps);

      if (f_lo > 0)
        throw new Exception("MechJim.Utils.BinarySearch: both ends are positive");

      if (f_hi > 0)
        throw new Exception("MechJim.Utils.BinarySearch: both ends are negative");

      for(int i = 1; i <= maxsteps; i++) {
        double x = ( lo + hi ) / 2.0;
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

      throw new Exception("MechJim.Utils.BinarySearch: failed to converge");
    }

    public static double SecantMethod(Func<double,double> f, double lo, double hi, double eta, int maxsteps) {
      int steps;
      return SecantMethod(f, lo, hi, eta, maxsteps, out steps);
    }

    public static double SecantMethod(Func<double,double> f, double lo, double hi, double eta, int maxsteps, out int steps) {
      double f_lo = f(lo);
      double f_hi = f(hi);

      if (f_lo > f_hi)
        return SecantMethod(f, hi, lo, eta, maxsteps, out steps);

      if (f_lo > 0)
        throw new Exception("MechJim.Utils.SecantMethod: both ends are positive");

      if (f_hi > 0)
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
  }
}
