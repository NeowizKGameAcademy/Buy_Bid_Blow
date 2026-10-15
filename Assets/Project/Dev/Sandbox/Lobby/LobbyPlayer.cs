using Mirror;
using UnityEngine;

namespace BuyBidBlow.LobbySandbox
{
    public sealed class LobbyPlayer : NetworkBehaviour
    {
        [SyncVar] public int seat;
        [SyncVar] public bool ready;
        [SyncVar] public int top;
        [SyncVar] public int bottom;
        [SyncVar] public int tie;
        [SyncVar] public string nickname;
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            if(NetworkManager.singleton is LobbyRoom room)room.NotifyLocalPlayerReady();
        }
        public static readonly Color[] Colors = {new Color(.6f,.12f,.2f), new Color(.1f,.5f,.7f), new Color(.8f,.6f,.12f), new Color(.2f,.65f,.35f), new Color(.55f,.3f,.7f), Color.black};
        BBBWolfAppearance appearance;
        public static Vector3 SeatPosition(int index)=>new Vector3((2.5f-index)*2.1f,.04f,-10.1f);
        void Awake()
        {
            appearance = GetComponentInChildren<BBBWolfAppearance>(true);
            foreach (var a in GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;
            var character=GetComponentInChildren<BBBWolfCharacter>(true);
            if(character) {character.StopAction();character.SetMovementSpeed(0);}
        }
        void LateUpdate()
        {
            transform.SetPositionAndRotation(SeatPosition(seat), Quaternion.Euler(0,180,0));
            transform.localScale=Vector3.one*1.5f;
            if (appearance) { appearance.SetOutfit(top,bottom); appearance.SetTieColor(Colors[Mathf.Clamp(tie,0,Colors.Length-1)]); }
        }
        [Command] public void SetReady(bool value) { ready = value; }
        [Command] public void Customize(int upper, int lower, int color)
        {
            if (upper < 0 || upper > 2 || lower < 0 || lower > 1 || color < 0 || color >= Colors.Length) return;
            if (top == upper && bottom == lower && tie == color) return;
            top = upper; bottom = lower; tie = color; ready = false;
        }
    }
}
