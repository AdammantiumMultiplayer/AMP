using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMP.Network.Packets {
    public enum PacketType : byte {
        UNKNOWN                 = 0,

        WELCOME                 = 1,
        DISCONNECT,
        ERROR,
        MESSAGE,

        PLAYER_DATA             = 10,
        PLAYER_POSITION,
        PLAYER_EQUIPMENT,
        PLAYER_RAGDOLL,
        PLAYER_HEALTH_SET,
        PLAYER_HEALTH_CHANGE,

        ITEM_SPAWN              = 20,
        ITEM_DESPAWN,
        ITEM_POSITION,
        ITEM_OWNER,
        ITEM_SNAPPING_SNAP,
        ITEM_SNAPPING_UNSNAP,
        ITEM_IMBUE,

        LEVEL_CHANGE            = 39,

        CREATURE_SPAWN          = 40,
        CREATURE_POSITION,
        CREATURE_HEALTH_SET,
        CREATURE_HEALTH_CHANGE,
        CREATURE_DESPAWN,
        CREATURE_PLAY_ANIMATION,
        CREATURE_RAGDOLL,
        CREATURE_SLICE,
        CREATURE_OWNER,
    }
}
