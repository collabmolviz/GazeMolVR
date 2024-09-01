using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace UMol {

    public class PrecomputedRepresentationManager {
        public Dictionary<string, MeshData> precomputedRep = new Dictionary<string, MeshData>();
        public Dictionary<string, Dictionary<UnityMolResidue, List<int>>> precomputedCartoonAsso = new Dictionary<string, Dictionary<UnityMolResidue, List<int>>>();
        public Dictionary<string, int[]> precomputedSurfAsso = new Dictionary<string, int[]>();

        public void Clear(string sName) {
            var keys = precomputedRep.Keys.ToArray();

            foreach (string k in keys) {
                if (k.StartsWith(sName + "_")) {
                    precomputedRep.Remove(k);
                    precomputedCartoonAsso.Remove(k);
                    precomputedSurfAsso.Remove(k);
                }
            }
        }

        public void Clear() {
            var keys = precomputedRep.Keys.ToArray();

            foreach (string k in keys) {
                precomputedRep.Remove(k);
                precomputedCartoonAsso.Remove(k);
                precomputedSurfAsso.Remove(k);
            }
            precomputedRep.Clear();
            precomputedCartoonAsso.Clear();
            precomputedSurfAsso.Clear();
        }
        public void Clear(string sName, string repName) {
            var keys = precomputedRep.Keys.ToArray();
            foreach (string k in keys) {
                if (k.StartsWith(sName + "_") && k.EndsWith(repName)) {
                    precomputedRep.Remove(k);
                    precomputedCartoonAsso.Remove(k);
                    precomputedSurfAsso.Remove(k);
                }
            }
        }

        public bool ContainsRep(string key) {
            return precomputedRep.ContainsKey(key);
        }
    }

}