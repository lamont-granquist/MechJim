using System;

namespace MechJim.Manager {
    [AttributeUsage(AttributeTargets.Class)]
        public class EnableAttribute : Attribute {
            public EnableAttribute(params Type[] klasses) {
                this.klasses = klasses;
            }
            public Type[] klasses { get; set; }
        }
}
