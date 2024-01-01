using AMP.GameInteraction;
using AMP.Threading;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte)PacketType.BOOK_AVAILABILITY)]
    public class BookAvailabilityPacket : AMPPacket {
        [SyncedVar] public bool enable_item_book;
        [SyncedVar] public bool enable_spawn_book;

        public BookAvailabilityPacket() { }

        public BookAvailabilityPacket(bool enable_item_book, bool enable_spawn_book) {
            this.enable_item_book = enable_item_book;
            this.enable_spawn_book = enable_spawn_book;
        }

        public override bool ProcessClient(NetamiteClient client) {
            ModManager.clientSync.syncData.enable_item_book = this.enable_item_book;
            ModManager.clientSync.syncData.enable_spawn_book = this.enable_spawn_book;

            Dispatcher.Enqueue(() => {
                LevelFunc.UpdateBookAvailability();
            });

            return true;
        }
    }
}
