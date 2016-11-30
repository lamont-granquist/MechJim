using System.Collections.Generic;
using System;

namespace MechJim.PartWrapper {
    public class PartWrapperList<T> : List<T>
    {
        public PartWrapperList() : base() {}

        public void StartMark() {
            Clear();
        }

        public void AddPart(Part p) {
            Add( (T)Activator.CreateInstance(typeof(T), new object[] { p }) );
        }

        public void EndMark() {
            /* nothing here for now */
        }
    }
}
