using AMP.Data;
using AMP.Extension;
using AMP.Logging;
using AMP.Network.Packets.Implementation;
using AMP.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading;
using ThunderRoad;
using ThunderRoad.Modules;
using UnityEngine;

namespace AMP.GameInteraction {
    /// <summary>
    /// Crystal Hunt in multiplayer.
    ///
    /// The progress source (host, or lobby admin on a dedicated server) sends its progress (days, level progression, lore, ...) and the world map.
    /// The other Crystal Hunt players play on a temporary copy of their character (AMP_Session) with the progress of the source,
    /// their own stuff (skills, shards, gold, stash) stays theirs. When leaving, the own stuff gets written back into the real character,
    /// the progress of the source doesn't. The real save file is never touched during the session.
    /// </summary>
    internal static class CrystalHuntSync {
        internal const string SESSION_ID = "AMP_Session";
        private const string BACKUP_FILE = "crystalhunt_session.json";

        // Fields of the save data that belong to the player, they never get taken over from the source
        private static readonly string[] personalFields = { "$type", "gameModeId", "kills", "deaths", "savedGameModeOptionsValue" };

        #region State
        // Set by the server, this client sends its progress to everyone else
        private static bool isSource = false;

        // Session (only on the receiving side)
        private static PlayerSaveData session = null;
        private static string originalId = null;
        private static string originalSaveDataJson = null;

        private static string hostProgressJson = null;
        private static string hostMapJson = null;
        private static bool applyingMap = false;

        // While the character gets swapped, level changes have to wait, otherwise the game mode loads twice
        private static bool busy = false;
        private static readonly Queue<Action> deferred = new Queue<Action>();

        private static bool quitting = false;
        #endregion

        #region Events
        private static bool registered = false;

        internal static void RegisterEvents() {
            if(registered) return;
            EventManager.OnUpdateMap           += OnUpdateMap;
            EventManager.OnDungeonSuccessEvent += OnDungeonSuccess;
            EventManager.OnLoreRead            += OnLoreRead;
            TimeTrackModule.OnDayPassed        += OnDayPassed;
            registered = true;
        }

        internal static void UnregisterEvents() {
            if(!registered) return;
            EventManager.OnUpdateMap           -= OnUpdateMap;
            EventManager.OnDungeonSuccessEvent -= OnDungeonSuccess;
            EventManager.OnLoreRead            -= OnLoreRead;
            TimeTrackModule.OnDayPassed        -= OnDayPassed;
            registered = false;
        }

        private static void OnUpdateMap(EventTime eventTime) {
            if(eventTime != EventTime.OnEnd) return;

            if(isSource) {
                ScheduleSend(true, true);
            } else if(session != null && !applyingMap && hostMapJson != null) {
                // The game built its own random map, put the one of the source back
                Dispatcher.Enqueue(ApplyMap);
            }
        }

        private static void OnDungeonSuccess(LevelInstance levelInstance) {
            if(isSource) ScheduleSend(true, false);
        }

        private static void OnLoreRead(int hash) {
            if(isSource) ScheduleSend(true, false);
        }

        private static void OnDayPassed(int daysPassed, EventTime eventTime) {
            if(eventTime == EventTime.OnEnd && isSource) ScheduleSend(true, false);
        }

        internal static void OnLevelLoaded() {
            if(isSource) ScheduleSend(true, true);
        }
        #endregion

        #region Sending (source)
        private static bool sendProgress = false;
        private static bool sendMap = false;
        private static bool sendScheduled = false;
        private static int transferCounter = new System.Random().Next();

        /// <summary>
        /// Sends after a second, so multiple triggers in a row only send once
        /// </summary>
        private static void ScheduleSend(bool progress, bool map) {
            sendProgress |= progress;
            sendMap |= map;
            if(sendScheduled) return;
            sendScheduled = true;
            ModManager.instance.StartCoroutine(SendDelayed());
        }

        private static IEnumerator SendDelayed() {
            yield return new WaitForSecondsRealtime(1f);
            sendScheduled = false;

            bool progress = sendProgress;
            bool map = sendMap;
            sendProgress = false;
            sendMap = false;

            if(!isSource) yield break;
            if(ModManager.clientInstance == null || ModManager.clientInstance.netclient == null || !ModManager.clientInstance.netclient.IsConnected) yield break;
            if(!(CurrentSaveData() is CrystalHuntSaveData)) yield break;

            try {
                if(progress) {
                    string json = BuildProgressJson();
                    if(json != null) Send(CrystalHuntSyncPacket.Kind.Progress, json);
                }
                if(map) {
                    string json = BuildMapJson();
                    if(json != null) Send(CrystalHuntSyncPacket.Kind.Map, json);
                }
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, $"Couldn't send the Crystal Hunt progress.\n{e}");
            }
        }

        private static void Send(CrystalHuntSyncPacket.Kind kind, string json) {
            byte[] data = Compress(json);
            int parts = Math.Max(1, (data.Length + CrystalHuntSyncPacket.MAX_PART_SIZE - 1) / CrystalHuntSyncPacket.MAX_PART_SIZE);
            if(parts > ushort.MaxValue) {
                Log.Err(Defines.CLIENT, $"Crystal Hunt {kind} is too big to send ({data.Length} bytes).");
                return;
            }

            int transferId = Interlocked.Increment(ref transferCounter);
            for(int i = 0; i < parts; i++) {
                int offset = i * CrystalHuntSyncPacket.MAX_PART_SIZE;
                int length = Math.Min(CrystalHuntSyncPacket.MAX_PART_SIZE, data.Length - offset);
                byte[] part = new byte[length];
                Buffer.BlockCopy(data, offset, part, 0, length);
                new CrystalHuntSyncPacket(kind, transferId, (ushort) i, (ushort) parts, part).SendToServerReliable();
            }
            Log.Debug(Defines.CLIENT, $"Sent Crystal Hunt {kind}: {json.Length} chars, {data.Length} bytes compressed, {parts} parts.");
        }

        private static string BuildProgressJson() {
            GameModeSaveData saveData = CurrentSaveData();
            if(saveData == null) return null;

            JObject obj = JObject.FromObject(saveData, JsonSerializer.Create(Catalog.GetJsonNetSerializerSettings()));
            foreach(string field in personalFields) obj.Remove(field);
            return obj.ToString(Formatting.None);
        }

        private static string BuildMapJson() {
            CrystalHuntLevelInstancesModule module = GetMapModule();
            if(module == null || module.LevelInstances == null || module.LevelInstances.Count == 0) return null;
            return JsonConvert.SerializeObject(module.LevelInstances, Catalog.GetJsonNetSerializerSettings());
        }
        #endregion

        #region Receiving
        private class TransferBuffer {
            public int transferId;
            public byte[][] parts;
            public int received;
        }
        private static readonly object assemblyLock = new object();
        private static readonly Dictionary<byte, TransferBuffer> assemblies = new Dictionary<byte, TransferBuffer>();
        private static readonly Dictionary<byte, int> lastCompleted = new Dictionary<byte, int>();

        /// <summary>
        /// Called on the network thread
        /// </summary>
        internal static void OnPacket(CrystalHuntSyncPacket packet) {
            if(packet.kind == (byte) CrystalHuntSyncPacket.Kind.Source) {
                bool source = packet.data != null && packet.data.Length > 0 && packet.data[0] == 1;
                Dispatcher.Enqueue(() => SetSource(source));
                return;
            }
            if(packet.kind != (byte) CrystalHuntSyncPacket.Kind.Progress && packet.kind != (byte) CrystalHuntSyncPacket.Kind.Map) return;
            if(packet.count == 0 || packet.index >= packet.count || packet.data == null) return;

            byte[] complete = null;
            lock(assemblyLock) {
                int last;
                if(lastCompleted.TryGetValue(packet.kind, out last) && last == packet.transferId) return; // Got that one already (sent again on join)

                TransferBuffer assembly;
                if(!assemblies.TryGetValue(packet.kind, out assembly) || assembly.transferId != packet.transferId || assembly.parts.Length != packet.count) {
                    assembly = new TransferBuffer() { transferId = packet.transferId, parts = new byte[packet.count][] };
                    assemblies[packet.kind] = assembly;
                }
                if(assembly.parts[packet.index] == null) {
                    assembly.parts[packet.index] = packet.data;
                    assembly.received++;
                }
                if(assembly.received < assembly.parts.Length) return;

                assemblies.Remove(packet.kind);
                lastCompleted[packet.kind] = packet.transferId;

                using(MemoryStream ms = new MemoryStream()) {
                    foreach(byte[] part in assembly.parts) ms.Write(part, 0, part.Length);
                    complete = ms.ToArray();
                }
            }

            string json;
            try {
                json = Decompress(complete);
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, $"Couldn't unpack Crystal Hunt data.\n{e}");
                return;
            }

            CrystalHuntSyncPacket.Kind kind = (CrystalHuntSyncPacket.Kind) packet.kind;
            int partCount = packet.count;
            Dispatcher.Enqueue(() => {
                Log.Debug(Defines.CLIENT, $"Received Crystal Hunt {kind} in {partCount} parts.");
                if(kind == CrystalHuntSyncPacket.Kind.Progress) OnProgress(json);
                else OnMap(json);
            });
        }

        private static void SetSource(bool source) {
            if(isSource == source) return;
            isSource = source;
            if(source) {
                Log.Info(Defines.CLIENT, "This game is now the source of the Crystal Hunt progress.");
                ScheduleSend(true, true);
            }
        }

        private static void OnProgress(string json) {
            if(isSource) return;
            hostProgressJson = json;

            if(session != null) {
                // Same object the game mode modules hold, so no reload needed
                if(!busy) PopulateProgress(json, session.mode.saveData);
                return;
            }
            if(busy) return; // Session is being started, it uses the latest json

            if(!(Player.characterData?.mode?.data is CrystalHuntGameModeData)) {
                Log.Debug(Defines.CLIENT, "Got Crystal Hunt progress, but playing a Sandbox character, ignoring it.");
                return;
            }
            ModManager.instance.StartCoroutine(StartSessionRoutine());
        }

        private static void OnMap(string json) {
            if(isSource) return;
            hostMapJson = json;
            if(session != null && !busy) ApplyMap();
        }
        #endregion

        #region Session
        /// <summary>
        /// Runs the action right away, or after the character swap if one is running.
        /// Used for level changes, those would otherwise load the game mode at the same time.
        /// </summary>
        internal static void RunWhenReady(Action action) {
            if(!busy) {
                action();
            } else {
                deferred.Enqueue(action);
            }
        }

        private static void FinishBusy() {
            busy = false;
            while(deferred.Count > 0) {
                Action action = deferred.Dequeue();
                try { action(); } catch(Exception e) { Log.Err(e); }
            }
        }

        private static IEnumerator StartSessionRoutine() {
            busy = true;

            // Don't swap in the middle of a level load
            while(Level.current == null || !Level.current.loaded) yield return null;

            bool started = false;
            try {
                started = StartSession();
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, $"Couldn't start the Crystal Hunt session.\n{e}");
            }

            if(started) {
                yield return ReloadGameMode(session.mode.data);
                try {
                    if(hostProgressJson != null) PopulateProgress(hostProgressJson, session.mode.saveData);
                    ApplyMap();
                } catch(Exception e) {
                    Log.Err(Defines.CLIENT, e);
                }
                Log.Info(Defines.CLIENT, "Playing Crystal Hunt with the progress of the host, your own character stays untouched.");
            }

            FinishBusy();
        }

        private static bool StartSession() {
            PlayerSaveData original = Player.characterData;
            if(original == null || original.mode == null || !(original.mode.data is CrystalHuntGameModeData) || original.mode.saveData == null) return false;
            if(hostProgressJson == null) return false;

            if(original.ID == SESSION_ID) {
                // Shouldn't happen, the backup is needed to get back to the real character
                Log.Err(Defines.CLIENT, $"The temporary character {SESSION_ID} is loaded, not starting a session. Restart the game to restore your character.");
                return false;
            }

            JsonSerializerSettings settings = Catalog.GetJsonNetSerializerSettings();

            // Backup first, if the game crashes the real character gets restored on the next start
            string saveDataJson = JsonConvert.SerializeObject(original.mode.saveData, settings);
            if(!WriteBackup(original.ID, saveDataJson)) return false;

            // Current gold and shards into the serialized values, so the copy has them
            original.inventory.SaveCurrencies();

            PlayerSaveData temp = JsonConvert.DeserializeObject<PlayerSaveData>(original.ToJson(), settings);
            temp.Check();
            temp.ID = SESSION_ID;

            // The inventory in the save is from the last save, take the current one
            if(Player.currentCreature != null && Player.currentCreature.container != null) {
                temp.inventory.SetPlayerInventory(Player.currentCreature.container.CloneContents());
            }

            PopulateProgress(hostProgressJson, temp.mode.saveData);

            originalId = original.ID;
            originalSaveDataJson = saveDataJson;
            session = temp;

            Swap(original, temp);
            return true;
        }

        /// <summary>
        /// Writes the own stuff back into the real character. Called when the connection gets closed.
        /// </summary>
        internal static void EndSession() {
            isSource = false;
            hostProgressJson = null;
            hostMapJson = null;
            ResetAssemblies();

            if(session == null) return;

            if(quitting) {
                // Coroutines don't run anymore, the backup restores everything on the next start
                Log.Info(Defines.CLIENT, "Game is closing during a Crystal Hunt session, your character gets restored on the next start.");
                session = null;
                return;
            }

            PlayerSaveData temp = session;
            session = null;

            try {
                bool isCurrent = Player.characterData == temp;
                if(isCurrent) temp.inventory.SaveCurrencies();

                PlayerSaveData final = BuildFinal(temp, originalId, originalSaveDataJson);

                if(isCurrent) {
                    Swap(temp, final);
                    final.SaveAsync(true); // Reads the inventory of the current player creature
                    ModManager.instance.StartCoroutine(ReloadGameMode(final.mode.data));
                } else {
                    // Player switched the character in the meantime, just save the stuff of the session
                    final.SaveAsync(false);
                }

                ModManager.instance.StartCoroutine(DeleteSessionSave(temp));
                DeleteBackup();
                Log.Info(Defines.CLIENT, "Crystal Hunt session ended, your skills, gold, shards and stash have been saved to your character.");
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, $"Couldn't write back the Crystal Hunt session, it gets restored on the next start.\n{e}");
            }

            originalId = null;
            originalSaveDataJson = null;
        }

        /// <summary>
        /// The connection is closed for a server switch, the session continues on the new server
        /// </summary>
        internal static void ResetConnection() {
            isSource = false;
            ResetAssemblies();
        }

        internal static void OnApplicationQuit() {
            quitting = true;
        }

        /// <summary>
        /// Copy of the session character with the real id and the real progress, but everything else from the session.
        /// </summary>
        private static PlayerSaveData BuildFinal(PlayerSaveData temp, string id, string saveDataJson) {
            JsonSerializerSettings settings = Catalog.GetJsonNetSerializerSettings();

            JObject character = JObject.Parse(temp.ToJson());
            JObject realProgress = JObject.Parse(saveDataJson);

            // Kills and deaths of the session belong to the player
            JObject sessionProgress = character["mode"]?["saveData"] as JObject;
            if(sessionProgress != null) {
                foreach(string field in new string[] { "kills", "deaths" }) {
                    if(sessionProgress[field] != null) realProgress[field] = sessionProgress[field];
                }
            }

            character["ID"] = id;
            ((JObject) character["mode"])["saveData"] = realProgress;

            PlayerSaveData final = JsonConvert.DeserializeObject<PlayerSaveData>(character.ToString(Formatting.None), settings);
            final.Check();
            return final;
        }

        private static IEnumerator DeleteSessionSave(PlayerSaveData temp) {
            // The game might still be writing an autosave of it
            yield return new WaitForSecondsRealtime(3f);
            yield return temp.DeleteSaveCoroutine();
            Log.Debug(Defines.CLIENT, $"Deleted the temporary character {SESSION_ID}.");
        }

        private static void Swap(PlayerSaveData from, PlayerSaveData to) {
            if(from != null && from.mode != null && from.mode.data != null) from.mode.data.UnloadPlayerSaveData(from);
            Player.characterData = to;
            to.mode.data.LoadPlayerSaveData(to);
        }

        private static MethodInfo unloadGameMode;
        private static MethodInfo loadGameMode;

        /// <summary>
        /// The game mode modules keep a reference to the save data they got when loading, so they have to be loaded again after a swap
        /// </summary>
        private static IEnumerator ReloadGameMode(GameModeData data) {
            GameModeManager manager = GameModeManager.instance;
            if(manager == null || data == null) yield break;

            if(unloadGameMode == null) unloadGameMode = typeof(GameModeManager).GetMethod("UnloadGameMode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if(loadGameMode   == null) loadGameMode   = typeof(GameModeManager).GetMethod("LoadGameMode",   BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if(unloadGameMode == null || loadGameMode == null) {
                Log.Err(Defines.CLIENT, "Couldn't find the game mode loading of the game, the Crystal Hunt progress only applies after loading a level.");
                yield break;
            }

            IEnumerator load = null;
            try {
                if(manager.currentGameMode != null) unloadGameMode.Invoke(manager, new object[] { manager.currentGameMode });
                load = (IEnumerator) loadGameMode.Invoke(manager, new object[] { data });
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, $"Couldn't reload the game mode.\n{e}");
            }
            if(load != null) yield return manager.StartCoroutine(load);
        }

        private static void PopulateProgress(string json, GameModeSaveData target) {
            if(target == null || json == null) return;

            JsonSerializer serializer = JsonSerializer.Create(Catalog.GetJsonNetSerializerSettings());
            serializer.ObjectCreationHandling = ObjectCreationHandling.Replace; // Lists get replaced, not appended to
            using(JsonTextReader reader = new JsonTextReader(new StringReader(json))) {
                serializer.Populate(reader, target);
            }
        }

        private static void ApplyMap() {
            if(session == null || hostMapJson == null) return;

            CrystalHuntLevelInstancesModule module = GetMapModule();
            if(module == null) return;

            try {
                List<LevelInstance> instances = JsonConvert.DeserializeObject<List<LevelInstance>>(hostMapJson, Catalog.GetJsonNetSerializerSettings());
                if(instances == null) return;
                module.LevelInstances = instances;

                applyingMap = true;
                EventManager.InvokeUpdateMap(EventTime.OnEnd); // Redraws the map board
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, $"Couldn't apply the world map of the host.\n{e}");
            } finally {
                applyingMap = false;
            }
        }
        #endregion

        #region Crash recovery
        private static bool recoveryChecked = false;

        /// <summary>
        /// If the game was closed during a session, the real character still has its old state and the session one has the own stuff.
        /// Merges them the same way as when leaving. Runs once the first level is loaded, the catalog is needed for it.
        /// </summary>
        internal static void CheckRecovery() {
            EventManager.onLevelLoad += RecoveryOnLevelLoad;
        }

        private static void RecoveryOnLevelLoad(LevelData levelData, LevelData.Mode mode, EventTime eventTime) {
            if(eventTime != EventTime.OnEnd) return;
            EventManager.onLevelLoad -= RecoveryOnLevelLoad;
            if(recoveryChecked) return;
            recoveryChecked = true;
            ModManager.instance.StartCoroutine(RecoveryRoutine());
        }

        private static IEnumerator RecoveryRoutine() {
            string backupPath = GetBackupPath();
            bool hasBackup = backupPath != null && File.Exists(backupPath);

            List<PlayerSaveData> characters = null;
            yield return PlayerSaveData.LoadCharacterSaves(result => characters = result);
            if(characters == null) yield break;

            PlayerSaveData temp = characters.Find(c => c != null && c.ID == SESSION_ID);

            if(!hasBackup) {
                if(temp != null) {
                    Log.Warn(Defines.CLIENT, $"Found a leftover temporary character {SESSION_ID} without a session backup, deleting it.");
                    yield return temp.DeleteSaveCoroutine();
                }
                yield break;
            }

            string id = null;
            string saveDataJson = null;
            try {
                JObject backup = JObject.Parse(File.ReadAllText(backupPath));
                id = (string) backup["originalId"];
                saveDataJson = (string) backup["saveData"];
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, $"Couldn't read the Crystal Hunt session backup.\n{e}");
                yield break;
            }

            if(temp == null) {
                // Session never got saved, so the real character is unchanged
                Log.Info(Defines.CLIENT, "Found a Crystal Hunt session backup, but no session save, nothing to restore.");
                DeleteBackup();
                yield break;
            }

            PlayerSaveData original = characters.Find(c => c != null && c.ID == id);
            if(original != null && original.lastPlayTime > temp.lastPlayTime) {
                // The real character got played after the session (without the mod?), don't overwrite that
                Log.Warn(Defines.CLIENT, $"The character {id} is newer than the Crystal Hunt session, not restoring the session.");
            } else {
                PlayerSaveData final = null;
                try {
                    final = BuildFinal(temp, id, saveDataJson);
                } catch(Exception e) {
                    Log.Err(Defines.CLIENT, $"Couldn't restore the Crystal Hunt session, keeping the backup.\n{e}");
                    yield break;
                }

                final.SaveAsync(false);
                Log.Info(Defines.CLIENT, $"Restored the character {id} after the Crystal Hunt session got interrupted, the skills, gold, shards and stash of the session have been saved.");
                yield return new WaitForSecondsRealtime(1f);
            }

            yield return temp.DeleteSaveCoroutine();
            DeleteBackup();
        }
        #endregion

        #region Helpers
        private static GameModeSaveData CurrentSaveData() {
            return Player.characterData?.mode?.saveData;
        }

        private static CrystalHuntLevelInstancesModule GetMapModule() {
            if(GameModeManager.instance == null || GameModeManager.instance.currentGameMode == null) return null;
            CrystalHuntLevelInstancesModule module;
            if(!GameModeManager.instance.currentGameMode.TryGetModule<CrystalHuntLevelInstancesModule>(out module)) return null;
            return module;
        }

        private static void ResetAssemblies() {
            lock(assemblyLock) {
                assemblies.Clear();
                lastCompleted.Clear();
            }
        }

        private static string GetBackupPath() {
            if(!GameManager.platform.TryGetSavePath(out string savePath)) return null;
            return Path.Combine(savePath, "Mods", "MultiplayerMod", BACKUP_FILE);
        }

        private static bool WriteBackup(string id, string saveDataJson) {
            string path = GetBackupPath();
            if(path == null) {
                Log.Err(Defines.CLIENT, "Can't write the Crystal Hunt session backup, save path not retrievable. Not starting the session.");
                return false;
            }
            try {
                JObject backup = new JObject {
                    ["originalId"] = id,
                    ["saveData"] = saveDataJson
                };
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, backup.ToString(Formatting.None));
                return true;
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, $"Couldn't write the Crystal Hunt session backup, not starting the session.\n{e}");
                return false;
            }
        }

        private static void DeleteBackup() {
            try {
                string path = GetBackupPath();
                if(path != null && File.Exists(path)) File.Delete(path);
            } catch(Exception e) {
                Log.Err(Defines.CLIENT, e);
            }
        }

        private static byte[] Compress(string text) {
            byte[] raw = Encoding.UTF8.GetBytes(text);
            using(MemoryStream ms = new MemoryStream()) {
                using(GZipStream gz = new GZipStream(ms, System.IO.Compression.CompressionLevel.Optimal)) {
                    gz.Write(raw, 0, raw.Length);
                }
                return ms.ToArray();
            }
        }

        private static string Decompress(byte[] data) {
            using(MemoryStream input = new MemoryStream(data))
            using(GZipStream gz = new GZipStream(input, CompressionMode.Decompress))
            using(MemoryStream output = new MemoryStream()) {
                gz.CopyTo(output);
                return Encoding.UTF8.GetString(output.ToArray());
            }
        }
        #endregion
    }
}
