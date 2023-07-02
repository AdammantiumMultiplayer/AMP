using AMP.Datatypes;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Data;
using Netamite.Server.Definition;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_HAND_FINGERS)]
    public class HandPositionPacket : NetPacket {
        [SyncedVar]       public long  creatureId;
        [SyncedVar]       public byte  creatureType;
        [SyncedVar]       public byte  side;
        [SyncedVar(true)] public float thumbWeight;
        [SyncedVar(true)] public float indexWeight;
        [SyncedVar(true)] public float middleWeight;
        [SyncedVar(true)] public float ringWeight;
        [SyncedVar(true)] public float littleWeight;

        public HandPositionPacket() { }

        public HandPositionPacket(long creatureId, byte creatureType, byte side, float thumbWeight, float indexWeight, float middleWeight, float ringWeight, float littleWeight) {
            this.creatureId   = creatureId;
            this.creatureType = creatureType;
            this.side         = side;
            this.thumbWeight  = thumbWeight;
            this.indexWeight  = indexWeight;
            this.middleWeight = middleWeight;
            this.ringWeight   = ringWeight;
            this.littleWeight = littleWeight;
        }

        public HandPositionPacket(CreatureNetworkData ind, Side side) {
            this.creatureId   = ind.networkedId;
            this.creatureType = (byte) ItemHolderType.CREATURE;
            this.side         = (byte) side;

            ReadHand(ind.creature.GetHand(side));
        }

        public HandPositionPacket(PlayerNetworkData ind, Side side) {
            this.creatureId   = ind.clientId;
            this.creatureType = (byte)ItemHolderType.PLAYER;
            this.side         = (byte)side;

            ReadHand(ind.creature.GetHand(side));
        }

        private void ReadHand(RagdollHand ragdollHand) {
            if(ragdollHand != null && ragdollHand.poser != null) {
                thumbWeight  = ragdollHand.poser.thumbCloseWeight;
                indexWeight  = ragdollHand.poser.indexCloseWeight;
                middleWeight = ragdollHand.poser.middleCloseWeight;
                ringWeight   = ragdollHand.poser.ringCloseWeight;
                littleWeight = ragdollHand.poser.littleCloseWeight;
            }
        }

        public override bool ProcessClient(NetamiteClient client) {
            Creature creature = null;

            switch(creatureType) {
                case (byte) ItemHolderType.PLAYER:
                    if(ModManager.clientSync.syncData.players.ContainsKey(creatureId)) {
                        PlayerNetworkData ps = ModManager.clientSync.syncData.players[creatureId];
                        creature = ps.creature;
                        //name = "player " + ps.name;
                    }
                    break;
                case (byte) ItemHolderType.CREATURE:
                    if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                        CreatureNetworkData cs = ModManager.clientSync.syncData.creatures[creatureId];
                        creature = cs.creature;
                        //name = "creature " + cs.creatureType;
                    }
                    break;
                default: break;
            }

            if(creature != null) {
                RagdollHand hand = creature.GetHand((Side) side);
                if(hand != null && hand.poser != null) {
                    hand.poser.UpdatePoseThumb(thumbWeight);
                    hand.poser.UpdatePoseIndex(indexWeight);
                    hand.poser.UpdatePoseMiddle(middleWeight);
                    hand.poser.UpdatePoseRing(ringWeight);
                    hand.poser.UpdatePoseLittle(littleWeight);
                }
            }

            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientInformation client) {
            server.SendToAllExcept(this, client.ClientId);
            return true;
        }
    }
}
