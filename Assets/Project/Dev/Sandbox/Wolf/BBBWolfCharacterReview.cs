using BuyBidBlow.Characters;
using UnityEngine;

// Sandbox only; excluded from player builds by BuyBidBlow.Dev.
public class BBBWolfCharacterReview : MonoBehaviour
{
    public BBBWolfCharacter character;
    public float yaw = 180;
    private int action;
    private bool paused;
    private float movement;
    private static readonly string[] Labels = {
        "Locomotion", "Walk (prototype)", "Raise arms", "Wave", "Sit", "Bid paddle",
        "Mixamo / Walking", "Mixamo / Defeat", "Mixamo / Hand Raising",
        "Mixamo / Flair", "Mixamo / Hip Hop Dancing"
    };

    private void OnGUI()
    {
        if (!character || !character.animator || !character.appearance) return;
        GUILayout.BeginArea(new Rect(16, 16, 280, 740), GUI.skin.box);
        GUILayout.Label("BUY, BID, BLOW / ANIMATOR");
        GUILayout.Label("Expression (independent layer)");
        int face = GUILayout.SelectionGrid((int)character.expression,
            new[] { "Happy", "Suspicious", "Shocked", "Blank bluff" }, 2);
        if (face != (int)character.expression) character.SetExpression((BBBWolfCharacter.Face)face);

        var appearance = character.appearance;
        GUILayout.Label("Outfit");
        int upper = GUILayout.SelectionGrid(appearance.Upper, new[] { "Suit", "Shirt", "Vest" }, 3);
        int lower = GUILayout.SelectionGrid(appearance.Lower, new[] { "Long", "Shorts" }, 2);
        if (upper != appearance.Upper || lower != appearance.Lower) appearance.SetOutfit(upper, lower);
        GUILayout.Label("Body animation");
        int next = GUILayout.SelectionGrid(action, Labels, 1);
        if (next != action)
        {
            action = next;
            character.PlayAction((BBBWolfCharacter.Action)action);
            character.SetPlaybackPaused(paused);
        }
        if (GUILayout.Button("Replay selected"))
        {
            character.PlayAction((BBBWolfCharacter.Action)action);
            character.SetPlaybackPaused(paused);
        }
        GUILayout.Label("Locomotion speed");
        float nextMovement = GUILayout.HorizontalSlider(movement, 0, 1);
        if (!Mathf.Approximately(nextMovement, movement))
        {
            movement = nextMovement;
            character.SetMovementSpeed(movement);
            character.StopAction();
            character.SetPlaybackPaused(paused);
            action = 0;
        }
        bool nextPaused = GUILayout.Toggle(paused, "Pause");
        if (nextPaused != paused) { paused = nextPaused; character.SetPlaybackPaused(paused); }
        if (paused)
        {
            float phase = Mathf.Repeat(character.animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 1);
            float nextPhase = GUILayout.HorizontalSlider(phase, 0, .999f);
            if (!Mathf.Approximately(nextPhase, phase)) character.Seek(nextPhase);
        }
        GUILayout.Label("Rotate");
        yaw = GUILayout.HorizontalSlider(yaw, 0, 360);
        if (character.model) character.model.localRotation = Quaternion.Euler(0, yaw, 0);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Red")) appearance.SetTieColor(new Color(.54f, .16f, .22f));
        if (GUILayout.Button("Blue")) appearance.SetTieColor(new Color(.13f, .32f, .65f));
        if (GUILayout.Button("Gold")) appearance.SetTieColor(new Color(.82f, .61f, .12f));
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
