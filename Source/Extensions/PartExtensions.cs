using System;
using UnityEngine;
using System.Collections.Generic;

namespace MechJim.Extensions {
    public static class PartExtensions {
        public static bool IsFairing(this Part p) {
            return p.Modules.Contains("ProceduralFairingDecoupler") || p.Modules.Contains("ModuleProceduralFairing");
        }

        public static void SendMethodToModuleNamed(this Part p, string method, string module) {
            if (p.Modules.Contains(module)) {
                PartModule m = p.Modules[module];
                m.GetType().GetMethod(method).Invoke(m, null);
            }
        }

        public static void DeployFairing(this Part p) {
            p.SendMethodToModuleNamed("ModuleProceduralFairing", "DeployFairing");  // stock and KW fairings
            p.SendMethodToModuleNamed("ProceduralFairingDecoupler", "Jettison");    // proc fairings
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
