using System;
using UnityEngine;

namespace MechJim.Extensions {
    public static class PartExtensions {
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
