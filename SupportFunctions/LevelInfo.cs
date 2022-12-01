using AMP.Data;
using AMP.Logging;
using System.Collections.Generic;
using ThunderRoad;

namespace AMP.SupportFunctions {
    internal class LevelInfo {
        internal static bool ReadLevelInfo(ref string level, ref string mode, ref Dictionary<string, string> options) {
            if(Level.current != null && Level.current.data != null && Level.current.data.id != null && Level.current.data.id.Length > 0) {
                level = Level.current.data.id;
                mode = Level.current.mode.name;

                options = new Dictionary<string, string>();
                foreach(KeyValuePair<string, string> entry in Level.current.options) {
                    options.Add(entry.Key, entry.Value);
                }

                if(Level.current.dungeon != null && !options.ContainsKey(LevelOption.DungeonSeed.ToString())) {
                    options.Add(LevelOption.DungeonSeed.ToString(), Level.current.dungeon.seed.ToString());
                }

                return true;
            }
            
            return false;
        }

        internal static bool ReadLevelInfo(LevelData levelData, ref string level, ref string mode) {
            if(levelData != null && levelData.id != null && levelData.id.Length > 0) {
                level = levelData.id;
                mode = levelData.GetMode().name;

                return true;
            }
            return false;
        }

        internal static void TryLoadLevel(string level, string mode, Dictionary<string, string> options) {
            if(GameManager.local == null) {
                Log.Err(Defines.CLIENT, $"GameManager seems not to be loaded, can't change level.");
                return;
            }

            LevelData ld = Catalog.GetData<LevelData>(level);
            if(ld == null) {
                Log.Err(Defines.CLIENT, $"Level {level} not found, please check you mods.");
                return;
            }


            LevelData.Mode ldm = ld.GetMode(mode);
            if(ldm == null) {
                Log.Err(Defines.CLIENT, $"Couldn't switch to level {level}. Mode {mode} not found, please check you mods.");
                return;
            }

            Log.Info(Defines.CLIENT, $"Changing to level {level} with mode {mode}.");

            GameManager.LoadLevel(ld, ldm, options);
        }

        internal static bool IsLoading() {
            return Level.current == null || !Level.current.loaded || Level.current.data.id == "CharacterSelection";
        }
    }
}
