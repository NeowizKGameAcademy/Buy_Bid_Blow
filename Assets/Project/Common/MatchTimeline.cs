using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Round;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Common
{
    // When things happen in a match, in server time (NetworkTime.time). MatchNet carries what happened.
    // The server publishes the whole schedule at match start, so clients can act ahead of a moment
    // ("10 seconds before the close") and late joiners get it in full. Lives on the MatchNet prefab.
    public sealed class MatchTimeline : NetworkBehaviour
    {
        public static MatchTimeline Current { get; private set; }

        public readonly SyncList<TimelineSpan> spans = new SyncList<TimelineSpan>();

        // Raised on clients by the current timeline as NetworkTime passes a span's start or end; at the same
        // instant, ends come first. Static so listeners subscribe once in OnEnable whichever match is running.
        // Moments before this client spawned the timeline are not replayed.
        public static event Action<TimelineSpan> SpanStarted, SpanEnded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() { Current = null; SpanStarted = null; SpanEnded = null; }

        double passed;
        readonly List<(double at, bool start, TimelineSpan span)> due = new List<(double, bool, TimelineSpan)>();

        [Server]
        public void Publish(IEnumerable<TimelineSpan> schedule)
        {
            spans.Clear();
            foreach (var span in schedule) spans.Add(span);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            passed = NetworkTime.time;
            Current = this;
        }

        public override void OnStopClient()
        {
            if (Current == this) Current = null;
            base.OnStopClient();
        }

        public bool TryGetActive(TimelineSpanKind kind, double time, out TimelineSpan span)
        {
            foreach (var s in spans)
                if (s.Kind == kind && s.Contains(time)) { span = s; return true; }
            span = default;
            return false;
        }

        // The earliest span of this kind that has not started yet.
        public bool TryGetNext(TimelineSpanKind kind, double time, out TimelineSpan span)
        {
            span = default;
            bool found = false;
            foreach (var s in spans)
                if (s.Kind == kind && s.StartsAt > time && (!found || s.StartsAt < span.StartsAt)) { span = s; found = true; }
            return found;
        }

        // The span of this kind that ended most recently.
        public bool TryGetLast(TimelineSpanKind kind, double time, out TimelineSpan span)
        {
            span = default;
            bool found = false;
            foreach (var s in spans)
                if (s.Kind == kind && s.EndsAt <= time && (!found || s.EndsAt > span.EndsAt)) { span = s; found = true; }
            return found;
        }

        void Update()
        {
            if (!isClient || Current != this) return;
            // Client time is corrected in small steps; only move forward so no moment fires twice.
            double now = NetworkTime.time;
            if (now <= passed) return;
            due.Clear();
            foreach (var s in spans)
            {
                if (s.StartsAt > passed && s.StartsAt <= now) due.Add((s.StartsAt, true, s));
                if (s.EndsAt > passed && s.EndsAt <= now) due.Add((s.EndsAt, false, s));
            }
            passed = now;
            due.Sort((a, b) => a.at != b.at ? a.at.CompareTo(b.at) : a.start.CompareTo(b.start));
            foreach (var (_, start, span) in due)
                (start ? SpanStarted : SpanEnded)?.Invoke(span);
        }
    }
}
