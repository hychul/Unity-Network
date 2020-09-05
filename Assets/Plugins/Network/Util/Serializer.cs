using System;
using System.IO;

public class Serializer {
	private MemoryStream buffer = null;
	private int	offset = 0;
	private EndianType endianType;

	public enum EndianType {
		BigEndian = 0,
		LittleEndian,
	}

	public Serializer() {
		buffer = new MemoryStream();

		int val = 1;
		byte[] conv = BitConverter.GetBytes(val);
		endianType = (conv[0] == 1) ? EndianType.LittleEndian : EndianType.BigEndian;
	}

	public EndianType GetEndianness() {
		return endianType;
	}

	public long GetDataSize() {
		return buffer.Length;
	}

	public byte[] GetSerializedData() {
		return buffer.ToArray();
	}

	public void Clear() {
		byte[] buffer = this.buffer.GetBuffer();
		Array.Clear(buffer, 0, buffer.Length);

		this.buffer.Position = 0;
		this.buffer.SetLength(0);
		offset = 0;
	}

	public bool SetDeserializedData(byte[] data) {
		Clear();

		try {
			buffer.Write(data, 0, data.Length);
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			return false;
		}

		return 	true;
	}

	protected bool Serialize(bool element) {
		byte[] data = BitConverter.GetBytes(element);

		return WriteBuffer(data, sizeof(bool));
	}

	protected bool Serialize(char element) {
		byte[] data = BitConverter.GetBytes(element);

		return WriteBuffer(data, sizeof(char));
	}

	protected bool Serialize(float element) {
		byte[] data = BitConverter.GetBytes(element);

		return WriteBuffer(data, sizeof(float));
	}

	protected bool Serialize(double element) {
		byte[] data = BitConverter.GetBytes(element);

		return WriteBuffer(data, sizeof(double));
	}

	protected bool Serialize(short element) {
		byte[] data = BitConverter.GetBytes(element);

		return WriteBuffer(data, sizeof(short));
	}	

	protected bool Serialize(ushort element) {
		byte[] data = BitConverter.GetBytes(element);

		return WriteBuffer(data, sizeof(ushort));
	}		

	protected bool Serialize(int element) {
		byte[] data = BitConverter.GetBytes(element);

		return WriteBuffer(data, sizeof(int));
	}	

	protected bool Serialize(uint element) {
		byte[] data = BitConverter.GetBytes(element);

		return WriteBuffer(data, sizeof(uint));
	}		

	protected bool Serialize(long element) {
		byte[] data = BitConverter.GetBytes(element);

		return WriteBuffer(data, sizeof(long));
	}	

	protected bool Serialize(ulong element) {
		byte[] data = BitConverter.GetBytes(element);

		return WriteBuffer(data, sizeof(ulong));
	}

	protected bool Serialize(byte[] element, int length) {
		if (endianType == EndianType.LittleEndian) {
			Array.Reverse(element);	
		}

		return WriteBuffer(element, length);
	}

	protected bool Serialize(string element, int length) {
		byte[] data = new byte[length];

		byte[] buffer = System.Text.Encoding.UTF8.GetBytes(element);
		int size = Math.Min(buffer.Length, data.Length);
		Buffer.BlockCopy(buffer, 0, data, 0, size);

		if (endianType == EndianType.LittleEndian) {
			Array.Reverse(data);	
		}

		return WriteBuffer(data, data.Length);
	}

	protected bool Deserialize(ref bool element) {
		int size = sizeof(bool);
		byte[] data = new byte[size];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			element = BitConverter.ToBoolean(data, 0);
			return true;
		}

		return false;
	}

	protected bool Deserialize(ref char element) {
		int size = sizeof(char);
		byte[] data = new byte[size];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			element = BitConverter.ToChar(data, 0);
			return true;
		}

		return false;
	}

	protected bool Deserialize(ref float element) {
		int size = sizeof(float);
		byte[] data = new byte[size];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			element = BitConverter.ToSingle(data, 0);
			return true;
		}

		return false;
	}

	protected bool Deserialize(ref double element) {
		int size = sizeof(double);
		byte[] data = new byte[size];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			element = BitConverter.ToDouble(data, 0);
			return true;
		}

		return false;
	}	

	protected bool Deserialize(ref short element) {
		int size = sizeof(short);
		byte[] data = new byte[size];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			element = BitConverter.ToInt16(data, 0);
			return true;
		}

		return false;
	}

	protected bool Deserialize(ref ushort element) {
		int size = sizeof(ushort);
		byte[] data = new byte[size];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			element = BitConverter.ToUInt16(data, 0);
			return true;
		}

		return false;
	}

	protected bool Deserialize(ref int element) {
		int size = sizeof(int);
		byte[] data = new byte[size];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			element = BitConverter.ToInt32(data, 0);
			return true;
		}

		return false;
	}

	protected bool Deserialize(ref uint element) {
		int size = sizeof(uint);
		byte[] data = new byte[size];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			element = BitConverter.ToUInt32(data, 0);
			return true;
		}

		return false;
	}

	protected bool Deserialize(ref long element) {
		int size = sizeof(long);
		byte[] data = new byte[size];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			element = BitConverter.ToInt64(data, 0);
			return true;
		}

		return false;
	}

	protected bool Deserialize(ref ulong element) {
		int size = sizeof(ulong);
		byte[] data = new byte[size];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			element = BitConverter.ToUInt64(data, 0);
			return true;
		}

		return false;
	}

	protected bool Deserialize(ref byte[] element, int length) {
		bool ret = ReadBuffer(ref element, length);

		if (endianType == EndianType.LittleEndian) {
			Array.Reverse(element);	
		}

		if (ret == true) {
			return true;
		}

		return false;
	}

	protected bool Deserialize(ref string element, int length) {
		byte[] data = new byte[length];

		bool ret = ReadBuffer(ref data, data.Length);
		if (ret == true) {
			if (endianType == EndianType.LittleEndian) {
				Array.Reverse(data);
			}
			string str = System.Text.Encoding.UTF8.GetString(data);
			element = str.Trim('\0');

			return true;
		}

		return false;
	}

	protected bool ReadBuffer(ref byte[] data, int size) {
		try {
			buffer.Position = offset;
			buffer.Read(data, 0, size);
			offset += size;
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			return false;
		}

		if (endianType == EndianType.LittleEndian) {
			Array.Reverse(data);
		}

		return true;
	}

	protected bool WriteBuffer(byte[] data, int size) {
		if (endianType == EndianType.LittleEndian) {
			Array.Reverse(data);
		}

		try {
			buffer.Position = offset;
			buffer.Write(data, 0, size);
			offset += size;
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			return false;
		}

		return true;
	}

	protected bool ReadRawBuffer(ref byte[] data, int size) {
		try {
			buffer.Position = offset;
			buffer.Read(data, 0, size);
			offset += size;
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			return false;
		}

		return true;
	}

	protected bool WriteRawBuffer(byte[] data, int size) {
		try {
			buffer.Position = offset;
			buffer.Write(data, 0, size);
			offset += size;
		} catch (Exception e) {
			NetworkLogger.Log(e.ToString());
			return false;
		}

		return true;
	}
}
