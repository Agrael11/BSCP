//#define MOREINFO

using CommonAPI;
using System.Text.Json.Nodes;

namespace Server
{
    internal class Program
    {
        private int version = 2; // Current version of the protocol
        private const string protocolName = "BSCP";
        int port = 5050;
        enum MessageType
        {
            General,
            Client,
            Error,
            Action,
            Debug
        }

#if MOREINFO
        public void DebugPrintByteArray(byte[] array, bool sending)
        {
            var strings = array.Select(b => Convert.ToString(b, 2).PadLeft(8,'0'));
            WebCommWriteLine($"{(sending?"[Out]":"[In]")} {string.Join(' ', strings)}", MessageType.Debug);
        }
#endif

        void WebCommWriteLine(string message, MessageType type = MessageType.General)
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

        static void Main(string[] args)
        {
            Program server = new Program();
            if (args.Length > 0 && int.TryParse(args[0], out int portNumber))
            {
                server.port = portNumber;
            }
            server.StartServer();
        }

        public void StartServer()
        {
            // Code to start the server on the specified port
            WebCommWriteLine($"{protocolName} v{version} Server started on port {port}");
            System.Net.Sockets.TcpListener listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();
            WebCommWriteLine("Waiting for a connection...");
            while (true)
            {
                var client = listener.AcceptTcpClient();
                WebCommWriteLine("Client connected.");
                // Handle client connection in a separate method or thread
                HandleClient(client);
            }
        }
        public byte[] ReadStreamPureLength(Stream stream, int length, CrazyRSA? rsa, int count = 1)
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
#if MOREINFO
            DebugPrintByteArray(buffer, false);
#endif
            return buffer;
        }

        public byte[] ReadStream(Stream stream, int length, CrazyRSA? rsa, int count = 1)
        {
            length = ((length + 1)*count + 7) / 8;
            return ReadStreamPureLength(stream, length, rsa, count);
        }

        public void WriteStream(Stream stream, byte[] data, CrazyRSA? rsa)
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
#if MOREINFO
            DebugPrintByteArray(data, true);
#endif
        }

        public CrazyHandshake.HandshakeType ReadHandshake(Stream stream, CrazyRSA? rsa)
        {
            var data = ReadStream(stream, CrazyHandshake.CharacterSize, rsa);
            var receveddata = CrazyType.FromByteArray(data, true, CrazyHandshake.CharacterSize);
            var handshake = new CrazyHandshake(receveddata[0]);
            
            WebCommWriteLine($"Received handshake: {Enum.GetName(handshake.GetHandshake())}", MessageType.Client);
            return handshake.GetHandshake();
        }

        public bool ReadPublicKeyRSA(Stream stream, CrazyRSA? rsa)
        {
            if (rsa is null)
            {
                WebCommWriteLine("RSA object is not initialized. Cannot receive public key.", MessageType.Error);
                return false;
            }
            var length = ReadNumber(stream, null);
            var response = ReadStreamPureLength(stream, length, null);
            if (rsa.ImportRSAKey(response))
            {
                WebCommWriteLine("Public key received from client.");
                return true;
            }
            else
            {
                WebCommWriteLine("Failed to import public key from client.", MessageType.Error);
                return false;
            }
        }

        public int ReadVersion(Stream stream, CrazyRSA? rsa)
        {
            var data = ReadStream(stream, CommonAPI.CrazyVersion.CharacterSize, rsa);
            var receveddata = CrazyType.FromByteArray(data, true, CommonAPI.CrazyVersion.CharacterSize);
            var version = new CrazyVersion(receveddata[0]);
            WebCommWriteLine($"Received version: {version.GetVersion()}", MessageType.Client);
            return version.GetVersion();
        }

        public int ReadNumber(Stream stream, CrazyRSA? rsa)
        {
            var data = ReadStream(stream, CrazyTwelveBitNumber.CharacterSize, rsa);
            var receveddata = CrazyType.FromByteArray(data, true, CrazyTwelveBitNumber.CharacterSize);
            var number = new CrazyTwelveBitNumber(receveddata[0]);
            WebCommWriteLine($"Received number: {number.GetValue()}", MessageType.Client);
            return number.GetValue();
        }

        public string ReadString(Stream stream, CrazyRSA? rsa)
        {
            var byteData = ReadStream(stream, CrazyTwelveBitNumber.CharacterSize, rsa);
            var receveddata = CrazyType.FromByteArray(byteData, true, CrazyTwelveBitNumber.CharacterSize);
            var lng = new CrazyTwelveBitNumber(receveddata[0]);
            var length = lng.GetValue();

            byteData = ReadStream(stream, CrazyCharacter.CharacterSize, rsa, length);
            var stringData = CrazyCharacter.FromByteArrayString(byteData, true);
            var text = CrazyCharacter.ToString(stringData);
            var checksumValid = CrazyCharacter.CompareChecksum(stringData, text);
            if (!checksumValid)
            {
                WebCommWriteLine($"Checksum mismatch when reading string {text}.", MessageType.Error);
            }
            else
            {
                WebCommWriteLine($"Checksum valid for string {text}.");
            }
            return text;
        }

        public void SendHandshake(CrazyHandshake.HandshakeType type, Stream stream, CrazyRSA? rsa)
        {
            var handshake = new CrazyHandshake(type);
            var datatosend = CrazyType.ToByteArray([handshake], false);
            WriteStream(stream, datatosend, rsa);
            WebCommWriteLine($"Sent handshake: {Enum.GetName(type)}", MessageType.Client);
        }

        public void SendPublicKeyRSA(Stream stream, CrazyRSA rsa)
        {
            var bytes = rsa.ExportRSAKey();
            var length = bytes.Length;
            SendNumber(length, stream, null);
            WriteStream(stream, bytes, null);
            WebCommWriteLine("Public RSA key sent to client.");
        
        }
        
        public void SendPublicKeyAES(Stream stream, CrazyRSA rsa)
        {
            var bytes = rsa.ExportAESKey();
            var encoded = rsa.Encrypt(bytes);
            var length = encoded.Length;
            SendNumber(length, stream, rsa);
            WriteStream(stream, bytes, rsa);
            WebCommWriteLine("Public AES key sent to client.");
        }

        public void SendNumber(int number, Stream stream, CrazyRSA rsa)
        {
            var numberToSend = new CrazyTwelveBitNumber(number);
            var datatosend = CrazyType.ToByteArray([numberToSend], false);
            WriteStream(stream, datatosend, rsa);
            WebCommWriteLine($"Sent number: {number}", MessageType.Client);
        }

        public void SendString(string message, Stream stream, CrazyRSA rsa)
        {
            var messageLength = message.Length;
            var asciiChars = CrazyCharacter.FromString(message);
            CrazyCharacter.SetChecksum(ref asciiChars);
            var messageData = CrazyType.ToByteArray(asciiChars, false);
            var checksumValid = CrazyCharacter.CompareChecksum(asciiChars, message);
            if (!checksumValid)
            {
                WebCommWriteLine("Checksum mismatch when sending string.", MessageType.Error);
                return;
            }

            SendNumber(messageLength, stream, rsa);
            
            WriteStream(stream, messageData, rsa);
            
            WebCommWriteLine($"Sent string: {message}", MessageType.Client);
        }

        public void SendOkay(Stream stream, CrazyRSA? rsa)
        {
            var versionResponse = new CrazyReceiveStatus(CrazyReceiveStatus.Status.Success);
            var datatosend = CrazyType.ToByteArray([versionResponse], false);
            WriteStream(stream, datatosend, rsa);
            WebCommWriteLine("Sent OK response.", MessageType.Client);
        }

        public void SendFail(Stream stream, CrazyRSA? rsa)
        {
            var versionResponse = new CrazyReceiveStatus(CrazyReceiveStatus.Status.Failure);
            var datatosend = CrazyType.ToByteArray([versionResponse], false);
            WriteStream(stream, datatosend, rsa);
            WebCommWriteLine("Sent failure response.", MessageType.Client);
        }

        public void HandleClient(System.Net.Sockets.TcpClient client)
        {
            var rsa = new CrazyRSA();
            int stage = 0;
            int lastnumber = 0;
            // Code to handle client communication
            using (var stream = client.GetStream())
            {
                while (true)
                {
                    if (stage == -1)
                    {
                        break;
                    }
                    if (stage == 0)
                    {
                        var shake = ReadHandshake(stream, null);
                        if (shake != CrazyHandshake.HandshakeType.ClientHello)
                        {
                            WebCommWriteLine("Invalid handshake type received.", MessageType.Error);
                            break;
                        }
                        SendHandshake(CrazyHandshake.HandshakeType.ServerHello, stream, null);
                        var version = ReadVersion(stream, null);
                        if (version != this.version)
                        {
                            WebCommWriteLine("Unsupported version received.", MessageType.Error);
                            SendFail(stream, null);
                            break;
                        }
                        else
                        {
                            SendOkay(stream, null);
                            stage++;
                        }
                        ReadPublicKeyRSA(stream, rsa);
                        SendPublicKeyRSA(stream, rsa);
                        SendPublicKeyAES(stream, rsa);
                        rsa.EnableAES();
                    }
                    else if (stage == 1)
                    {
                        var handshake = ReadHandshake(stream, rsa);
                        switch (handshake)
                        {
                            case CrazyHandshake.HandshakeType.SendingNumber:
                                var number = ReadNumber(stream, rsa);
                                if (number == 0x3C)
                                {
                                    WebCommWriteLine("... 0x3C = Activating Json Mode.", MessageType.Action);
                                }
                                lastnumber = number;
                                SendOkay(stream, rsa);
                                break;
                            case CrazyHandshake.HandshakeType.SendingString:
                                var text = ReadString(stream, rsa);
                                SendOkay(stream, rsa);
                                if (lastnumber == 0x3C)
                                {
                                    var json = JsonObject.Parse(text);
                                    if (json is null)
                                    {
                                        break;
                                    }
                                    var type = json["requestType"];
                                    if (type is not null)
                                    {
                                        var responseJson = new JsonObject();
                                        switch (type.AsValue().ToString())
                                        {
                                            case "ping":
                                                WebCommWriteLine("Received ping request.", MessageType.Action);
                                                responseJson.Add("responseType", "pong");
                                                responseJson.Add("responseData", new JsonArray());
                                                WebCommWriteLine("Responding with pong.", MessageType.Action);
                                                break;
                                            default:
                                                WebCommWriteLine("Received unkown request.", MessageType.Action);
                                                responseJson.Add("responseType", "unknownRequest");
                                                responseJson.Add("responseData", new JsonArray());
                                                WebCommWriteLine("Responding with unknownRequest.", MessageType.Action);
                                                break;
                                        }
                                        var responseString = responseJson.ToJsonString();
                                        SendString(responseString, stream, rsa);
                                    }
                                }
                                break;
                            case CrazyHandshake.HandshakeType.Goodbye:
                                WebCommWriteLine("Client requested to disconnect.");
                                stage = -1;
                                break;
                        }
                    }
                }
            }
            client.Close();
            WebCommWriteLine("Client disconnected.");
        }
    }
}
