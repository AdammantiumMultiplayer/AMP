using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace AMP {
    internal class ModLoader : LevelModule {
        public override IEnumerator OnLoadCoroutine() {
            new GameObject().AddComponent<ModManager>();

            yield break;
        }
    }
}
