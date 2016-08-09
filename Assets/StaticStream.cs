using System;

// not thread safe !!
public class StaticStream {

    static StaticStream _instance = new StaticStream();
    public static StaticStream Begin(byte[] buffer) {
        return _instance.Init(buffer);
    }

    byte[] buffer;
    int position;

    public int Position { get { return position; } }
    
    private StaticStream() {}

    StaticStream Init(byte[] buffer) {
        this.buffer = buffer;
        this.position = 0;
        return this;
    }

    public StaticStream SetPosition(int position) {
        this.position = position;
        return this;
    }

    public int End() { return position; }

    #region writers
    public StaticStream WriteFloat(float value) {
        return WriteFixed32(UInt32FromFloat(value));
    }
    public StaticStream WriteInt64(long value) {
        while (true) {
            if ((value & ~0x7FL) == 0) {
                buffer[position++] = (byte)(value);
                return this;
            } else {
                buffer[position++] = (byte)(((int)value & 0x7F) | 0x80);
                value = (long)((uint)value >> 7); // value >>>= 7;
            }
        }
    }
    public StaticStream WriteInt32(int value) {
        buffer[position++] = (byte)(value);
        if (value >= 256) {
            buffer[position++] = (byte)(value >> 8);
            if (value >= 65536) {
                buffer[position++] = (byte)(value >> 16);
                if (value >= 16777216) {
                    buffer[position++] = (byte)(value >> 24);
                }
            }
        }
        return this;
    }
    public StaticStream WriteFixed32(uint value) {
        buffer[position++] = (byte)(value);
        buffer[position++] = (byte)(value >> 8);
        buffer[position++] = (byte)(value >> 16);
        buffer[position++] = (byte)(value >> 24);
        return this;
    }
    #endregion

    #region utils
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
    #endregion
}
