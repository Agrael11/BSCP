namespace CommonAPI
{
    public class CrazyType
    {
        public byte BitLength { get; private set; }
        internal long ActualData;

        public CrazyType(byte bitLength)
        {
            BitLength = bitLength;
            ActualData = 0;
        }

        public CrazyType(byte bitLength, long data)
        {
            BitLength = bitLength;
            ActualData = data;
        }

        public void SetData(long data)
        {
            ActualData = data;
        }

        public virtual long GetData()
        {
            return (ActualData) & ((1 << BitLength) - 1);
        }

        public static byte[] ToByteArray(CrazyType[] values, bool sending)
        {
            var bits = new List<bool>();
            foreach (var value in values)
            {
                for (int i = 0; i < value.BitLength; i++)
                {
                    bits.Add((value.ActualData & (1L << (value.BitLength - i - 1))) != 0);
                }
                if (sending)
                {
                    bits.Add(true);
                }
            }

            var byteListLength = (bits.Count + 7) / 8;
            int totalPadding = (8 - (bits.Count % 8)) % 8;
            var startPadding = totalPadding / 2;

            var byteList = new List<byte>(byteListLength);

            var bitIndex = 0;
            for (int i = 0; i < byteListLength; i++)
            {
                var byteValue = (byte)0;
                for (int j = 0; j < 8; j++)
                {
                    if (startPadding > 0)
                    {
                        startPadding--;
                        continue;
                    }
                    if (bitIndex >= bits.Count)
                        continue;
                    var bit = bits[bitIndex];
                    if (bit)
                    {
                        byteValue |= (byte)(1 << (7 - j));
                    }

                    bitIndex++;
                }
                byteList.Add(byteValue);
            }

            return [.. byteList];
        }

        public static CrazyType[] FromByteArray(byte[] values, bool receiving, byte bitLength)
        {
            if (receiving) bitLength++;
            var totalInputLength = values.Length * 8;
            var totalOutputValues = totalInputLength / bitLength;
            var totalOutputBits = totalOutputValues * bitLength;
            var padding = totalInputLength - totalOutputBits;
            var startPadding = padding / 2;

            var bits = new List<bool>();
            foreach (var value in values)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (startPadding > 0)
                    {
                        startPadding--;
                        continue;
                    }
                    bits.Add((value & (1 << (7 - i))) != 0);
                }
            }
            var outputValues = new CrazyType[totalOutputValues];
            
            for (var i = 0; i < totalOutputValues; i++)
            {
                var startBitIndex = i * bitLength;
                var value = new CrazyType(bitLength);
                for (var j = 0; j < bitLength; j++)
                {
                    var bitIndex = startBitIndex + j;
                    var bit = bits[bitIndex];
                    if (bit)
                    {
                        value.ActualData |= (1L << (bitLength - j - 1));
                    }
                }
                outputValues[i] = value;
            }

            if (receiving)
            {
                for (int i = 0; i < outputValues.Length; i++)
                {
                    if ((outputValues[i].ActualData & 1) != 1)
                    {
                        throw new InvalidOperationException("The last bit of the value must be 1 when receiving sent data.");
                    }
                    outputValues[i].BitLength--;
                    outputValues[i].ActualData = (outputValues[i].ActualData >> 1);
                }
            }

            return outputValues;
        }

        public override string ToString()
        {
            long data = (long)(ActualData);
            return $"{ActualData} - 0b{Convert.ToString(data,2).PadLeft(BitLength,'0')} ({BitLength})";
        }
    }
}
