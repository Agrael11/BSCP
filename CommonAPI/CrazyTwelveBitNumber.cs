namespace CommonAPI
{
    public class CrazyTwelveBitNumber : CrazyType
    {
        public static readonly byte CharacterSize = 12;
        public CrazyTwelveBitNumber(int value) : base(CharacterSize)
        {
            var fixedvalue = value & 0xFFF; // Ensure the value is within 12 bits
            ActualData = fixedvalue;
        }

        public CrazyTwelveBitNumber(CrazyType original) : base(CharacterSize)
        {
            ActualData = original.ActualData & 0xFFF; // Ensure the value is within 12 bits
        }

        public void SetValue(int value)
        {
            var fixedvalue = value & 0xFFF; // Ensure the value is within 12 bits
            SetData(fixedvalue);
        }

        public int GetValue()
        {
            return (int)(ActualData & 0xFFF); // Return the value as an int, ensuring it's within 12 bits
        }
    }
}
