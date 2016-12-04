using UnityEngine;
using KSP.UI.Screens;
using MechJim.Extensions;

namespace MechJim.Manager {
    public class AutoStage: ManagerBase {
        public int maxstage { get; set; }

        public AutoStage(Core core): base(core) {
        }

        public bool FlamedOutEngines() {
            for (int i = 0; i < vessel.parts.Count; i++) {
                Part p = vessel.parts[i];
                if (p.IsEngine() && !p.EngineHasFuel())
                    return true;
            }
            return false;
        }

        public override void OnFixedUpdate() {
            /* FIXME: copy MJs logic that walks the parts tree and makes sure not to drop firing engines or draining tanks */
            if (!StageManager.CanSeparate)
                return;

            if (StageManager.CurrentStage <= maxstage)
                return;

            if (vesselState.thrustMaximum == 0.0) {
                Debug.Log("activating next stage");
                StageManager.ActivateNextStage();
            }

            if (FlamedOutEngines()) {
                Debug.Log("activating next stage");
                StageManager.ActivateNextStage();
            }
        }
    }
}
