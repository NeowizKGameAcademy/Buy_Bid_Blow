using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BuyBidBlow.UI.Lobby;
using UnityEditor;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    public static class LobbySpriteImporter
    {
        const string Root=LobbyBuilder.Root;
        [Serializable] class Spec {public Item[] assets;}
        [Serializable] class Item {public string id;public string layout;public bool slice;}
        static readonly string[] States={"Normal","Focused","Pressed","Disabled"};
        [MenuItem("Tools/BuyBidBlow/Lobby/Import UI Sprites")]
        public static void Import()
        {
            var spec=JsonUtility.FromJson<Spec>(File.ReadAllText(Root+"/SpriteRequirements.json"));
            var entries=new List<LobbySpriteLibrary.Entry>();
            var report=new StringBuilder("# Lobby sprite verification\n\nBuilt-in image_gen production assets. Sprite rects trim transparent margins without modifying PNG pixels.\n\n");
            var html=new StringBuilder("<!doctype html><html lang='ko'><meta charset='utf-8'><title>Lobby UI Sprites</title><style>body{background:#102a26;color:#fff0c5;font:16px system-ui;margin:32px}main{display:grid;grid-template-columns:repeat(auto-fill,minmax(240px,1fr));gap:20px}article{border:1px solid #7c683c;border-radius:12px;padding:14px;background:#1c3934}svg{width:100%;height:150px;background:repeating-conic-gradient(#294d46 0% 25%,#365b54 0% 50%) 50%/20px 20px}h3{font-size:14px}p{opacity:.8}</style><h1>로비 UI 스프라이트</h1><p>24종 · 버튼 3종 × 4상태 · 총 33개 독립 스프라이트. 글자는 Unity 텍스트로 추가합니다.</p><main>");
            foreach(var item in spec.assets)
            {
                string path=Root+"/Sprites/"+item.id+".png";
                var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                if(!source.LoadImage(File.ReadAllBytes(path)))throw new Exception("Cannot decode "+path);
                var pixels=source.GetPixels32();int w=source.width,h=source.height;
                // Generated PNGs can contain invisible alpha 1/255 at a canvas corner.
                if(pixels[0].a>8)throw new Exception("Opaque background corner: "+item.id);
                var slices=new List<SpriteMetaData>();
                int count=item.layout=="states"?4:1;
                for(int index=0;index<count;index++)
                {
                    int x0=count==1?0:(index%2)*w/2;
                    int y0=count==1?0:(index<2?h/2:0);
                    int x1=count==1?w:x0+w/2, y1=count==1?h:y0+h/2;
                    // A small alpha threshold excludes invisible compression specks; source is untouched.
                    int left=x1,right=x0,bottom=y1,top=y0;
                    for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*w+x].a>=24)
                    {left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
                    if(left>=right||bottom>=top)throw new Exception("Empty sprite: "+item.id+" "+index);
                    left=Math.Max(x0,left-4);right=Math.Min(x1-1,right+4);bottom=Math.Max(y0,bottom-4);top=Math.Min(y1-1,top+4);
                    var rect=new Rect(left,bottom,right-left+1,top-bottom+1);
                    // Preserve decorative logo as a whole; rounded backgrounds support horizontal nine slicing.
                    bool sliced=item.slice && !item.id.StartsWith("01_");
                    float bx=sliced?Mathf.Min(rect.width*.25f,rect.height*.5f):0;
                    float by=sliced?rect.height*.25f:0;
                    string name=item.id+(count==1?"":"_"+States[index]);
                    slices.Add(new SpriteMetaData{name=name,rect=rect,alignment=(int)SpriteAlignment.Center,pivot=new Vector2(.5f,.5f),border=new Vector4(bx,by,bx,by)});
                    // SVG is a non-destructive image viewport, not another raster asset.
                    html.Append($"<article><h3>{name}</h3><svg viewBox='{left} {h-top-1} {rect.width} {rect.height}'><image href='Sprites/{item.id}.png' width='{w}' height='{h}'/></svg><p>{(sliced?"9-slice":"Simple / preserve aspect")}</p></article>");
                }
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(!importer){AssetDatabase.ImportAsset(path);importer=(TextureImporter)AssetImporter.GetAtPath(path);}
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Multiple;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.sRGBTexture=true;
                importer.npotScale=TextureImporterNPOTScale.None;importer.filterMode=FilterMode.Bilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
                importer.spritePixelsPerUnit=100;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
#pragma warning disable CS0618
                importer.spritesheet=slices.ToArray();
#pragma warning restore CS0618
                importer.SaveAndReimport();
                var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                if(sprites.Length!=count)throw new Exception("Unexpected sprite count: "+item.id+" "+sprites.Length);
                foreach(var s in sprites)entries.Add(new LobbySpriteLibrary.Entry{id=s.name,sprite=s});
                report.Append($"- {item.id}: {w} × {h}, transparent corner PASS, {sprites.Length} sprite(s), center alpha {pixels[(h/2)*w+w/2].a}/255.\n");
                UnityEngine.Object.DestroyImmediate(source);
            }
            if(entries.Count!=33)throw new Exception("Expected 33 sprites, got "+entries.Count);
            string libraryPath=Root+"/LobbySpriteLibrary.asset";
            var library=AssetDatabase.LoadAssetAtPath<LobbySpriteLibrary>(libraryPath);
            if(!library){library=ScriptableObject.CreateInstance<LobbySpriteLibrary>();AssetDatabase.CreateAsset(library,libraryPath);}
            library.entries=entries.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
            File.WriteAllText(Root+"/SpriteCatalog.html",html.Append("</main></html>").ToString());
            File.WriteAllText(Root+"/Verification.md",report.Append("\nPASS: 24 PNG files, 33 nonempty imported sprites including 12 button-state sprites.\n").ToString());
            AssetDatabase.Refresh();
        }
        [InitializeOnLoadMethod]static void Request()
        {
            if(!File.Exists("Library/LobbySprites.request"))return;
            EditorApplication.delayCall+=()=>{
                File.Delete("Library/LobbySprites.request");
                try{Import();File.WriteAllText("Library/LobbySprites.result","OK: 24 textures / 33 sprites");}
                catch(Exception ex){File.WriteAllText("Library/LobbySprites.result",ex.ToString());Debug.LogException(ex);}
            };
        }
    }
}
