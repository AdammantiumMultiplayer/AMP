﻿using AMP.Network.Packets.Attributes;
using AMP.Network.Packets.Exceptions;
using AMP.Network.Packets.Extension;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace AMP.Network.Packets {
    [PacketDefinition((byte) PacketType.UNKNOWN)]
    public class NetPacket {

        public byte[] GetData() {
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

                        object[] array = new object[val.Count];
                        val.CopyTo(array, 0);

                        //Console.WriteLine(memberInfo.Name + "\t" + t + "\t" + val);

                        stream.WriteArray(array, (atts[0] as SyncedVar).LowPrecision);
                    } else {
                        object val = memberInfo.GetValue(this);

                        //Console.WriteLine(memberInfo.Name + "\t" + t + "\t" + val);

                        stream.Write(val, (atts[0] as SyncedVar).LowPrecision);
                    }
                }
            }

            return stream.ToArray();
        }

        public void SetData(byte[] data) {
            BinaryNetStream stream = new BinaryNetStream(data);

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

        private byte getPacketType() {
            return ((PacketDefinition) GetType().GetCustomAttribute(typeof(PacketDefinition), true)).packetType;
        }
    }
}