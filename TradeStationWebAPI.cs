using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace TradeStationWebApiDemo
{
    public class TradeStationWebApi
    {
        private string Key { get; set; }
        private string Secret { get; set; }
        private string Host { get; set; }
        private string RedirectUri { get; set; }
        private AccessToken Token { get; set; }
        private readonly HttpClient _httpClient = new();

        public TradeStationWebApi(string key, string secret, string environment, string redirecturi)
        {
            this.Key = key;
            this.Secret = secret;
            this.RedirectUri = redirecturi;

            if (environment.Equals("LIVE")) this.Host = "https://api.tradestation.com/v2";
            if (environment.Equals("SIM")) this.Host = "https://sim.api.tradestation.com/v2";

            // Disable Tls 1.0 and use Tls 1.2
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;

            this.Token = GetAccessToken(GetAuthorizationCode()).Result; // Note: this blocking call is for the sake of the example
        }

        private string GetAuthorizationCode()
        {
            // Display the authorization URL
            Console.WriteLine("Go here and login:");
            Console.WriteLine(string.Format("{0}/{1}", this.Host,
                                            string.Format(
                                                "authorize?client_id={0}&response_type=code&redirect_uri={1}",
                                                this.Key,
                                                this.RedirectUri)));

            // Ask the user to manually enter the code
            Console.WriteLine("\nAfter authorizing the application, you will be redirected to a webpage.");
            Console.WriteLine("Please copy the 'code' parameter from the redirected URL and paste it here.");

            // Read the code from the console
            string code = Console.ReadLine();

            code = code.Replace("%3D", "=");
            return code;
        }


        private string GetAuthorizationCode2()
        {
            Console.WriteLine("Go here and login:");
            Console.WriteLine(string.Format("{0}/{1}", this.Host,
                                            string.Format(
                                                "authorize?client_id={0}&response_type=code&redirect_uri={1}",
                                                this.Key,
                                                this.RedirectUri)));

            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add(this.RedirectUri);
                listener.Start();
                Console.WriteLine("\nEmbedded HTTP Server is Listening for Authorization Code...");

                var context = listener.GetContext();
                var req = context.Request;
                var res = context.Response;

                var responseString = "<html><body><script>window.open('','_self').close();</script></body></html>";
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                res.ContentLength64 = buffer.Length;
                var output = res.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();

                listener.Stop();
                return req.QueryString.Get("code");
            }
        }

        private async Task<AccessToken> GetAccessToken(string authcode)
        {
            Console.WriteLine("Trading the Auth Code for an Access Token...");

            var requestUri = $"{Host}/security/authorize";

            var postData = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = authcode,
                ["client_id"] = Key,
                ["redirect_uri"] = RedirectUri,
                ["client_secret"] = Secret
            };

            using var content = new FormUrlEncodedContent(postData);

            try
            {
                var response = await _httpClient.PostAsync(requestUri, content);
                var responseBody = await response.Content.ReadAsStringAsync();


                return JsonSerializer.Deserialize<AccessToken>(responseBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(-1);
                throw;
            }
        }

        private async Task<T> GetDeserializedResponse<T>(HttpRequestMessage request)
        {
            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseBody);
        }

        internal async Task<IEnumerable<Symbol>> SymbolSuggest(string suggestText)
        {
            var resourceUri = new Uri($"{this.Host}/data/symbols/suggest/{suggestText}?oauth_token={this.Token.access_token}");

            Console.WriteLine("Searching for symbols ... ");

            try
            {
                var response = await _httpClient.GetAsync(resourceUri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<Symbol>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                Environment.Exit(-1);
                throw;
            }
        }

        internal async Task<IEnumerable<AccountInfo>> GetUserAccounts()
        {
            var resourceUri = new Uri($"{this.Host}/users/{this.Token.userid}/accounts?oauth_token={this.Token.access_token}");

            Console.WriteLine("Getting Accounts");

            try
            {
                var response = await _httpClient.GetAsync(resourceUri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<AccountInfo>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                Environment.Exit(-1);
                throw;
            }
        }

        internal async Task GetQuoteChanges(string symbols)
        {
            var resourceUri = new Uri($"{this.Host}/stream/quote/changes/{symbols}?oauth_token={this.Token.access_token}");

            Console.WriteLine("Streaming Quote/Changes");

            try
            {
                using var response = await _httpClient.GetStreamAsync(resourceUri);
                using var reader = new StreamReader(response, Encoding.UTF8);

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;

                    var quote = JsonSerializer.Deserialize<Quote>(line);
                    Console.WriteLine($"{quote.Symbol}: ASK = {quote.Ask}; BID = {quote.Bid}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                Environment.Exit(-1);
                throw;
            }
        }

        public async Task<IEnumerable<OrderDetail>> GetOrders(IEnumerable<int> accountKeys)
        {
            var resourceUri = new Uri($"{this.Host}/accounts/{String.Join(",", accountKeys)}/orders?oauth_token={this.Token.access_token}");

            Console.WriteLine("Getting Orders");

            try
            {
                var response = await _httpClient.GetAsync(resourceUri);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<OrderDetail>>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                Environment.Exit(-1);
                throw;
            }
        }

        public async Task<IEnumerable<Quote>> GetQuotes(IEnumerable<string> symbols)
        {
            // encode symbols (eg: replace " " with "%20")
            var encodedSymbols = symbols.Select(symbol =>
            {
                var urlEncode = System.Web.HttpUtility.UrlEncode(symbol);
                return urlEncode != null ? urlEncode.Replace("+", "%20") : null;
            });

            var resourceUri = new Uri($"{this.Host}/data/quote/{String.Join(",", encodedSymbols)}?oauth_token={this.Token.access_token}");

            Console.WriteLine("Getting Quotes");

            try
            {
                var response = await _httpClient.GetStringAsync(resourceUri);
                return JsonSerializer.Deserialize<IEnumerable<Quote>>(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                Environment.Exit(-1);
                throw;
            }
        }

        public async Task<IEnumerable<Confirmation>> GetConfirmations(Order order)
        {
            var orderJson = JsonSerializer.Serialize(order);
            var resourceUri = new Uri($"{this.Host}/orders/confirm?oauth_token={this.Token.access_token}");

            Console.WriteLine("Getting Order Confirmation");

            using var content = new StringContent(orderJson, Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync(resourceUri, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<Confirmation>>(responseContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
                Environment.Exit(-1);
                throw;
            }
        }

        public async Task<IEnumerable<OrderResult>> PlaceOrder(Order order)
        {
            var requestUri = $"{Host}/orders?oauth_token={Token.access_token}";

            Console.WriteLine("Placing Order");

            var orderjson = JsonSerializer.Serialize(order);
            using var content = new StringContent(orderjson, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(requestUri, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<IEnumerable<OrderResult>>(responseBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(-1);
                throw;
            }
        }
    }
}
