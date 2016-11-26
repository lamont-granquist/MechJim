/* portions plagiarized from MechJeb */

using System;

namespace MechJim.Extensions {
    public static class VesselExtensions {
        public static ManeuverNode PlaceManeuverNode(this Vessel vessel, Orbit patch, Vector3d dV, double UT) {
            for (int i = 0; i < 3; i++) {
                if (double.IsNaN(dV[i]) || double.IsInfinity(dV[i])) {
                    throw new Exception("MechJim.Extensions.VesselExtension.PlaceManeuverNode: bad dV: " + dV);
                }
            }

            if (double.IsNaN(UT) || double.IsInfinity(UT)) {
                throw new Exception("MechJim.Extensions.VesselExtension.PlaceManeuverNode: bad UT: " + UT);
            }

            /* never place nodes in the past */
            UT = Math.Max(UT, Planetarium.GetUniversalTime());

            Vector3d nodeDV = patch.DeltaVToManeuverNodeCoordinates(UT, dV);
            ManeuverNode mn = vessel.patchedConicSolver.AddManeuverNode(UT);
            mn.OnGizmoUpdated(nodeDV, UT);
            return mn;
        }

        public static Vector3d forward(this Vessel vessel) {
            return vessel.GetTransform().up;
        }
    }
}
