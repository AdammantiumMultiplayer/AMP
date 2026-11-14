using AMP.GameInteraction;
using AMP.Network.Data;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;

namespace AMP.Network.Packets.Implementation {
    /// <summary>
    /// Transfers the Crystal Hunt progress or world map of the progress source (host or lobby admin) in parts.
    /// The data is GZip compressed json, split into parts of at most MAX_PART_SIZE bytes, because Netamite doesn't split packets itself.
    /// </summary>
    [PacketDefinition((byte) PacketType.CRYSTALHUNT_SYNC)]
    public class CrystalHuntSyncPacket : AMPPacket {
        public enum Kind : byte {
            Progress = 0, // Source -> Server -> Others: CrystalHuntSaveData json
            Map      = 1, // Source -> Server -> Others: LevelInstances json
            Source   = 2  // Server -> Client: data[0] == 1 if the client is the progress source now
        }

        internal const int MAX_PART_SIZE = 3500;

        [SyncedVar] public byte   kind;
        [SyncedVar] public int    transferId;
        [SyncedVar] public ushort index;
        [SyncedVar] public ushort count;
        [SyncedVar] public byte[] data = new byte[0];

        public CrystalHuntSyncPacket() { }

        public CrystalHuntSyncPacket(Kind kind, int transferId, ushort index, ushort count, byte[] data) {
            this.kind       = (byte) kind;
            this.transferId = transferId;
            this.index      = index;
            this.count      = count;
            this.data       = data ?? new byte[0];
        }

        public static CrystalHuntSyncPacket SourceInfo(bool isSource) {
            return new CrystalHuntSyncPacket(Kind.Source, 0, 0, 1, new byte[] { (byte) (isSource ? 1 : 0) });
        }

        public override bool ProcessClient(NetamiteClient client) {
            CrystalHuntSync.OnPacket(this);
            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            ModManager.serverInstance?.crystalHuntRelay.OnPacket(client, this);
            return true;
        }
    }
}
