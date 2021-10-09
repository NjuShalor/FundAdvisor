using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzFunction.FundAdvisor.Notification
{
    internal class BarkNotificationClient : INotificationClient
    {
        Task INotificationClient.SendMessageAsync(string title, IEnumerable<string> message)
        {
            /*
            string content = string.Join('\n', messages);
            Uri uri = new Uri($"https://www.geeksforever.in:8001/{Constants.BarkDeviceId}/{title}/{WebUtility.UrlEncode(content)}");
            using (HttpClientHandler clientHandler = new HttpClientHandler())
            {

                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                clientHandler.SslProtocols = SslProtocols.Tls12;
                using (HttpClient client = new HttpClient(clientHandler))
                {
                    var response = client.GetAsync(uri).Result;
                    return response.IsSuccessStatusCode;
                }
            }*/
            throw new NotImplementedException();
        }
    }
}
