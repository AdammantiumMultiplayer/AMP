using AMP.Logging;
using AMP.Network.Data;
using AMP.Network.Data.Sync;
using System;

namespace AMP.Events {
    public class ServerEvents {

        #region Player
        public delegate void OnPlayerJoin(ClientData player);
        public static event OnPlayerJoin onPlayerJoin;

        public delegate void OnPlayerQuit(ClientData player);
        public static event OnPlayerQuit onPlayerQuit;

        public delegate void OnPlayerKilled(ClientData killed, ClientData killer);
        public static event OnPlayerKilled onPlayerKilled;

        public delegate void OnPlayerDamaged(ClientData damaged, float damage, ClientData damager);
        public static event OnPlayerDamaged onPlayerDamaged;

        public delegate void OnPlayerSpawned(ClientData player);
        public static event OnPlayerSpawned onPlayerSpawned;
        #endregion

        #region Item
        public delegate void OnItemSpawned(ItemNetworkData item, ClientData clientSpawned);
        public static event OnItemSpawned onItemSpawned;

        public delegate void OnItemDespawned(ItemNetworkData item, ClientData clientDespawned);
        public static event OnItemDespawned onItemDespawned;

        public delegate void OnItemOwnerChanged(ItemNetworkData item, ClientData oldOwner, ClientData newOwner);
        public static event OnItemOwnerChanged onItemOwnerChanged;
        #endregion

        #region Creature
        public delegate void OnCreatureSpawned(CreatureNetworkData creature, ClientData spawner);
        public static event OnCreatureSpawned onCreatureSpawned;

        public delegate void OnCreatureDespawned(CreatureNetworkData creature, ClientData spawner);
        public static OnCreatureDespawned onCreatureDespawned;

        public delegate void OnCreatureKilled(CreatureNetworkData creature, ClientData killer);
        public static OnCreatureKilled onCreatureKilled;

        public delegate void OnCreatureOwnerChanged(CreatureNetworkData creature, ClientData oldOwner, ClientData newOwner);
        public static OnCreatureOwnerChanged onCreatureOwnerChanged;

        public delegate void OnCreatureDamaged(CreatureNetworkData creature, float damage, ClientData damager);
        public static OnCreatureDamaged onCreatureDamaged;
        #endregion


        #region Player Events
        internal static void InvokeOnPlayerJoin(ClientData client) {
            if(onPlayerJoin == null) return;

            foreach(Delegate handler in onPlayerJoin.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(client);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }

        internal static void InvokeOnPlayerQuit(ClientData client) {
            if(onPlayerQuit == null) return;

            foreach(Delegate handler in onPlayerQuit.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(client);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }

        internal static void InvokeOnPlayerKilled(ClientData killed, ClientData killer) {
            if(onPlayerKilled == null) return;

            foreach(Delegate handler in onPlayerKilled.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(killed, killer);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }

        internal static void InvokeOnPlayerDamaged(ClientData damaged, float damage, ClientData damager) {
            if(onPlayerDamaged == null) return;

            foreach(Delegate handler in onPlayerDamaged.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(damaged, damage, damager);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }

        internal static void InvokeOnPlayerSpawned(ClientData client) {
            if(onPlayerSpawned == null) return;

            foreach(Delegate handler in onPlayerSpawned.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(client);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }
        #endregion


        #region Item Events
        internal static void InvokeOnItemSpawned(ItemNetworkData itemData, ClientData clientSpawned) {
            if(onItemSpawned == null) return;

            foreach(Delegate handler in onItemSpawned.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(itemData, clientSpawned);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }

        internal static void InvokeOnItemDespawned(ItemNetworkData itemData, ClientData clientDespawned) {
            if(onItemDespawned == null) return;

            foreach(Delegate handler in onItemDespawned.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(itemData, clientDespawned);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }

        internal static void InvokeOnItemOwnerChanged(ItemNetworkData itemData, ClientData oldOwner, ClientData newOwner) {
            if(onItemOwnerChanged == null) return;

            foreach(Delegate handler in onItemOwnerChanged.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(itemData, oldOwner, newOwner);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }
        #endregion


        #region Creature Events
        internal static void InvokeOnCreatureSpawned(CreatureNetworkData creatureData, ClientData clientSpawned) {
            if(onCreatureSpawned == null) return;

            foreach(Delegate handler in onCreatureSpawned.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(creatureData, clientSpawned);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }

        internal static void InvokeOnCreatureDespawned(CreatureNetworkData creatureData, ClientData clientDespawned) {
            if(onCreatureDespawned == null) return;

            foreach(Delegate handler in onCreatureDespawned.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(creatureData, clientDespawned);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }

        internal static void InvokeOnCreatureKilled(CreatureNetworkData creatureData, ClientData killer) {
            if(onCreatureKilled == null) return;

            foreach(Delegate handler in onCreatureKilled.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(creatureData, killer);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }

        internal static void InvokeOnCreatureOwnerChanged(CreatureNetworkData creatureData, ClientData oldOwner, ClientData newOwner) {
            if(onCreatureOwnerChanged == null) return;

            foreach(Delegate handler in onCreatureOwnerChanged.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(creatureData, oldOwner, newOwner);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }

        internal static void InvokeOnCreatureDamaged(CreatureNetworkData creature, float damage, ClientData damager) {
            if(onCreatureDamaged == null) return;

            foreach(Delegate handler in onCreatureDamaged.GetInvocationList()) {
                try {
                    handler.DynamicInvoke(creature, damage, damager);
                } catch(Exception e) {
                    Log.Err(e);
                }
            }
        }
        #endregion
    }
}
