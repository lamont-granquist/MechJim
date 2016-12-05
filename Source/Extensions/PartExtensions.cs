using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace MechJim.Extensions {
    public static class PartExtensions {
        /* fairing */

        public static bool IsFairing(this Part p) {
            return p.Modules.Contains("ProceduralFairingDecoupler") || p.Modules.Contains("ModuleProceduralFairing");
        }

        public static void DeployFairing(this Part p) {
            p.SendMethodToModuleNamed("DeployFairing", "ModuleProceduralFairing");  // stock and KW fairings
            p.SendMethodToModuleNamed("Jettison", "ProceduralFairingDecoupler");    // proc fairings
        }

        /* panel */

        public static bool IsSolarPanel(this Part p) {
            return p.Modules.Contains("ModuleDeployableSolarPanel");
        }

        public static void ExtendPanel(this Part p) {
            p.SendMethodToModuleNamed("Extend", "ModuleDeployableSolarPanel");
        }

        public static void RetractPanel(this Part p) {
            p.SendMethodToModuleNamed("Retract", "ModuleDeployableSolarPanel");
        }

        /* engine */

        public static bool IsEngine(this Part p) {
            return p.Modules.Contains("ModuleEngines");
        }

        public static bool EngineHasFuel(this Part p) {
            ModuleEngines m = (ModuleEngines)p.Modules["ModuleEngines"];
            return !m.getFlameoutState;
        }

        public static bool IsReactionWheel(this Part p) {
            return p.Modules.Contains("ModuleReactionWheel");
        }

        public static bool IsRCS(this Part p) {
            return p.Modules.Contains("ModuleRCS");
        }

        public static bool IsControlSurface(this Part p) {
            return p.Modules.Contains("ModuleControlSurface");
        }

        public static bool IsOtherTorque(this Part p) {
            return p.Modules.Contains("ITorqueProvider") && !p.IsRCS() && !p.IsControlSurface() && !p.IsReactionWheel() && !p.IsEngine();
        }

        public static void SendMethodToModuleNamed(this Part p, string method, string module) {
            if (p.Modules.Contains(module)) {
                PartModule pm = p.Modules[module];
                MethodInfo mi = pm.GetType().GetMethod(method);
                if (mi != null) mi.Invoke(pm, null);
            }
        }

        public static bool IsUnfiredDecoupler(this Part p)
        {
            for (int i = 0; i < p.Modules.Count; i++)
            {
                PartModule m = p.Modules[i];
                ModuleDecouple mDecouple = m as ModuleDecouple;
                if (mDecouple != null)
                {
                    if (!mDecouple.isDecoupled && mDecouple.stagingEnabled && p.stagingOn) return true;
                    break;
                }

                ModuleAnchoredDecoupler mAnchoredDecoupler = m as ModuleAnchoredDecoupler;
                if (mAnchoredDecoupler != null)
                {
                    if (!mAnchoredDecoupler.isDecoupled && mAnchoredDecoupler.stagingEnabled && p.stagingOn) return true;
                    break;
                }

                ModuleDockingNode mDockingNode = m as ModuleDockingNode;
                if (mDockingNode != null)
                {
                    if (mDockingNode.staged && mDockingNode.stagingEnabled  && p.stagingOn) return true;
                    break;
                }

                if (m.moduleName == "ProceduralFairingDecoupler")
                {
                    if (!m.Fields["decoupled"].GetValue<bool>(m) && p.stagingOn) return true;
                    break;
                }
            }
            return false;
        }
    }
}
