using System;
using System.Linq;
using System.Collections.Generic;
using BuyBidBlow.Characters;
using BuyBidBlow.Network;
using BuyBidBlow.Systems.Room;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.LowLevel;

namespace BuyBidBlow.UI.Lobby
{
    public sealed class LobbyHud : MonoBehaviour
    {
        public Font font;
        public Camera view;
        public GameNetworkManager room;
        public LobbySpriteLibrary library;
        [SerializeField] ConnectionController session;
        [SerializeField] RectTransform canvasRect, selector, leftArrow, rightArrow;
        [SerializeField] RectTransform[] seats;
        [SerializeField] Text[] names, statuses, categories;
        [SerializeField] Image[] plates;
        [SerializeField] GameObject[] crowns, selfBadges, vacant;
        [SerializeField] Button ready, options, exit, previous, next;
        Button start;
        [SerializeField] Text readyText, count, notice, optionRows;
        [SerializeField] GameObject connection, optionPanel;
        [SerializeField] InputField address, nickname, port;
        [SerializeField] Text connectionStatus;
        public Transform selectionRing;
        static readonly Vector2 C=Vector2.one*.5f;
        static readonly Color Cream=new Color(1,.94f,.77f);
        static readonly string[] CategoryNames={"상의","하의","넥타이"};
        int category,focus,optionRow;
        int connectionFocus=-1;
        bool typing;
        bool composing;
        Keyboard imeKeyboard;
        void OnEnable(){imeKeyboard=Keyboard.current;if(imeKeyboard!=null)imeKeyboard.onIMECompositionChange+=CompositionChanged;}
        void OnDisable(){if(imeKeyboard!=null)imeKeyboard.onIMECompositionChange-=CompositionChanged;imeKeyboard=null;composing=false;heldKeys.Clear();}
        void CompositionChanged(IMECompositionString value){composing=value.ToString().Length>0;}
        Button pressed;
        float pressedUntil;
        public int SelectedCategory=>category;
        public bool OptionsOpen=>optionPanel.activeSelf;
        void Awake()
        {
            var preview=GameObject.Find("Lobby Art Preview - Editor Only");if(preview)Destroy(preview);
            if(!canvasRect)BuildLayout();
            if(!nickname||!port)UpgradeConnectionPanel();
            if(!session){session=room.GetComponent<ConnectionController>();if(!session)session=room.gameObject.AddComponent<ConnectionController>();}
            nickname.text=PlayerPrefs.GetString(Nickname.PreferenceKey,"");
            address.text=DirectConnectionSettings.LastAddress;port.text=DirectConnectionSettings.LastPort;
            if(!FindFirstObjectByType<EventSystem>()) {
                var go=new GameObject("Lobby UI EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));go.transform.SetParent(transform);
                var module=go.GetComponent<InputSystemUIInputModule>();module.AssignDefaultActions();module.move=null;module.submit=null;module.cancel=null;
            }
            RefreshCustomizationKeyHints();
            EnsureTextInput();
            ready.onClick.AddListener(ToggleReady);options.onClick.AddListener(ToggleOptions);exit.onClick.AddListener(Leave);
            CreateStartButton();
            HideHostButton();
            previous.onClick.AddListener(()=>ChangeVariant(-1));next.onClick.AddListener(()=>ChangeVariant(1));
            connection.transform.Find("Join").GetComponent<Button>().onClick.AddListener(Join);
            connection.transform.Find("Cancel").GetComponent<Button>().onClick.AddListener(()=>session.Cancel());
            optionPanel.SetActive(false);
        }
        PlayerNet Local()=>FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).FirstOrDefault(p=>p.isLocalPlayer);
        readonly HashSet<KeyCode> heldKeys=new HashSet<KeyCode>();
        void OnApplicationFocus(bool focused){if(!focused)heldKeys.Clear();}
        // Event input keeps keyboard-only navigation working in the Editor Game view as well.
        // Rendering remains entirely uGUI; this method draws no IMGUI controls.
        void OnGUI()
        {
            var e=Event.current;
            if(e.type==EventType.KeyUp){heldKeys.Remove(e.keyCode);return;}
            if(e.type!=EventType.KeyDown||e.control||e.alt||e.command||e.keyCode==KeyCode.None)return;
            if(!heldKeys.Add(e.keyCode))return;
            KeyCode key=e.keyCode;
            bool enter=key==KeyCode.Return||key==KeyCode.KeypadEnter;
            if(optionPanel.activeSelf) {
                if(key==KeyCode.Escape||key==KeyCode.O){ToggleOptions();e.Use();return;}
                if(key==KeyCode.UpArrow)optionRow=(optionRow+2)%3;
                if(key==KeyCode.DownArrow)optionRow=(optionRow+1)%3;
                int d=key==KeyCode.RightArrow?1:key==KeyCode.LeftArrow?-1:0;
                if(optionRow==0&&d!=0)AudioListener.volume=Mathf.Clamp01(AudioListener.volume+d*.05f);
                if(enter){if(optionRow==1)Screen.fullScreen=!Screen.fullScreen;else if(optionRow==2)ToggleOptions();}
                RefreshOptions();e.Use();return;
            }
            if(session.IsBusy) {if(key==KeyCode.Escape){session.Cancel();e.Use();}return;}
            if(typing||nickname.isFocused||address.isFocused||port.isFocused) {
                // Let the input field finish Korean IME composition before submitting.
                if(enter&&composing)return;
                if(key==KeyCode.Escape){StopTyping();e.Use();}
                else if(key==KeyCode.Tab){FocusConnection(((nickname.isFocused?0:address.isFocused?1:2)+(e.shift?4:1))%5);e.Use();}
                else if(enter){if(nickname.isFocused)FocusInput(address);else if(address.isFocused)FocusInput(port);else Join();e.Use();}return;
            }
            if(key==KeyCode.O){ToggleOptions();e.Use();return;}
            if(session.CanConnect) {
                if(key==KeyCode.J){FocusInput(address);e.Use();}
                if(key==KeyCode.N){FocusInput(nickname);e.Use();}
                if(key==KeyCode.Tab){FocusConnection((connectionFocus+(e.shift?4:1)+5)%5);e.Use();}
                if(enter){if(connectionFocus==4)Join();else FocusInput(nickname);e.Use();}return;
            }
            if(key==KeyCode.Escape){Leave();e.Use();return;}
            if(key==KeyCode.Tab){focus=(focus+1)%3;e.Use();return;}
            if(enter||key==KeyCode.Space){if(focus==0)ToggleReady();else if(focus==1)ToggleOptions();else Leave();e.Use();return;}
            if(key==KeyCode.W){SelectCategory(-1);e.Use();}
            if(key==KeyCode.S){SelectCategory(1);e.Use();}
            if(key==KeyCode.A){ChangeVariant(-1);e.Use();}
            if(key==KeyCode.D){ChangeVariant(1);e.Use();}
        }
        void RefreshCustomizationKeyHints()
        {
            for(int i=0;i<2;i++) {
                var cap=selector.Find("Key "+i); if(!cap)continue;
                var arrow=cap.Find("Arrow");if(arrow)arrow.gameObject.SetActive(false);
                var label=cap.Find("Key Label");
                if(label)label.GetComponent<Text>().text=i==0?"W":"S";
                else Text("Key Label",cap,i==0?"W":"S",17,new Vector2(24,24),Vector2.zero,C);
            }
            foreach(var button in new[]{previous,next}) {
                if(button.transform.Find("Key Hint"))continue;
                Text("Key Hint",button.transform,button==previous?"A":"D",18,new Vector2(40,24),new Vector2(0,-42),C);
            }
        }
        static void EnsureTextInput()
        {
            var eventSystem=EventSystem.current;if(!eventSystem)return;
            var textInput=eventSystem.GetComponent<LobbyTextInputBridge>();if(!textInput)textInput=eventSystem.gameObject.AddComponent<LobbyTextInputBridge>();
            foreach(var module in eventSystem.GetComponents<BaseInputModule>())if(module.inputOverride!=textInput)module.inputOverride=textInput;
        }
        void LateUpdate(){EnsureTextInput();Refresh(false);}
        public void SelectCategory(int d){category=(category+d+3)%3;focus=0;}
        public void ChangeVariant(int d)
        {
            var p=Local();if(!p||!NetworkClient.ready)return;
            p.Customize(category==0?(p.top+d+3)%3:p.top,category==1?(p.bottom+d+2)%2:p.bottom,category==2?(p.tie+d+6)%6:p.tie);
            Pulse(d<0?previous:next);focus=0;
        }
        // Host-only start button, cloned from the ready button and placed above it.
        void CreateStartButton()
        {
            start=Instantiate(ready,ready.transform.parent);start.name="Start";
            var rect=(RectTransform)start.transform;rect.anchoredPosition+=new Vector2(0,((RectTransform)ready.transform).rect.height+12);
            foreach(var label in start.GetComponentsInChildren<Text>(true))label.text="게임 시작";
            start.onClick.AddListener(StartMatch);start.gameObject.SetActive(false);
        }
        public void StartMatch(){var p=Local();if(p&&NetworkClient.ready){p.CmdStartMatch();Pulse(start);}}
        // Clients only join; the dedicated server is a separate console build.
        void HideHostButton()
        {
            connection.transform.Find("Host").gameObject.SetActive(false);
            var help=connection.transform.Find("Help").GetComponent<Text>();
            help.text=help.text.Replace(" · H: 방 만들기","").Replace("  ·  H: 방 만들기","");
        }
        public void ToggleReady(){var p=Local();if(p&&NetworkClient.ready){p.SetReady(!p.ready);Pulse(ready);}}
        public void ToggleOptions(){optionPanel.SetActive(!optionPanel.activeSelf);optionRow=0;RefreshOptions();}
        void RefreshOptions()
        {
            var rows=new[]{"음량  "+Mathf.RoundToInt(AudioListener.volume*100)+"%   ← →","전체화면  "+(Screen.fullScreen?"켜짐":"꺼짐")+"   Enter","닫기   Esc"};
            optionRows.text=string.Join("\n\n",rows.Select((r,i)=>(i==optionRow?"▶  ":"    ")+r));
        }
        void FocusInput(InputField field)
        {
            connectionFocus=field==nickname?0:field==address?1:2;
            nickname.DeactivateInputField();address.DeactivateInputField();port.DeactivateInputField();typing=true;
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(field.gameObject);
            field.ActivateInputField();
        }
        void FocusConnection(int index)
        {
            if(index<3){FocusInput(index==0?nickname:index==1?address:port);return;}
            if(index==3)index=4;
            StopTyping();connectionFocus=index;
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(connection.transform.Find(index==3?"Host":"Join").gameObject);
        }
        void StopTyping()
        {
            typing=false;nickname.DeactivateInputField();address.DeactivateInputField();port.DeactivateInputField();
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
        }
        public void Join(){if(session.Join(nickname.text,address.text,port.text))StopTyping();}
        public void Leave()=>session.Leave();
        void Pulse(Button b){pressed=b;pressedUntil=Time.unscaledTime+.15f;}
        Sprite S(string id)=>library.Find(id);
        void State(Button b,string id,bool enabled,bool selected)
        {
            b.interactable=enabled;b.image.sprite=S(id+"_"+(!enabled?"Disabled":b==pressed&&Time.unscaledTime<pressedUntil?"Pressed":selected?"Focused":"Normal"));
        }
        Vector2 Project(Vector3 world){var p=view.WorldToViewportPoint(world);return new Vector2((p.x-.5f)*canvasRect.rect.width,(p.y-.5f)*canvasRect.rect.height);}
        public void Refresh(bool preview)
        {
            if(!canvasRect||!view)return;
            view.fieldOfView=2*Mathf.Atan(Mathf.Tan(24*Mathf.Deg2Rad)*(16f/9f)/view.aspect)*Mathf.Rad2Deg;
            var players=preview?Array.Empty<PlayerNet>():FindObjectsByType<PlayerNet>(FindObjectsSortMode.None).Where(p=>p.isClient).ToArray();
            var local=players.FirstOrDefault(p=>p.isLocalPlayer);int localSeat=preview?0:local?local.seat:-1;
            int owner=players.Length>0?players.Min(p=>p.seat):-1;
            count.text=(preview?4:players.Length)+" / 6";
            for(int i=0;i<6;i++) {
                var p=players.FirstOrDefault(p=>p.seat==i);bool occupied=preview?i<4:p;bool done=preview?i==2:p&&p.ready;bool mine=i==localSeat;
                var head=Project(PlayerNet.SeatPosition(i)+Vector3.up*3f);
                seats[i].anchoredPosition=head+new Vector2(0,mine?136:24);
                names[i].text=occupied?(p&&!string.IsNullOrEmpty(p.nickname)?p.nickname:"WOLF "+(i+1).ToString("00")):"참가 대기";
                names[i].supportRichText=false;names[i].resizeTextForBestFit=true;names[i].resizeTextMinSize=13;names[i].resizeTextMaxSize=21;
                names[i].transform.parent.GetComponent<Image>().sprite=S(occupied?"08_Nameplate":"20_VacantNameplate");
                statuses[i].text=occupied?(done?"준비 완료":"꾸미는 중"):"";
                plates[i].transform.Find("Ready Check").gameObject.SetActive(done);
                plates[i].transform.Find("Editing Dots").gameObject.SetActive(!done);
                plates[i].gameObject.SetActive(occupied);plates[i].color=done?new Color(.15f,.8f,.28f):new Color(1,.7f,.12f);
                crowns[i].SetActive(occupied&&i==(preview?0:owner));selfBadges[i].SetActive(mine);
                vacant[i].SetActive(!occupied);vacant[i].GetComponent<RectTransform>().anchoredPosition=Project(PlayerNet.SeatPosition(i)+Vector3.up*1.45f);
            }
            selector.gameObject.SetActive(localSeat>=0);leftArrow.gameObject.SetActive(localSeat>=0);rightArrow.gameObject.SetActive(localSeat>=0);
            if(localSeat>=0) {
                selector.anchoredPosition=Project(PlayerNet.SeatPosition(localSeat)+Vector3.up*3f)+new Vector2(0,16);
                var chest=Project(PlayerNet.SeatPosition(localSeat)+Vector3.up*1.55f);
                leftArrow.anchoredPosition=chest+new Vector2(-104,0);rightArrow.anchoredPosition=chest+new Vector2(104,0);
                for(int i=0;i<3;i++){categories[i].text=CategoryNames[(category+i+2)%3];categories[i].color=i==1?new Color(1,.8f,.32f):new Color(Cream.r,Cream.g,Cream.b,.42f);}
            }
            if(selectionRing){selectionRing.gameObject.SetActive(localSeat>=0);if(localSeat>=0)selectionRing.position=PlayerNet.SeatPosition(localSeat)+Vector3.up*.02f;}
            bool allReady=players.Length>=room.MinPlayers&&players.All(p=>p.ready);
            bool roomHost=local&&local.seat==owner;
            if(start)start.gameObject.SetActive(!preview&&roomHost&&allReady);
            bool enabled=preview||local;
            readyText.text=local&&local.ready?"준비 취소":"준비 완료";
            State(ready,"24_ReadyButtonStates",enabled,focus==0);State(options,"04_SmallButtonStates",true,focus==1||optionPanel.activeSelf);
            State(exit,"04_SmallButtonStates",preview||(session&&session.State==ConnectionState.InRoom),focus==2);
            State(previous,"18_RoundButtonStates",enabled,false);State(next,"18_RoundButtonStates",enabled,false);
            bool showConnection=!preview&&(!session||session.State!=ConnectionState.InRoom)&&!optionPanel.activeSelf;
            connection.SetActive(showConnection);
            if(session) {
                bool canEdit=session.CanConnect;nickname.interactable=canEdit;address.interactable=canEdit;port.interactable=canEdit;
                connection.transform.Find("Host").GetComponent<Button>().interactable=canEdit;
                connection.transform.Find("Join").GetComponent<Button>().interactable=canEdit;
                var cancel=connection.transform.Find("Cancel");cancel.gameObject.SetActive(session.IsBusy);cancel.GetComponent<Button>().interactable=session.State!=ConnectionState.Stopping;
                connection.transform.Find("Help").gameObject.SetActive(!session.IsBusy);
                connectionStatus.text=session.State==ConnectionState.Idle?session.Message:session.State==ConnectionState.Failed?session.Message:session.IsBusy?session.Message:"";
            }
            notice.text=preview?"":session&&session.IsBusy?session.Message:allReady?(roomHost?"전원 준비 완료 · 게임 시작을 누르세요":"전원 준비 완료 · 방장의 시작을 기다리는 중"):session&&session.State==ConnectionState.InRoom?session.EndpointHint:"";
        }
        RectTransform Node(string name,Transform parent,Vector2 size,Vector2 pos,Vector2 anchor)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=C;r.sizeDelta=size;r.anchoredPosition=pos;return r;
        }
        Image Image(string name,Transform parent,string id,Vector2 size,Vector2 pos,Vector2 anchor,bool sliced=false)
        {
            var image=Node(name,parent,size,pos,anchor).gameObject.AddComponent<Image>();image.sprite=S(id);image.raycastTarget=false;
            image.type=sliced?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;
            if(sliced)image.pixelsPerUnitMultiplier=Mathf.Max(1,image.sprite.rect.height/size.y);return image;
        }
        Text Text(string name,Transform parent,string value,int pt,Vector2 size,Vector2 pos,Vector2 anchor)
        {
            var t=Node(name,parent,size,pos,anchor).gameObject.AddComponent<Text>();t.font=font;t.fontSize=pt;t.fontStyle=FontStyle.Bold;t.color=Cream;t.alignment=TextAnchor.MiddleCenter;t.text=value;t.raycastTarget=false;return t;
        }
        Button Button(string name,Transform parent,string id,string label,Vector2 size,Vector2 pos,Vector2 anchor)
        {
            var image=Image(name,parent,id+"_Normal",size,pos,anchor,id!="18_RoundButtonStates");image.raycastTarget=true;
            var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.transition=Selectable.Transition.SpriteSwap;b.navigation=new Navigation{mode=Navigation.Mode.None};
            b.spriteState=new SpriteState{highlightedSprite=S(id+"_Focused"),selectedSprite=S(id+"_Focused"),pressedSprite=S(id+"_Pressed"),disabledSprite=S(id+"_Disabled")};
            if(label!="")Text("Label",image.transform,label,22,size-Vector2.one*12,Vector2.zero,C);return b;
        }
        public void BuildLayout()
        {
            var old=transform.Find("Lobby HUD");if(old)DestroyImmediate(old.gameObject);
            var root=new GameObject("Lobby HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=0;
            canvasRect=root.GetComponent<RectTransform>();var tl=new Vector2(0,1);var tr=Vector2.one;
            var logo=Image("Logo",root.transform,"01_LogoPlate",new Vector2(450,120),new Vector2(273,-90),tl);
            Text("Game Title",logo.transform,"BUY, BID, BLOW",35,new Vector2(410,75),new Vector2(0,-8),C);
            Image("Gavel",logo.transform,"02_Gavel",new Vector2(40,40),new Vector2(0,43),C);
            var sub=Image("Subtitle",root.transform,"03_SubtitlePlate",new Vector2(280,44),new Vector2(273,-151),tl,true);
            Text("Label",sub.transform,"경매장 대기실",24,new Vector2(240,40),Vector2.zero,C);
            var counter=Image("Player Count",root.transform,"04_SmallButtonStates_Normal",new Vector2(132,58),new Vector2(-416,-69),tr,true);
            Image("Person",counter.transform,"05_Person",new Vector2(24,24),new Vector2(-39,0),C);count=Text("Count",counter.transform,"4 / 6",25,new Vector2(80,42),new Vector2(14,0),C);
            options=Button("Options",root.transform,"04_SmallButtonStates","옵션",new Vector2(130,58),new Vector2(-268,-69),tr);
            options.transform.Find("Label").GetComponent<RectTransform>().anchoredPosition=new Vector2(15,0);Image("Cog",options.transform,"06_Options",new Vector2(24,24),new Vector2(-36,0),C);
            exit=Button("Exit",root.transform,"04_SmallButtonStates","나가기",new Vector2(144,58),new Vector2(-115,-69),tr);
            exit.transform.Find("Label").GetComponent<RectTransform>().anchoredPosition=new Vector2(15,0);Image("Door",exit.transform,"07_Exit",new Vector2(26,26),new Vector2(-44,0),C);
            seats=new RectTransform[6];names=new Text[6];statuses=new Text[6];plates=new Image[6];crowns=new GameObject[6];selfBadges=new GameObject[6];vacant=new GameObject[6];
            for(int i=0;i<6;i++) {
                seats[i]=Node("Player "+(i+1),root.transform,new Vector2(190,100),Vector2.zero,C);
                var name=Image("Nameplate",seats[i],"08_Nameplate",new Vector2(176,40),Vector2.zero,C,true);names[i]=Text("Name",name.transform,"WOLF "+(i+1).ToString("00"),21,new Vector2(160,34),Vector2.zero,C);
                plates[i]=Image("Status",seats[i],"11_StatusBadge",new Vector2(150,34),new Vector2(0,-43),C,true);statuses[i]=Text("Status Text",plates[i].transform,"꾸미는 중",17,new Vector2(108,30),new Vector2(14,0),C);
                Image("Ready Check",plates[i].transform,"12_Check",new Vector2(17,17),new Vector2(-53,0),C);
                var dots=Node("Editing Dots",plates[i].transform,new Vector2(26,16),new Vector2(-53,0),C);
                for(int j=0;j<3;j++)Image("Dot "+j,dots,"13_StatusDot",new Vector2(5,5),new Vector2((j-1)*8,0),C);
                crowns[i]=Image("Host Crown",seats[i],"09_HostCrown",new Vector2(29,29),new Vector2(0,30),C).gameObject;
                var self=Image("Self Badge",seats[i],"10_LocalBadge",new Vector2(34,34),new Vector2(94,-22),C);selfBadges[i]=self.gameObject;Text("Self",self.transform,"나",17,new Vector2(26,26),Vector2.zero,C).color=new Color(.16f,.11f,.04f);
                var marker=Node("Vacant "+(i+1),root.transform,new Vector2(126,126),Vector2.zero,C);vacant[i]=marker.gameObject;Image("Dashed Ring",marker,"21_VacantCircle",new Vector2(126,126),Vector2.zero,C);Image("Plus",marker,"22_VacantPlus",new Vector2(30,30),Vector2.zero,C);
            }
            selector=Node("Keyboard Category Selector",root.transform,new Vector2(168,110),Vector2.zero,C);Image("Selected Row",selector,"14_CategoryHighlight",new Vector2(146,32),Vector2.zero,C,true);
            categories=new Text[3];for(int i=0;i<3;i++)categories[i]=Text("Category "+i,selector,CategoryNames[i],i==1?25:18,new Vector2(140,30),new Vector2(0,(1-i)*31),C);
            Image("Up",selector,"15_CategoryChevron",new Vector2(14,8),new Vector2(0,51),C);Image("Down",selector,"15_CategoryChevron",new Vector2(14,8),new Vector2(0,-51),C).transform.localRotation=Quaternion.Euler(0,0,180);
            for(int i=0;i<2;i++){var cap=Image("Key "+i,selector,"16_Keycap",new Vector2(24,24),new Vector2(95,14-i*28),C,true);Image("Arrow",cap.transform,"17_KeyboardArrow",new Vector2(10,12),Vector2.zero,C).transform.localRotation=Quaternion.Euler(0,0,i*180);}
            previous=Button("Previous Variant",root.transform,"18_RoundButtonStates","",new Vector2(54,54),Vector2.zero,C);leftArrow=previous.GetComponent<RectTransform>();
            next=Button("Next Variant",root.transform,"18_RoundButtonStates","",new Vector2(54,54),Vector2.zero,C);rightArrow=next.GetComponent<RectTransform>();
            Image("Chevron",previous.transform,"19_SideChevron",new Vector2(17,25),Vector2.zero,C);Image("Chevron",next.transform,"19_SideChevron",new Vector2(17,25),Vector2.zero,C).transform.localRotation=Quaternion.Euler(0,0,180);
            ready=Button("Ready",root.transform,"24_ReadyButtonStates","",new Vector2(350,92),new Vector2(-223,94),new Vector2(1,0));
            Image("Check",ready.transform,"12_Check",new Vector2(32,32),new Vector2(-113,0),C).color=new Color(.12f,.08f,.02f);readyText=Text("Ready Label",ready.transform,"준비 완료",36,new Vector2(245,66),new Vector2(31,0),C);readyText.color=new Color(.12f,.08f,.02f);
            notice=Text("Match Status",root.transform,"",18,new Vector2(650,38),new Vector2(0,60),new Vector2(.5f,0));
            var panel=Image("Connection",root.transform,"08_Nameplate",new Vector2(600,240),new Vector2(0,-180),C,true);connection=panel.gameObject;Text("Title",panel.transform,"대기실 접속",30,new Vector2(540,42),new Vector2(0,74),C);
            var field=Image("Host Address",panel.transform,"14_CategoryHighlight",new Vector2(360,42),new Vector2(0,20),C,true);field.raycastTarget=true;address=field.gameObject.AddComponent<InputField>();address.targetGraphic=field;address.textComponent=Text("Address",field.transform,"localhost",21,new Vector2(332,36),Vector2.zero,C);address.text="localhost";address.characterLimit=128;
            Button("Host",panel.transform,"04_SmallButtonStates","H  방 만들기",new Vector2(220,48),new Vector2(-125,-42),C);Button("Join",panel.transform,"04_SmallButtonStates","J  접속",new Vector2(220,48),new Vector2(125,-42),C);Text("Help",panel.transform,"J: 주소 입력 → Enter 접속   ·   H: 호스트",17,new Vector2(550,30),new Vector2(0,-89),C);
            var dialog=Image("Options Dialog",root.transform,"08_Nameplate",new Vector2(520,340),Vector2.zero,C,true);optionPanel=dialog.gameObject;Text("Title",dialog.transform,"옵션",32,new Vector2(460,50),new Vector2(0,112),C);optionRows=Text("Rows",dialog.transform,"",23,new Vector2(460,210),new Vector2(0,-20),C);RefreshOptions();optionPanel.SetActive(false);
            RefreshCustomizationKeyHints();
            UpgradeConnectionPanel();
            Canvas.ForceUpdateCanvases();Refresh(!Application.isPlaying);
        }
        public void UpgradeConnectionPanel()
        {
            if(!connection)return;
            connection.GetComponent<RectTransform>().sizeDelta=new Vector2(620,330);
            connection.transform.Find("Title").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,118);
            address.GetComponent<RectTransform>().anchoredPosition=new Vector2(66,-8);
            address.GetComponent<RectTransform>().sizeDelta=new Vector2(360,42);
            address.lineType=InputField.LineType.SingleLine;
            var label=connection.transform.Find("Address Label");
            if(!label)Text("Address Label",connection.transform,"접속 주소",20,new Vector2(110,42),new Vector2(-180,-8),C);
            var existing=connection.transform.Find("Nickname");
            if(existing)nickname=existing.GetComponent<InputField>();
            else {
                var image=Image("Nickname",connection.transform,"14_CategoryHighlight",new Vector2(360,42),new Vector2(66,52),C,true);image.raycastTarget=true;
                nickname=image.gameObject.AddComponent<InputField>();nickname.targetGraphic=image;
                nickname.textComponent=Text("Value",image.transform,"",22,new Vector2(332,36),Vector2.zero,C);nickname.textComponent.supportRichText=false;
                var hint=Text("Placeholder",image.transform,"닉네임 입력 (1~12글자)",19,new Vector2(332,36),Vector2.zero,C);hint.color=new Color(Cream.r,Cream.g,Cream.b,.5f);nickname.placeholder=hint;
                nickname.lineType=InputField.LineType.SingleLine;nickname.characterLimit=96;
            }
            if(!connection.transform.Find("Nickname Label"))Text("Nickname Label",connection.transform,"닉네임",20,new Vector2(110,42),new Vector2(-180,52),C);
            connection.transform.Find("Host").GetComponent<RectTransform>().anchoredPosition=new Vector2(-125,-69);
            connection.transform.Find("Join").GetComponent<RectTransform>().anchoredPosition=new Vector2(125,-69);
            var help=connection.transform.Find("Help").GetComponent<Text>();help.text="Tab: 항목 이동  ·  Enter: 선택  ·  H: 방 만들기  ·  J: 주소";help.fontSize=15;help.rectTransform.anchoredPosition=new Vector2(0,-118);
            var status=connection.transform.Find("Validation");connectionStatus=status?status.GetComponent<Text>():Text("Validation",connection.transform,"",16,new Vector2(570,30),new Vector2(0,-149),C);
            connectionStatus.color=new Color(1,.63f,.42f);
            nickname.navigation=new Navigation{mode=Navigation.Mode.None};address.navigation=new Navigation{mode=Navigation.Mode.None};
            UpgradeDirectConnectionPanel();
        }
        public void UpgradeDirectConnectionPanel()
        {
            if(!connection)return;
            connection.GetComponent<RectTransform>().sizeDelta=new Vector2(700,410);
            connection.transform.Find("Title").GetComponent<RectTransform>().anchoredPosition=new Vector2(0,156);
            nickname.GetComponent<RectTransform>().anchoredPosition=new Vector2(66,96);
            connection.transform.Find("Nickname Label").GetComponent<RectTransform>().anchoredPosition=new Vector2(-180,96);
            address.GetComponent<RectTransform>().anchoredPosition=new Vector2(66,36);
            connection.transform.Find("Address Label").GetComponent<RectTransform>().anchoredPosition=new Vector2(-180,36);
            var existing=connection.transform.Find("Port");
            if(existing)port=existing.GetComponent<InputField>();
            else {
                var image=Image("Port",connection.transform,"14_CategoryHighlight",new Vector2(360,42),new Vector2(66,-24),C,true);image.raycastTarget=true;
                port=image.gameObject.AddComponent<InputField>();port.targetGraphic=image;port.textComponent=Text("Value",image.transform,"7777",22,new Vector2(332,36),Vector2.zero,C);
                port.text="7777";port.contentType=InputField.ContentType.IntegerNumber;port.characterLimit=5;port.lineType=InputField.LineType.SingleLine;
            }
            if(!connection.transform.Find("Port Label"))Text("Port Label",connection.transform,"포트",20,new Vector2(110,42),new Vector2(-180,-24),C);
            connection.transform.Find("Host").GetComponent<RectTransform>().anchoredPosition=new Vector2(-125,-89);
            connection.transform.Find("Join").GetComponent<RectTransform>().anchoredPosition=new Vector2(125,-89);
            if(!connection.transform.Find("Cancel"))Button("Cancel",connection.transform,"04_SmallButtonStates","Esc  접속 취소",new Vector2(220,42),new Vector2(0,-142),C);
            connection.transform.Find("Cancel").gameObject.SetActive(false);
            var help=connection.transform.Find("Help").GetComponent<Text>();help.text="Tab: 항목 이동 · Enter: 선택 · H: 방 만들기 · J: 주소";help.rectTransform.anchoredPosition=new Vector2(0,-141);
            connectionStatus.rectTransform.anchoredPosition=new Vector2(0,-177);connectionStatus.rectTransform.sizeDelta=new Vector2(660,42);connectionStatus.fontSize=15;
            port.navigation=new Navigation{mode=Navigation.Mode.None};
        }
    }
}
