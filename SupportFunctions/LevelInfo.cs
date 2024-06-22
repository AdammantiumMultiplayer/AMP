using AMP.Data;
using AMP.Logging;
using System;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace AMP.SupportFunctions {
    internal class LevelInfo {

        private const string IsAreaOption = "IsArea";

        internal static bool ReadLevelInfo(out string level, out string mode, out Dictionary<string, string> options) {
            if(Level.current != null && Level.current.data != null && Level.current.data.id != null && Level.current.data.id.Length > 0) {
                level = Level.current.data.id;
                mode = Level.current.mode.name;

                options = new Dictionary<string, string>();
                foreach(KeyValuePair<string, string> entry in Level.current.options) {
                    options.Add(entry.Key, entry.Value);
                }

                if(AreaManager.Instance != null && AreaManager.Instance.CurrentArea != null) {
                    options.Add(IsAreaOption, "true");
                }

                if(Level.current != null && !options.ContainsKey(LevelOption.Seed.ToString())) {
                    options.Add(LevelOption.Seed.ToString(), Level.seed.ToString());
                }

                if(mode == "CrystalHunt") mode = "Sandbox";
                return true;
            }
            level = "";
            mode = "";
            options = new Dictionary<string, string>();
            return false;
        }

        internal static bool ReadLevelInfo(LevelData levelData, out string level, out string mode) {
            if(levelData != null && levelData.id != null && levelData.id.Length > 0) {
                level = levelData.id;
                mode = levelData.GetMode().name;

                if(mode == "CrystalHunt") mode = "Sandbox";

                return true;
            }
            level = "";
            mode = "";
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

            Log.Info(Defines.CLIENT, $"Changing to level {level} with mode {mode}.\nOptions:\n{string.Join(Environment.NewLine, options)}");
            
            if(options.ContainsKey(IsAreaOption)) options.Remove(IsAreaOption);

            LevelManager.LoadLevel(ld, ldm, options);
        }

        internal static bool IsLoading() {
            return Level.current == null || !Level.current.loaded || Level.current.data == null || Level.current.data.id == null || Level.current.data.id == "CharacterSelection" || Level.current.data.id == "MainMenu";
        }

        internal static bool IgnoreSeed(string level) {
            level = level.ToLower();

            if(level == "arena")   return true;
            if(level == "market")  return true;
            if(level == "citadel") return true;
            if(level == "ruins")   return true;
            if(level == "canyon")  return true;
            if(level == "home")    return true;

            return false;
        }

        /// <summary>
        /// Checks if the option contains a IsArea key
        /// Returns true if its not an area, so the seed gets ignored.
        /// </summary>
        /// <param name="options">Dictionary</param>
        /// <returns>true - Not an area / false - a area</returns>
        internal static bool IgnoreSeed(Dictionary<string, string> options) {
            if(options.ContainsKey(IsAreaOption)) {
                try {
                    return !bool.Parse(options[IsAreaOption]);
                } catch { }
            }

            return true;
        }
        
        internal static bool SameOptions(Dictionary<string, string> currentOptions, Dictionary<string, string> newOptions, bool ignoreSeed = false) {
            List<LevelOption> optionsToCheck = new List<LevelOption>(new LevelOption[] {
                LevelOption.Difficulty,
                LevelOption.DungeonLength,
                LevelOption.DungeonRoom,
                LevelOption.Seed
            });

            if(ignoreSeed || (!ignoreSeed && IgnoreSeed(currentOptions))) {
                optionsToCheck.Remove(LevelOption.Seed);
            }

            foreach(LevelOption option in optionsToCheck) {
                string key = option.ToString();
                if(newOptions.ContainsKey(key)) {
                    if(currentOptions.ContainsKey(key)) {
                        if(! newOptions[key].Equals(currentOptions[key], StringComparison.OrdinalIgnoreCase)) {
                            return false;
                        }
                    } else {
                        return false;
                    }
                }
            }

            return true;
        }


        internal static bool IsInActiveArea(SpawnableArea area) {
            // If its finding a area, check if its active, and only allow spawning if it is
            if(area != null && area.SpawnedArea != null) {
                return area.SpawnedArea.IsActive;
            }
            // If no area was found, but there is an area manager, its out of bounds, so dont spawn
            return AreaManager.Instance.CurrentArea == null;
        }

        internal static bool IsInActiveArea(Vector3 position) {
            // If there is no AreaManager active, just allow spawning
            if(AreaManager.Instance == null) return true;
            
            // If there is no current Area, just allow spawning
            if(AreaManager.Instance.CurrentArea == null) return true;

            // Check the area the item is in
            return IsInActiveArea(AreaManager.Instance.CurrentArea.FindRecursive(position));
        }
    }
}
