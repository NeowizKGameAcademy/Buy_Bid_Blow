using System;
using BuyBidBlow.Characters;
using BuyBidBlow.Common;
using BuyBidBlow.Sandbox.StockGacha;
using BuyBidBlow.UI.Notebook;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
namespace BuyBidBlow.Sandbox.Labor
{
    // Geometry is deterministic on both the client and headless server. No network prefab required.
    public sealed class LaborRoom:MonoBehaviour
    {
        public static LaborRoom Current;
        public static LaborState State;
        public Vector3 doorPosition;
        public Vector3 hallExit;
        public Vector3 origin=new Vector3(100,0,0);
        public Font font;
        public const int BlocksPerCompany=5;
        public const int BombStart=3*BlocksPerCompany;
        public const int CargoCount=BombStart+2;
        public readonly Transform[] cargo=new Transform[CargoCount];
        readonly Transform[] cargoLabels=new Transform[CargoCount];
        public readonly Vector3[] spawns=new Vector3[CargoCount];
        public Vector3 Entry=>origin+new Vector3(0,.2f,-7);
        public Vector3 Bin(int company)=>origin+new Vector3((company-1)*5,.8f,7);
        Material[] materials;
        string notice;float noticeUntil;
        void Awake()
        {
            Current=this;State=default;
            materials=new[]{Mat(new Color(.85f,.65f,.2f)),Mat(new Color(.25f,.6f,.35f)),Mat(new Color(.75f,.25f,.2f)),Mat(new Color(.12f,.12f,.15f)),Mat(new Color(.055f,.16f,.14f))};
            Part("Workroom floor",origin+new Vector3(0,-.25f,0),new Vector3(20,.5f,20),4);
            Part("Back wall",origin+new Vector3(0,2.5f,10),new Vector3(20,5,.4f),4);
            Part("Left wall",origin+new Vector3(-10,2.5f,0),new Vector3(.4f,5,20),4);
            Part("Right wall",origin+new Vector3(10,2.5f,0),new Vector3(.4f,5,20),4);
            Part("Front wall",origin+new Vector3(0,2.5f,-10),new Vector3(20,5,.4f),4);
            var door=Part("Lobby door interaction",doorPosition,new Vector3(1.8f,3.5f,4.1f),4);
            door.GetComponent<Renderer>().enabled=false;
            Target(door,100,"작업장으로 이동");
            var back=Part("Return to exchange",origin+new Vector3(0,1.5f,-9),new Vector3(3,3,.4f),0);
            Target(back,101,"거래소로 돌아가기");Label(back.transform.position+Vector3.up*2,"EXCHANGE  [E]",true);
            string[] names={"STRAW","TIMBER","BRICK"};
            for(int c=0;c<3;c++)
            {
                var bin=Part(names[c]+" receiving bin",Bin(c),new Vector3(3,1.6f,2),c);
                bin.GetComponent<Renderer>().enabled=false;
                var pieces=new[]{new Vector3(0,-.7f,0),new Vector3(-1.4f,0,0),new Vector3(1.4f,0,0),new Vector3(0,0,.9f),new Vector3(0,0,-.9f)};
                var sizes=new[]{new Vector3(3,.2f,2),new Vector3(.2f,1.6f,2),new Vector3(.2f,1.6f,2),new Vector3(3,1.6f,.2f),new Vector3(3,1.6f,.2f)};
                for(int wall=0;wall<pieces.Length;wall++)Part("Bin wall",Bin(c)+pieces[wall],sizes[wall],c).transform.SetParent(bin.transform,true);
                Target(bin,200+c,new[]{"짚","목재","벽돌"}[c]+" 투입");Label(Bin(c)+Vector3.up*1.7f,names[c]+"\nDELIVER [E]");
            }
            for(int i=0;i<CargoCount;i++)
            {
                int c=i/BlocksPerCompany;bool bomb=i>=BombStart;
                spawns[i]=origin+(bomb?new Vector3(5+(i-BombStart)*2,.5f,-4):new Vector3((i%BlocksPerCompany-2)*1.4f-2,.5f,-5+c*2));
                var item=Part(bomb?"Bomb "+i:names[c]+" cargo "+i,spawns[i],Vector3.one*.65f,bomb?3:c,bomb?PrimitiveType.Sphere:PrimitiveType.Cube);
                Target(item,i,bomb?"폭탄 들기 (무급 / -$3)":new[]{"짚","목재","벽돌"}[c]+" 블록 들기");cargo[i]=item.transform;
                if(bomb)Part("Bomb fuse",spawns[i]+Vector3.up*.4f,new Vector3(.08f,.25f,.08f),0).transform.SetParent(item.transform,true);
                cargoLabels[i]=Label(spawns[i]+Vector3.up*.65f,bomb?"BOMB -3":names[c]);
            }
            Label(origin+new Vector3(0,3,8),"COMPANY WORKSHOP\nBLOCK +1 / BOMB -3");
            for(int x=-1;x<=1;x++)
            {
                var lamp=new GameObject("Workshop light");lamp.transform.SetParent(transform);lamp.transform.position=origin+new Vector3(x*6,4,0);
                var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.range=18;light.intensity=4;
            }
        }
        Material Mat(Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"));m.color=color;return m;}
        GameObject Part(string name,Vector3 position,Vector3 scale,int color,PrimitiveType type=PrimitiveType.Cube)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(transform);go.transform.position=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=materials[color];return go;
        }
        void Target(GameObject go,int id,string label){var target=go.AddComponent<LaborTarget>();target.id=id;target.label=label;}
        Transform Label(Vector3 position,string text,bool reverse=false)
        {
            var go=new GameObject(text);go.transform.SetParent(transform);go.transform.position=position;
            go.transform.rotation=reverse?Quaternion.Euler(0,180,0):Quaternion.identity;
            var mesh=go.AddComponent<TextMesh>();mesh.text=text;mesh.fontSize=40;mesh.characterSize=.035f;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.color=new Color(1,.85f,.45f);
            return go.transform;
        }
        public void Receive(LaborState state)
        {
            State=state;if(!string.IsNullOrEmpty(state.Message)){notice=state.Message;noticeUntil=Time.unscaledTime+4;}
            if(state.Move&&NetworkClient.localPlayer)
            {
                var player=NetworkClient.localPlayer;player.transform.position=state.Teleport;
                player.transform.rotation=Vector3.Distance(state.Teleport,origin)<25?Quaternion.identity:Quaternion.LookRotation(new Vector3(-state.Teleport.x,0,-state.Teleport.z));
                if(player.TryGetComponent<Rigidbody>(out var body)){body.position=state.Teleport;body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
                Physics.SyncTransforms();
            }
        }
        void Update()
        {
            var local=NetworkClient.localPlayer;
            if(local&&Keyboard.current!=null&&Keyboard.current.gKey.wasPressedThisFrame&&!NetworkNotebookController.BlocksInput&&
                !(FindFirstObjectByType<StockGachaSandboxPanel>()?.IsOpen??false))
                NetworkClient.Send(new LaborRequest{Session=State.Session,Target=300});
            for(int i=0;i<cargo.Length;i++)
            {
                if(!cargo[i])continue;
                uint owner=State.Owners!=null&&i<State.Owners.Length?State.Owners[i]:0;
                bool held=owner!=0;bool available=State.Available!=null&&State.Available[i];
                cargo[i].gameObject.SetActive(held||available);
                if(cargoLabels[i])cargoLabels[i].gameObject.SetActive(available&&!held);
                foreach(var collider in cargo[i].GetComponentsInChildren<Collider>(true))collider.enabled=!held&&available;
                if(held&&NetworkClient.spawned.TryGetValue(owner,out var identity))
                {
                    var camera=identity.isLocalPlayer?identity.GetComponentInChildren<Camera>():null;
                    var anchor=camera?camera.transform:identity.transform;
                    cargo[i].position=anchor.position+anchor.forward*(camera ? .9f : .8f)+(camera?Vector3.down*.4f:Vector3.up*1.1f);
                    cargo[i].rotation=anchor.rotation;
                }
                else{cargo[i].position=spawns[i];cargo[i].rotation=Quaternion.identity;}
            }
        }
        void OnGUI()
        {
            if(!NetworkClient.localPlayer||!MatchNet.Current||MatchNet.Current.phase!=MatchNet.Phase.Playing)return;
            if(Vector3.Distance(NetworkClient.localPlayer.transform.position,origin)>25)return;
            var style=new GUIStyle(GUI.skin.box){font=font,fontSize=17,alignment=TextAnchor.UpperLeft,wordWrap=true};
            string changes=State.Adjustments==null?"":$"짚 {State.Adjustments[0]:+0;-0;0} / 목재 {State.Adjustments[1]:+0;-0;0} / 벽돌 {State.Adjustments[2]:+0;-0;0}";
            GUI.Box(new Rect(20,Screen.height-200,500,180),$"회사 작업장 · E 들기/투입 · G 반납\n급여: 현재 주가 20% · 이번 라운드 {State.Paid}/5회\n폭탄: 무급 / -$3 · {State.Bombs}/1회\n주가 보정 (이번 라운드 / 최대 ±$5): {changes}\n{(Time.unscaledTime<noticeUntil?notice:"블록은 같은 회사 통에 투입 · 소모 후 다음 라운드에 재배치")}",style);
        }
        void OnDestroy(){if(Current==this)Current=null;if(materials!=null)foreach(var m in materials)Destroy(m);}
    }
}
