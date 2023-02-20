using AMP.Logging;
using AMP.Network.Client.NetworkComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;

namespace AMP.Unsafe.Patching {
    internal class CreaturePatch : Creature {

        public State patched_state {
            get {
                Log.Err("HOLY SHIT IS THIS A STUPID THING TO DO...");

                if(isKilled) {
                    return State.Dead;
                }

                if(ragdoll.state == Ragdoll.State.Destabilized || ragdoll.state == Ragdoll.State.Inert) {
                    if(GetComponent<NetworkPlayerCreature>() == null) {
                        return State.Destabilized;
                    }
                }

                return State.Alive;
            }
        }

        public bool IsEnemy(Creature creatureTarget) {
            Log.Err("HOLY SHIT IS THIS A STUPID THING TO DO...");

            if(faction.attackBehaviour == GameData.Faction.AttackBehaviour.Passive) {
                return false;
            }

            if(faction.attackBehaviour == GameData.Faction.AttackBehaviour.Ignored || creatureTarget.faction.attackBehaviour == GameData.Faction.AttackBehaviour.Ignored) {
                return false;
            }

            if(creatureTarget == this || creatureTarget.state != State.Alive) {
                return false;
            }

            if(faction.attackBehaviour != GameData.Faction.AttackBehaviour.Agressive && factionId == creatureTarget.factionId) {
                return false;
            }

            return true;
        }
    }
}
