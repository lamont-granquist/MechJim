using System;
using System.Text;
using UnityEngine;
using System.Collections.Generic;

namespace MechJim {
    public class Window {
        private PopupDialog window;
        private Core core;

        private double ellip_PeA;
        private double ellip_ApA;
        private double periap_PeA;
        private double apoap_ApA;

        public Window(Core core) {
            this.core = core;
        }

        /* starting */
        public void Start() {
            GameEvents.onGamePause.Add(onPause);
            GameEvents.onGameUnpause.Add(onUnpause);
        }

        /* leaving scene */
        public void OnDestroy() {
            GameEvents.onGamePause.Remove(onPause);
            GameEvents.onGameUnpause.Remove(onUnpause);
        }

        private void onPause() {
            if (window != null)
                window.gameObject.SetActive(false);
        }

        private void onUnpause() {
            if (window != null)
                window.gameObject.SetActive(true);
        }

        public void SpawnWindow() {
            List<DialogGUIBase> dialog = new List<DialogGUIBase>();

/*            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput("ApA", false, 10, s => { apoap_ApA = Convert.ToDouble(s); return Convert.ToString(apoap_ApA); }),
                        new DialogGUIButton("Apoapsis", Apoapsis, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput("PeA", false, 10, s => { periap_PeA = Convert.ToDouble(s); return Convert.ToString(periap_PeA); }),
                        new DialogGUIButton("Periapsis", Periapsis, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput("ApA", false, 10, s => { ellip_ApA = Convert.ToDouble(s); return Convert.ToString(ellip_ApA); }),
                        new DialogGUITextInput("PeA", false, 10, s => { ellip_PeA = Convert.ToDouble(s); return Convert.ToString(ellip_PeA); }),
                        new DialogGUIButton("Ellipticize", Ellipticize, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUIButton("Circularize", Circularize, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUIButton("Prograde", ProgradeToggle, false),
                        })); */
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUIButton("NodeExecute", () => { core.node.enabled = !core.node.enabled; }, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUIButton("Mission", () => { core.mission.enabled = !core.mission.enabled; }, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput(Convert.ToString(core.steering.pitchRatePI.Kp), false, 10,
                                s => { core.steering.pitchRatePI.Kp = Convert.ToDouble(s); return s; }),
                        new DialogGUITextInput(Convert.ToString(core.steering.pitchRatePI.Ki), false, 10,
                                s => { core.steering.pitchRatePI.Ki = Convert.ToDouble(s); return s; }),
                        new DialogGUITextInput(Convert.ToString(core.steering.pitchRatePI.Kd), false, 10,
                                s => { core.steering.pitchRatePI.Kd = Convert.ToDouble(s); return s; }),
                        new DialogGUIButton("pitch", NodeExecute, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput(Convert.ToString(core.steering.rollRatePI.Kp), false, 10,
                                s => { core.steering.rollRatePI.Kp = Convert.ToDouble(s); return s; }),
                        new DialogGUITextInput(Convert.ToString(core.steering.rollRatePI.Ki), false, 10,
                                s => { core.steering.rollRatePI.Ki = Convert.ToDouble(s); return s; }),
                        new DialogGUITextInput(Convert.ToString(core.steering.rollRatePI.Kd), false, 10,
                                s => { core.steering.rollRatePI.Kd = Convert.ToDouble(s); return s; }),
                        new DialogGUIButton("roll", NodeExecute, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput(Convert.ToString(core.steering.yawRatePI.Kp), false, 10,
                                s => { core.steering.yawRatePI.Kp = Convert.ToDouble(s); return s; }),
                        new DialogGUITextInput(Convert.ToString(core.steering.yawRatePI.Ki), false, 10,
                                s => { core.steering.yawRatePI.Ki = Convert.ToDouble(s); return s; }),
                        new DialogGUITextInput(Convert.ToString(core.steering.yawRatePI.Kd), false, 10,
                                s => { core.steering.yawRatePI.Kd = Convert.ToDouble(s); return s; }),
                        new DialogGUIButton("yaw", NodeExecute, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput(Convert.ToString(core.steering.pitchPI.Ts), false, 10,
                                s => { core.steering.pitchPI.Ts = Convert.ToDouble(s); return s; }),
                        new DialogGUIButton("pitch Ts", NodeExecute, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput(Convert.ToString(core.steering.rollPI.Ts), false, 10,
                                s => { core.steering.rollPI.Ts = Convert.ToDouble(s); return s; }),
                        new DialogGUIButton("roll Ts", NodeExecute, false),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput(Convert.ToString(core.steering.yawPI.Ts), false, 10,
                                s => { core.steering.yawPI.Ts = Convert.ToDouble(s); return s; }),
                        new DialogGUIButton("yaw Ts", NodeExecute, false),
                        }));

            dialog.Add(new DialogGUIButton("Dismiss", core.toolbar.SetFalse, true));

            window = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog("foo", "bar", UISkinManager.defaultSkin, new Rect(0.5f, 0.5f, 250, 120), dialog.ToArray()),
                    false, UISkinManager.defaultSkin, false, "");
        }

        void NodeExecute() {
            core.node.enabled = !core.node.enabled;
        }

        void ProgradeToggle() {
            core.ProgradeToggle();
        }

        void Circularize() {
            var maneuver = new Maneuver.Circularize(FlightGlobals.ActiveVessel, FlightGlobals.ActiveVessel.orbit, Planetarium.GetUniversalTime());
            maneuver.PlaceManeuverNode();
        }

        void Ellipticize() {
            var maneuver = new Maneuver.Ellipticize(FlightGlobals.ActiveVessel, FlightGlobals.ActiveVessel.orbit, Planetarium.GetUniversalTime(), ellip_PeA, ellip_ApA);
            maneuver.PlaceManeuverNode();
        }

        void Periapsis() {
            var maneuver = new Maneuver.Periapsis(FlightGlobals.ActiveVessel, FlightGlobals.ActiveVessel.orbit, Planetarium.GetUniversalTime(), periap_PeA);
            maneuver.PlaceManeuverNode();
        }

        void Apoapsis() {
            var maneuver = new Maneuver.Apoapsis(FlightGlobals.ActiveVessel, FlightGlobals.ActiveVessel.orbit, Planetarium.GetUniversalTime(), apoap_ApA);
            maneuver.PlaceManeuverNode();
        }

        public void DismissWindow() {
            if (window != null)
                window.Dismiss();
        }
    }
}
