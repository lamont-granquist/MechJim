using System;
using UnityEngine;
using System.Collections.Generic;

namespace MechJim.Extensions {
    public static class PartExtensions {
        public static bool IsFairing(this Part p) {
            return p.Modules.Contains("ProceduralFairingDecoupler") || p.Modules.Contains("ModuleProceduralFairing");
        }

        public static void DeployFairing(this Part p) {
            if (p.Modules.Contains("ModuleProceduralFairing")) // stock and KW fairings
                p.Modules.Send("DeployFairing");
            if (p.Modules.Contains("ProceduralFairingDecoupler"))  // proc fairings
                p.Modules.Send("Jettison");
        }

        public static bool IsEngine(this Part p) {
            for (int i = 0; i < p.Modules.Count; i++) {
                PartModule m = p.Modules[i];
                if (m is ModuleEngines) return true;
            }
            return false;
        }

        public static bool EngineHasFuel(this Part p) {
            for (int i = 0; i < p.Modules.Count; i++) {
                PartModule m = p.Modules[i];
                ModuleEngines eng = m as ModuleEngines;
                if (eng != null) return !eng.getFlameoutState;
            }
            return false;
        }
    }
}
