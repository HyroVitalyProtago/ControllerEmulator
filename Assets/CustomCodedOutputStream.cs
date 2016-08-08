// using Google.ProtocolBuffers;
using System;

// https://github.com/google/protobuf/blob/master/java/core/src/main/java/com/google/protobuf/CodedOutputStream.java
public static class CustomCodedOutputStream {

	public static byte[] Bytes { get; set; }
	public static int Position { get; set; }

	// unsafe method
	// static unsafe uint UInt32FromFloat(float value) { return *((uint*)&value); }

	// safe method
	static float[] f = new float[1];
	static uint[] ui = new uint[1];
	static uint UInt32FromFloat(float value) {
		f[0] = value;
		Buffer.BlockCopy(f, 0, ui, 0, 4);
		return ui[0];
	}

	public static void WriteFloat(float value) {
		WriteFixed32(UInt32FromFloat(value));
	}

	public static void WriteInt64(long value) {
		while (true) {
			if ((value & ~0x7FL) == 0) {
				Bytes[Position++] = (byte)(value);
				return;
			} else {
				Bytes[Position++] = (byte) (((int) value & 0x7F) | 0x80);
				value = (long) ((uint) value >> 7); // value >>>= 7;
			}
		}
	}
	public static void WriteInt32(int value) {
		Bytes[Position++] = (byte)(value);
		if (value >= 256) {
			Bytes[Position++] = (byte)(value >> 8);
			if (value >= 65536) {
				Bytes[Position++] = (byte)(value >> 16);
				if (value >= 16777216) {
					Bytes[Position++] = (byte)(value >> 24);
				}
			}
		}
	}
	public static void WriteFixed32(uint value) {
		Bytes[Position++] = (byte)(value);
		Bytes[Position++] = (byte)(value >> 8);
		Bytes[Position++] = (byte)(value >> 16);
		Bytes[Position++] = (byte)(value >> 24);
	}
}
