using Netamite.Network.Packet;
using Netamite.Network.Packet.Attributes;
using UnityEngine;

namespace AMP.Network.Packets.Implementation {
    [PacketDefinition((byte) PacketType.UNKNOWN)]
    public class TestPacket : NetPacket {

        [SyncedVar] public byte       byte_test = 0;
        [SyncedVar] public byte[]     byte_arr_test = new byte[0];
        [SyncedVar] public short      short_test = 0;
        [SyncedVar] public int        int_test = 0;
        [SyncedVar] public long       long_test = 0;
        [SyncedVar] public float      float_test = 0;
        [SyncedVar] public bool       bool_test = false;
        [SyncedVar] public string     string_test = "test";
        [SyncedVar] public Vector3    vec3_test = Vector3.one;
        [SyncedVar] public Quaternion quaternion_test = Quaternion.identity;
        [SyncedVar] public Color      color_test = Color.white;

        [SyncedVar(true)] public Vector3[] vector3s = { Vector3.one, Vector3.one, Vector3.one, Vector3.one, Vector3.one, Vector3.one, Vector3.one, Vector3.one, Vector3.one };

        public TestPacket() { }

    }
}
