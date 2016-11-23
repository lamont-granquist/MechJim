using MechJim.Extensions;

namespace MechJim.Maneuver {
    public abstract class ManeuverBase {
        public ManeuverBase(Vessel vessel, Orbit o, double UT) {
            this.vessel = vessel;
            this.o = o;
            this.UT = UT;
        }

        public Vessel vessel { get; set; }
        public Orbit o { get; set; }
        public double UT { get; set; }

        public abstract Vector3d DeltaV();

        public ManeuverNode PlaceManeuverNode() {
            return vessel.PlaceManeuverNode(o, DeltaV(), UT);
        }
    }
}
