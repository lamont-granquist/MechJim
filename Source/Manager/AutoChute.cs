using UnityEngine;
using KSP.UI.Screens;
using MechJim.Extensions;
using System.Collections.Generic;

namespace MechJim.Manager {
    public class AutoChute: ManagerBase {
        public double deploy_altitude { get; set; }

        public AutoChute(Core core): base(core) {
            deploy_altitude = 2000;
        }

        public void DeployChutes() {
            foreach(var pair in vesselState.parachutes) {
                Part p = pair.Key;

                List<ModuleParachute> mlist = p.Modules.GetModules<ModuleParachute>();
                for(int i = 0; i < mlist.Count; i++) {
                    ModuleParachute chute = mlist[i];
                    if (chute.deploymentState == ModuleParachute.deploymentStates.STOWED && chute.deploymentSafeState == ModuleParachute.deploymentSafeStates.SAFE)
                        chute.Deploy();
                }
            }
        }

        public override void OnFixedUpdate() {
            if (vessel.altitude > mainBody.RealMaxAtmosphereAltitude())
                return;
            if (vessel.radarAltitude > deploy_altitude)
                return;
            if (!vesselState.parachuteDeployable)
                return;
            /* must be falling by at least 5m/s */
            if (Vector3d.Dot(orbit.SwappedOrbitalVelocityAtUT(vesselState.time), orbit.Up(vesselState.time)) > -5)
                return;
            DeployChutes();
        }
    }
}
