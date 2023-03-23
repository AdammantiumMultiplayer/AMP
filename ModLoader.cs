using ThunderRoad;
using UnityEngine;

namespace AMP {
    public class ModLoader : ThunderScript {
        public override void ScriptLoaded(ThunderRoad.ModManager.ModData modData) {
            new GameObject().AddComponent<ModManager>();
        }
    }
}
