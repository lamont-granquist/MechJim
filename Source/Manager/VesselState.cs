using System;
using UnityEngine;

namespace MechJim.Manager {
    public class VesselState: ManagerBase {
        public VesselState(Core core): base(core) {
            enabled = true;
        }

        public double mass { get { return vessel.totalMass; } }
        public double time { get { return Planetarium.GetUniversalTime(); } }

        public Vector3d CoMD { get { return vessel.CoMD; } }
        public Vector3d orbitalVelocity { get { return vessel.obt_velocity; } }
        public Vector3d surfaceVelocity { get { return vessel.srf_velocity; } }

        public Vector3d up { get { return (CoMD - mainBody.position).normalized; } }
        public Vector3d north { get { return vessel.north; } }
        public Vector3d east { get { return vessel.east; } }
        public Vector3d forward { get { return vessel.GetTransform().up; } }

        public Quaternion rotationSurface;
        public Quaternion rotationVesselSurface;

        public Vector3d velocityMainBodySurface;


        public double rocketAoA;
        /* public double planeAoA;
        public double planeAoS; */

        public Vector3d radialPlus;   //unit vector in the plane of up and velocityVesselOrbit and perpendicular to velocityVesselOrbit
        public Vector3d radialPlusSurface; //unit vector in the plane of up and velocityVesselSurface and perpendicular to velocityVesselSurface
        public Vector3d normalPlus;    //unit vector perpendicular to up and velocityVesselOrbit
        public Vector3d normalPlusSurface;  //unit vector perpendicular to up and velocityVesselSurface

        public Vector3d angularVelocity;

        public double altitude { get { return vessel.altitude; } }
        public double altitude1;      // altitude after deltatime

        public double atmPressure;    // pressure in atm
        public double atmPressure1;   // pressure in atm after deltatime

        public double thrustMaximum;
        public double thrustMinimum;
        public double thrustCurrent;

        public double totalVe;

        public override void OnFixedUpdate() {
            rotationSurface = Quaternion.LookRotation(north, up);
            rotationVesselSurface = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) * rotationSurface);

            velocityMainBodySurface = rotationSurface * surfaceVelocity;

            angularVelocity = Quaternion.Inverse(vessel.GetTransform().rotation) * vessel.rootPart.rb.angularVelocity;

            radialPlusSurface = Vector3d.Exclude(surfaceVelocity, up).normalized;
            radialPlus = Vector3d.Exclude(orbitalVelocity, up).normalized;
            normalPlusSurface = -Vector3d.Cross(radialPlusSurface, surfaceVelocity.normalized);
            normalPlus = -Vector3d.Cross(radialPlus, orbitalVelocity.normalized);

            rocketAoA = Vector3d.Angle(vessel.ReferenceTransform.rotation * Quaternion.Euler(-90, 0, 0) * Vector3d.forward, surfaceVelocity);

            atmPressure = vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres;
            altitude1 = altitude + TimeWarp.fixedDeltaTime * vessel.verticalSpeed;
            atmPressure1 = FlightGlobals.getStaticPressure(altitude1) * PhysicsGlobals.KpaToAtmospheres;

            thrustMaximum = thrustMinimum = thrustCurrent = 0.0;

            double thrustOverVe = 0.0;

            /* FIXME: make betterer vessel simulation */
            for (int i = 0; i < vessel.parts.Count; i++) {
                Part p = vessel.parts[i];
                for (int m = 0; m < p.Modules.Count; m++) {
                    PartModule pm = p.Modules[m];
                    if (!pm.isEnabled)
                        continue;

                    if (pm is ModuleEngines) {
                        var e = pm as ModuleEngines;
                        if ((!e.EngineIgnited) || (!e.isEnabled)) {
                            continue;
                        }
                        float thrustLimiter = e.thrustPercentage / 100f;

                        double Isp0 = e.atmosphereCurve.Evaluate((float)atmPressure);
                        double Isp1 = e.atmosphereCurve.Evaluate((float)atmPressure1);
                        double Isp = Math.Min(Isp0, Isp1);
                        double Ve = Isp * e.g;

                        double maxThrust = e.maxFuelFlow * e.flowMultiplier * Ve;
                        double minThrust = e.minFuelFlow * e.flowMultiplier * Ve;

                        /* handle RealFuels engines */
                        if (e.finalThrust == 0.0f && minThrust > 0.0f)
                            minThrust = maxThrust = 0.0;

                        double eMaxThrust = minThrust + (maxThrust - minThrust) * thrustLimiter;
                        double eMinThrust = e.throttleLocked ? eMaxThrust : minThrust;
                        double eCurrentThrust = e.finalThrust;

                        thrustMaximum += eMaxThrust;
                        thrustMinimum += eMinThrust;
                        thrustCurrent += eCurrentThrust;

                        thrustOverVe += eMaxThrust / Ve;

                        /* FIXME: Cosine losses */
                    }
                }
            }

            totalVe = thrustMaximum / thrustOverVe;
        }
    }
}
