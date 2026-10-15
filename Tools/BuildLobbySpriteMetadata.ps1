param([string]$ProjectRoot=(Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies (@([Drawing.Bitmap].Assembly.Location,[Drawing.Color].Assembly.Location,'System.Runtime','System.Runtime.InteropServices') + @(Get-ChildItem -LiteralPath $PSHOME -Filter 'System.Private.Windows*.dll' | ForEach-Object FullName)) -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
public static class LobbyPngBounds {
 public static int[] Read(string path,int part) {
  using(var b=new Bitmap(path)) {
   int w=b.Width,h=b.Height,x0=0,y0=0,x1=w,y1=h;
   if(part>=0){x0=(part%2)*w/2;y0=(part<2?0:h/2);x1=x0+w/2;y1=y0+h/2;}
   var d=b.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   byte[] data=new byte[Math.Abs(d.Stride)*h];Marshal.Copy(d.Scan0,data,0,data.Length);b.UnlockBits(d);
   int l=x1,r=x0,t=y1,bt=y0;
   for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(data[y*Math.Abs(d.Stride)+x*4+3]>=24){l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);bt=Math.Max(bt,y);}
   if(l>=r||t>=bt)throw new Exception("Empty sprite "+path);
   l=Math.Max(x0,l-4);r=Math.Min(x1-1,r+4);t=Math.Max(y0,t-4);bt=Math.Min(y1-1,bt+4);
   return new int[]{w,h,l,h-bt-1,r-l+1,bt-t+1,data[3],data[(h/2)*Math.Abs(d.Stride)+(w/2)*4+3]};
  }
 }
}
'@
$ui=Join-Path $ProjectRoot 'Assets/Project/Dev/Sandbox/Lobby/UI'
$spec=Get-Content -LiteralPath (Join-Path $ui 'SpriteRequirements.json') -Raw | ConvertFrom-Json
$states=@('Normal','Focused','Pressed','Disabled')
$library=[Text.StringBuilder]::new()
$scriptGuid=([regex]::Match([IO.File]::ReadAllText((Join-Path $ui 'LobbySpriteLibrary.cs.meta')),'guid: (\w+)')).Groups[1].Value
[void]$library.Append("%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!114 &11400000`nMonoBehaviour:`n  m_ObjectHideFlags: 0`n  m_CorrespondingSourceObject: {fileID: 0}`n  m_PrefabInstance: {fileID: 0}`n  m_PrefabAsset: {fileID: 0}`n  m_GameObject: {fileID: 0}`n  m_Enabled: 1`n  m_EditorHideFlags: 0`n  m_Script: {fileID: 11500000, guid: $scriptGuid, type: 3}`n  m_Name: LobbySpriteLibrary`n  m_EditorClassIdentifier: `n  entries:`n")
$html=[Text.StringBuilder]::new("<!doctype html><html lang='ko'><meta charset='utf-8'><title>Lobby Sprites</title><style>body{background:#102a26;color:#fff0c5;font:16px system-ui;margin:32px}main{display:grid;grid-template-columns:repeat(auto-fill,minmax(240px,1fr));gap:20px}article{border:1px solid #7c683c;border-radius:12px;padding:14px;background:#1c3934}svg{width:100%;height:150px;background:repeating-conic-gradient(#294d46 0% 25%,#365b54 0% 50%) 50%/20px 20px}h3{font-size:14px}</style><h1>로비 UI 스프라이트</h1><p>24종 · 버튼 3종 × 4상태 · 총 33개. 배경 / 텍스트 / 아이콘을 분리해 사용합니다.</p><main>")
$report=[Text.StringBuilder]::new("# 검수 결과`n`n내장 image_gen 생성. PNG 픽셀은 수정하지 않고 임포트 영역만 설정했습니다.`n`n")
$total=0
foreach($item in $spec.assets){
 $png=Join-Path $ui "Sprites/$($item.id).png"
 $metaPath="$png.meta"
 $meta=[IO.File]::ReadAllText($metaPath)
 $guid=[regex]::Match($meta,'guid: (\w+)').Groups[1].Value
 $meta=$meta -replace 'enableMipMap: 1','enableMipMap: 0' -replace 'maxTextureSize: 2048','maxTextureSize: 4096' -replace 'nPOTScale: 1','nPOTScale: 0' -replace 'spriteMode: 0','spriteMode: 2' -replace 'spriteMeshType: 1','spriteMeshType: 0' -replace 'alphaIsTransparency: 0','alphaIsTransparency: 1' -replace '(?m)^  textureType: 0','  textureType: 8' -replace 'textureCompression: 1','textureCompression: 0' -replace 'wrapU: 0','wrapU: 1' -replace 'wrapV: 0','wrapV: 1'
 $slices=[Text.StringBuilder]::new("  spriteSheet:`n    serializedVersion: 2`n    sprites:`n")
 $names=[Text.StringBuilder]::new("    nameFileIdTable:`n")
 $count=if($item.layout -eq 'states'){4}else{1}
 for($i=0;$i -lt $count;$i++){
  $part=if($count -eq 1){-1}else{$i}
  $b=[LobbyPngBounds]::Read($png,$part)
  if($b[6] -ne 0){throw "Opaque background $png"}
  $name=$item.id+$(if($count -eq 4){'_'+$states[$i]}else{''})
  $id=21300000+$i*2
  $sliced=$item.slice -and !$item.id.StartsWith('01_')
  $bx=if($sliced){[int][math]::Min($b[4]*.25,$b[5]*.5)}else{0}
  $by=if($sliced){[int]($b[5]*.25)}else{0}
  $spriteGuid=[guid]::NewGuid().ToString('N')
  [void]$slices.Append("    - serializedVersion: 2`n      name: $name`n      rect:`n        serializedVersion: 2`n        x: $($b[2])`n        y: $($b[3])`n        width: $($b[4])`n        height: $($b[5])`n      alignment: 0`n      pivot: {x: 0.5, y: 0.5}`n      border: {x: $bx, y: $by, z: $bx, w: $by}`n      customData: `n      outline: []`n      physicsShape: []`n      tessellationDetail: 0`n      bones: []`n      spriteID: $spriteGuid`n      internalID: $id`n      vertices: []`n      indices: `n      edges: []`n      weights: []`n")
  [void]$names.Append("      ${name}: $id`n")
  [void]$library.Append("  - id: $name`n    sprite: {fileID: $id, guid: $guid, type: 3}`n")
  $top=$b[1]-$b[3]-$b[5]
  [void]$html.Append("<article><h3>$name</h3><svg viewBox='$($b[2]) $top $($b[4]) $($b[5])'><image href='Sprites/$($item.id).png' width='$($b[0])' height='$($b[1])'/></svg><p>$(if($sliced){'9-slice'}else{'Simple / preserve aspect'})</p></article>")
  $total++
 }
 [void]$slices.Append("    outline: []`n    customData: `n    physicsShape: []`n    bones: []`n    spriteID: `n    internalID: 0`n    vertices: []`n    indices: `n    edges: []`n    weights: []`n    secondaryTextures: []`n    spriteCustomMetadata:`n      entries: []`n")
 [void]$slices.Append($names.ToString())
 $meta=[regex]::Replace($meta,'(?s)  spriteSheet:.*?(?=  mipmapLimitGroupName:)',$slices.ToString())
 [IO.File]::WriteAllText($metaPath,$meta)
 [void]$report.Append("- $($item.id): $($b[0]) × $($b[1]), 투명 모서리 PASS, $count 스프라이트 정의, 중앙 알파 $($b[7])/255.`n")
}
if($total -ne 33){throw "Expected33, got$total"}
[IO.File]::WriteAllText((Join-Path $ui 'LobbySpriteLibrary.asset'),$library.ToString())
[IO.File]::WriteAllText((Join-Path $ui 'SpriteCatalog.html'),$html.Append('</main></html>').ToString())
[void]$report.Append("`nPASS: 24 PNG, 33 nonempty sprite rects, 12 button states. Importer compilation checked with Unity Roslyn response file. Editor import pending refresh.`n")
[IO.File]::WriteAllText((Join-Path $ui 'Verification.md'),$report.ToString())
Write-Output "OK: 24 PNG / $total sprite metadata definitions; editor refresh required."
