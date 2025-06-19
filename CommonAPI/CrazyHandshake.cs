using System.Runtime.CompilerServices;

namespace CommonAPI
{
    public class CrazyHandshake : CrazyType
    {
        public static readonly byte CharacterSize = 21;
        public enum HandshakeType
        {
            None = -42,
            ClientHello = 72,
            ServerHello = 13,
            SendingNumber = 42,
            SendingString = 39,
            Goodbye = 0
        }

        public CrazyHandshake(HandshakeType type) : base (CharacterSize)
        {
            SetData((int)type);
        }

        public CrazyHandshake(CrazyType type) : base(CharacterSize)
        {
            if (type.BitLength != CharacterSize)
            {
                throw new ArgumentException($"Invalid bit length: {type.BitLength}. Expected: {CharacterSize}.");
            }
            ActualData = type.ActualData;
        }

        public HandshakeType GetHandshake()
        {
            return (HandshakeType)GetData();
        }

        public void SetHandshake(HandshakeType type)
        {
            SetData((int)type);
        }
    }
}
