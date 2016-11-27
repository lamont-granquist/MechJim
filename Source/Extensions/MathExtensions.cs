/* portions plagiarized from MechJeb */
using System;
using UnityEngine;

namespace MechJim.Extensions {
    public static class MathExtensions {
        public static Vector3 xzy(this Vector3 v) {
            float t = v.z;
            v.z = v.y;
            v.y = t;
            return v;
        }
    }
}
