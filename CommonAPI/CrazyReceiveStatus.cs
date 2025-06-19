namespace CommonAPI
{
    public class CrazyReceiveStatus : CrazyType
    {
        public static readonly byte CharacterSize = 9;
        public enum Status
        {
            Unknown = 0,
            Success = 1,
            Failure = 2,
            Ignore = -153
        }
        public CrazyReceiveStatus(Status status) : base(CharacterSize)
        {
            this.ActualData = (long)status;
        }

        public CrazyReceiveStatus(CrazyType data) : base(CharacterSize)
        {
            ActualData = data.ActualData;
        }

        public Status GetStatus()
        {
            return (Status)GetData();
        }

        public void SetStatus(Status status)
        {
            SetData((long)status);
        }
    }
}
