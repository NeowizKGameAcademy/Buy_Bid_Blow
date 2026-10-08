using CueAudio;
using UnityEngine;
using UnityEngine.Events;

public sealed class CueUsageEventFixture : MonoBehaviour
{
    public CueReference sound;
    public UnityEvent onPlay = new UnityEvent();
}
