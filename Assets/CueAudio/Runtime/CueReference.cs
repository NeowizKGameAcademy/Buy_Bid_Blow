using System;
using UnityEngine;

namespace CueAudio
{
    [Serializable]
    public struct CueReference
    {
        [SerializeField] private CueCatalog catalog;
        [SerializeField] private string id;
        public CueCatalog Catalog => catalog;
        public string Id => id;
        public CueReference(CueCatalog catalog, string id) { this.catalog = catalog; this.id = id; }
        public override string ToString() => id ?? "(None)";
    }
}
