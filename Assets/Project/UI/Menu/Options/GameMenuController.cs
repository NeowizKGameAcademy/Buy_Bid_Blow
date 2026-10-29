using System.Collections;
using BuyBidBlow.Characters;
using BuyBidBlow.Network;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Menu
{
    [DefaultExecutionOrder(10000)]
    public sealed class GameMenuController : MonoBehaviour
    {
        public GameObject shade,menuPanel,settingsPanel,leavePanel,controlsPanel;
        public Button launcher,resume,settings,controls,leave,apply,defaults,closeSettings,confirmLeave,cancelLeave,closeControls;
        public Slider[] sliders;public Text[] values;public Button display;public Text displayValue,status;
        public Image[] settingHighlights;
        public Button displayPrevious,displayNext;
        static GameMenuController instance;
        public static bool BlocksInput=>instance&&instance.shade.activeSelf;
        public bool IsSettingsOpen=>settingsPanel.activeSelf;
        SettingsValues draft;int row,openedFrame;bool directSettings,leaving;
        public static GameMenuController Ensure()
        {
            if(!Application.isPlaying)return null;
            if(instance)return instance;
            instance=FindFirstObjectByType<GameMenuController>();if(instance)return instance;
            var prefab=Resources.Load<GameObject>("BBB_GameMenu");
            return prefab?Instantiate(prefab).GetComponent<GameMenuController>():null;
        }
        void Awake()
        {
            if(instance&&instance!=this){Destroy(gameObject);return;}instance=this;
            GameSettings.Load();
            launcher.onClick.AddListener(OpenMenu);resume.onClick.AddListener(Close);
            settings.onClick.AddListener(()=>OpenSettings(false));controls.onClick.AddListener(()=>Show(controlsPanel));leave.onClick.AddListener(RequestLeave);
            closeControls.onClick.AddListener(OpenMenu);cancelLeave.onClick.AddListener(OpenMenu);confirmLeave.onClick.AddListener(()=>StartCoroutine(LeaveToMain()));
            defaults.onClick.AddListener(()=>{draft=SettingsValues.Defaults;RenderSettings();});
            apply.onClick.AddListener(()=>{GameSettings.Apply(draft);status.text="설정을 저장했습니다.";});
            closeSettings.onClick.AddListener(()=>{if(directSettings)Close();else OpenMenu();});
            display.onClick.AddListener(()=>Adjust(4,1));
            if(displayPrevious)displayPrevious.onClick.AddListener(()=>{row=4;Adjust(4,-1);});
            if(displayNext)displayNext.onClick.AddListener(()=>{row=4;Adjust(4,1);});
            for(int i=0;i<sliders.Length;i++){int index=i;sliders[i].onValueChanged.AddListener(v=>{SetValue(index,Mathf.RoundToInt(v));RenderValues();});}
            for(int i=0;i<5;i++){int index=i;var trigger=(i<4?sliders[i].gameObject:display.gameObject).AddComponent<EventTrigger>();var entry=new EventTrigger.Entry{eventID=EventTriggerType.PointerDown};entry.callback.AddListener(_=>{row=index;RenderValues();});trigger.triggers.Add(entry);}
            Close();
        }
        void OnDestroy(){if(instance==this)instance=null;}
        public void OpenSettings(bool direct=true)
        {Prepare();directSettings=direct;draft=GameSettings.Current;row=0;status.text="W/S 항목 · A/D 조절 · E 실행 · Esc 닫기";Show(settingsPanel);if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);RenderSettings();}
        public void OpenMenu(){Prepare();directSettings=false;row=0;Show(menuPanel);Select(resume);}
        void Prepare()
        {
            // The local body (scene Player) stops by itself while BlocksInput (PlayerInputReader).
            var p=FindFirstObjectByType<PlayerInputReader>();if(p){p.GetComponentInChildren<BuyBidBlow.UI.HUD.RadialMenuController>(true)?.EndSelection(false);p.GetComponentInChildren<BuyBidBlow.UI.Market.TradePanelController>(true)?.Close();p.GetComponentInChildren<BuyBidBlow.UI.Notebook.NetworkNotebookController>(true)?.Close();}
            openedFrame=Time.frameCount;
        }
        void Show(GameObject panel){shade.SetActive(true);foreach(var go in new[]{menuPanel,settingsPanel,leavePanel,controlsPanel})go.SetActive(go==panel);openedFrame=Time.frameCount;}
        public void Close()
        {
            shade.SetActive(false);bool play=NetworkClient.localPlayer&&NetworkClient.localPlayer.GetComponent<PlayerNet>().inMatch&&!BuyBidBlow.UI.HUD.NetworkMatchFeedback.BlocksInput;
            Cursor.lockState=play?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!play;
            var menu=FindFirstObjectByType<MainMenuActions>();if(menu&&EventSystem.current)Select(menu.settingsButton);
        }
        public void RequestLeave(){Show(leavePanel);row=0;Select(cancelLeave);}
        public IEnumerator LeaveToMain()
        {
            if(leaving)yield break;leaving=true;
            if(NetworkManager.singleton is GameNetworkManager room){if(NetworkServer.active)room.StopHost();else if(NetworkClient.active)room.StopClient();}
            float deadline=Time.realtimeSinceStartup+5;
            while(NetworkClient.active&&Time.realtimeSinceStartup<deadline)yield return null;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;Time.timeScale=1;
            // The manager is DontDestroyOnLoad; drop it so the next hall load keeps the scene's own manager.
            if(SceneManager.GetActiveScene().name!="SC_MainMenu"){if(NetworkManager.singleton)Destroy(NetworkManager.singleton.gameObject);SceneManager.LoadScene("SC_MainMenu");}else Close();
            leaving=false;
        }
        void LateUpdate()
        {
            bool inGame=NetworkClient.localPlayer&&NetworkClient.localPlayer.GetComponent<PlayerNet>().inMatch;
            launcher.gameObject.SetActive(inGame);
            if(!shade.activeSelf)return;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        }
        void Update()
        {
            var key=Keyboard.current;if(key==null||openedFrame==Time.frameCount)return;
            if(!shade.activeSelf){if(key.escapeKey.wasPressedThisFrame&&NetworkClient.localPlayer)OpenMenu();return;}
            if(key.escapeKey.wasPressedThisFrame){if(settingsPanel.activeSelf&&!directSettings||controlsPanel.activeSelf||leavePanel.activeSelf)OpenMenu();else Close();return;}
            int delta=key.sKey.wasPressedThisFrame?1:key.wKey.wasPressedThisFrame?-1:0;
            if(settingsPanel.activeSelf){row=(row+delta+8)%8;if(delta!=0){RenderSettings();if(row>=5)Select(new[]{defaults,apply,closeSettings}[row-5]);else if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);}if(delta!=0)status.text=row<5?new[]{"전체 음량","배경음","효과음","마우스 감도","화면 모드"}[row]+" 선택 · A/D 조절":"E로 실행";
                if(key.aKey.wasPressedThisFrame)Adjust(row,-1);if(key.dKey.wasPressedThisFrame)Adjust(row,1);
                if(key.eKey.wasPressedThisFrame){if(row==4)Adjust(4,1);else if(row==5)defaults.onClick.Invoke();else if(row==6)apply.onClick.Invoke();else if(row==7)closeSettings.onClick.Invoke();}
            }else{var buttons=menuPanel.activeSelf?new[]{resume,settings,controls,leave}:leavePanel.activeSelf?new[]{cancelLeave,confirmLeave}:new[]{closeControls};row=(row+delta+buttons.Length)%buttons.Length;if(delta!=0)Select(buttons[row]);if(key.eKey.wasPressedThisFrame)buttons[row].onClick.Invoke();}
        }
        static void Select(Button button){if(EventSystem.current)EventSystem.current.SetSelectedGameObject(button.gameObject);}
        void SetValue(int index,int value){switch(index){case 0:draft.Master=value;break;case 1:draft.Music=value;break;case 2:draft.Effects=value;break;case 3:draft.Sensitivity=value;break;}}
        void Adjust(int index,int delta){if(index<4&&index>=0){int[] v={draft.Master,draft.Music,draft.Effects,draft.Sensitivity};SetValue(index,Mathf.Clamp(v[index]+delta*5,index==3?1:0,100));}else if(index==4)draft.DisplayMode=(draft.DisplayMode+delta+3)%3;RenderSettings();}
        void RenderSettings(){int[] v={draft.Master,draft.Music,draft.Effects,draft.Sensitivity};for(int i=0;i<4;i++)sliders[i].SetValueWithoutNotify(v[i]);RenderValues();}
        void RenderValues(){int[] v={draft.Master,draft.Music,draft.Effects,draft.Sensitivity};for(int i=0;i<4;i++){values[i].text=v[i]+(i<3?"%":"");values[i].color=i==row?new Color(1,.8f,.35f):MenuWidgets.Cream;}if(settingHighlights!=null)for(int i=0;i<settingHighlights.Length;i++)if(settingHighlights[i])settingHighlights[i].enabled=i==row;displayValue.text=new[]{"창 모드","전체 화면","테두리 없는 창"}[draft.DisplayMode];}
    }
}
