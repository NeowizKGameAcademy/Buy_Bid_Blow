using System;
using System.IO;
using System.Linq;
using System.Text;
using BuyBidBlow.Systems.Market;
using BuyBidBlow.Systems.Match;
using UnityEditor;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    public static class PriceChangePreview
    {
        private const string SettingsPath = "Assets/Project/Systems/Match/MatchSettings.json";

        [MenuItem("Tools/BuyBidBlow/Preview Price Changes")]
        public static void Preview()
        {
            var settings = JsonUtility.FromJson<MatchSettings>(File.ReadAllText(SettingsPath));
            settings.Validate();

            int seed = Environment.TickCount;
            var random = new System.Random(seed);
            int[] prices = settings.RollStartingPrices(random);

            var log = new StringBuilder($"Price changes (seed {seed})\nStart: {string.Join(" / ", prices)}\n");

            for (int round = 1; round <= settings.RoundCount; round++)
            {
                var changes = PriceChangeGenerator.Generate(settings.PriceChange, random);
                for (int i = 0; i < changes.Length; i++)
                    prices[i] = Math.Max(settings.MinPrice, prices[i] + changes[i].Amount);
                log.AppendLine($"R{round}: {string.Join(", ", changes.Select(c => c.ToString()))}  ->  {string.Join(" / ", prices)}");
            }

            Debug.Log(log.ToString());
        }
    }
}
