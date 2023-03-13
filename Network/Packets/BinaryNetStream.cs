using AMP.Extension;
using AMP.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using SystemHalf;
using UnityEngine;

namespace AMP.Network.Packets {
    public class BinaryNetStream {
        private List<byte> buffer = new List<byte>();

        private byte[] readableBuffer;
        private int readPos;
        private bool disposed;


        internal BinaryNetStream() {
            readPos = 0;
        }

        internal BinaryNetStream(PacketType type) {
            readPos = 0;
            Write((byte) type);
        }

        internal BinaryNetStream(byte[] data) {
            readPos = 0;
            SetBytes(data);
        }

        internal void SetBytes(byte[] data) {
            Write(data);
            readableBuffer = buffer.ToArray();
        }

        internal void InsertInt(int value) {
            buffer.InsertRange(0, BitConverter.GetBytes(value));
        }

        internal byte[] ToArray() {
            readableBuffer = buffer.ToArray();
            return readableBuffer;
        }

        internal int Length() {
            return buffer.Count;
        }

        internal int UnreadLength() {
            return Length() - readPos;
        }

        internal void ResetPos() {
            readPos = 0;
        }

        internal void Reset(bool shouldReset = true) {
            if(shouldReset) {
                buffer.Clear();
                readableBuffer = null;
                readPos = 0;
                return;
            }
            readPos -= 4;
        }

        #region Write to the Buffer
        internal void WriteLength() {
            buffer.InsertRange(0, BitConverter.GetBytes((short) buffer.Count));
        }

        internal void Write(byte value) {
            buffer.Add(value);
        }

        internal void Write(byte[] value) {
            buffer.AddRange(value);
        }

        internal void Write(short value) {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void Write(ushort value) {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void Write(uint value) {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void Write(int value) {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void Write(ulong value) {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void Write(long value) {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void Write(float value) {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void WriteLP(float value) { // Write Low Precision
            Write(Half.GetBytes(new Half(value)));
        }

        internal void Write(bool value) {
            buffer.AddRange(BitConverter.GetBytes(value));
        }

        internal void Write(string value) {
            byte[] str_bytes = Encoding.UTF8.GetBytes(value);

            Write((ushort) str_bytes.Length);
            if(str_bytes.Length > 0) buffer.AddRange(str_bytes);
        }

        internal void Write(Vector3 value) {
            Write(value.x);
            Write(value.y);
            Write(value.z);
        }

        internal void WriteLP(Vector3 value) { // Write Low Precision
            WriteLP(value.x);
            WriteLP(value.y);
            WriteLP(value.z);
        }

        internal void Write(Quaternion value) {
            Write(value.x);
            Write(value.y);
            Write(value.z);
            Write(value.w);
        }

        internal void WriteLP(Quaternion value) {
            Write(value.Compress());
        }

        internal void Write(Color value) {
            WriteLP(value.r);
            WriteLP(value.g);
            WriteLP(value.b);
        }

        internal void WriteArray(object[] val, bool lowPrecision = false) {
            if(val == null) {
                Write((byte) 0);
                return;
            }

            Type type = val.GetType();

            if(type.IsArray) {
                Write((byte) val.Length);

                foreach(object v in val) {
                    Write(v, lowPrecision);
                }
            }
        }

        internal void Write(object val, bool lowPrecision = false) {
            Type type = val.GetType();

            switch(Type.GetTypeCode(type)) {
                case TypeCode.Byte:
                    Write((byte)   val); break;

                case TypeCode.Int16:
                    Write((short)  val); break;
                case TypeCode.Int32:
                    Write((int)    val); break;
                case TypeCode.Int64:
                    Write((long)   val); break;

                case TypeCode.UInt16:
                    Write((ushort) val); break;
                case TypeCode.UInt32:
                    Write((uint) val); break;
                case TypeCode.UInt64:
                    Write((ulong) val); break;

                case TypeCode.String:
                    Write((string) val); break;

                case TypeCode.Boolean:
                    Write((bool)   val); break;

                default:
                    if(type == typeof(float)) {
                        if(lowPrecision) WriteLP((float) val);
                        else             Write  ((float) val);
                    } else if(type == typeof(Quaternion)) {
                        if(lowPrecision) WriteLP((Quaternion) val);
                        else             Write  ((Quaternion) val);
                    } else if(type == typeof(Vector3)) {
                        if(lowPrecision) WriteLP((Vector3) val);
                        else             Write  ((Vector3) val);
                    } else if(type == typeof(Color)) {
                        Write((Color) val);
                    }

                    break;
            }
        }
        #endregion

        #region Read from buffer
        internal PacketType ReadType(bool moveReadPos = true) {
            return (PacketType) ReadByte(moveReadPos);
        }

        internal byte ReadByte(bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                byte result = readableBuffer[readPos];
                if(moveReadPos) {
                    readPos++;
                }
                return result;
            }
            throw new Exception("Could not read value of type 'byte'!");
        }

        internal byte[] ReadBytes(int length, bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                byte[] result = buffer.GetRange(readPos, length).ToArray();
                if(moveReadPos) {
                    readPos += length;
                }
                return result;
            }
            throw new Exception("Could not read value of type 'byte[]'!");
        }

        internal short ReadShort(bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                short result = BitConverter.ToInt16(readableBuffer, readPos);
                if(moveReadPos) {
                    readPos += 2;
                }
                return result;
            }
            throw new Exception("Could not read value of type 'short'!");
        }

        internal ushort ReadUShort(bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                ushort result = BitConverter.ToUInt16(readableBuffer, readPos);
                if(moveReadPos) {
                    readPos += 2;
                }
                return result;
            }
            throw new Exception("Could not read value of type 'ushort'!");
        }

        internal int ReadInt(bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                int result = BitConverter.ToInt32(readableBuffer, readPos);
                if(moveReadPos) {
                    readPos += 4;
                }
                return result;
            }
            throw new Exception("Could not read value of type 'int'!");
        }

        internal uint ReadUInt(bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                uint result = BitConverter.ToUInt32(readableBuffer, readPos);
                if(moveReadPos) {
                    readPos += 2;
                }
                return result;
            }
            throw new Exception("Could not read value of type 'ushort'!");
        }

        internal long ReadLong(bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                long result = BitConverter.ToInt64(readableBuffer, readPos);
                if(moveReadPos) {
                    readPos += 8;
                }
                return result;
            }
            throw new Exception("Could not read value of type 'long'!");
        }

        internal ulong ReadULong(bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                ulong result = BitConverter.ToUInt64(readableBuffer, readPos);
                if(moveReadPos) {
                    readPos += 2;
                }
                return result;
            }
            throw new Exception("Could not read value of type 'ushort'!");
        }

        internal float ReadFloat(bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                float result = BitConverter.ToSingle(readableBuffer, readPos);
                if(moveReadPos) {
                    readPos += 4;
                }
                return result;
            }
            throw new Exception("Could not read value of type 'float'!");
        }

        internal float ReadFloatLP(bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                ushort result = BitConverter.ToUInt16(readableBuffer, readPos);
                if(moveReadPos) {
                    readPos += 2;
                }
                return (float)Half.ToHalf(result);
            }
            throw new Exception("Could not read value of type 'ushort'!");
        }

        internal bool ReadBool(bool moveReadPos = true) {
            if(buffer.Count > readPos) {
                bool result = BitConverter.ToBoolean(readableBuffer, readPos);
                if(moveReadPos) {
                    readPos++;
                }
                return result;
            }
            throw new Exception("Could not read value of type 'bool'!");
        }

        internal string ReadString(bool moveReadPos = true) {
            string result;
            try {
                int num = ReadUShort(true);
                //Log.Warn($"{readableBuffer.Length} {readPos} {num}");
                if(num <= 0) {
                    result = "";
                } else {
                    string str = Encoding.UTF8.GetString(readableBuffer, readPos, num);
                    if(moveReadPos && num >= 0) {
                        readPos += num;
                    }
                    result = str;
                }
            } catch (Exception e) {
                throw new Exception("Could not read value of type 'string'!\n" + e);
            }
            return result;
        }

        internal Vector3 ReadVector3(bool moveReadPos = true) {
            return new Vector3(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }

        internal Vector3 ReadVector3LP(bool moveReadPos = true) {
            return new Vector3(ReadFloatLP(moveReadPos), ReadFloatLP(moveReadPos), ReadFloatLP(moveReadPos));
        }

        internal Quaternion ReadQuaternion(bool moveReadPos = true) {
            return new Quaternion(ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos), ReadFloat(moveReadPos));
        }
        internal Quaternion ReadQuaternionLP(bool moveReadPos = true) {
            byte maxIndex = ReadByte(moveReadPos);
            if(maxIndex >= 4 && maxIndex <= 7) {
                return QuaternionExtension.Decompress(maxIndex);
            } else {
                short[] data = new short[3] {
                    ReadShort(moveReadPos), ReadShort(moveReadPos), ReadShort(moveReadPos)
                };
                return QuaternionExtension.Decompress(maxIndex, data);
            }
        }

        internal Color ReadColor(bool moveReadPos = true) {
            return new Color(ReadFloatLP(moveReadPos), ReadFloatLP(moveReadPos), ReadFloatLP(moveReadPos));
        }

        internal object[] ReadArray(Type type, bool lowPrecision = false, bool moveReadPos = true) {
            byte length = ReadByte(moveReadPos);

            object[] values = new object[length];
            for(byte i = 0; i < length; i++) {
                values[i] = Read(type, lowPrecision, moveReadPos);
            }
            return values;
        }
        
        internal object Read(Type type, bool lowPrecision = false, bool moveReadPos = true) {
            switch(Type.GetTypeCode(type)) {
                case TypeCode.Byte:
                    return ReadByte(moveReadPos);

                case TypeCode.Int16:
                    return ReadShort(moveReadPos);
                case TypeCode.Int32:
                    return ReadInt(moveReadPos);
                case TypeCode.Int64:
                    return ReadLong(moveReadPos);

                case TypeCode.UInt16:
                    return ReadUShort(moveReadPos);
                case TypeCode.UInt32:
                    return ReadUInt(moveReadPos);
                case TypeCode.UInt64:
                    return ReadULong(moveReadPos);

                case TypeCode.String:
                    return ReadString(moveReadPos);

                case TypeCode.Boolean:
                    return ReadBool(moveReadPos);

                default:
                    if(type == typeof(float)) {
                        if(lowPrecision) return ReadFloatLP(moveReadPos);
                        else             return ReadFloat  (moveReadPos);
                    } else if(type == typeof(Quaternion)) {
                        if(lowPrecision) return ReadQuaternionLP(moveReadPos);
                        else             return ReadQuaternion  (moveReadPos);
                    } else if(type == typeof(Vector3)) {
                        if(lowPrecision) return ReadVector3LP(moveReadPos);
                        else             return ReadVector3  (moveReadPos);
                    } else if(type == typeof(Color)) {
                        return ReadColor();
                    }

                    return null;
            }
        }
        #endregion

        protected virtual void Dispose(bool disposing) {
            if(!disposed) {
                if(disposing) {
                    buffer = null;
                    readableBuffer = null;
                    readPos = 0;
                }
                disposed = true;
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
