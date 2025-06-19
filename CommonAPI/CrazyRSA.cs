using System.Security.Cryptography;

namespace CommonAPI
{
    public class CrazyRSA
    {
        public static readonly int KeySize = 3072;
        public static readonly int AESSize = 32+16+8+2; // 256 bits for AES key
        public int BlockSize
        {
            get
            {
                if (!aesEnabled)
                    return KeySize / 8;
                return aes.BlockSize / 8;
            }
        }
        private RSA rsaMine;
        private RSA rsaTheir;
        private Aes aes;
        private bool aesEnabled = false;

        public CrazyRSA()
        {
            rsaMine = RSA.Create(KeySize);
            rsaTheir = RSA.Create(KeySize);
            aes = Aes.Create();
        }

        public int[] CalculateChecksumAES(byte[] key, byte[] iv)
        {
            ulong checksum = 0;
            for (var i = 0; i < key.Length; i++)
            {
                checksum += key[i];
                if (i%2 == 0)
                {
                    checksum ^= iv[i/2];
                }
            }
            var high = checksum >> 32;
            var low = checksum & 0xFFFFFFFF;

            return [(int)high, (int)low];
        }

        public bool CheckAndRemoveChecksumAES(byte[] data, out byte[] key, out byte[] iv)
        {
            key = data[5..(5 + 32)];
            iv = data[(5 + 32)..(5 + 32 + 16)];
            if (data[0] != 0xEE || data[^1] != 0x11)
            {
                return false; // Invalid format
            }

            var high = BitConverter.ToInt32(data, 1);
            var low = BitConverter.ToInt32(data, 5 + 32 + 16);
            var checksum = CalculateChecksumAES(key, iv);

            if (checksum[1] != high || checksum[0] != low)
            {
                return false;
            }
            return true; // Return the data without the checksum and markers
        }

        public byte[] CalculateChecksumRSA(byte[] data)
        {
            ulong checksum = 0;
            foreach (var b in data)
            {
                checksum += b;
            }
            var high = checksum >> 32;
            var low = checksum & 0xFFFFFFFF;
            checksum = (high ^ low) & 0xFFFFFFFF;
            high = checksum >> 16;
            low = checksum & 0xFFFF;
            checksum = (high + low) & 0xFFFF;
            high = checksum >> 8;
            low = checksum & 0xFF;
            return [(byte)high, (byte)low];
        }

        public bool CheckAndRemoveChecksumRSA(byte[] data, out byte[] newData)
        {
            newData = data[2..^2];
            if (data[0] != 0xFF || data[^1] != 0x00)
            {
                return false; // Invalid format
            }

            var checksum = CalculateChecksumRSA(newData);

            if (checksum[0] != data[^2] || checksum[1] != data[1])
            {
                return false;
            }
            return true; // Return the data without the checksum and markers
        }

        public int GetPaddedSize(int size)
        {
            var exact = size % BlockSize;
            if (exact != 0 || aesEnabled)
            {
                size = ((size / BlockSize) + 1) * BlockSize;
            }


            return size;
        }

        public void EnableAES()
        {
            aesEnabled = true;
        }

        public byte[] ExportRSAKey()
        {
            var key = rsaMine.ExportRSAPublicKey().ToList();
            var checksum = CalculateChecksumRSA([.. key]);
            key.Insert(0, checksum[1]);
            key.Insert(0, 0xFF);
            key.Add(checksum[0]);
            key.Add(0);
            return [.. key];
        }

        public byte[] ExportAESKey()
        {
            aes.GenerateKey();
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            var data = new List<byte>(aes.Key);
            data.AddRange(aes.IV);
            var checksum = CalculateChecksumAES(aes.Key, aes.IV);
            var high = checksum[1];
            var low = checksum[0];
            data.Insert(0, 0xEE); // Start marker
            data.InsertRange(1, BitConverter.GetBytes(high)); // Checksum high byte
            data.AddRange(BitConverter.GetBytes(low)); // Checksum low byte
            data.Add(0x11); // End marker
            return [..data];
        }

        public bool ImportAESKey(byte[] key)
        {
            if (key.Length != AESSize)
            {
                Console.WriteLine("Invalid AES key or IV length.");
                return false;
            }
            CheckAndRemoveChecksumAES(key, out byte[] Key, out byte[] IV);
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            return true;
        }

        public bool ImportRSAKey(byte[] PublicKey)
        {
            if (!CheckAndRemoveChecksumRSA(PublicKey, out byte[] DecryptedPublicKey))
            {
                Console.WriteLine("Invalid key format or checksum.");
                return false;
            }
            rsaTheir.ImportRSAPublicKey(DecryptedPublicKey, out _);
            return true;
        }
        
        public byte[] Encrypt(byte[] Message)
        {
            if (aesEnabled)
            {
                var encryptor = aes.CreateEncryptor();
                return encryptor.TransformFinalBlock(Message, 0, Message.Length);
            }

            var encrypted = rsaTheir.Encrypt(Message, RSAEncryptionPadding.OaepSHA256);
            return encrypted;
        }

        public byte[] Decrypt(byte[] EncryptedMessage)
        {
            if (aesEnabled)
            {
                var decryptor = aes.CreateDecryptor();
                return decryptor.TransformFinalBlock(EncryptedMessage, 0, EncryptedMessage.Length);
            }
            
            var decrypted = rsaMine.Decrypt(EncryptedMessage, RSAEncryptionPadding.OaepSHA256);
            return decrypted;
        }

        public override string ToString()
        {
            return "CrazyRSA Instance"; // Customize the string representation as needed
        }
        // Add methods for encryption, decryption, key generation, etc. as required
    }
}
