using AMP.Extension;
using AMP.GameInteraction;
using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using AMP.Network.Packets.Implementation;
using ThunderRoad;

namespace AMP.Network.Client {
    internal class NetworkLocalPlayer : NetworkCreature {

        internal static NetworkLocalPlayer Instance;

        void Awake() {
            creature = Player.currentCreature;

            Instance = this;

            //Log.Warn("INIT Local Player");

            RegisterEvents();
        }

        public override ManagedLoops EnabledManagedLoops => 0;

        internal override bool IsSending() {
            return true;
        }

        #region Register Events
        private bool registeredEvents = false;
        internal new void RegisterEvents() {
            if(registeredEvents) return;

            foreach(Wearable w in creature.equipment.wearableSlots) {
                w.OnItemEquippedEvent += W_OnItemEquippedEvent;
            }

            creature.OnHealEvent   += Creature_OnHealEvent;
            creature.OnDamageEvent += Creature_OnDamageEvent;
            creature.OnKillEvent   += Creature_OnKillEvent;

            //Player.currentCreature.handLeft.caster.magicSource.GetComponentInChildren<Trigger>().callBack += (other, enter) => { Log.Warn(Player.currentCreature.handLeft.caster.spellInstance); };
            //Player.currentCreature.handRight.caster.magicSource.GetComponentInChildren<Trigger>().callBack += (other, enter) => { Log.Warn(Player.currentCreature.handRight.caster.spellInstance); };
            //Player.currentCreature.handLeft.caster.spellInstance

            RegisterGrabEvents();

            SendHealthPacket();

            registeredEvents = true;
        }
        #endregion

        #region Unregister Events
        protected override void ManagedOnDisable() {
            Destroy(this);
            UnregisterEvents();
        }

        internal new void UnregisterEvents() {
            if(!registeredEvents) return;

            foreach(Wearable w in creature.equipment.wearableSlots) {
                w.OnItemEquippedEvent -= W_OnItemEquippedEvent;
            }

            creature.OnHealEvent   -= Creature_OnHealEvent;
            creature.OnDamageEvent -= Creature_OnDamageEvent;
            creature.OnKillEvent   -= Creature_OnKillEvent;

            UnregisterGrabEvents();

            registeredEvents = false;
        }
        #endregion

        #region Wearable Events
        private void W_OnItemEquippedEvent(Item item) {
            if(ModManager.clientInstance == null) return;
            if(ModManager.clientSync == null) return;

            CreatureEquipment.Read(ModManager.clientSync.syncData.myPlayerData);
            new PlayerEquipmentPacket(ModManager.clientSync.syncData.myPlayerData).SendToServerReliable();
        }
        #endregion

        #region Creature Events
        private void Creature_OnHealEvent(float heal, Creature healer, EventTime eventTime) {
            SendHealthPacket();
        }

        private void Creature_OnDamageEvent(CollisionInstance collisionInstance, EventTime eventTime) {
            SendHealthPacket();
        }

        private void Creature_OnKillEvent(CollisionInstance collisionInstance, EventTime eventTime) {
            if(eventTime == EventTime.OnStart) return;
            SendHealthPacket();
        }
        #endregion

        internal void SendHealthPacket() {
            if(creature == null) return;

            if(creature.isKilled) {
                ModManager.clientSync.syncData.myPlayerData.health = 0;
            } else {
                ModManager.clientSync.syncData.myPlayerData.health = creature.currentHealth / creature.maxHealth;
            }

            new PlayerHealthSetPacket(ModManager.clientSync.syncData.myPlayerData).SendToServerReliable();
        }
    }
}
