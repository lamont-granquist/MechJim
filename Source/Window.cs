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

            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput("ApA", false, 10, s => { apoap_ApA = Convert.ToDouble(s); return Convert.ToString(apoap_ApA); }),
                        new DialogGUIButton("Apoapsis", Apoapsis),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput("PeA", false, 10, s => { periap_PeA = Convert.ToDouble(s); return Convert.ToString(periap_PeA); }),
                        new DialogGUIButton("Periapsis", Periapsis),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUITextInput("ApA", false, 10, s => { ellip_ApA = Convert.ToDouble(s); return Convert.ToString(ellip_ApA); }),
                        new DialogGUITextInput("PeA", false, 10, s => { ellip_PeA = Convert.ToDouble(s); return Convert.ToString(ellip_PeA); }),
                        new DialogGUIButton("Ellipticize", Ellipticize),
                        }));
            dialog.Add(new DialogGUIHorizontalLayout(new DialogGUIBase[] {
                        new DialogGUIButton("Circularize", Circularize),
                        }));

            dialog.Add(new DialogGUIButton("Dismiss", core.toolbar.SetFalse, true));

            window = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new MultiOptionDialog("foo", "bar", UISkinManager.defaultSkin, new Rect(0.5f, 0.5f, 250, 120), dialog.ToArray()),
                    false, UISkinManager.defaultSkin, false, "");
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
