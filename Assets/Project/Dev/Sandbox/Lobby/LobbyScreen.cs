using System.Linq;
using Mirror;
using UnityEngine;

namespace BuyBidBlow.LobbySandbox
{
    public sealed class LobbyScreen : MonoBehaviour
    {
        public Font font;
        public Camera view;
        public LobbyRoom room;
        string address = "localhost";
        GUIStyle title, text, badge;
        void OnGUI()
        {
            if (title == null) {
                title = new GUIStyle(GUI.skin.label) {font=font,fontSize=32,fontStyle=FontStyle.Bold};
                text = new GUIStyle(GUI.skin.label) {font=font,fontSize=18,wordWrap=true};
                badge = new GUIStyle(text) {alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            }
            GUI.skin.font=font;
            GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            var players=FindObjectsByType<LobbyPlayer>(FindObjectsSortMode.None).Where(p=>p.isClient).OrderBy(p=>p.seat).ToArray();
            GUI.Box(new Rect(20,20,1240,90),"");
            GUI.Label(new Rect(44,30,900,42),"BUY, BID, BLOW  /  대기실",title);
            GUI.Label(new Rect(46,76,1150,28),$"참가자 {players.Length} / 6     최소 3명  ·  외형을 선택하고 준비를 완료하세요",text);
            for(int i=0;i<6;i++) {
                var p=players.FirstOrDefault(x=>x.seat==i);
                var pos=view.WorldToViewportPoint(new Vector3((i-2.5f)*2.1f,2.6f,-10.1f));
                GUI.Box(new Rect(pos.x*1280-80,(1-pos.y)*720-16,160,54),"");
                GUI.Label(new Rect(pos.x*1280-80,(1-pos.y)*720-16,160,54),p ? $"PLAYER {i+1}"+(p.isLocalPlayer?" (나)":"")+"\n"+(p.ready?"준비 완료":"외형 선택 중") : "빈 자리\n참가 대기",badge);
            }
            GUI.Box(new Rect(20,556,1240,144),"");
            if (!NetworkClient.active && !NetworkServer.active) {
                GUI.Label(new Rect(42,572,900,28),"대기실 접속 테스트 · 호스트를 만들거나 호스트 IP로 접속하세요",text);
                address=GUI.TextField(new Rect(42,615,250,36),address,128);
                if(GUI.Button(new Rect(308,615,180,36),"방 만들기 (Host)")) room.StartHost();
                if(GUI.Button(new Rect(504,615,180,36),"접속 (Join)")) {room.networkAddress=address;room.StartClient();}
            } else {
                var local=players.FirstOrDefault(p=>p.isLocalPlayer);
                if(local) {
                    GUI.Label(new Rect(42,572,1000,28),"커스터마이징  ·  변경하면 준비가 해제됩니다",text);
                    if(GUI.Button(new Rect(42,615,170,38),$"상의 {local.top+1}  ›")) local.Customize((local.top+1)%3,local.bottom,local.tie);
                    if(GUI.Button(new Rect(224,615,170,38),$"하의 {local.bottom+1}  ›")) local.Customize(local.top,(local.bottom+1)%2,local.tie);
                    if(GUI.Button(new Rect(406,615,170,38),$"넥타이 {local.tie+1}  ›")) local.Customize(local.top,local.bottom,(local.tie+1)%6);
                    if(GUI.Button(new Rect(594,615,190,38),local.ready?"준비 취소":"준비 완료")) local.SetReady(!local.ready);
                    GUI.Label(new Rect(800,606,270,62),players.Length>=3 && players.All(p=>p.ready)?"전원 준비 완료\n매치 전환 연결 예정":"다른 참가자 준비 대기",text);
                } else GUI.Label(new Rect(42,600,900,45),NetworkClient.isConnected?"플레이어 생성 중…":"호스트에 접속 중…",text);
                if(GUI.Button(new Rect(1090,615,146,38),"나가기")) {if(NetworkServer.active)room.StopHost();else room.StopClient();}
            }
            GUI.matrix=Matrix4x4.identity;
        }
    }
}
