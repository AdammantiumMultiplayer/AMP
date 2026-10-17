using AMP.Datatypes;
using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using Netamite.Server.Definition;
using ThunderRoad;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition(true, (byte) PacketType.CREATURE_HAND_POSE)]
    public class HandPosePacket : AMPPacket {
        [SyncedVar]       public int    creatureId;
        [SyncedVar]       public byte   creatureType;
        [SyncedVar]       public byte   side;
        [SyncedVar]       public string targetPoseId;
        [SyncedVar]       public string defaultPoseId;

        public HandPosePacket() { }

        public HandPosePacket(int creatureId, byte creatureType, byte side, string targetPoseId, string defaultPoseId) {
            this.creatureId    = creatureId;
            this.creatureType  = creatureType;
            this.side          = side;
            this.targetPoseId  = targetPoseId;
            this.defaultPoseId = defaultPoseId;
        }

        public HandPosePacket(CreatureNetworkData ind, Side side) {
            this.creatureId   = ind.networkedId;
            this.creatureType = (byte) ItemHolderType.CREATURE;
            this.side         = (byte) side;

            ReadHand(ind.creature.GetHand(side));
        }

        public HandPosePacket(PlayerNetworkData ind, Side side) {
            this.creatureId   = ind.clientId;
            this.creatureType = (byte)ItemHolderType.PLAYER;
            this.side         = (byte)side;

            ReadHand(ind.creature.GetHand(side));
        }

        private void ReadHand(RagdollHand ragdollHand) {
            if(ragdollHand != null && ragdollHand.poser != null) {
                targetPoseId  = ragdollHand.poser.targetHandPoseId;
                defaultPoseId = ragdollHand.poser.defaultHandPoseId;
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
                    if(hand.poser.targetHandPoseId != targetPoseId) {
                        HandPoseData targetPose = Catalog.GetData<HandPoseData>(targetPoseId);
                        if (targetPose == null) {
                            targetPose = Catalog.GetData<HandPoseData>("DefaultClose");
                            Log.Warn($"Couldn't find hand pose {targetPoseId}. Probably missing mods. Falling back to DefaultClose.");
                        }
                        hand.poser.SetTargetPose(targetPose);
                    }
                    if (hand.poser.defaultHandPoseId != defaultPoseId) {
                        HandPoseData defaultPose = Catalog.GetData<HandPoseData>(defaultPoseId);
                        if (defaultPose == null) {
                            defaultPose = Catalog.GetData<HandPoseData>("DefaultOpen");
                            Log.Warn($"Couldn't find hand pose {defaultPoseId}. Probably missing mods.  Falling back to DefaultOpen.");
                        }
                        
                        hand.poser.SetDefaultPose(defaultPose);
                    }
                    hand.AttachFingerMeshBoneToRagdoll();
                }
            }

            return true;
        }

        public override bool ProcessServer(NetamiteServer server, ClientData client) {
            server.SendToAllExcept(this, client.ClientId);
            return true;
        }
    }
}
