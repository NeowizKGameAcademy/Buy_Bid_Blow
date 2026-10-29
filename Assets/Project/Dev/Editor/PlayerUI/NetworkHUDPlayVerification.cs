using System;
using System.IO;
using System.Reflection;
using BuyBidBlow.Common;
using BuyBidBlow.UI.HUD;
using BuyBidBlow.UI.Lobby;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    [InitializeOnLoad]
    public static class NetworkHUDPlayVerification
    {
        const string Key = "NetworkHUDPlayVerification";
        static double started;
        static int inputStep;
        static double lastInput;
        static UnityEngine.InputSystem.Keyboard keyboard;
        static NetworkHUDPlayVerification() { EditorApplication.update += Tick; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Project/Scenes/SC_ExchangeFurnished.unity");
            SessionState.SetBool(Key,true);
            EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            try
            {
                if (started == 0)
                {
                    // Headless editor has no focused Game View. Route virtual input to
                    // the player update without changing the project's saved settings.
                    var inputSettings=Object.Instantiate(UnityEngine.InputSystem.InputSystem.settings);
                    inputSettings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
                    inputSettings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    UnityEngine.InputSystem.InputSystem.settings=inputSettings;
                    var session = Object.FindFirstObjectByType<HallSession>();
                    if (!session) throw new Exception("Missing hall session");
                    session.enabled = false; session.lobbyPresentation.SetActive(false); session.gameplayPresentation.SetActive(true);
                    var state = new GameObject("HUD play fixture",typeof(Mirror.NetworkIdentity),typeof(MatchNet));
                    var match = state.GetComponent<MatchNet>(); match.totalRounds = 17; match.round = 1; match.phase = MatchNet.Phase.Playing;
                    match.roundClosesAt = Mirror.NetworkTime.time + 60;
                    typeof(MatchNet).GetProperty("Current",BindingFlags.Public|BindingFlags.Static).SetValue(null,match);
                    typeof(BuyBidBlow.Characters.PlayerNet).GetProperty("LocalPortfolio").SetValue(null,new BuyBidBlow.Systems.Portfolio.PortfolioSnapshot(200,0,new System.Collections.Generic.Dictionary<BuyBidBlow.Systems.Types.Company,int>()));
                    var hud = Object.FindFirstObjectByType<NetworkPlayerHUD>();
                    if (!hud) throw new Exception("Production HUD is not active");
                    hud.Refresh(); Canvas.ForceUpdateCanvases();
                    if (hud.GetComponent<RectTransform>().lossyScale.x <= 0 || !hud.roundStart.gameObject.activeInHierarchy)
                        throw new Exception("Canvas scale or round notice invalid in Play");
                    started = EditorApplication.timeSinceStartup;
                    return;
                }
                if (EditorApplication.timeSinceStartup-started < 4) return;
                var view = Object.FindFirstObjectByType<NetworkPlayerHUD>();
                if (!view || view.roundStart.gameObject.activeSelf || view.timeLabel.text == "--:--")
                    throw new Exception("Runtime timer or timed notice failed");
                var notebook=Object.FindFirstObjectByType<BuyBidBlow.UI.Notebook.NetworkNotebookController>();
                if(!notebook) throw new Exception("Production notebook controller missing");
                if(EditorApplication.timeSinceStartup-lastInput < .25) return;
                if(inputStep == 0)
                { keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();Press(UnityEngine.InputSystem.Key.Tab);inputStep++;return; }
                if(inputStep == 1)
                { if(!notebook.view.IsOpen || !BuyBidBlow.UI.Notebook.NetworkNotebookController.BlocksInput)throw new Exception($"Tab failed to open notebook: view={notebook.view.IsOpen}, match={MatchNet.Current != null}, portfolio={BuyBidBlow.Characters.PlayerNet.LocalPortfolio != null}, trading={notebook.trading && notebook.trading.IsOpen}, key={keyboard.tabKey.isPressed}, enabled={keyboard.enabled}, current={UnityEngine.InputSystem.Keyboard.current == keyboard}, controller={notebook.isActiveAndEnabled}, focused={Application.isFocused}");Release();inputStep++;return; }
                if(inputStep == 2){Press(UnityEngine.InputSystem.Key.Tab);inputStep++;return;}
                if(inputStep == 3){if(notebook.view.IsOpen)throw new Exception("Tab failed to close notebook");Release();inputStep++;return;}
                if(inputStep == 4){Press(UnityEngine.InputSystem.Key.Tab);inputStep++;return;}
                if(inputStep == 5){if(!notebook.view.IsOpen)throw new Exception("Tab failed to reopen notebook");Release();inputStep++;return;}
                if(inputStep == 6){Press(UnityEngine.InputSystem.Key.Escape);inputStep++;return;}
                if(notebook.view.IsOpen)throw new Exception("Escape failed to close notebook");
                File.WriteAllText("Library/NetworkHUDPlayVerification.result","PASS: production hall activates HUD; Canvas positive scale; network timer; round notice auto close; Tab opens/closes/reopens notebook; gameplay input blocked; Esc closes. Injected network state, no multiplayer transport.");
                Finish(0);
            }
            catch(Exception e) { File.WriteAllText("Library/NetworkHUDPlayVerification.result",e.ToString()); Debug.LogException(e); Finish(1); }
        }
        static void Finish(int code) { SessionState.SetBool(Key,false); EditorApplication.Exit(code); }
        static void Press(UnityEngine.InputSystem.Key key){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(key));lastInput=EditorApplication.timeSinceStartup;}
        static void Release(){UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState());lastInput=EditorApplication.timeSinceStartup;}
    }
}
