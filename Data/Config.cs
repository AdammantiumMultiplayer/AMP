using ThunderRoad;

namespace AMP.Data {
    public static class Config {

        public const long DISCORD_APP_ID = 1005583334392471583;


        public static readonly ItemData.Type[] ignoredTypes = {
            ItemData.Type.Body,
            ItemData.Type.Spell,
            //ItemData.Type.Prop,
            ItemData.Type.Wardrobe
        };

        public static readonly object[,] itemCategoryReplacement = {
            { ItemData.Type.Shield, "ShieldRound" },
            { ItemData.Type.Food,   "FoodApple" },

            { ItemData.Type.Misc,   "SwordShortCommon" } // Others
        };

        public const int TICK_RATE = 10;

        public const float MOVEMENT_TIME = 1.05f; // 1.05 to compensate for lag
        public static float MOVEMENT_DELTA_TIME {
            get { return MOVEMENT_TIME / TICK_RATE; }
        }

        public const float NET_COMP_DISABLE_DELAY = 0.5f; // Time in seconds on how long it there is no packet to disable the smoothing on that item


        public const int MAX_ITEMS_FOR_CLIENT = 150; // TODO: Maybe implement a item limit per client

        // Assume the item is the same if they are the same if they are not that much apart
        public const float SMALL_ITEM_CLONE_MAX_DISTANCE = 0.01f * 0.01f; //~1cm
        public const float MEDIUM_ITEM_CLONE_MAX_DISTANCE = 0.1f * 0.1f; //~10cm
        public const float BIG_ITEM_CLONE_MAX_DISTANCE = 1f * 1f; //~1m

        // Min distance for a ragdoll to move (in sum for every bone)
        public const float REQUIRED_RAGDOLL_MOVE_DISTANCE = 0.025f * 0.025f;

        // Min distance a item needs to move before its position is updated
        public const float REQUIRED_MOVE_DISTANCE = 0.025f * 0.025f; // ~2.5cm

        // Min distance a player needs to move before its position is updated
        public const float REQUIRED_PLAYER_MOVE_DISTANCE = 0.01f * 0.01f; // ~1cm

        // Min distance a item needs to rotate before its position is updated
        public const float REQUIRED_ROTATION_DISTANCE = 2f * 2f; // ~2°


        public static bool PLAYER_FULL_BODY_SYNCING = true;
    }
}
