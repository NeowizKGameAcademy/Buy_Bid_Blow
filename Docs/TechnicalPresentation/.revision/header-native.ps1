function ColorValue([string]$hex){return [Convert]::ToInt32($hex.Substring(0,2),16)+256*[Convert]::ToInt32($hex.Substring(2,2),16)+65536*[Convert]::ToInt32($hex.Substring(4,2),16)}
function AddText($slide,[string]$value,$x,$y,$w,$h,$size,[string]$color,$bold){
 $box=$slide.Shapes.AddTextbox(1,$x*.75,$y*.75,$w*.75,$h*.75)
 $box.TextFrame.MarginLeft=0;$box.TextFrame.MarginRight=0;$box.TextFrame.MarginTop=0;$box.TextFrame.MarginBottom=0
 $box.TextFrame.TextRange.Text=$value
 $box.TextFrame.TextRange.Font.Name='Malgun Gothic';$box.TextFrame.TextRange.Font.Size=$size*.75
 $box.TextFrame.TextRange.Font.Color.RGB=(ColorValue $color);$box.TextFrame.TextRange.Font.Bold=$bold
 return $box
}
$app=New-Object -ComObject PowerPoint.Application
$deck=$app.Presentations.Open('C:\훈련생폴더\3. 유니티\Buy_Bid_Blow\Docs\TechnicalPresentation\.revision\toc-base.pptx',$true,$false,$false)
for($n=2;$n -le 18;$n++){
 $slide=$deck.Slides.Item($n)
 if($n -eq 2){
  $slide.Background.Fill.ForeColor.RGB=(ColorValue 'F5F2EA')
  $null=AddText $slide '목차' 64 49 1152 72 54 'F5F2EA' -1
  $null=AddText $slide '기획 의도부터 실제 구현과 문제 해결까지' 64 126 1152 42 23 'D5DFD9' 0
  $null=AddText $slide '02' 1175 675 50 25 16 '68766E' 0
  $groups=@(
   @('01','기획과 공간','게임 개요 / 세계관 / 캐릭터 / 레벨 구조','03–06'),
   @('02','화면과 플레이 경험','메인·로비 / HUD / 거래·경매 / 수첩 / 피드백 / 결과','07–16'),
   @('03','구현과 문제 해결','UI와 서버의 역할 / 본인 입찰 인상 수정','17–18'),
   @('04','회고','기획 의도와 다음 검증','19'))
  for($j=0;$j -lt 4;$j++){
   $y=220+$j*112;$g=$groups[$j]
   $null=AddText $slide $g[0] 64 $y 90 58 40 '8B632B' -1
   $null=AddText $slide $g[1] 180 $y 850 45 31 '123A37' -1
   $null=AddText $slide $g[2] 180 ($y+49) 850 36 23 '52635D' 0
   $null=AddText $slide $g[3] 1090 ($y+12) 126 40 25 '8B632B' -1
  }
 }else{
  $title=$slide.Shapes.Item(1);$sub=$slide.Shapes.Item(2)
  $title.Left=48;$title.Top=36.75;$title.Width=864;$title.Height=54;$title.TextFrame.TextRange.Font.Color.RGB=(ColorValue 'F5F2EA')
  $sub.Left=48;$sub.Top=94.5;$sub.Width=864;$sub.Height=31.5;$sub.TextFrame.TextRange.Font.Color.RGB=(ColorValue 'D5DFD9');$sub.TextFrame.TextRange.Font.Size=17.25
  $slide.Shapes.Item(3).TextFrame.TextRange.Text=$n.ToString('00')
 }
 $band=$slide.Shapes.AddShape(1,0,0,960,138);$band.Fill.ForeColor.RGB=(ColorValue '123A37');$band.Line.Visible=0;$band.ZOrder(1)
 $section=if($n -eq 2){'CONTENTS'}elseif($n -le 6){'01  기획과 공간'}elseif($n -le 16){'02  화면과 플레이 경험'}else{'03  구현과 문제 해결'}
 $null=AddText $slide $section 64 14 1050 26 16 'E4C58B' -1
}
$deck.Export('C:\훈련생폴더\3. 유니티\Buy_Bid_Blow\Docs\TechnicalPresentation\.revision\v5','PNG',1280,720)
$deck.SaveAs('C:\훈련생폴더\3. 유니티\Buy_Bid_Blow\Docs\TechnicalPresentation\.revision\candidate-v5-native.pptx',24)
$deck.Close()
[Runtime.InteropServices.Marshal]::ReleaseComObject($deck) | Out-Null
[Runtime.InteropServices.Marshal]::ReleaseComObject($app) | Out-Null
'19 slides rendered'
