using AMP.Logging;
using AMP.Network.Data.Sync;
using Netamite.Client.Definition;
using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using System.Collections.Generic;
using ThunderRoad;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.CREATURE_WAYPOINTS)]
    public class CreatureWaypointsPacket : AMPPacket {
        [SyncedVar]       public int creatureId;
        [SyncedVar(true)] public Vector3[] waypoints;

        public CreatureWaypointsPacket() { }

        public CreatureWaypointsPacket(int creatureId, Vector3[] waypoints) {
            this.creatureId = creatureId;
            this.waypoints = waypoints;
        }

        public override bool ProcessClient(NetamiteClient client) {
            if(ModManager.clientSync.syncData.creatures.ContainsKey(creatureId)) {
                CreatureNetworkData cnd = ModManager.clientSync.syncData.creatures[creatureId];

                if(waypoints != null && cnd.creature != null && cnd.creature.brain != null && cnd.creature.brain.instance != null) {
                    BrainModulePatrol module = cnd.creature.brain.instance.GetModule<BrainModulePatrol>();
                    if(module != null) {
                        List<WayPoint> waypointList = new List<WayPoint>();
                        foreach(var waypoint in waypoints) {
                            GameObject gobj = new GameObject("wp");
                            WayPoint wp2 = gobj.AddComponent<WayPoint>();
                            gobj.transform.position = waypoint;
                            wp2.target = gobj.transform;

                            waypointList.Add(wp2);
                        }
                        module.waypoints = waypointList.ToArray();
                        module.targetWaypointIndex = 0;
                    }
                }

            }
            return true;
        }
    }
}
