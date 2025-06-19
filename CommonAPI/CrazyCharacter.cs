using System.Text;

namespace CommonAPI
{
    public class CrazyCharacter : CrazyType
    {
        public static readonly byte CharacterSize = 12;

        public CrazyCharacter() : base(CharacterSize)
        {
            SetData('0');
        }
        public CrazyCharacter(char c) : base(CharacterSize)
        {
            SetData(c);
        }

        public CrazyCharacter(CrazyType other) : base(CharacterSize, other.ActualData)
        {
        }

        public void SetData(char c)
        {
            ActualData = ((long)c)<<2;
        }

        public static void SetChecksum(ref CrazyCharacter[] chars)
        {
            var checksum = CalculateChecksum(ToString(chars));
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i].SetChecksum(checksum[i]);
            }
        }

        public static bool CompareChecksum(CrazyCharacter[] chars, string str)
        {
            var checksum = CalculateChecksum(str);
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i].GetChecksum() != checksum[i])
                {
                    return false;
                }
            }
            return true;
        }

        public void SetChecksum(byte checksum)
        {
            var checksumAligned = (ulong)((checksum & 0b1100)>>2) | (ulong)((checksum & 0b11)<<10);
            ActualData = (long)(((ulong)ActualData & 0x3FC) | checksumAligned);
        }

        public byte GetChecksum()
        { 
            var checksumUnaligned = ActualData ^ (ActualData & 0x3FC);
            var checksum = (byte)((checksumUnaligned >> 10) | ((checksumUnaligned & 0b11)<<2));
            return checksum;
        }

        public static byte[] CalculateChecksum(string str)
        {
            var str1 = (byte)str[0];
            var str2 = (byte)str[^1];
            var strL = (byte)(str.Length & 0xFF);
            var magic = str1 ^ str2 ^ strL;
            var checksum = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                var data = (byte)(str[i] & 0xFF);
                var checksumValue = (byte)(data + magic);
                checksumValue = (byte)((checksumValue >> 4) ^ (checksumValue & 0xF));
                checksum[i] = checksumValue;
            }

            return checksum;
        }

        public static byte[] CalculateChecksum(byte[] str)
        {
            var str1 = (byte)str[0];
            var str2 = (byte)str[^1];
            var strL = (byte)(str.Length & 0xFF);
            var magic = str1 ^ str2 ^ strL;
            var checksum = new byte[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                var data = (byte)(str[i] & 0xFF);
                var checksumValue = (byte)(data + magic);
                checksumValue = (byte)((checksumValue >> 4) ^ (checksumValue & 0xF));
                checksum[i] = checksumValue;
            }

            return checksum;
        }

        public override long GetData()
        {
            var data = this.ActualData;
            data >>= 2;
            return data&0xFF;
        }

        public override string ToString()
        {
            byte data = (byte)(GetData()&0xFF);
            return $"{(char)(data)} - 0b{Convert.ToString((long)ActualData, 2).PadLeft(BitLength,'0')} ({BitLength})";
        }

        public static CrazyCharacter[] FromString(string str)
        {
            var chars = new CrazyCharacter[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                chars[i] = new CrazyCharacter(str[i]);
            }
            return chars;
        }

        public static string ToString(CrazyCharacter[] chars)
        {
            var str = new StringBuilder(chars.Length);
            foreach (var c in chars)
            {
                str.Append((char)(c.GetData() & 0xFF));
            }
            return str.ToString();
        }

        public static byte[] ToByteArray(string str, bool send)
        {
            var chars = new CrazyCharacter[str.Length];
            for (int i = 0; i < str.Length; i++)
            {
                chars[i] = new CrazyCharacter(str[i]);
            }
            return ToByteArray(chars, send);
        }

        public static string FromByteArray(byte[] values, bool receiving)
        {
            var chars = CrazyType.FromByteArray(values, receiving, CharacterSize);
            var str = new StringBuilder(chars.Length);
            foreach (var c in chars)
            {
                str.Append((char)((c.GetData() >> 2) & 0xFF));
            }
            return str.ToString();
        }

        public static CrazyCharacter[] FromByteArrayString(byte[] values, bool receiving)
        {
            var chars = CrazyType.FromByteArray(values, receiving, CharacterSize);
            var result = new CrazyCharacter[chars.Length];
            for (int i = 0; i < chars.Length;  i++)
            {
                result[i] = new CrazyCharacter(chars[i]);
            }
            return result;
        }
    }
}
