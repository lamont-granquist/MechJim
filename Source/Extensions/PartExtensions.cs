using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace MechJim.Extensions {
    public static class PartExtensions {
        public static bool IsFairing(this Part p) {
            return p.Modules.Contains("ProceduralFairingDecoupler") || p.Modules.Contains("ModuleProceduralFairing");
        }

        public static void DeployFairing(this Part p) {
            p.SendMethodToModuleNamed("DeployFairing", "ModuleProceduralFairing");  // stock and KW fairings
            p.SendMethodToModuleNamed("Jettison", "ProceduralFairingDecoupler");    // proc fairings
        }

        public static bool IsSolarPanel(this Part p) {
            return p.Modules.Contains("ModuleDeployableSolarPanel");
        }

        public static void ExtendPanel(this Part p) {
            p.SendMethodToModuleNamed("Extend", "ModuleDeployableSolarPanel");
        }

        public static void RetractPanel(this Part p) {
            p.SendMethodToModuleNamed("Retract", "ModuleDeployableSolarPanel");
        }

        public static void SendMethodToModuleNamed(this Part p, string method, string module) {
            if (p.Modules.Contains(module)) {
                PartModule pm = p.Modules[module];
                MethodInfo mi = pm.GetType().GetMethod(method);
                if (mi != null) mi.Invoke(pm, null);
            }
        }

        public static bool IsEngine(this Part p) {
            return p.Modules.Contains("ModuleEngines");
        }

        public static bool EngineHasFuel(this Part p) {
            ModuleEngines m = (ModuleEngines)p.Modules["ModuleEngines"];
            return !m.getFlameoutState;
        }
    }
}
