using System;
using System.Collections.Generic;
using MechJim.Extensions;
using UnityEngine;

namespace MechJim.Manager {
    public class Mission: ManagerBase {
        public Mission(Core core): base(core) {
        }

        public override void OnDisable() {
            core.autostage.enabled = false;
            core.ascent.enabled = false;
        }

        public override void OnEnable() {
            core.autostage.enabled = true;
            core.ascent.enabled = true;
        }

        /* FIXME: turn this into a state machine */
        public override void OnFixedUpdate() {
        }
    }
}
