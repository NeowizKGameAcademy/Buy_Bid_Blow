using System;
using BuyBidBlow.Common;
using BuyBidBlow.Systems.Market;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow.UI.Ticker
{
    [DisallowMultipleComponent]
    public sealed class TickerBoardDisplay : MonoBehaviour
    {
        public enum ScreenMode { ByPlacement, Timer, Overview, Company }
        public ScreenMode mode;
        [Min(1)] public float companySeconds=8;
        public TMP_FontAsset numberFont;
        public TMP_FontAsset captionFont;
        public Texture2D ledTexture;
        [SerializeField] RectTransform surface;
        [SerializeField] GameObject timerGroup, overviewGroup, companyGroup;
        [SerializeField] TMP_Text timer;
        [SerializeField] TMP_Text[] prices, changes, percentages;
        [SerializeField] TickerPlot[] smallPlots;
        [SerializeField] TickerIcon companyIcon;
        [SerializeField] TMP_Text companyPrice, companyChange, latestPrice;
        [SerializeField] TMP_Text[] priceTicks, roundTicks;
        [SerializeField] Image latestBadge;
        [SerializeField] TickerPlot largePlot;
        MatchNet source;
        // Runtime layers over the screens: the scheduled video and the short black cut between channels.
        RawImage videoLayer;
        Image cutLayer;
        string shownChannel;
        float cutUntil;
        int cachedCount=-1, cachedCompany=-1;
        int[] history=Array.Empty<int>();
        static readonly Color White=new Color(.96f,.94f,.86f);
        static readonly Color Gold=new Color(1,.66f,.16f);
        static readonly Color Red=new Color(1,.19f,.2f);
        static readonly Color Blue=new Color(.12f,.61f,1);
        static readonly Color Muted=new Color(.54f,.58f,.60f);
        public ScreenMode ResolvedMode=>mode!=ScreenMode.ByPlacement?mode:
            name.EndsWith("_Left",StringComparison.Ordinal)?ScreenMode.Timer:
            name.EndsWith("_Right",StringComparison.Ordinal)?ScreenMode.Company:ScreenMode.Overview;

        // The scheduled channel id, or null while the board shows its fixed screen.
        public string CurrentChannel=>shownChannel;

        void Awake(){EnsureLayers();ApplyMode();ShowWaiting();}
        void OnEnable(){cachedCount=-1;}
        void Update()
        {
            var current=MatchNet.Current;
            if(!ReferenceEquals(source,current))
            {
                source=current;cachedCount=-1;cachedCompany=-1;
                history=Array.Empty<int>();ShowWaiting();
            }
            var director=TickerDirector.Active;
            TickerShot shot=default;
            bool scheduled=director&&director.TryGetShot(name,out shot);
            Program(director,shot);
            if(!current||!surface)return;
            // Poll after Mirror's message application, not in a partially applied SyncList callback.
            int count=current.priceHistory.Count;
            if(count<3||count%3!=0)return;
            if(count!=cachedCount)
            {
                history=new int[count];current.priceHistory.CopyTo(history,0);cachedCount=count;
                cachedCompany=-1;
                PresentOverview(history);
            }
            double elapsed=Math.Max(0,NetworkTime.time-current.firstRoundStartsAt);
            int company=scheduled&&shot.Channel.screen=="Company"?shot.Company:(int)(elapsed/Math.Max(1,companySeconds))%3;
            if(company!=cachedCompany){PresentCompany(history,company,-PriceChangeGenerator.HistoryRoundCount);cachedCompany=company;}
            SetClock(current.phase==MatchNet.Phase.Finished?0:current.SecondsLeft);
        }

        public void ApplyMode()=>Show(ResolvedMode);
        void Show(ScreenMode screen)
        {
            if(!timerGroup)return;
            timerGroup.SetActive(screen==ScreenMode.Timer);
            overviewGroup.SetActive(screen==ScreenMode.Overview);
            companyGroup.SetActive(screen==ScreenMode.Company);
        }
        // Shows the director's channel for this board, or the fixed screen when nothing is scheduled.
        void Program(TickerDirector director,TickerShot shot)
        {
            if(!videoLayer)return;
            if(director&&director.TryGetIntroTexture(out var introTexture))
            {
                Show(ResolvedMode);videoLayer.enabled=true;videoLayer.texture=introTexture;
                int slice=name.EndsWith("_Right",StringComparison.Ordinal)?0:name.EndsWith("_Center",StringComparison.Ordinal)?1:2;
                videoLayer.uvRect=new Rect(slice/3f,0,1f/3f,1);cutLayer.enabled=false;
                return;
            }
            if(director&&director.IntroBlank)
            {
                timerGroup.SetActive(false);overviewGroup.SetActive(false);companyGroup.SetActive(false);videoLayer.enabled=false;cutLayer.enabled=false;
                return;
            }
            string channel=shot.IsSet?shot.Channel.id:null;
            if(channel!=shownChannel)
            {
                if(shownChannel!=null&&channel!=null)cutUntil=Time.unscaledTime+director.Schedule.Data.cutSeconds;
                shownChannel=channel;
            }
            bool video=shot.IsSet&&!string.IsNullOrEmpty(shot.Channel.video);
            if(!shot.IsSet)Show(ResolvedMode);
            else if(!video)Show((ScreenMode)Enum.Parse(typeof(ScreenMode),shot.Channel.screen));
            var texture=video?director.VideoTexture(shot.Channel.video):null;
            videoLayer.enabled=texture;
            if(texture)
            {
                float slices=director.Schedule.BoardCount;
                videoLayer.texture=texture;
                videoLayer.uvRect=shot.Slot>=0&&shot.Channel.wide?new Rect(shot.Slot/slices,0,1/slices,1):new Rect(0,0,1,1);
            }
            cutLayer.enabled=Time.unscaledTime<cutUntil;
        }
        void EnsureLayers()
        {
            if(!surface||videoLayer)return;
            videoLayer=Node(surface,"Video feed",0,0,1280,720).gameObject.AddComponent<RawImage>();
            videoLayer.raycastTarget=false;videoLayer.enabled=false;
            cutLayer=Panel(surface,"Channel cut",0,0,1280,720,Color.black);cutLayer.enabled=false;
            // The LED grid stays on top of everything shown.
            var grid=surface.Find("Subtle LED grid");if(grid)grid.SetAsLastSibling();
        }
        public void ShowWaiting()
        {
            if(!surface)return;
            SetClock(0);
            for(int c=0;c<3;c++)
            {
                prices[c].text="—";changes[c].text="—";percentages[c].text="";
                changes[c].color=percentages[c].color=Muted;
                smallPlots[c].Present(Array.Empty<float>(),Muted);
            }
            companyPrice.text="—";companyChange.text="";companyIcon.SetCompany(0);
            largePlot.Present(Array.Empty<float>(),Muted);latestBadge.gameObject.SetActive(false);
            foreach(var tick in priceTicks)tick.text="";
            foreach(var tick in roundTicks)tick.text="";
        }
        // Editor preview and tests use the same presentation path as the replicated data.
        public void PresentSnapshot(int[] publicHistory,double seconds,int company)
        {
            if(publicHistory==null||publicHistory.Length<3||publicHistory.Length%3!=0){ShowWaiting();return;}
            SetClock(seconds);
            PresentOverview(publicHistory);PresentCompany(publicHistory,Mathf.Clamp(company,0,2));
        }
        static float[] Series(int[] data,int company)
        {
            var points=new float[data.Length/3];
            for(int i=0;i<points.Length;i++)points[i]=data[i*3+company];
            return points;
        }
        void SetClock(double seconds)=>timer.text="<mspace=0.62em>"+TickerFormat.Clock(seconds)+"</mspace>";
        static Color ChangeColor(int current,int previous)=>current>previous?Red:current<previous?Blue:Muted;
        void PresentOverview(int[] data)
        {
            int end=data.Length-3,prior=Math.Max(0,end-3);
            for(int c=0;c<3;c++)
            {
                int value=data[end+c],previous=data[prior+c];var tint=ChangeColor(value,previous);
                prices[c].text="$"+value;
                changes[c].text=TickerFormat.Change(value,previous);
                percentages[c].text=TickerFormat.Percent(value,previous);
                changes[c].color=percentages[c].color=tint;
                smallPlots[c].Present(Series(data,c),tint);
            }
        }
        void PresentCompany(int[] data,int c,int firstRound=0)
        {
            int end=data.Length-3,prior=Math.Max(0,end-3);
            int value=data[end+c],previous=data[prior+c];var tint=ChangeColor(value,previous);
            companyIcon.SetCompany(c);companyPrice.text="$"+value;
            companyChange.text=TickerFormat.Change(value,previous)+" ("+TickerFormat.Percent(value,previous)+")";
            companyChange.color=tint;
            var series=Series(data,c);largePlot.Present(series,tint);
            for(int i=0;i<5;i++)priceTicks[i].text=(largePlot.minimum+(largePlot.maximum-largePlot.minimum)*i/4).ToString("0");
            int ticks=Math.Min(5,series.Length);
            for(int i=0;i<5;i++)
            {
                if(i>=ticks){roundTicks[i].text="";continue;}
                int sample=ticks==1?0:Mathf.RoundToInt((float)i*(series.Length-1)/(ticks-1));
                int round=firstRound+sample;
                roundTicks[i].text=round==0?"START":"R"+round;
                var p=roundTicks[i].rectTransform.anchoredPosition;
                p.x=58+(series.Length==1?0:(float)sample/(series.Length-1))*1050-38;
                roundTicks[i].rectTransform.anchoredPosition=p;
            }
            float normalized=(value-largePlot.minimum)/(largePlot.maximum-largePlot.minimum);
            latestBadge.gameObject.SetActive(true);latestBadge.color=tint;
            var badgePosition=latestBadge.rectTransform.anchoredPosition;
            badgePosition.y=-(636-normalized*460-22);latestBadge.rectTransform.anchoredPosition=badgePosition;
            latestPrice.text=value.ToString();
        }

        public void BuildSurface()
        {
            if(!numberFont)throw new InvalidOperationException("Ticker needs its Oxanium font asset.");
            var old=transform.Find("Ticker UI");
            if(old){if(Application.isPlaying)Destroy(old.gameObject);else DestroyImmediate(old.gameObject);}
            var screen=transform.Find("Screen").GetComponent<MeshFilter>();var bounds=screen.sharedMesh.bounds;
            var go=new GameObject("Ticker UI",typeof(RectTransform),typeof(Canvas));
            surface=(RectTransform)go.transform;surface.SetParent(transform,false);
            surface.sizeDelta=new Vector2(1280,720);
            surface.localPosition=new Vector3(bounds.center.x,bounds.center.y,bounds.max.z+.018f);
            surface.localRotation=Quaternion.Euler(0,180,0);
            surface.localScale=new Vector3(bounds.size.x/1280*.985f,bounds.size.y/720*.985f,1);
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            canvas.additionalShaderChannels=AdditionalCanvasShaderChannels.TexCoord1|AdditionalCanvasShaderChannels.Normal|AdditionalCanvasShaderChannels.Tangent;
            Panel(surface,"Black screen",0,0,1280,720,new Color(.008f,.011f,.015f));
            timerGroup=Node(surface,"Timer",0,0,1280,720).gameObject;
            timer=Text(timerGroup.transform,"Countdown","00:00",60,130,1160,460,260,Gold,TextAlignmentOptions.Center);
            timer.text="<mspace=0.62em>00:00</mspace>";
            overviewGroup=Node(surface,"Market overview",0,0,1280,720).gameObject;
            prices=new TMP_Text[3];changes=new TMP_Text[3];percentages=new TMP_Text[3];smallPlots=new TickerPlot[3];
            string[] symbols={"STRW","TMBR","BRCK"};
            for(int c=0;c<3;c++)
            {
                float y=20+c*226;
                Icon(overviewGroup.transform,c,30,y+30,120);
                Text(overviewGroup.transform,"Symbol "+c,symbols[c],168,y+62,205,80,49,White);
                prices[c]=Text(overviewGroup.transform,"Price "+c,"—",380,y+45,250,100,86,White);
                changes[c]=Text(overviewGroup.transform,"Change "+c,"—",640,y+35,240,70,58,Muted);
                percentages[c]=Text(overviewGroup.transform,"Percent "+c,"",640,y+108,240,56,37,Muted);
                smallPlots[c]=Plot(overviewGroup.transform,"Trend "+c,910,y+42,310,124,false);
                if(c<2)Panel(overviewGroup.transform,"Divider "+c,24,y+212,1232,1.5f,new Color(.65f,.39f,.08f));
            }
            Text(overviewGroup.transform,"Change basis","이전 라운드 대비",950,686,280,24,18,Muted,TextAlignmentOptions.Right,true);
            companyGroup=Node(surface,"Company detail",0,0,1280,720).gameObject;
            companyIcon=Icon(companyGroup.transform,0,36,25,100);
            companyPrice=Text(companyGroup.transform,"Price","—",170,28,290,100,88,White);
            companyChange=Text(companyGroup.transform,"Change","",480,32,700,65,48,Red);
            Text(companyGroup.transform,"Change basis","이전 라운드 대비",485,100,600,30,24,Muted,TextAlignmentOptions.Left,true);
            largePlot=Plot(companyGroup.transform,"Price chart",58,176,1050,460,true);
            priceTicks=new TMP_Text[5];roundTicks=new TMP_Text[5];
            for(int i=0;i<5;i++)
            {
                priceTicks[i]=Text(companyGroup.transform,"Price tick "+i,"",1140,616-i*115,110,40,30,Muted);
                roundTicks[i]=Text(companyGroup.transform,"Round tick "+i,"",20+i*262.5f,654,92,40,24,Muted,TextAlignmentOptions.Center);
            }
            latestBadge=Panel(companyGroup.transform,"Latest price marker",1130,300,114,44,Red);
            latestPrice=Text(latestBadge.transform,"Value","",0,0,114,44,30,White,TextAlignmentOptions.Center);
            if(ledTexture)
            {
                var grid=Node(surface,"Subtle LED grid",0,0,1280,720).gameObject.AddComponent<RawImage>();
                grid.texture=ledTexture;grid.uvRect=new Rect(0,0,320,180);grid.color=new Color(0,0,0,.22f);grid.raycastTarget=false;
            }
            ApplyMode();ShowWaiting();
        }
        static RectTransform Node(Transform parent,string label,float x,float y,float w,float h)
        {
            var r=new GameObject(label,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;
        }
        TMP_Text Text(Transform parent,string label,string content,float x,float y,float w,float h,float size,Color tint,TextAlignmentOptions alignment=TextAlignmentOptions.Left,bool caption=false)
        {
            var text=Node(parent,label,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();
            text.font=caption&&captionFont?captionFont:numberFont;text.text=content;text.fontSize=size;
            text.color=tint;text.alignment=alignment;text.textWrappingMode=TextWrappingModes.NoWrap;
            text.enableAutoSizing=true;text.fontSizeMin=size*.65f;text.fontSizeMax=size;text.raycastTarget=false;
            return text;
        }
        static Image Panel(Transform parent,string label,float x,float y,float w,float h,Color tint)
        {
            var image=Node(parent,label,x,y,w,h).gameObject.AddComponent<Image>();image.color=tint;image.raycastTarget=false;return image;
        }
        static TickerIcon Icon(Transform parent,int company,float x,float y,float size)
        {
            var icon=Node(parent,"Company icon "+company,x,y,size,size).gameObject.AddComponent<TickerIcon>();
            icon.company=company;icon.color=White;icon.raycastTarget=false;return icon;
        }
        static TickerPlot Plot(Transform parent,string label,float x,float y,float w,float h,bool detail)
        {
            var plot=Node(parent,label,x,y,w,h).gameObject.AddComponent<TickerPlot>();plot.detailed=detail;plot.raycastTarget=false;return plot;
        }
    }
}
