using AMP.Network.Server;
using System.Collections.Generic;
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

        public static readonly RagdollPart.Type[] playerRagdollTypesToFreeze = {
            RagdollPart.Type.LeftArm,
            RagdollPart.Type.LeftHand,

            RagdollPart.Type.RightArm,
            RagdollPart.Type.RightHand
        };

        public static readonly Dictionary<ItemData.Type, string> itemCategoryReplacement = new Dictionary<ItemData.Type, string>() {
            { ItemData.Type.Shield, "ShieldRound" },
            { ItemData.Type.Food,   "FoodApple" },

            { ItemData.Type.Misc,   "SwordShortCommon" } // Others
        };

        public static readonly Dictionary<string, string> itemNameReplacement = new Dictionary<string, string>() {
            { "dagger",   "DaggerCommon"        },
            { "pickaxe",  "ToolPickaxe"         },
            { "axe",      "AxeShortCommon"      },
            { "spear",    "SpearBoar"           },
            { "hammer",   "MaceShortBlacksmith" },
            { "crossbow", "BowCommon"           },
            { "bolt",     "Arrow"               },
            { "bullet",   "Arrow"               },
            { "staff",    "StaffDruid"          },
            { "shield",   "ShieldRound"         },
            { "potion",   "PotionHealth"        },
            { "food",     "FoodApple"           },
        };

        public const int TICK_RATE = 10;

        public const float MOVEMENT_TIME = 1.05f; // 1.05 to compensate for lag
        public const float MOVEMENT_DELTA_TIME = MOVEMENT_TIME / TICK_RATE;

        public const float NET_COMP_DISABLE_DELAY = 0.5f; // Time in seconds on how long there is no packet to disable the smoothing on that item

        // Assume the item is the same if they are the same if they are not that much apart
        public const float SMALL_ITEM_CLONE_MAX_DISTANCE = 0.01f * 0.01f; //~1cm
        public const float MEDIUM_ITEM_CLONE_MAX_DISTANCE = 0.2f * 0.2f; //~20cm
        public const float BIG_ITEM_CLONE_MAX_DISTANCE = 1f * 1f; //~1m

        // Min distance for a ragdoll to move (in sum for every bone)
        public const float REQUIRED_RAGDOLL_MOVE_DISTANCE = 0.01f * 0.01f; // ~1cm

        // Min distance a item needs to move before its position is updated
        public const float REQUIRED_MOVE_DISTANCE = 0.025f * 0.025f; // ~2.5cm

        // Min distance a player needs to move before its position is updated
        public const float REQUIRED_PLAYER_MOVE_DISTANCE = 0.01f * 0.01f; // ~1cm

        // Min distance a item needs to rotate before its position is updated
        public const float REQUIRED_ROTATION_DISTANCE = 2f * 2f; // ~2°

        public static bool PLAYER_FULL_BODY_SYNCING = true;
        
        public const float SHORT_WAIT_DEALY = 0.01f;
        public const float  LONG_WAIT_DEALY = 0.05f;

        public const int       LATENCY_COMP_ADDITION = 0; // 1000 / TICK_RATE;
        public const float MAX_LATENCY_COMP_FACTOR   = 0.75f;
    }
}
