using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System;

namespace TradeStationWebApiDemo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var tradeStationApiSettings = configuration.GetSection("TradeStationApi").Get<TradeStationApiSettings>();

            var api = new TradeStationWebApi(
                tradeStationApiSettings.APIKey,
                tradeStationApiSettings.APISecret,
                tradeStationApiSettings.Environment,
                tradeStationApiSettings.RedirectUri
            );

            // Get Accounts
            var accounts = await api.GetUserAccounts();
            foreach (var account in accounts.ToArray())
            {
                Console.WriteLine("Key: {0}\t\tName: {1}\t\tType: {2}\t\tTypeDescription: {3}",
                                  account.Key, account.Name, account.Type, account.TypeDescription);
            }
        }
    }
}