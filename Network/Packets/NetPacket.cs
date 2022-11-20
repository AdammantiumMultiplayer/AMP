using AMP.Logging;
using AMP.Network.Packets.Attributes;
using AMP.Network.Packets.Exceptions;
using AMP.Network.Packets.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AMP.Network.Packets {
    [PacketDefinition((byte) PacketType.UNKNOWN)]
    public class NetPacket : IDisposable {

        private static Dictionary<PacketType, Type> packetTypes = new Dictionary<PacketType, Type>();

        private static Type GetPacketImplementation(PacketType packetType) {
            if(packetType == PacketType.UNKNOWN) return null;

            if(packetTypes.ContainsKey(packetType)) {
                return packetTypes[packetType];
            }

            Type[] typelist = Assembly.GetAssembly(typeof(NetPacket)).GetTypes()
                    .Where(t => string.Equals( t.Namespace
                                            , "AMP.Network.Packets.Implementation"
                                            , StringComparison.Ordinal
                                            )
                        )
                    .ToArray();

            for(int i = 0; i < typelist.Length; i++) {
                PacketType thisPacketType = (PacketType) getPacketType(typelist[i]);
                if(thisPacketType == packetType) {
                    if(packetTypes.ContainsKey(thisPacketType)) continue;
                    packetTypes.Add(thisPacketType, typelist[i]);
                    return typelist[i];
                }
            }
            return null;
        }

        public static NetPacket ReadPacket(byte[] data, bool hasLength = false) {
            BinaryNetStream stream = new BinaryNetStream(data);

            if(hasLength) stream.ReadShort(); // Flush away the length
            PacketType type = (PacketType) stream.ReadByte(false);
            
            Type implementationType = GetPacketImplementation(type);
            if(implementationType == null) return null;

            object packetInstance = Activator.CreateInstance(implementationType);
            if(packetInstance is NetPacket) {
                NetPacket np = (NetPacket) packetInstance;
                np.SetData(data, hasLength);
                return np;
            }

            return null;
        }

        public byte[] GetData(bool writeLength = false) {
            BinaryNetStream stream = new BinaryNetStream();

            stream.Write(getPacketType());

            Type myType = GetType();
            MemberInfo[] myMembers = myType.GetMembers();

            for(int i = 0; i < myMembers.Length; i++) {
                MemberInfo memberInfo = myMembers[i];

                object[] atts = memberInfo.GetCustomAttributes(typeof(SyncedVar), true);

                if(atts.Length > 0) {
                    Type t = memberInfo.GetUnderlyingType();

                    if(t.IsArray) {
                        System.Collections.IList val = (System.Collections.IList) memberInfo.GetValue(this);
                        
                        if(val == null) {
                            stream.WriteArray(null);
                        } else {
                            object[] array = new object[val.Count];
                            val.CopyTo(array, 0);

                            //Console.WriteLine(memberInfo.Name + "\t" + t + "\t" + val);

                            stream.WriteArray(array, (atts[0] as SyncedVar).LowPrecision);
                        }
                    } else {
                        object val = memberInfo.GetValue(this);

                        //Console.WriteLine(memberInfo.Name + "\t" + t + "\t" + val);

                        stream.Write(val, (atts[0] as SyncedVar).LowPrecision);
                    }
                }
            }

            if(writeLength) {
                stream.WriteLength();
            }

            return stream.ToArray();
        }

        public void SetData(byte[] data, bool hasLength = false) {
            BinaryNetStream stream = new BinaryNetStream(data);

            if(hasLength) stream.ReadShort(); // Flush away the length

            PacketType dataPacketType = (PacketType) stream.ReadByte();
            PacketType myPacketType   = (PacketType) getPacketType();

            if(dataPacketType != myPacketType) {
                throw new PacketMismatch(myPacketType, dataPacketType);
            }

            Type myType = GetType();
            MemberInfo[] myMembers = myType.GetMembers();

            for(int i = 0; i < myMembers.Length; i++) {
                MemberInfo memberInfo = myMembers[i];

                object[] atts = memberInfo.GetCustomAttributes(typeof(SyncedVar), true);

                if(atts.Length > 0) {
                    Type t = memberInfo.GetUnderlyingType();

                    if(t.IsArray) {
                        object[] val = stream.ReadArray(t.GetElementType(), (atts[0] as SyncedVar).LowPrecision);
                        
                        if(val.Length > 0) {
                            Array filledArray = Array.CreateInstance(val[0].GetType(), val.Length);
                            Array.Copy(val, filledArray, val.Length);

                            //Console.WriteLine(memberInfo.Name + "\t" + t + "\t" + filledArray);

                            memberInfo.SetValue(this, filledArray);
                        }
                    } else {
                        object val = stream.Read(t, (atts[0] as SyncedVar).LowPrecision);

                        //Console.WriteLine(memberInfo.Name + "\t" + t + "\t" + val);

                        memberInfo.SetValue(this, val);
                    }
                }
            }
        }

        public byte getPacketType() {
            return getPacketType(GetType());
        }

        private static byte getPacketType(Type type) {
            Attribute attribute = type.GetCustomAttribute(typeof(PacketDefinition), true);
            if(attribute != null) { 
                return ((PacketDefinition) attribute).packetType;
            }
            return (byte) PacketType.UNKNOWN;
        }

        public void Dispose() {
            
        }
    }
}
