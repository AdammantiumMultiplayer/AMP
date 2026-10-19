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

        public static readonly string[] ignoredItems = {
            "inventorybag"
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
            { "knife",    "DaggerSkinning"      },
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
            { "maul",     "MaceLongMaul"        },
            { "mace",     "MaceShortFlanged"    },
            { "torch",    "Torch"               },
            { "lamp",     "Torch"               },
            { "sickle",   "ToolSickle"          },
        };

        public static readonly Dictionary<string, string> wardrobeNameReplacement = new Dictionary<string, string>() {
            { "shirt",    "ApparelShirt04"         },
            { "pants",    "ApparelCivilianLegs"    },
            { "boots",    "ApparelSoldierBoots"    },
            { "chest",    "ApparelSoldierJacket"   },
            { "helmet",   "ApparelSoldierHelmet"   },
            { "hood",     "ApparelBattlemageCloak" },
        };
		
        // Spell id used when a synced spell isn't found locally (e.g. mod not installed)
        public const string FALLBACK_SPELL_ID = "Gravity";

        public static int BASE_TICK_RATE = 10;
        public static int PLAYER_TICK_RATE = 30;

        public const float MOVEMENT_TIME = 1.0f; // 1.05 to compensate for lag
        public static float MOVEMENT_DELTA_TIME {
            get { return MOVEMENT_TIME / BASE_TICK_RATE; }
        }
        public static float PLAYER_MOVEMENT_DELTA_TIME {
            get { return MOVEMENT_TIME / PLAYER_TICK_RATE; }
        }

        public const long NET_COMP_DISABLE_DELAY = 1000; // Time in milliseconds on how long there is no packet to disable the smoothing on that item

        // Debug: Shows a sphere (ragdoll state) and a cube (ownership) above every synced NPC, see CreatureDebugIndicator for the colors
        public static bool SHOW_CREATURE_DEBUG_INDICATOR = true;
        public static bool LOG_RAGDOLL_DEBUGGING = true;
        
        // Assume the item is the same if they are the same if they are not that much apart
        public const float SMALL_ITEM_CLONE_MAX_DISTANCE = 0.1f; //~10cm
        public const float MEDIUM_ITEM_CLONE_MAX_DISTANCE = 0.5f; //~50cm
        public const float BIG_ITEM_CLONE_MAX_DISTANCE = 2f; //~2m

        // Min distance for a ragdoll to move (in sum for every bone)
        public const float REQUIRED_RAGDOLL_MOVE_DISTANCE = 0.1f ; // ~10cm

        // Min rotation of a single ragdoll bone before the ragdoll is updated (so turning a hand in place gets synced as well)
        public const float REQUIRED_RAGDOLL_ROTATION_DISTANCE = 5f; // ~5°

        // Min distance a item needs to move before its position is updated
        public const float REQUIRED_MOVE_DISTANCE = 0.05f; // ~5cm

        // Min distance a player needs to move before its position is updated
        public const float REQUIRED_PLAYER_MOVE_DISTANCE = 0.05f; // ~5cm

        // Min distance a item needs to rotate before its position is updated
        public const float REQUIRED_ROTATION_DISTANCE = 2f; // ~2°

        // Min change in a finger close weight before the hand pose is resent
        public const float REQUIRED_HAND_POSE_CHANGE = 0.02f;

        public static bool PLAYER_FULL_BODY_SYNCING = true;
       
        public const float SHORT_WAIT_DELAY = 0.01f;
        public const float LONG_WAIT_DELAY = 0.05f;

        public const int       LATENCY_COMP_ADDITION = 0; // 1000 / TICK_RATE;
        public const float MAX_LATENCY_COMP_FACTOR   = 0.75f;

        public const float REQUIRED_DAMAGE_FOR_CREATURE_TRANSFER = 0.01f; // 1% of max health
    }
}
