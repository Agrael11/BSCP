using System.Text.Json.Nodes;

namespace BSCPTester
{
    internal class Program
    {
        
        static void Main(string[] args)
        {
            using var wc = new WebComm();
            Console.WriteLine("Send Json Mode Activation");
            wc.SendTBNumber(0x3C);
            JsonObject jsonObject = new JsonObject();
            JsonArray jsonArray = new JsonArray();
            jsonObject.Add("requestType", "ping");
            jsonObject.Add("requestData", jsonArray);
            Console.WriteLine("Sent PING request");
            wc.SendString(jsonObject.ToJsonString());
            var result = wc.ReceiveString()??"";
            var jsonResponse = JsonNode.Parse(result);
            if (jsonResponse != null && jsonResponse["responseType"]?.ToString() == "pong")
            {
                Console.WriteLine("Received PONG response");
            }
            else
            {
                Console.WriteLine("Did not receive expected PONG response");
            }
            //wc.Dispose();
        }
    }
}
