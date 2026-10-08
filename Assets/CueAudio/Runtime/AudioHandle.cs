namespace CueAudio
{
    public readonly struct AudioHandle
    {
        private readonly CueAudioEngine engine;
        private readonly int slot;
        private readonly ulong generation;
        internal AudioHandle(CueAudioEngine engine, int slot, ulong generation)
        { this.engine = engine; this.slot = slot; this.generation = generation; }

        public bool IsPlaying => engine != null && engine.IsActive(slot, generation);
        public void Stop(float fadeOutSeconds = 0) => engine?.Stop(slot, generation, fadeOutSeconds);
    }
}
