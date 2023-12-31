namespace AMP.Network.Packets {
    public enum PacketType : byte {
        UNKNOWN                 = 0,

        // First 10 values are reserved for Netamite

        PLAYER_DATA             = 10,
        PLAYER_POSITION,
        PLAYER_EQUIPMENT,
        PLAYER_RAGDOLL,
        PLAYER_HEALTH_SET,
        PLAYER_HEALTH_CHANGE,
        PLAYER_TELEPORT,

        ITEM_SPAWN              = 20,
        ITEM_DESPAWN,
        ITEM_POSITION,
        ITEM_OWNER,
        ITEM_SNAPPING_SNAP,
        ITEM_SNAPPING_UNSNAP,
        ITEM_IMBUE,
        ITEM_BREAK,
        ITEM_SLIDE,

        PREPARE_LEVEL_CHANGE    = 40,
        DO_LEVEL_CHANGE,
        CLEAR_DATA,
        NAMETAG_VISIBILITY,
        CHANGE_FACTION,

        CREATURE_SPAWN          = 50,
        CREATURE_POSITION,
        CREATURE_HEALTH_SET,
        CREATURE_HEALTH_CHANGE,
        CREATURE_DESPAWN,
        CREATURE_PLAY_ANIMATION,
        CREATURE_RAGDOLL,
        CREATURE_SLICE,
        CREATURE_OWNER,
        CREATURE_HAND_FINGERS,
        CREATURE_WAYPOINTS,

        MAGIC_SET               = 100,
        MAGIC_CHARGE,

        MOD_LIST                = 110,

        SIZE_CHANGE             = 240,

        DISPLAY_TEXT            = 250,

        ALLOW_TRANSMISSION      = 251,
        PING                    = 252,

        SERVER_INFO             = 253,
        SERVER_JOIN             = 254,
        SERVER_STATUS_PING      = 255
    }
}
