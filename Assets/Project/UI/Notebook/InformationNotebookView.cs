using System;
using System.Collections.Generic;
using BuyBidBlow.Systems.Types;
using UnityEngine;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Notebook {
// Displays only information explicitly awarded to this player. Never reads future prices.
public sealed class InformationNotebookView : MonoBehaviour {
 public GameObject overlay;
 public Text pageRange;
 public Text[] roundNumbers,roundStates,cellValues;
 public Image[] cellMarks,currentBands,currentMarkers,cellFrames;
 public RectTransform scrollThumb;
 public Sprite riseMark,fallMark,flatMark;
 // The owner locks or restores player input while the notebook is open.
 public event Action<bool> OpenChanged;
 public bool IsOpen {get;private set;}
 public int FirstRound {get;private set;}=1;
 int currentRound=1,totalRounds=13;
 readonly Dictionary<int,Direction> known=new Dictionary<int,Direction>();
 static readonly Color Ink=new Color(.04f,.18f,.18f),Past=new Color(.43f,.41f,.37f);
 public void SetOpen(bool open,int round){
 if(open==IsOpen)return;BuyBidBlow.Audio.GameAudio.Play("ui.notebook",this);IsOpen=open;if(open)FirstRound=Mathf.Clamp(round-2,1,Mathf.Max(1,totalRounds-7));
 overlay.SetActive(open);OpenChanged?.Invoke(open);RenderRows();
 }
 public void Scroll(int rows){int before=FirstRound;FirstRound=Mathf.Clamp(FirstRound+rows,1,Mathf.Max(1,totalRounds-7));if(before!=FirstRound)BuyBidBlow.Audio.GameAudio.Play("ui.page",this);RenderRows();}
 public void Present(int round,int total,IReadOnlyList<Info> information){currentRound=round;totalRounds=total;known.Clear();foreach(var entry in information){known[entry.Round*3+(int)entry.CompanyA]=entry.DirectionA;known[entry.Round*3+(int)entry.CompanyB]=entry.DirectionB;}FirstRound=Mathf.Clamp(FirstRound,1,Mathf.Max(1,totalRounds-7));if(IsOpen)RenderRows();}
 void RenderRows(){if(roundNumbers==null||roundNumbers.Length==0)return;int end=Mathf.Min(totalRounds,FirstRound+roundNumbers.Length-1);pageRange.text=$"{FirstRound}–{end} / {totalRounds}R";
 for(int row=0;row<roundNumbers.Length;row++){int round=FirstRound+row;bool past=round<currentRound,valid=round<=totalRounds,now=round==currentRound;roundNumbers[row].text=valid?round+"R":"";roundNumbers[row].color=past?Past:Ink;roundStates[row].text=!valid?"":past?"완료":now?"현재":"";roundStates[row].color=past?Past:Ink;currentBands[row].enabled=valid&&now;currentMarkers[row].enabled=valid&&now;
 for(int col=0;col<3;col++){int i=row*3+col;Direction d=default;bool has=valid&&known.TryGetValue(round*3+col,out d);cellMarks[i].enabled=has;cellValues[i].text=has?(d==Direction.Rise?"↑ 상승":d==Direction.Fall?"↓ 하락":"— 보합"):"";if(has){cellMarks[i].sprite=d==Direction.Rise?riseMark:d==Direction.Fall?fallMark:flatMark;cellMarks[i].color=new Color(1,1,1,past?.52f:.82f);Color color=d==Direction.Rise?new Color(.36f,.065f,.04f):d==Direction.Fall?new Color(.035f,.12f,.26f):new Color(.2f,.21f,.2f);color.a=past?.68f:1;cellValues[i].color=color;}}
 for(int col=0;col<4;col++)cellFrames[row*4+col].color=new Color(1,1,1,past?.45f:.72f);
 }
 if(scrollThumb){var pos=scrollThumb.anchoredPosition;pos.y=-246-(FirstRound-1)/(float)Mathf.Max(1,totalRounds-8)*440;scrollThumb.anchoredPosition=pos;}
 }
}
}
