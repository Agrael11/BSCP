namespace CommonAPI
{
    public class CrazyVersion : CrazyType
    {
        public static readonly byte CharacterSize = 10;
        public CrazyVersion(int version) : base(CharacterSize)
        {
            this.ActualData = version;
        }

        public CrazyVersion(CrazyType type) : base(CharacterSize)
        {
            if (type.BitLength != CharacterSize)
            {
                throw new ArgumentException($"Invalid bit length: {type.BitLength}. Expected: {CharacterSize}.");
            }
            ActualData = type.ActualData;
        }

        public int GetVersion()
        {
            return (int)GetData();
        }

        public void SetVersion(int version)
        {
            SetData(version);
        }
    }
}
