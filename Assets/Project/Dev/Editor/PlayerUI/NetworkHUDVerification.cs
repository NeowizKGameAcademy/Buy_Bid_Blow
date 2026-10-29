using System;
using System.Collections.Generic;
using System.Reflection;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Portfolio;
using BuyBidBlow.Systems.Types;
using BuyBidBlow.UI.HUD;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class NetworkHUDVerification
    {
        public static void Run()
        {
            var flags = BindingFlags.Static | BindingFlags.Public;
            var current = typeof(MatchNet).GetProperty("Current", flags);
            var portfolio = typeof(PlayerNet).GetProperty("LocalPortfolio", flags);
            var previousMatch = MatchNet.Current; var previousPortfolio = PlayerNet.LocalPortfolio;
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(NetworkHUDSetup.PrefabPath));
            var state = new GameObject("HUD verification state", typeof(Mirror.NetworkIdentity));
            try
            {
                var view = root.GetComponent<NetworkPlayerHUD>();
                current.SetValue(null,null); portfolio.SetValue(null,null); view.Refresh();
                Check(view.cashLabel.text == "—" && view.timeLabel.text == "--:--", "empty state");
                var match = state.AddComponent<MatchNet>(); match.totalRounds = 17; match.round = 2;
                // Isolated display fixture; no transport or actual server is started here.
                match.prices.IsWritable = () => true; match.prices.IsRecording = () => false;
                match.phase = MatchNet.Phase.Playing; match.auctionStage = MatchNet.AuctionStage.Open;
                match.roundClosesAt = Mirror.NetworkTime.time + 90;
                match.prices.Add(20); match.prices.Add(30); match.prices.Add(40);
                current.SetValue(null,match);
                portfolio.SetValue(null,new PortfolioSnapshot(200,35,new Dictionary<Company,int>{{(Company)0,2},{(Company)1,1}}));
                view.Refresh();
                Check(view.cashLabel.text == "$200" && view.totalLabel.text == "$270", "cash and valuation");
                Check(view.reservedLabel.text.Contains("$35") && view.reservedLabel.text.Contains("$165"), "reserved is deducted once");
                Check(view.holdings[0].text == "2" && view.holdings[2].text == "0", "holdings");
                Check(view.roundLabel.text == "2 / 17 ROUND" && view.phaseLabel.text == "공개 경매", "public state");
                Check(view.roundStart.gameObject.activeSelf, "initial round notification");
                view.roundStart.Hide(); view.Refresh(); Check(!view.roundStart.gameObject.activeSelf, "no repeated notification");
                match.round = 3; match.auctionStage = MatchNet.AuctionStage.None; view.Refresh();
                Check(view.roundStart.gameObject.activeSelf && view.roundStart.RoundLabel.text == "3 / 17 ROUND", "next round notification");
                match.phase = MatchNet.Phase.Finished; view.Refresh();
                Check(view.timeLabel.text == "00:00" && !view.roundStart.gameObject.activeSelf, "finished state");
                current.SetValue(null,null); portfolio.SetValue(null,null); view.Refresh();
                Check(view.cashLabel.text == "—", "disconnect clears private data");
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    Check(component && !component.GetType().Assembly.GetName().Name.Contains("Dev"), "no Dev runtime component");
                System.IO.File.WriteAllText("Library/NetworkHUDVerification.result", "PASS: empty, server state, portfolio, reservation, valuation, round notice, duplicate prevention, finish, disconnect, runtime dependencies.");
            }
            finally
            {
                Object.DestroyImmediate(root); Object.DestroyImmediate(state);
                current.SetValue(null,previousMatch); portfolio.SetValue(null,previousPortfolio);
            }
        }
        static void Check(bool condition,string message) { if(!condition) throw new Exception("HUD verification: " + message); }
    }
}
