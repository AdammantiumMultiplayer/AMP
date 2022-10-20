using AMP.Network.Packets.Attributes;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_PLAY_ANIMATION)]
    public class CreatureAnimationPacket : NetPacket {
        [SyncedVar] public long   creatureId;
        [SyncedVar] public string animationClip;
    }
}
