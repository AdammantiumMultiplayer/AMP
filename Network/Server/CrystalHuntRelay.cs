using AMP.Data;
using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Packets.Implementation;
using System.Collections.Generic;
using System.Linq;

namespace AMP.Network.Server {
    /// <summary>
    /// Server side of the Crystal Hunt sync. Only handles bytes, the server itself has no save data.
    ///
    /// One client is the progress source: the client running in the same process as the server (host),
    /// otherwise (dedicated server) the lobby admin, or the first player if there is none.
    /// Transfers of the source get stored and forwarded to everyone else, new players get the stored ones when joining.
    /// </summary>
    internal class CrystalHuntRelay {
        // 512 parts of 3500 bytes compressed are way more than a save could ever need, protects the server from garbage
        private const int MAX_PARTS = 512;

        private readonly Server server;
        private readonly object lockObj = new object();

        private int sourceId = int.MinValue;

        private class Transfer {
            public int transferId;
            public CrystalHuntSyncPacket[] parts;
            public int received;
        }
        private readonly Dictionary<byte, Transfer> pending = new Dictionary<byte, Transfer>();
        private readonly Dictionary<byte, CrystalHuntSyncPacket[]> complete = new Dictionary<byte, CrystalHuntSyncPacket[]>();

        internal CrystalHuntRelay(Server server) {
            this.server = server;
        }

        internal void OnPacket(ClientData client, CrystalHuntSyncPacket packet) {
            if(client == null || packet == null) return;
            if(packet.kind != (byte) CrystalHuntSyncPacket.Kind.Progress && packet.kind != (byte) CrystalHuntSyncPacket.Kind.Map) return;

            CrystalHuntSyncPacket[] finished = null;
            lock(lockObj) {
                if(client.ClientId != sourceId) {
                    Log.Debug(Defines.SERVER, $"Ignoring Crystal Hunt data of {client.ClientName}, not the progress source.");
                    return;
                }
                if(packet.count == 0 || packet.count > MAX_PARTS || packet.index >= packet.count || packet.data == null) return;

                Transfer transfer;
                if(!pending.TryGetValue(packet.kind, out transfer) || transfer.transferId != packet.transferId || transfer.parts.Length != packet.count) {
                    // A newer transfer replaces an unfinished older one
                    transfer = new Transfer() { transferId = packet.transferId, parts = new CrystalHuntSyncPacket[packet.count] };
                    pending[packet.kind] = transfer;
                }

                if(transfer.parts[packet.index] == null) {
                    transfer.parts[packet.index] = packet;
                    transfer.received++;
                }

                if(transfer.received == transfer.parts.Length) {
                    pending.Remove(packet.kind);
                    complete[packet.kind] = transfer.parts;
                    finished = transfer.parts;
                }
            }

            if(finished != null) {
                Log.Debug(Defines.SERVER, $"Received Crystal Hunt {(CrystalHuntSyncPacket.Kind) packet.kind} from {client.ClientName} in {finished.Length} parts, forwarding it.");
                foreach(CrystalHuntSyncPacket part in finished) {
                    server.netamiteServer.SendToAllExcept(part, client.ClientId);
                }
            }
        }

        /// <summary>
        /// Sends the stored progress and map to a joining player, before it loads the level.
        /// </summary>
        internal void SendStored(ClientData client) {
            List<CrystalHuntSyncPacket[]> transfers;
            lock(lockObj) {
                if(client.ClientId == sourceId) return;
                transfers = complete.Values.ToList();
            }
            foreach(CrystalHuntSyncPacket[] parts in transfers) {
                foreach(CrystalHuntSyncPacket part in parts) {
                    server.netamiteServer.SendTo(client, part);
                }
            }
        }

        /// <summary>
        /// Checks who the progress source is and tells the clients if it changed.
        /// Called when players join or leave and when permissions change.
        /// </summary>
        /// <param name="leavingClientId">Client that is leaving right now, it might still be in the client list</param>
        internal void UpdateSource(int leavingClientId = int.MinValue) {
            if(server.netamiteServer == null) return;

            ClientData[] clients = server.Clients.Where(c => c != null && c.greeted && c.ClientId != leavingClientId).OrderBy(c => c.ClientId).ToArray();

            int newSource = int.MinValue;

            // Hosting and playing in the same game, the host is the source
            if(ModManager.clientInstance != null && ModManager.clientInstance.netclient != null && ModManager.clientInstance.netclient.IsConnected) {
                int ownId = ModManager.clientInstance.netclient.ClientId;
                if(clients.Any(c => c.ClientId == ownId)) newSource = ownId;
            }

            // Dedicated server, the lobby admin (first player) is the source
            if(newSource == int.MinValue) {
                ClientData admin = clients.FirstOrDefault(c => c.permissionLevel >= PermissionLevel.LOBBY_ADMIN);
                if(admin == null) admin = clients.FirstOrDefault();
                if(admin != null) newSource = admin.ClientId;
            }

            int oldSource;
            lock(lockObj) {
                if(newSource == sourceId) return;
                oldSource = sourceId;
                sourceId = newSource;
                pending.Clear(); // Unfinished parts of the old source are useless now
            }

            ClientData oldClient = clients.FirstOrDefault(c => c.ClientId == oldSource);
            if(oldClient != null) oldClient.SendPacket(CrystalHuntSyncPacket.SourceInfo(false));

            ClientData newClient = clients.FirstOrDefault(c => c.ClientId == newSource);
            if(newClient != null) {
                Log.Info(Defines.SERVER, $"{newClient.ClientName} is now the source of the Crystal Hunt progress.");
                newClient.SendPacket(CrystalHuntSyncPacket.SourceInfo(true));
            }
        }

        internal void OnClientLeft(ClientData client) {
            lock(lockObj) {
                if(client.ClientId != sourceId) return;
                sourceId = int.MinValue;
                pending.Clear();
                // The stored transfers stay, so the others keep that progress until a new source sends its own
            }
        }
    }
}
