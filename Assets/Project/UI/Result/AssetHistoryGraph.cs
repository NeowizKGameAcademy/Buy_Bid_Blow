using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace BuyBidBlow.UI.Result
{
    public sealed class AssetHistoryGraph : Graphic, IPointerClickHandler
    {
        public static readonly Color[] Palette={new Color(1,.76f,.25f),new Color(.3f,.8f,1),new Color(1,.45f,.4f),new Color(.45f,.95f,.65f),new Color(.8f,.6f,1),new Color(1,.65f,.85f)};
        public ResultEntry[] Entries=Array.Empty<ResultEntry>();public int Selected,Round;public int Maximum {get;private set;}=200;
        public event Action<int> RoundSelected;
        public void Present(ResultEntry[] entries,int selected,int round){Entries=entries;Selected=selected;Round=round;Maximum=100;foreach(var e in entries)if(e.AssetHistory!=null)foreach(int v in e.AssetHistory)Maximum=Mathf.Max(Maximum,v);Maximum=Mathf.CeilToInt(Maximum/100f)*100;SetVerticesDirty();}
        public void OnPointerClick(PointerEventData e){if(Entries.Length==0)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out var p);int count=Entries[Selected].AssetHistory?.Length??0;if(count>0)RoundSelected?.Invoke(Mathf.RoundToInt(Mathf.InverseLerp(rectTransform.rect.xMin,rectTransform.rect.xMax,p.x)*(count-1)));}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            for(int i=0;i<=4;i++)Line(vh,new Vector2(r.xMin,r.yMin+r.height*i/4),new Vector2(r.xMax,r.yMin+r.height*i/4),1,new Color(1,1,1,.13f));
            for(int n=0;n<Entries.Length;n++){var values=Entries[n].AssetHistory;if(values==null||values.Length<2)continue;var c=Palette[n%Palette.Length];c.a=n==Selected?1:.38f;for(int i=1;i<values.Length;i++)Line(vh,Point(r,i-1,values),Point(r,i,values),n==Selected?4:2,c);}
            if(Entries.Length>0){var values=Entries[Selected].AssetHistory;if(values!=null&&values.Length>1){float x=Mathf.Lerp(r.xMin,r.xMax,Mathf.Clamp(Round,0,values.Length-1)/(float)(values.Length-1));Line(vh,new Vector2(x,r.yMin),new Vector2(x,r.yMax),2,new Color(1,1,1,.65f));}}
        }
        Vector2 Point(Rect r,int i,int[] v)=>new Vector2(Mathf.Lerp(r.xMin,r.xMax,i/(float)(v.Length-1)),Mathf.Lerp(r.yMin,r.yMax,v[i]/(float)Maximum));
        static void Line(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color){var d=(b-a).normalized;var p=new Vector2(-d.y,d.x)*width*.5f;int start=vh.currentVertCount;foreach(var pos in new[]{a-p,a+p,b+p,b-p}){var v=UIVertex.simpleVert;v.position=pos;v.color=color;vh.AddVert(v);}vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);}
    }
}
