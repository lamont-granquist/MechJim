using System;
using UnityEngine;
using MechJim.Extensions;
using System.Collections.Generic;

namespace MechJim.Manager {
    public class VesselState: ManagerBase {
        public VesselState(Core core): base(core) { }

        public Dictionary<Part, double> engines = new Dictionary<Part, double>();
        public Dictionary<Part, double> solarpanels = new Dictionary<Part, double>();
        public Dictionary<Part, double> fairings = new Dictionary<Part, double>();
        public Dictionary<Part, double> reactionWheels = new Dictionary<Part, double>();
        public Dictionary<Part, double> rcs = new Dictionary<Part, double>();
        public Dictionary<Part, double> controlSurfaces = new Dictionary<Part, double>();
        public Dictionary<Part, double> otherTorque = new Dictionary<Part, double>();

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

        // Total torque
        public Vector3 torqueAvailable;

        // Torque from different components
        public Vector6 torqueReactionWheel  = new Vector6(); // torque available from Reaction wheels
        public Vector6 torqueRcs            = new Vector6(); // torque available from RCS from stock code (not working properly ATM)
        public Vector6 torqueControlSurface = new Vector6(); // torque available from Aerodynamic control surfaces
        public Vector6 torqueGimbal         = new Vector6(); // torque available from Gimbaled engines
        public Vector6 torqueOthers         = new Vector6(); // torque available from Mostly FAR

        public void SweepDict(Dictionary<Part, double> dict) {
            foreach(KeyValuePair<Part, double> pair in dict) {
                if (pair.Value != time) {
                    dict.Remove(pair.Key);
                }
            }
        }

        public void AddDict(Dictionary<Part, double> dict, Part p) {
            dict[p] = time;
        }

        public void Sweep() {
            SweepDict(engines);
            SweepDict(solarpanels);
            SweepDict(fairings);
            SweepDict(reactionWheels);
            SweepDict(rcs);
            SweepDict(controlSurfaces);
            SweepDict(otherTorque);
        }

        private int FindDecouplingStage(Part p) {
            if (p.IsUnfiredDecoupler())
                return p.inverseStage;
            if (p.parent == null)
                return -1;
            return FindDecouplingStage(p.parent);
        }

        private void WrapParts() {
            /* FIXME: make betterer vessel simulation */
            for (int i = 0; i < vessel.parts.Count; i++) {
                Part p = vessel.parts[i];

                int decouplingStage = FindDecouplingStage(p);

                if (p.IsEngine())
                    AddDict(engines, p);
                if (p.IsSolarPanel())
                    AddDict(solarpanels, p);
                if (p.IsFairing())
                    AddDict(fairings, p);
                if (p.IsReactionWheel())
                    AddDict(reactionWheels, p);
                if (p.IsRCS())
                    AddDict(rcs, p);
                if (p.IsControlSurface())
                    AddDict(controlSurfaces, p);
                if (p.IsOtherTorque())
                    AddDict(otherTorque, p);
                Debug.Log("Part = " + p + " inverse stage: " + p.inverseStage + " decoupling stage: " + decouplingStage);
            }

            Sweep();
        }

        private void AnalyzeEngines() {
            torqueGimbal.Reset();

            double thrustOverVe = 0.0;

            foreach(var pair in engines) {
                Part p = pair.Key;

                List<ModuleGimbal> glist = p.Modules.GetModules<ModuleGimbal>();
                for (int m = 0; m < glist.Count; m++) {
                    Vector3 pos;
                    Vector3 neg;
                    ModuleGimbal g = glist[m];
                    g.GetPotentialTorque(out pos, out neg);
                    torqueRcs.Add(pos);
                    torqueRcs.Add(-neg);
                }

                List<ModuleEngines> elist = p.Modules.GetModules<ModuleEngines>();

                for (int m = 0; m < elist.Count; m++) {
                    ModuleEngines e = elist[m];
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

            totalVe = thrustMaximum / thrustOverVe;
        }

        private void AnalyzeRCS() {
            torqueRcs.Reset();

            foreach(var pair in rcs) {
                Part p = pair.Key;

                List<ModuleRCS> mlist = p.Modules.GetModules<ModuleRCS>();

                for (int m = 0; m < mlist.Count; m++) {
                    Vector3 pos;
                    Vector3 neg;
                    ModuleRCS rw = mlist[m];
                    rw.GetPotentialTorque(out pos, out neg);
                    torqueRcs.Add(pos);
                    torqueRcs.Add(-neg);
                }
            }
        }

        private void AnalyzeReactionWheels() {
            torqueReactionWheel.Reset();

            foreach(var pair in reactionWheels) {
                Part p = pair.Key;

                List<ModuleReactionWheel> mlist = p.Modules.GetModules<ModuleReactionWheel>();

                for (int m = 0; m < mlist.Count; m++) {
                    Vector3 pos;
                    Vector3 neg;
                    ModuleReactionWheel rw = mlist[m];
                    rw.GetPotentialTorque(out pos, out neg);
                    torqueReactionWheel.Add(pos);
                    torqueReactionWheel.Add(-neg);
                }
            }
        }

        private void AnalyzeControlSurfaces() {
            torqueControlSurface.Reset();

            foreach(var pair in controlSurfaces) {
                Part p = pair.Key;

                List<ModuleControlSurface> mlist = p.Modules.GetModules<ModuleControlSurface>();

                for (int m = 0; m < mlist.Count; m++) {
                    Vector3 pos;
                    Vector3 neg;
                    ModuleControlSurface cs = mlist[m];
                    cs.GetPotentialTorque(out pos, out neg);
                    torqueReactionWheel.Add(pos);
                    torqueReactionWheel.Add(-neg);
                }
            }
        }

        private void AnalyzeOtherTorque() {
            torqueOthers.Reset();

            /* this is a special list of ITorqueProvider-containing Parts that are *NOT* Engines, RW, RCS, Control Surfaces */
            foreach(var pair in otherTorque) {
                Part p = pair.Key;

                List<ITorqueProvider> mlist = p.Modules.GetModules<ITorqueProvider>();

                for (int m = 0; m < mlist.Count; m++) {
                    Vector3 pos;
                    Vector3 neg;
                    ITorqueProvider it = mlist[m];
                    it.GetPotentialTorque(out pos, out neg);
                    torqueOthers.Add(pos);
                    torqueOthers.Add(-neg);
                }
            }
        }

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

            WrapParts();

            AnalyzeEngines();
            AnalyzeRCS();
            AnalyzeReactionWheels();
            AnalyzeControlSurfaces();
            AnalyzeOtherTorque();

            torqueAvailable = Vector3d.zero;

            torqueAvailable += Vector3d.Max(torqueGimbal.positive, torqueGimbal.negative);
            torqueAvailable += Vector3d.Max(torqueReactionWheel.positive, torqueReactionWheel.negative);
            torqueAvailable += Vector3d.Max(torqueRcs.positive, torqueRcs.negative);
            torqueAvailable += Vector3d.Max(torqueOthers.positive, torqueOthers.negative);
            torqueAvailable += Vector3d.Max(torqueControlSurface.positive, torqueControlSurface.negative);
        }
    }
}
