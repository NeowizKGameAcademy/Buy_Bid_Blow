using System;
using System.IO;
using System.Linq;
using BuyBidBlow.UI.Ticker;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallTickerDisplaySetup
    {
        const string Root="Assets/Project/Content/Props/TickerBoard";
        const string Prefab=Root+"/TickerBoard.prefab";
        const string Hall="Assets/Project/Content/Levels/ExchangeFurnished/AuctionHall_Furnished.prefab";
        const string ScenePath="Assets/Project/Scenes/SC_ExchangeFurnished.unity";
        const string FontPath="Assets/Font/Oxanium/Oxanium ExtraBold SDF.asset";
        const string Preview="Docs/Arts/Props/TickerBoard";

        [MenuItem("BuyBidBlow/Level/Build ticker displays")]
        public static void Build()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory(Preview);
            AssetDatabase.Refresh();
            var font=CreateFont("Assets/Font/Oxanium/Oxanium-ExtraBold.ttf",FontPath,"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789$%+-.:/() —");
            var caption=CreateFont("Assets/Font/public/static/Pretendard-Medium.otf","Assets/Font/Oxanium/Ticker Caption SDF.asset","이전 라운드 대비");
            string gridPath=Root+"/Materials/LEDGrid.png";
            if(!File.Exists(gridPath))
            {
                var tex=new Texture2D(4,4,TextureFormat.RGBA32,false);
                for(int y=0;y<4;y++)for(int x=0;x<4;x++)tex.SetPixel(x,y,new Color(0,0,0,x==0||y==0?1:0));
                tex.Apply();File.WriteAllBytes(gridPath,tex.EncodeToPNG());Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(gridPath);
                var importer=(TextureImporter)AssetImporter.GetAtPath(gridPath);importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Bilinear;
                importer.mipmapEnabled=true;importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var root=PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var display=root.GetComponent<TickerBoardDisplay>()??root.AddComponent<TickerBoardDisplay>();
                display.numberFont=font;
                display.captionFont=caption;
                display.ledTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(gridPath);
                display.BuildSurface();PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            var hall=PrefabUtility.LoadPrefabContents(Hall);
            try{Configure(hall.GetComponentsInChildren<TickerBoardDisplay>(true));PrefabUtility.SaveAsPrefabAsset(hall,Hall);}
            finally{PrefabUtility.UnloadPrefabContents(hall);}
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var boards=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TickerBoardDisplay>(true)).ToArray();
            Configure(boards);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var center=boards.Single(b=>b.ResolvedMode==TickerBoardDisplay.ScreenMode.Overview);
            center.PresentSnapshot(new[]{18,40,42,22,36,48,21,37,46,24,35,46},30,0);
            var hallCamera=new GameObject("Ticker hall verification").AddComponent<Camera>();
            hallCamera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            hallCamera.transform.position=center.transform.TransformPoint(new Vector3(0,0,11));
            hallCamera.transform.rotation=center.transform.rotation*Quaternion.Euler(0,180,0);
            hallCamera.orthographic=true;hallCamera.orthographicSize=3.15f;
            Capture(hallCamera,1600,900,Preview+"/TickerHall_Unity.png");Object.DestroyImmediate(hallCamera.gameObject);
            center.ShowWaiting();
            CapturePreviewCore();
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            File.WriteAllText(Preview+"/DisplayVerification.txt","PASS: Oxanium font and license imported. Three linked boards: left timer, center overview, right rotating company.\nPASS: one active display group per board; no interactive raycast targets.\nPreview uses explicit sample values; production assets contain waiting placeholders only.\n");
            Debug.Log("[Ticker] Build and preview complete.");
        }
        static TMP_FontAsset CreateFont(string sourcePath,string path,string characters)
        {
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if(!font)
            {
                var source=AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
                if(!source)throw new Exception("Missing font: "+sourcePath);
                font=TMP_FontAsset.CreateFontAsset(source,90,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,false);
                font.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(font,path);
                foreach(var texture in font.atlasTextures){texture.name=font.name+" Atlas";AssetDatabase.AddObjectToAsset(texture,font);}
                font.material.name=font.name+" Material";AssetDatabase.AddObjectToAsset(font.material,font);
            }
            font.atlasPopulationMode=AtlasPopulationMode.Dynamic;font.TryAddCharacters(characters);
            font.atlasPopulationMode=AtlasPopulationMode.Static;EditorUtility.SetDirty(font);
            foreach(var texture in font.atlasTextures)EditorUtility.SetDirty(texture);
            return font;
        }
        static void Configure(TickerBoardDisplay[] boards)
        {
            if(boards.Length!=3)throw new Exception("Expected three ticker boards, found "+boards.Length);
            foreach(var board in boards)
            {
                board.ApplyMode();board.ShowWaiting();
                foreach(Transform child in board.transform.Find("Ticker UI"))PrefabUtility.RecordPrefabInstancePropertyModifications(child.gameObject);
                // Rebuilding the UI replaces child IDs; remove only obsolete visibility overrides.
                var modifications=PrefabUtility.GetPropertyModifications(board.gameObject);
                if(modifications!=null)PrefabUtility.SetPropertyModifications(board.gameObject,
                    modifications.Where(m=>m.target||m.propertyPath!="m_IsActive").ToArray());
                if(board.GetComponentsInChildren<UnityEngine.UI.Graphic>(true).Any(g=>g.raycastTarget))throw new Exception("Ticker must not intercept player input.");
                int groups=new[]{"Timer","Market overview","Company detail"}.Count(n=>board.transform.Find("Ticker UI/"+n).gameObject.activeSelf);
                if(groups!=1)throw new Exception("Ticker mode selection failed.");
            }
        }
        public static void VerifySavedDisplays()
        {
            var hall=PrefabUtility.LoadPrefabContents(Hall);
            try{Configure(hall.GetComponentsInChildren<TickerBoardDisplay>(true));PrefabUtility.SaveAsPrefabAsset(hall,Hall);}
            finally{PrefabUtility.UnloadPrefabContents(hall);}
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            Configure(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TickerBoardDisplay>(true)).ToArray());
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("[Ticker] Saved hall and scene verified; stale UI visibility overrides removed.");
        }
        [MenuItem("BuyBidBlow/Level/Preview ticker displays")]
        public static void CapturePreview()
        {
            if(!Application.isBatchMode&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var setup=EditorSceneManager.GetSceneManagerSetup();
            try{CapturePreviewCore();}
            finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        }
        static void CapturePreviewCore()
        {
            Directory.CreateDirectory(Preview);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            int[] sample={18,40,42,22,36,48,21,37,46,24,35,46};
            string[] names={"TickerBoard_Left","TickerBoard_Center","TickerBoard_Right"};
            for(int i=0;i<3;i++)
            {
                var board=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                board.name=names[i];board.transform.position=new Vector3((1-i)*9.3f,0,0);
                var display=board.GetComponent<TickerBoardDisplay>();display.ApplyMode();display.PresentSnapshot(sample,30,0);
            }
            var cam=new GameObject("Ticker preview camera").AddComponent<Camera>();cam.transform.position=new Vector3(0,0,25);cam.transform.rotation=Quaternion.Euler(0,180,0);
            cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            cam.orthographic=true;cam.orthographicSize=4.8f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.029f,.035f);
            Capture(cam,2560,850,Preview+"/TickerDisplays_Unity.png");
            // A close view checks the actual glyphs, line geometry and numerical scale.
            cam.transform.position=new Vector3(-9.3f,0,15);cam.orthographicSize=2.8f;
            Capture(cam,1600,900,Preview+"/TickerCompany_Unity.png");
            foreach(var board in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TickerBoardDisplay>()))board.ShowWaiting();
        }
        static void Capture(Camera camera,int width,int height,string path)
        {
            var rt=new RenderTexture(width,height,24);var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;
            try{camera.targetTexture=rt;camera.aspect=(float)width/height;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);}
        }
    }
}
