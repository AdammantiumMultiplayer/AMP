using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AMP.Network.Data {
	public class Packet : IDisposable {
		private List<byte> buffer;

		private byte[] readableBuffer;
		private int readPos;
		private bool disposed;

		public enum Type {
			welcome		= 1,
			disconnect	= 2,
			error		= 3,
			message		= 4,
			playerData	= 5,
			playerInfo	= 6,
			itemInfo	= 7,
		}

		public Packet() {
			buffer = new List<byte>();
			readPos = 0;
		}

		public Packet(int _id) {
			buffer = new List<byte>();
			readPos = 0;
			Write(_id);
		}

		public Packet(byte[] _data) {
			buffer = new List<byte>();
			readPos = 0;
			SetBytes(_data);
		}

		public void SetBytes(byte[] _data) {
			Write(_data);
			readableBuffer = buffer.ToArray();
		}

		public void InsertInt(int _value) {
			buffer.InsertRange(0, BitConverter.GetBytes(_value));
		}

		public byte[] ToArray() {
			readableBuffer = buffer.ToArray();
			return readableBuffer;
		}

		public int Length() {
			return buffer.Count;
		}

		public int UnreadLength() {
			return Length() - readPos;
		}

		public void Reset(bool _shouldReset = true) {
			if(_shouldReset) {
				buffer.Clear();
				readableBuffer = null;
				readPos = 0;
				return;
			}
			readPos -= 4;
		}

		#region writing
		public void WriteLength() {
			buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count));
		}

		public void Write(byte _value) {
			buffer.Add(_value);
		}

		public void Write(byte[] _value) {
			buffer.AddRange(_value);
		}

		public void Write(short _value) {
			buffer.AddRange(BitConverter.GetBytes(_value));
		}

		public void Write(int _value) {
			buffer.AddRange(BitConverter.GetBytes(_value));
		}

		public void Write(long _value) {
			buffer.AddRange(BitConverter.GetBytes(_value));
		}

		public void Write(float _value) {
			buffer.AddRange(BitConverter.GetBytes(_value));
		}

		public void Write(bool _value) {
			buffer.AddRange(BitConverter.GetBytes(_value));
		}

		public void Write(string _value) {
			Write(_value.Length);
			buffer.AddRange(Encoding.ASCII.GetBytes(_value));
		}

		public void Write(Vector3 _value) {
			Write(_value.x);
			Write(_value.y);
			Write(_value.z);
		}

		public void Write(Quaternion _value) {
			Write(_value.x);
			Write(_value.y);
			Write(_value.z);
			Write(_value.w);
		}

		public void Write(Color _value) {
			Write(_value.r);
			Write(_value.g);
			Write(_value.b);
		}

		public void Write(Vector3 _value, Vector3 _oldValue) {
			if(_value == _oldValue) Write(false);
			else { Write(true); Write(_value); }
		}

		public void Write(Quaternion _value, Quaternion _oldValue) {
			if(_value == _oldValue) Write(false);
			else { Write(true); Write(_value); }
		}

		//public void Write(PlayerData _value) {
		//	Write(_value.id);
		//	Write(_value.head);
		//	Write(_value.leftHand);
		//	Write(_value.rightHand);
		//	Write(_value.body);
		//}
		//
		//public void Write(ObjectData _value) {
		//	Write(_value.position);
		//	Write(_value.rotation);
		//	Write(_value.velocity);
		//}
		//
		//public void Write(Multiplayer.DataHolders.ItemData _value) {
		//	Write(_value.networkId);
		//	Write(_value.clientsideId);
		//	Write(_value.itemId);
		//	Write(_value.objectData);
		//	Write(_value.playerControl);
		//}

		#endregion

		#region reading
		public byte ReadByte(bool _moveReadPos = true) {
			if(buffer.Count > readPos) {
				byte result = readableBuffer[readPos];
				if(_moveReadPos) {
					readPos++;
				}
				return result;
			}
			throw new Exception("Could not read value of type 'byte'!");
		}

		public byte[] ReadBytes(int _length, bool _moveReadPos = true) {
			if(buffer.Count > readPos) {
				byte[] result = buffer.GetRange(readPos, _length).ToArray();
				if(_moveReadPos) {
					readPos += _length;
				}
				return result;
			}
			throw new Exception("Could not read value of type 'byte[]'!");
		}

		public short ReadShort(bool _moveReadPos = true) {
			if(buffer.Count > readPos) {
				short result = BitConverter.ToInt16(readableBuffer, readPos);
				if(_moveReadPos) {
					readPos += 2;
				}
				return result;
			}
			throw new Exception("Could not read value of type 'short'!");
		}

		public int ReadInt(bool _moveReadPos = true) {
			if(buffer.Count > readPos) {
				int result = BitConverter.ToInt32(readableBuffer, readPos);
				if(_moveReadPos) {
					readPos += 4;
				}
				return result;
			}
			throw new Exception("Could not read value of type 'int'!");
		}

		public long ReadLong(bool _moveReadPos = true) {
			if(buffer.Count > readPos) {
				long result = BitConverter.ToInt64(readableBuffer, readPos);
				if(_moveReadPos) {
					readPos += 8;
				}
				return result;
			}
			throw new Exception("Could not read value of type 'long'!");
		}

		public float ReadFloat(bool _moveReadPos = true) {
			if(buffer.Count > readPos) {
				float result = BitConverter.ToSingle(readableBuffer, readPos);
				if(_moveReadPos) {
					readPos += 4;
				}
				return result;
			}
			throw new Exception("Could not read value of type 'float'!");
		}

		public bool ReadBool(bool _moveReadPos = true) {
			if(buffer.Count > readPos) {
				bool result = BitConverter.ToBoolean(readableBuffer, readPos);
				if(_moveReadPos) {
					readPos++;
				}
				return result;
			}
			throw new Exception("Could not read value of type 'bool'!");
		}

		public string ReadString(bool _moveReadPos = true) {
			string result;
			try {
				int num = ReadInt(true);
				string @string = Encoding.ASCII.GetString(readableBuffer, readPos, num);
				if(_moveReadPos && @string.Length > 0) {
					readPos += num;
				}
				result = @string;
			} catch {
				throw new Exception("Could not read value of type 'string'!");
			}
			return result;
		}

		public Vector3 ReadVector3(bool _moveReadPos = true) {
			return new Vector3(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
		}

		public Vector3 ReadVector3Optimised(bool _moveReadPos = true) {
			// If should change return new value
			if(ReadBool()) return new Vector3(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
			// If shouldn't change return 0
			else return Vector3.zero;
		}

		public Quaternion ReadQuaternion(bool _moveReadPos = true) {
			return new Quaternion(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
		}

		public Quaternion ReadQuaternionOptimised(bool _moveReadPos = true) {
			// If should change return new value
			if(ReadBool()) return new Quaternion(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
			// If shouldn't change return 0
			else return Quaternion.identity;
		}

		//public PlayerData ReadPlayerData(bool _moveReadPos = true) {
		//	return new PlayerData(ReadInt()) { head = ReadObjectData(_moveReadPos), leftHand = ReadObjectData(_moveReadPos), rightHand = ReadObjectData(_moveReadPos), body = ReadObjectData(_moveReadPos) };
		//}
		//
		//public ObjectData ReadObjectData(bool _moveReadPos = true) {
		//	return new ObjectData() { position = ReadVector3(_moveReadPos), rotation = ReadQuaternion(_moveReadPos), velocity = ReadVector3(_moveReadPos) };
		//}
		//
		//public ItemData ReadItemData(bool _moveReadPos = true) {
		//	int networkId = ReadInt();
		//	int clientSideId = ReadInt();
		//	return new ItemData(networkId, ReadString(), ReadObjectData()) { playerControl = ReadInt(), clientsideId = clientSideId };
		//}

		public Color ReadColor(bool _moveReadPos = true) {
			return new Color(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
		}
		#endregion

		protected virtual void Dispose(bool _disposing) {
			if(!disposed) {
				if(_disposing) {
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
