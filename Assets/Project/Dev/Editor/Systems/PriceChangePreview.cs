using System;
using System.IO;
using System.Linq;
using System.Text;
using BuyBidBlow.Systems.Match;
using UnityEditor;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    public sealed class PriceChangePreview : EditorWindow
    {
        private const string SettingsPath = "Assets/Project/Systems/Match/MatchSettings.json";
        [SerializeField] private int seed = 20261013;
        private MatchSettings settings;
        private MatchPriceSimulation simulation;
        private Vector2 scroll;
        private string error;
        private static readonly string[] Companies = { "Straw", "Timber", "Brick" };
        private static readonly Color[] Colors = {
            new Color(.88f, .66f, .15f), new Color(.25f, .72f, .45f), new Color(.9f, .36f, .32f)
        };

        [MenuItem("Tools/BuyBidBlow/Preview Price Changes")]
        public static void Preview()
        {
            var window = GetWindow<PriceChangePreview>("Match Prices");
            window.minSize = new Vector2(600, 540);
            window.Generate();
            window.Show();
        }

        private void OnEnable() => Generate();

        private void Generate()
        {
            try
            {
                settings = JsonUtility.FromJson<MatchSettings>(File.ReadAllText(SettingsPath));
                simulation = MatchPriceSimulation.Generate(settings, seed);
                error = null;
            }
            catch (Exception e) { simulation = null; error = e.Message; }
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            seed = EditorGUILayout.IntField("Seed", seed);
            if (GUILayout.Button("Reload / Replay", GUILayout.Width(120))) Generate();
            if (GUILayout.Button("New Seed", GUILayout.Width(90))) { seed = Environment.TickCount; Generate(); }
            EditorGUILayout.EndHorizontal();
            if (error != null) { EditorGUILayout.HelpBox(error, MessageType.Error); return; }
            if (simulation == null) return;
            int seconds = simulation.Rounds.Last().ClosesAtSeconds;
            EditorGUILayout.LabelField($"{MatchPriceSimulation.HistoryRoundCount} historical changes + {settings.RoundCount} match rounds | Match: {seconds / 60} min");
            string auctionTimes = string.Join(", ", settings.AuctionSlots.Select((slot, i) => $"#{i + 1} {slot.StartsAtSeconds}-{slot.StartsAtSeconds + slot.DurationSeconds}s"));
            EditorGUILayout.HelpBox($"Price floor: ${settings.MinPrice}. Prices change at round close only. Auctions: {auctionTimes}.", MessageType.Info);
            EditorGUILayout.LabelField("History is back-calculated to Start; below-floor paths are resampled. History does not predict future changes.", EditorStyles.wordWrappedMiniLabel);

            Rect chart = GUILayoutUtility.GetRect(100, 250, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) DrawChart(chart);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < Companies.Length; i++)
            {
                Color old = GUI.color;
                GUI.color = Colors[i];
                GUILayout.Label(Companies[i]);
                GUI.color = old;
            }
            if (GUILayout.Button("Export CSV", GUILayout.Width(100)))
            {
                string path = EditorUtility.SaveFilePanel("Save price preview", "", $"match-prices-{simulation.Seed}.csv", "csv");
                if (!string.IsNullOrEmpty(path)) File.WriteAllText(path, simulation.ToCsv(), new UTF8Encoding(true));
            }
            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawRow("Round / close", "Straw ($ / change)", "Timber ($ / change)", "Brick ($ / change)");
            DrawRow("History -5 (base)", simulation.HistoryPrices[0][0].ToString(), simulation.HistoryPrices[0][1].ToString(), simulation.HistoryPrices[0][2].ToString());
            for (int h = 1; h <= MatchPriceSimulation.HistoryRoundCount; h++)
            {
                string label = h == MatchPriceSimulation.HistoryRoundCount ? "Start / 00:00" : $"History {h - MatchPriceSimulation.HistoryRoundCount}";
                string[] values = Enumerable.Range(0, 3).Select(i => $"{simulation.HistoryPrices[h][i]} ({simulation.HistoryChanges[h - 1][i]:+0;-0;0})").ToArray();
                DrawRow(label, values[0], values[1], values[2]);
            }
            foreach (var row in simulation.Rounds)
            {
                string label = $"R{row.Round}{(row.HasAuctions ? " [Auction]" : "")} / {row.ClosesAtSeconds / 60:D2}:{row.ClosesAtSeconds % 60:D2}";
                string[] values = Enumerable.Range(0, 3).Select(i => {
                    string delta = row.AppliedChanges[i].ToString("+0;-0;0");
                    string floor = row.RawChanges[i] != row.AppliedChanges[i] ? $" [raw {row.RawChanges[i]:+0;-0;0}]" : "";
                    return $"{row.Prices[i]} ({delta}){floor}";
                }).ToArray();
                DrawRow(label, values[0], values[1], values[2]);
            }
            EditorGUILayout.EndScrollView();
        }

        private static void DrawRow(string round, string straw, string timber, string brick)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(round, GUILayout.Width(170));
            GUILayout.Label(straw, GUILayout.MinWidth(115));
            GUILayout.Label(timber, GUILayout.MinWidth(115));
            GUILayout.Label(brick, GUILayout.MinWidth(115));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawChart(Rect area)
        {
            Rect plot = new Rect(area.x + 45, area.y + 15, area.width - 65, area.height - 48);
            int max = Math.Max(simulation.HistoryPrices.Max(p => p.Max()), simulation.Rounds.Max(r => r.Prices.Max()));
            float ceiling = Mathf.Ceil((max + 2) / 10f) * 10;
            int historyCount = MatchPriceSimulation.HistoryRoundCount;
            Func<int, float> x = round => plot.x + plot.width * (round + historyCount) / (settings.RoundCount + historyCount);
            Func<int, float> y = price => plot.yMax - plot.height * price / ceiling;
            Handles.BeginGUI();
            EditorGUI.DrawRect(new Rect(x(-historyCount), plot.y, x(0) - x(-historyCount), plot.height), new Color(.35f, .55f, .8f, .12f));
            Handles.color = new Color(.6f, .6f, .6f, .8f);
            Handles.DrawLine(new Vector3(x(0), plot.y), new Vector3(x(0), plot.yMax));
            GUI.Label(new Rect(x(-historyCount) + 3, plot.y, x(0) - x(-historyCount), 20), "Past 5 changes");
            for (int price = 0; price <= ceiling; price += Math.Max(1, (int)ceiling / 5))
            {
                Handles.color = new Color(.5f, .5f, .5f, .25f);
                Handles.DrawLine(new Vector3(plot.x, y(price)), new Vector3(plot.xMax, y(price)));
                GUI.Label(new Rect(area.x, y(price) - 8, 42, 20), price.ToString());
            }
            foreach (int round in settings.AuctionRounds)
                EditorGUI.DrawRect(new Rect(x(round - 1), plot.y, x(round) - x(round - 1), plot.height), new Color(.5f, .5f, .5f, .1f));
            for (int company = 0; company < 3; company++)
            {
                var points = new Vector3[(historyCount + settings.RoundCount) * 2 + 1];
                int previous = simulation.HistoryPrices[0][company];
                points[0] = new Vector3(x(-historyCount), y(previous));
                for (int h = 1; h <= historyCount; h++)
                {
                    points[h * 2 - 1] = new Vector3(x(h - historyCount), y(previous));
                    previous = simulation.HistoryPrices[h][company];
                    points[h * 2] = new Vector3(x(h - historyCount), y(previous));
                }
                for (int r = 1; r <= settings.RoundCount; r++)
                {
                    points[(historyCount + r) * 2 - 1] = new Vector3(x(r), y(previous));
                    previous = simulation.Rounds[r - 1].Prices[company];
                    points[(historyCount + r) * 2] = new Vector3(x(r), y(previous));
                }
                Handles.color = Colors[company];
                Handles.DrawAAPolyLine(2.5f, points);
            }
            Handles.EndGUI();
            for (int r = -historyCount; r <= settings.RoundCount; r++)
            {
                if (area.width < 760 && r != 0 && r != -historyCount && r != settings.RoundCount && r % 2 != 0) continue;
                if (area.width < 760 && r == settings.RoundCount - 1) continue;
                GUI.Label(new Rect(x(r) - 12, plot.yMax + 3, 28, 20), r.ToString());
            }
            GUI.Label(new Rect(plot.x, plot.yMax + 22, plot.width, 20), "Close index (-5..0 = history, 0 = Start) | grey = auction round");
        }
    }
}
