using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace AMP {
    public class ModLoader : LevelModule {
        public override IEnumerator OnLoadCoroutine() {
            new GameObject().AddComponent<ModManager>();

            yield break;
        }

        public override IEnumerator OnPlayerSpawnCoroutine() {
            new GameObject().AddComponent<ModManager>();

            yield break;
        }
    }
}
