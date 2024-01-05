using AMP.Network.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace AMP.Data {
    public class Banlist {

        string path = "banlist.json";

        public class BanEntry {
            public string id = "";
            public string name = "";
            public string reason = "";
        }

        public List<BanEntry> banned = new List<BanEntry>();

        public static Banlist Load(string path) {
            bool save = true;
            Banlist banlist = new Banlist();
            if(File.Exists(path)) {
                string json = File.ReadAllText(path);
                try {
                    banlist = (Banlist) JsonConvert.DeserializeObject(json, typeof(Banlist));
                } catch(Exception) {
                    save = false;
                }
            }
            banlist.path = path;

            if(save) banlist.Save();

            return banlist;
        }

        public void Save() {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        internal void Ban(ClientData client, string reason) {
            if(!IsBanned(client)) {
                BanEntry banEntry = new BanEntry();
                banEntry.id = client.player.uniqueId;
                banEntry.name = client.ClientName;
                banEntry.reason = reason;
                banned.Add(banEntry);

                Save();
            }

            client.Kick("You got banned. Reason: " + reason);
        }

        internal bool IsBanned(ClientData client) {
            foreach(var entry in banned) {
                if(entry.id == client.player.uniqueId) { return true; }
            }
            return false;
        }

        public BanEntry Unban(string name) {
            BanEntry banEntry = null;
            foreach(var entry in banned) {
                if(entry.name.ToLower().Contains(name.ToLower())) {
                    banEntry = entry;
                    break;
                }
            }

            if(banEntry != null) {
                banned.Remove(banEntry);
                Save();
                return banEntry;
            }
            return null;
        }
    }
}
