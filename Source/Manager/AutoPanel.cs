using UnityEngine;
using KSP.UI.Screens;
using MechJim.Extensions;

namespace MechJim.Manager {
    public class AutoPanel: ManagerBase {
        /* TODO:
           - accept Extend or Retract messages from other modules to override this logic
           - on the next state state change go back to auto-managing the panels
           - e.g. send this module a 'retract' message before launch, then have it auto-extend at the atmosphere edge
           */

        public enum PanelState {
            EXTEND,
            RETRACT,
        }

        public PanelState state { get; set; }

        public AutoPanel(Core core): base(core) { }

        public void RetractAll() {
            for (int i = 0; i < vessel.parts.Count; i++) {
                Part p = vessel.parts[i];
                if (p.IsSolarPanel() && !p.ShieldedFromAirstream)
                    p.RetractPanel();
            }
        }

        public void ExtendAll() {
            for (int i = 0; i < vessel.parts.Count; i++) {
                Part p = vessel.parts[i];
                if (p.IsSolarPanel() && !p.ShieldedFromAirstream)
                    p.ExtendPanel();
            }
        }

        private bool ShouldOpenSolarPanels() {
            if (!mainBody.atmosphere)
                return true;

            if (!vessel.LiftedOff())
                return false;

            if (vessel.LandedOrSplashed)
                return false;

            if (vessel.altitude > mainBody.RealMaxAtmosphereAltitude())
                return true;

            return false;
        }

        public override void OnFixedUpdate() {
            if (ShouldOpenSolarPanels()) {
                if (state != PanelState.EXTEND) {
                    state = PanelState.EXTEND;
                    ExtendAll();
                }
            } else {
                if (state != PanelState.RETRACT) {
                    state = PanelState.RETRACT;
                    RetractAll();
                }
            }
        }
    }
}
