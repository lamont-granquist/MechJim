using System.Collections.Generic;
using System.Runtime.Serialization;
using System;
using UnityEngine;

namespace MechJim.PartWrapper {
    public class PartWrapperList<T> : List<T> where T: PartWrapperBase {
        public PartWrapperList() : base() {}

        private ObjectIDGenerator idgenerator = new ObjectIDGenerator();

        public void StartMark() {
            for(int i = 0; i < Count; i++) {
                this[i].mark = false;
            }
        }

        private bool FoundID(Part p) {
            bool first;
            long id = idgenerator.GetId(p, out first);
            for (int i = 0; i < Count; i++) {
                if ( idgenerator.GetId(this[i].part, out first) == id ) {
                    this[i].mark = true;
                    return true;
                }
            }
            return false;
        }

        public void AddPart(Part p) {
            if (!FoundID(p)) {
                Debug.Log("creating wrapper for part " + p);
                var obj = (T)Activator.CreateInstance(typeof(T), new object[] { p });
                obj.mark = true;
                Add( obj );
            }
        }

        public void Sweep() {
            for (int i = 0; i < Count; i++) {
                if (!this[i].mark) {
                    RemoveAt(i);
                }
            }
        }

        public string ToString() {
            List<string> strings = new List<string>();

            for (int i = 0; i < Count; i++) {
                strings.Add(this[i].part.ToString());
            }

            return string.Join(", ", strings.ToArray());
        }
    }
}
