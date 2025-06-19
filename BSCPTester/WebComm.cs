using CommonAPI;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace BSCPTester
{
    internal class WebComm : IDisposable
    {
        private int port;
        private TcpClient client;
        private Stream stream;
        private CrazyRSA? rsa = null; // RSA encryption object, can be set later

        enum MessageType
        {
            General,
            Client,
            Error,
            Action,
            Debug
        }

        private void WriteLine(string message, MessageType type = MessageType.General)
        {
            var oldColor = Console.ForegroundColor;
            switch (type)
            {
                case MessageType.General:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case MessageType.Client:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case MessageType.Action:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case MessageType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case MessageType.Debug:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    break;
            }
            if (type == MessageType.Debug)
            {
                Console.WriteLine($"[DEBUG] {message}");
            }
            else
            {
                Console.WriteLine($"[WebComm{((type == MessageType.Error) ? " Error" : "")}] {message}");
            }
            Console.ForegroundColor = oldColor;
        }

        public WebComm(int portNumber = 5050)
        {
            WriteLine("Initializing WebComm...");
            port = portNumber;
            client = new TcpClient();

            try
            {
                WriteLine($"Connecting to server on port {port}...");
                client.Connect("localhost", port);
                stream = client.GetStream();
                WriteLine("Connected to server.");
                SendStartHandshake();
                SendVersion(2);
                SendPublicRSAKey();
                ReceivePublicRSAKey();
                ReceivePublicAESKey();
                rsa.EnableAES();
            }
            catch (Exception ex)
            {
                WriteLine($"Error connecting to server: {ex.Message}");
            }
        }

        public byte[] ReadStreamPureLength(int length, int count = 1)
        {
            if (rsa is not null)
            {
                length = rsa.GetPaddedSize(length);
            }
            byte[] buffer = new byte[length];
            int bytesRead = 0;
            while (bytesRead < length)
            {
                int read = stream.Read(buffer, bytesRead, length - bytesRead);
                if (read == 0)
                {
                    throw new EndOfStreamException("Reached end of stream before reading expected number of bytes.");
                }
                bytesRead += read;
            }
            if (rsa is not null)
            {
                buffer = rsa.Decrypt(buffer);
            }

            return buffer;
        }

        public byte[] ReadStream(int length, int count = 1)
        {
            length = (length * count + 7) / 8;
            return ReadStreamPureLength(length, count);
        }

        public void WriteStream(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Data to write cannot be null or empty.", nameof(data));
            }

            if (rsa is not null)
            {
                data = rsa.Encrypt(data);
            }

            stream.Write(data, 0, data.Length);
        }

        private bool SendStartHandshake()
        {
            CrazyHandshake handshake = new CrazyHandshake(CrazyHandshake.HandshakeType.ClientHello);
            var data = new CrazyType[] { handshake };
            var bytes = CrazyType.ToByteArray(data, true);
            WriteStream(bytes);
            var responsedata = ReadStream(CrazyHandshake.CharacterSize);
            var response = CrazyType.FromByteArray(responsedata, false, CrazyHandshake.CharacterSize)[0];
            var responsehs = new CrazyHandshake(response);
            if (responsehs.GetHandshake() != CrazyHandshake.HandshakeType.ServerHello)
            {
                WriteLine("Server did not respond with ServerHello. Exiting.", MessageType.Error);
                return false;
            }
            WriteLine("Handshake successful. Server responded with ServerHello.");
            return true;
        }

        private bool SendVersion(int version)
        {
            var versionValue = new CrazyVersion(version);
            var data = new CrazyType[] { versionValue };
            var bytes = CrazyType.ToByteArray(data, true);
            WriteStream(bytes);
            var response = ReceiveReceiveStatus();
            if (response == CrazyReceiveStatus.Status.Success)
            {
                WriteLine("Version verified");
                return true;
            }
            else
            {
                WriteLine("Failed to verify version.", MessageType.Error);
            }
            return false;
        }

        public void SendPublicRSAKey()
        {
            var newrsa = new CrazyRSA();
            var bytes = newrsa.ExportRSAKey();
            SendTBNumber(bytes.Length, false);
            WriteStream(bytes);
            WriteLine("Public key sent to server.");
            rsa = newrsa; // Update the rsa object to the new one
        }

        private void SendHandshake(CrazyHandshake.HandshakeType type)
        {
            CrazyHandshake handshake = new CrazyHandshake(type);
            var data = new CrazyType[] { handshake };
            var bytes = CrazyType.ToByteArray(data, true);
            WriteStream(bytes);
            WriteLine($"Handshake of type {type} sent to server.");
        }

        public bool SendTBNumber(int number, bool withShake = true)
        {
            var numbervalue = new CrazyTwelveBitNumber(number);
            var data = new CrazyType[] { numbervalue };
            var bytes = CrazyType.ToByteArray(data, true);
            
            
            if (withShake) SendHandshake(CrazyHandshake.HandshakeType.SendingNumber);
            
            WriteStream(bytes);
            
            if (!withShake)
            {
                return true;
            }

            var response = ReceiveReceiveStatus();
            if (response == CrazyReceiveStatus.Status.Success)
            {
                WriteLine($"Number {number} sent successfully.");
                return true;
            }
            else
            {
                WriteLine($"Failed to send number {number}.", MessageType.Error);
                return false;
            }
        }

        public bool SendString(string message)
        {
            var messageLength = message.Length;
            var asciiChars = CrazyCharacter.FromString(message);
            CrazyCharacter.SetChecksum(ref asciiChars);
            var messageData = CrazyType.ToByteArray(asciiChars, true);
            var checksumValid = CrazyCharacter.CompareChecksum(asciiChars, message);
            if (!checksumValid)
            {
                WriteLine("Checksum mismatch when sending string.", MessageType.Error);
                return false;
            }


            SendHandshake(CrazyHandshake.HandshakeType.SendingString);
            SendTBNumber(messageLength, false);
            WriteStream(messageData);

            var response = ReceiveReceiveStatus();
            if (response == CrazyReceiveStatus.Status.Success)
            {
                WriteLine($"String '{message}' sent successfully.");
                return true;
            }
            else
            {
                WriteLine($"Failed to send string '{message}'.");
                return false;
            }
        }

        public CrazyHandshake.HandshakeType ReceiveHandshake()
        {
            var responseData = ReadStream(CrazyHandshake.CharacterSize);
            var response = CrazyType.FromByteArray(responseData, false, CrazyHandshake.CharacterSize)[0];
            var handshake = new CrazyHandshake(response);
            return handshake.GetHandshake();
        }

        public bool ReceivePublicRSAKey()
        {
            if (rsa is null)
            {
                WriteLine("RSA object is not initialized. Cannot receive public key.", MessageType.Error);
                return false;
            }
            var oldrsa = rsa; // Save the old RSA object
            rsa = null; // Temporarily set rsa to null to avoid conflicts
            var length = ReceiveTBNumber().GetValue();
            var response = ReadStreamPureLength(length);
            rsa = oldrsa; // Restore the old RSA object 
            if (rsa.ImportRSAKey(response))
            {
                WriteLine("Public key received from server.");
                return true;
            }
            else
            {
                WriteLine("Failed to import public key from server.", MessageType.Error);
                return false;
            }
        }
        public bool ReceivePublicAESKey()
        {
            if (rsa is null)
            {
                WriteLine("RSA object is not initialized. Cannot receive public key.", MessageType.Error);
                return false;
            }
            var length = ReceiveTBNumber().GetValue();
            var response = ReadStreamPureLength(length);
            if (rsa.ImportAESKey(response))
            {
                WriteLine("Public AES key received from server.");
                return true;
            }
            else
            {
                WriteLine("Failed AES to import public key from server.", MessageType.Error);
                return false;
            }
        }

        public CrazyTwelveBitNumber ReceiveTBNumber()
        {
            var responseData = ReadStream(CrazyTwelveBitNumber.CharacterSize);
            var response = CrazyType.FromByteArray(responseData, false, CrazyTwelveBitNumber.CharacterSize)[0];
            return new CrazyTwelveBitNumber(response);
        }

        public string ReceiveString()
        {
            var messageLength = ReceiveTBNumber().GetValue();


            if (messageLength == 0)
            {
                return string.Empty;
            }

            var responseBytess = ReadStream(CrazyCharacter.CharacterSize, messageLength);
            var text = CrazyCharacter.FromByteArray(responseBytess, false);
            var responseData = CrazyCharacter.FromByteArrayString(responseBytess, false);
            var checksumResult = CrazyCharacter.CompareChecksum(responseData, text);
            if (!checksumResult)
            {
                WriteLine($"Checksum mismatch when receiving string {text}.", MessageType.Error);
            }
            else {
                WriteLine($"Checksum valid for received string: {text}");
            }
            return text ;
        }

        public CrazyReceiveStatus.Status ReceiveReceiveStatus()
        {
            var responseData = ReadStream(CrazyReceiveStatus.CharacterSize);
            var response = CrazyType.FromByteArray(responseData, false, CrazyReceiveStatus.CharacterSize)[0];
            var receiveStatus = new CrazyReceiveStatus(response);
            return receiveStatus.GetStatus();
        }

        public void Dispose()
        {
            SendHandshake(CrazyHandshake.HandshakeType.Goodbye);
            stream.Close();
            stream.Dispose();
            client.Close();
            client.Dispose();
        }
    }
}
