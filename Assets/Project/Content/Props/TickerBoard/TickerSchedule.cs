using System;
using System.Collections.Generic;
using System.Linq;
using BuyBidBlow.Systems.Round;
using UnityEngine;

namespace BuyBidBlow.UI.Ticker
{
    // TickerSchedule.json, as JsonUtility reads it. Layouts: split (a channel per board), mirror (one channel on
    // every board), span (one wide channel across the boards, each showing its slice).
    [Serializable] public sealed class TickerChannelData
    {
        public string id;
        // Timer, Overview or Company for the built-in screens; empty for a video channel.
        public string screen;
        public int company;
        // Company screen that rotates through the companies every companySeconds.
        public bool cycle;
        // Video id from the director's clip list. Wide videos are cut into one slice per board under span.
        public string video;
        public bool wide;
    }
    [Serializable] public sealed class TickerSegmentData
    {
        public string layout;
        public string channel;
        // split only: one per board; an empty entry leaves that board to lower rules and the playlist.
        public string[] channels;
        public float seconds;
    }
    [Serializable] public sealed class TickerRuleData
    {
        // A MatchTimeline span kind and which of its edges the window is measured from.
        public string span, edge;
        public float from, to;
        public int priority;
        // Ignore the first span of the kind (round 1 opening is not a round change).
        public bool skipFirst;
        // Either one segment for the whole window, or a sequence played from `from` on (its seconds set the length).
        public string layout, channel;
        public string[] channels;
        public TickerSegmentData[] sequence;
        public double Length => sequence != null && sequence.Length > 0 ? sequence.Sum(s => (double)s.seconds) : to - from;
    }
    [Serializable] public sealed class TickerScheduleData
    {
        // Board object names from the audience's left to right; split channels and span slices follow this order.
        // TickerBoard_Left is named from the board's side, so the audience sees it on the right.
        public string[] boards;
        public TickerChannelData[] channels;
        public float cutSeconds, companySeconds;
        public TickerSegmentData[] playlist;
        public TickerRuleData[] rules;
    }

    // What one board shows now. Since is the server time its segment began (video position, cut detection).
    public struct TickerShot
    {
        public TickerChannelData Channel;
        public int Company;
        // Slice index under span, -1 otherwise.
        public int Slot;
        public double Since;
        public bool IsSet => Channel != null;
    }

    // Resolves the schedule from the match timeline and NetworkTime only, so every client and every board agree.
    public sealed class TickerSchedule
    {
        public readonly TickerScheduleData Data;
        readonly Dictionary<string, TickerChannelData> channels = new Dictionary<string, TickerChannelData>(StringComparer.Ordinal);
        readonly List<(TickerRuleData data, TimelineSpanKind kind)> rules = new List<(TickerRuleData, TimelineSpanKind)>();
        static readonly string[] Screens = { "Timer", "Overview", "Company" };

        public TickerSchedule(TickerScheduleData data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            foreach (var channel in data.channels ?? Array.Empty<TickerChannelData>())
                if (!string.IsNullOrEmpty(channel.id)) channels[channel.id] = channel;
            foreach (var rule in data.rules ?? Array.Empty<TickerRuleData>())
                if (Enum.TryParse<TimelineSpanKind>(rule.span, out var kind)) rules.Add((rule, kind));
        }

        public static TickerSchedule FromJson(string json) => new TickerSchedule(JsonUtility.FromJson<TickerScheduleData>(json));
        public int BoardCount => Data.boards?.Length ?? 0;
        public int IndexOf(string board) => Data.boards == null ? -1 : Array.IndexOf(Data.boards, board);

        // Problems that would make the schedule silently wrong; empty when it is usable.
        public List<string> Validate(ICollection<string> videos = null)
        {
            var errors = new List<string>();
            if (BoardCount == 0) errors.Add("boards is empty");
            if (Data.companySeconds <= 0) errors.Add("companySeconds must be positive");
            if (Data.cutSeconds < 0) errors.Add("cutSeconds must not be negative");
            foreach (var c in Data.channels ?? Array.Empty<TickerChannelData>())
            {
                bool video = !string.IsNullOrEmpty(c.video);
                if (string.IsNullOrEmpty(c.id)) errors.Add("a channel has no id");
                else if (video == !string.IsNullOrEmpty(c.screen)) errors.Add($"channel {c.id}: set exactly one of screen or video");
                else if (!video && Array.IndexOf(Screens, c.screen) < 0) errors.Add($"channel {c.id}: unknown screen {c.screen}");
                else if (video && videos != null && !videos.Contains(c.video)) errors.Add($"channel {c.id}: no clip for video {c.video}");
                if (c.company < 0 || c.company > 2) errors.Add($"channel {c.id}: company must be 0-2");
            }
            if (Data.playlist == null || Data.playlist.Length == 0) errors.Add("playlist is empty");
            else foreach (var s in Data.playlist) CheckSegment(s, "playlist", true, errors);
            foreach (var r in Data.rules ?? Array.Empty<TickerRuleData>())
            {
                string where = $"rule {r.span}.{r.edge}{r.from:+0;-0}";
                if (!Enum.TryParse<TimelineSpanKind>(r.span, out _)) errors.Add(where + ": unknown span");
                if (r.edge != "start" && r.edge != "end") errors.Add(where + ": edge must be start or end");
                if (r.sequence != null && r.sequence.Length > 0) foreach (var s in r.sequence) CheckSegment(s, where, true, errors);
                else
                {
                    if (r.to <= r.from) errors.Add(where + ": to must be after from");
                    CheckSegment(new TickerSegmentData { layout = r.layout, channel = r.channel, channels = r.channels, seconds = 1 }, where, false, errors);
                }
            }
            return errors;
        }

        void CheckSegment(TickerSegmentData s, string where, bool timed, List<string> errors)
        {
            if (timed && s.seconds <= 0) errors.Add(where + ": seconds must be positive");
            if (s.layout == "split")
            {
                if (s.channels == null || s.channels.Length != BoardCount) errors.Add(where + $": split needs {BoardCount} channels");
                else foreach (var id in s.channels) if (!string.IsNullOrEmpty(id)) CheckChannel(id, where, errors);
            }
            else if (s.layout == "mirror" || s.layout == "span") CheckChannel(s.channel, where, errors);
            else errors.Add(where + $": unknown layout {s.layout}");
        }

        void CheckChannel(string id, string where, List<string> errors)
        {
            if (string.IsNullOrEmpty(id) || !channels.ContainsKey(id)) errors.Add(where + $": unknown channel {id}");
        }

        // Fills one shot per board. False when there is no schedule to follow (no match timeline yet).
        public bool Resolve(IReadOnlyList<TimelineSpan> spans, double now, TickerShot[] shots)
        {
            Array.Clear(shots, 0, shots.Length);
            if (spans == null || spans.Count == 0) return false;
            // Highest priority first; rules listed earlier win ties.
            var active = new List<(TickerRuleData rule, double opens)>();
            foreach (var (rule, kind) in rules)
                if (TryOpen(rule, kind, spans, now, out double opens)) active.Add((rule, opens));
            active = active.OrderByDescending(a => a.rule.priority).ToList();
            foreach (var (rule, opens) in active)
            {
                if (rule.sequence != null && rule.sequence.Length > 0)
                {
                    double start = opens;
                    foreach (var segment in rule.sequence)
                    {
                        if (now < start + segment.seconds) { Fill(shots, segment, start, now); break; }
                        start += segment.seconds;
                    }
                }
                else Fill(shots, new TickerSegmentData { layout = rule.layout, channel = rule.channel, channels = rule.channels }, opens, now);
            }
            double origin = spans.Min(s => s.StartsAt), cycle = Data.playlist.Sum(s => (double)s.seconds);
            double loops = Math.Floor((now - origin) / cycle), at = origin + loops * cycle;
            foreach (var segment in Data.playlist)
            {
                if (now < at + segment.seconds) { Fill(shots, segment, at, now); break; }
                at += segment.seconds;
            }
            return true;
        }

        // The rule's window holds now: returns when it opened. The latest qualifying span wins.
        static bool TryOpen(TickerRuleData rule, TimelineSpanKind kind, IReadOnlyList<TimelineSpan> spans, double now, out double opens)
        {
            opens = 0;
            bool found = false;
            double first = double.PositiveInfinity;
            if (rule.skipFirst) foreach (var s in spans) if (s.Kind == kind) first = Math.Min(first, s.StartsAt);
            foreach (var s in spans)
            {
                if (s.Kind != kind || s.StartsAt == first) continue;
                double anchor = rule.edge == "end" ? s.EndsAt : s.StartsAt, start = anchor + rule.from;
                if (now >= start && now < start + rule.Length && (!found || start > opens)) { opens = start; found = true; }
            }
            return found;
        }

        // Only boards still empty take the segment, so higher rules keep theirs.
        void Fill(TickerShot[] shots, TickerSegmentData segment, double since, double now)
        {
            for (int i = 0; i < shots.Length; i++)
            {
                if (shots[i].IsSet) continue;
                string id = segment.layout == "split" ? (segment.channels != null && i < segment.channels.Length ? segment.channels[i] : null) : segment.channel;
                if (string.IsNullOrEmpty(id) || !channels.TryGetValue(id, out var channel)) continue;
                int company = channel.cycle ? (int)Math.Floor((now - since) / Data.companySeconds) % 3 : channel.company;
                shots[i] = new TickerShot { Channel = channel, Company = company, Slot = segment.layout == "span" ? i : -1, Since = since };
            }
        }
    }
}
