using BuyBidBlow.Network;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.Characters
{
    public sealed class PlayerNet : NetworkBehaviour
    {
        [SyncVar] public int seat;
        [SyncVar] public bool ready;
        [SyncVar] public int top;
        [SyncVar] public int bottom;
        [SyncVar] public int tie;
        [SyncVar] public string nickname;
        // Set by the server when the match starts. In the level the owner plays through the local Player
        // and its position is synced (NetworkTransform, client to server); other clients see this avatar.
        [SyncVar(hook=nameof(OnInMatchChanged))] public bool inMatch;
        // The player object lives from connection through lobby and match, across scene loads.
        public override void OnStartServer(){base.OnStartServer();DontDestroyOnLoad(gameObject);}
        public override void OnStartClient(){base.OnStartClient();DontDestroyOnLoad(gameObject);RefreshAvatar();}
        void OnInMatchChanged(bool _,bool value)=>RefreshAvatar();
        // The owner already sees its own wolf through the local Player, so its avatar hides in the level.
        void RefreshAvatar(){bool visible=!inMatch||!isLocalPlayer;foreach(Transform child in transform)child.gameObject.SetActive(visible);}
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();RefreshAvatar();
            if(NetworkManager.singleton is GameNetworkManager room)room.NotifyLocalPlayerReady();
        }
        public static readonly Color[] Colors = {new Color(.6f,.12f,.2f), new Color(.1f,.5f,.7f), new Color(.8f,.6f,.12f), new Color(.2f,.65f,.35f), new Color(.55f,.3f,.7f), Color.black};
        BBBWolfAppearance appearance;
        BBBWolfCharacter character;
        PlayerMove localBody;
        Vector3 lastPosition;
        float walkSpeed;
        // The furnished hall uses +Z for the under-mezzanine matching carpet.
        public static readonly Vector3 WaitingCenter = new Vector3(0, .04f, 9.6f);
        public static Quaternion WaitingRotation => Quaternion.identity;
        public static Vector3 SeatPosition(int index)=>WaitingCenter+Vector3.right*((index-2.5f)*2.1f);
        void Awake()
        {
            appearance = GetComponentInChildren<BBBWolfAppearance>(true);
            foreach (var a in GetComponentsInChildren<Animator>(true)) a.applyRootMotion = false;
            character=GetComponentInChildren<BBBWolfCharacter>(true);
            if(character) {character.StopAction();character.SetMovementSpeed(0);}
        }
        void LateUpdate()
        {
            if(inMatch){FollowInMatch();return;}
            transform.SetPositionAndRotation(SeatPosition(seat), WaitingRotation);
            transform.localScale=Vector3.one*1.5f;
            if (appearance) { appearance.SetOutfit(top,bottom); appearance.SetTieColor(Colors[Mathf.Clamp(tie,0,Colors.Length-1)]); }
        }
        void FollowInMatch()
        {
            transform.localScale=Vector3.one;
            if(isLocalPlayer)
            {
                if(!localBody)localBody=FindFirstObjectByType<PlayerMove>();
                if(localBody)transform.SetPositionAndRotation(localBody.transform.position,Quaternion.Euler(0,localBody.transform.eulerAngles.y,0));
                return;
            }
            // Remote wolves walk at the speed their synced position moves.
            var step=transform.position-lastPosition;step.y=0;lastPosition=transform.position;
            walkSpeed=Mathf.Lerp(walkSpeed,Time.deltaTime>0?step.magnitude/Time.deltaTime:0,.2f);
            if(character)character.SetMovementSpeed(Mathf.Clamp01(walkSpeed/WalkSpeedForFullBlend));
            if (appearance) { appearance.SetOutfit(top,bottom); appearance.SetTieColor(Colors[Mathf.Clamp(tie,0,Colors.Length-1)]); }
        }
        const float WalkSpeedForFullBlend=4f;
        [Command] public void SetReady(bool value) { ready = value; }
        // Only honored for the room host; the server checks readiness and player count.
        [Command] public void CmdStartMatch() { if (NetworkManager.singleton is GameNetworkManager room) room.RequestStartMatch(this); }
        [Command] public void Customize(int upper, int lower, int color)
        {
            if (upper < 0 || upper > 2 || lower < 0 || lower > 1 || color < 0 || color >= Colors.Length) return;
            if (top == upper && bottom == lower && tie == color) return;
            top = upper; bottom = lower; tie = color; ready = false;
        }
    }
}
