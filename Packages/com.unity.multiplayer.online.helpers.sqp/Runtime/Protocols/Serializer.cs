using System;
using System.IO;
using System.Text;

namespace Unity.Helpers.ServerQuery
{
    public class Serializer
    {
        public static ushort ByteSize => 1;
        public static ushort UShortSize => 2;
        public static ushort ShortSize => 2;
        public static ushort UIntSize => 4;
        public static ushort IntSize => 4;
        public static ushort FloatSize => 4;
        public static ushort ULongSize => 8;
        public static ushort StringWorstSize => 256;

        public int Size
        {
            get
            {
                switch (m_mode)
                {
                    case SerializationMode.Read:
                        return m_buffer.Length - m_cursor;       
                    case SerializationMode.Write:
                        return m_cursor;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private byte[] m_buffer;
        private int m_cursor = 0;

        private SerializationMode m_mode;
        public SerializationMode Mode => m_mode;
        
        public enum SerializationMode
        {
            Read,
            Write
        }
        
        public Serializer(byte[] buffer, SerializationMode mode)
        {
            m_mode = mode;
            m_buffer = buffer;
        }

        public byte[] Data()
        {
            return m_buffer;
        }

        public static ushort StringSize(string str)
        {
            if (str.Length > 255) throw new ArgumentException("String is to big to be serialized");
            return (ushort)(Encoding.ASCII.GetByteCount(str) + 1);
        }

        public int WriteA2SString(string s)
        {
            if (s.Length > 255) throw new ArgumentException("string to big to be serialized");

            int stringSize = Encoding.ASCII.GetByteCount(s) + 1;
            if (stringSize + m_cursor >= m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to serialize that data.");

            Encoding.ASCII.GetBytes(s).CopyTo(m_buffer, m_cursor);
            m_cursor += s.Length; // Verify the cursor gets pushed up here
            m_buffer[m_cursor++] = (byte)char.MinValue;
            return stringSize;
        }
        
        public int WriteString(string s)
        {
            if (s.Length > 255) throw new ArgumentException("String is to big to be serialized");

            int stringSize = Encoding.ASCII.GetByteCount(s) + 1;
            if (stringSize + m_cursor >= m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to serialize that data.");

            m_buffer[m_cursor++] = (byte)s.Length;
            Encoding.ASCII.GetBytes(s).CopyTo(m_buffer, m_cursor);
            m_cursor += s.Length;
            return stringSize;
        }

        public string ReadString()
        {
            if(m_cursor + 2 > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to read.");
            byte strLength = m_buffer[m_cursor++];
            if(m_cursor + strLength >= m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to read.");
            var str = Encoding.ASCII.GetString(m_buffer, m_cursor, strLength);
            m_cursor += strLength;
            return str;
        }
        
        public int WriteByte(byte b)
        {
            if(m_cursor + ByteSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to serialize that data.");
            m_buffer[m_cursor++] = b;
            return ByteSize;
        }

        public byte ReadByte()
        {
            if(m_cursor >= m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to read the data.");
            return m_buffer[m_cursor++];
        }
        
        public int WriteUShort(ushort val)
        {
            if(m_cursor + UShortSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to serialize that data.");
            var byteVal = BitConverter.GetBytes(val);
            //if(BitConverter.IsLittleEndian) Array.Reverse(byteVal);
            byteVal.CopyTo(m_buffer, m_cursor);
            m_cursor += byteVal.Length;
            return UShortSize;
        }

        public int WriteShort(short val)
        {
            if (m_cursor + ShortSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to serialize that data.");
            var byteVal = BitConverter.GetBytes(val);
            byteVal.CopyTo(m_buffer, m_cursor);
            m_cursor += byteVal.Length;
            return ShortSize;
        }

        public ushort ReadUShort()
        {
            if(m_cursor + UShortSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to read the data.");
            ushort val = 0;
            if (BitConverter.IsLittleEndian)
            {
                byte[] data = { m_buffer[m_cursor++], m_buffer[m_cursor++] };
                Array.Reverse(data);
                val = BitConverter.ToUInt16(data, 0);
            }
            else
            {
                val = BitConverter.ToUInt16(m_buffer, m_cursor);
                m_cursor += UShortSize;
            }
            
            return val;
        }
        public int WriteFloat(float val)
        {
            if (m_cursor + FloatSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to serialize that data.");
            var byteVal = BitConverter.GetBytes(val);
            byteVal.CopyTo(m_buffer, m_cursor);
            m_cursor += byteVal.Length;
            return FloatSize;
        }

        public int WriteUInt(uint val)
        {
            if(m_cursor + UIntSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to serialize that data.");
            var byteVal = BitConverter.GetBytes(val);
            if(BitConverter.IsLittleEndian) Array.Reverse(byteVal);
            byteVal.CopyTo(m_buffer, m_cursor);
            m_cursor += byteVal.Length;
            return UIntSize;
        }

        public uint ReadUInt()
        {
            if(m_cursor + UIntSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to read the data.");
            uint val = 0;
            if (BitConverter.IsLittleEndian)
            {
                byte[] data = { m_buffer[m_cursor++], m_buffer[m_cursor++], m_buffer[m_cursor++], m_buffer[m_cursor++] };
                Array.Reverse(data);
                val = BitConverter.ToUInt32(data, 0);
            }
            else
            {
                val = BitConverter.ToUInt32(m_buffer, m_cursor);
                m_cursor += UIntSize;
            }
            
            return val;
        }

        public int WriteInt(int val)
        {
            if (m_cursor + IntSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to serialize that data.");
            var byteVal = BitConverter.GetBytes(val);
            byteVal.CopyTo(m_buffer, m_cursor);
            m_cursor += byteVal.Length;
            return IntSize;
        }

        public int ReadInt()
        {
            if (m_cursor + IntSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to read the data.");
            int val = 0;
            val = BitConverter.ToInt32(m_buffer, m_cursor);
            m_cursor += IntSize;

            return val;
        }

        public int WriteULong(ulong val)
        {
            if(m_cursor + ULongSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to serialize that data.");
            var byteVal = BitConverter.GetBytes(val);
            if(BitConverter.IsLittleEndian) Array.Reverse(byteVal);
            byteVal.CopyTo(m_buffer, m_cursor);
            m_cursor += byteVal.Length;
            return ULongSize;
        }

        public ulong ReadULong()
        {
            if(m_cursor + ULongSize > m_buffer.Length) throw new InternalBufferOverflowException("Buffer is too small to read the data.");
            ulong val = 0;
            if (BitConverter.IsLittleEndian)
            {
                byte[] data = { m_buffer[m_cursor++], m_buffer[m_cursor++], m_buffer[m_cursor++], m_buffer[m_cursor++],m_buffer[m_cursor++], m_buffer[m_cursor++], m_buffer[m_cursor++], m_buffer[m_cursor++] };
                Array.Reverse(data);
                val = BitConverter.ToUInt64(data, 0);
            }
            else
            {
                val = BitConverter.ToUInt64(m_buffer, m_cursor);
                m_cursor += ULongSize;
            }
            
            return val;
        }
    }
}