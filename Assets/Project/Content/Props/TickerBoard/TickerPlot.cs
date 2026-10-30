using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow.UI.Ticker
{
    // Batched UI geometry: no per-frame GameObjects or line renderers.
    [ExecuteAlways, RequireComponent(typeof(CanvasRenderer))]
    public abstract class TickerGeometry : MaskableGraphic
    {
        // 두 점 사이에 두께 있는 선분 사각형 추가
        protected static void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var normal = new Vector2(-(b-a).y, (b-a).x).normalized * width * .5f;
            int start = vh.currentVertCount;
            vh.AddVert(a-normal,tint,Vector2.zero); vh.AddVert(a+normal,tint,Vector2.zero);
            vh.AddVert(b+normal,tint,Vector2.zero); vh.AddVert(b-normal,tint,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
        }
        // 지정 위치·크기의 사각형 추가
        protected static void Box(VertexHelper vh, Vector2 p, Vector2 size, Color tint)
        {
            int start=vh.currentVertCount;
            vh.AddVert(p,tint,Vector2.zero); vh.AddVert(p+new Vector2(0,size.y),tint,Vector2.zero);
            vh.AddVert(p+size,tint,Vector2.zero); vh.AddVert(p+new Vector2(size.x,0),tint,Vector2.zero);
            vh.AddTriangle(start,start+1,start+2); vh.AddTriangle(start,start+2,start+3);
        }
    }

    public sealed class TickerPlot : TickerGeometry
    {
        public float[] values = new float[0];
        public bool detailed;
        public float minimum, maximum=1;
        // 차트 값·색을 설정하고 가격 범위 계산 후 메시 갱신
        public void Present(float[] points, Color tint)
        {
            values=points; color=tint;
            TickerFormat.PriceRange(points,out minimum,out maximum);
            SetVerticesDirty();
        }
        // 격자(상세 모드), 가격 선, 최신가 점선·마커 메시 생성
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r=rectTransform.rect;
            if(detailed)
            {
                var grid=new Color(.34f,.38f,.42f,.28f);
                for(int i=0;i<=4;i++)
                {
                    float y=r.yMin+r.height*i/4;
                    Stroke(vh,new Vector2(r.xMin,y),new Vector2(r.xMax,y),1,grid);
                    float x=r.xMin+r.width*i/4;
                    Stroke(vh,new Vector2(x,r.yMin),new Vector2(x,r.yMax),1,grid);
                }
            }
            if(values==null||values.Length==0)return;
            Vector2 Point(int i)=>new Vector2(r.xMin+(values.Length==1?0:(float)i/(values.Length-1))*r.width,
                r.yMin+(values[i]-minimum)/(maximum-minimum)*r.height);
            for(int i=1;i<values.Length;i++)Stroke(vh,Point(i-1),Point(i),detailed?4:3,color);
            var last=Point(values.Length-1);
            if(detailed)
                for(float x=r.xMin;x<r.xMax;x+=16)Stroke(vh,new Vector2(x,last.y),new Vector2(Mathf.Min(x+7,r.xMax),last.y),1,new Color(color.r,color.g,color.b,.5f));
            if(detailed||values.Length==1)Box(vh,last-Vector2.one*4,Vector2.one*8,color);
        }
    }

    public static class TickerFormat
    {
        // 초를 올림해 mm:ss 문자열로 변환
        public static string Clock(double seconds)
        {
            int total=(int)System.Math.Ceiling(System.Math.Max(0,seconds));
            return (total/60).ToString("00")+":"+(total%60).ToString("00");
        }
        // 이전 대비 가격 변동을 부호 포함 문자열로 변환
        public static string Change(int current,int previous)=> (current-previous).ToString("+0;-0;0",System.Globalization.CultureInfo.InvariantCulture);
        // 이전 대비 변동률을 부호 포함 퍼센트 문자열로 변환
        public static string Percent(int current,int previous)=>previous<=0?"—":((current-previous)*100.0/previous).ToString("+0.00;-0.00;0.00",System.Globalization.CultureInfo.InvariantCulture)+"%";
        // 값 범위를 덮는 4칸 눈금의 최소·최대값 계산
        public static void PriceRange(float[] values,out float min,out float max)
        {
            if(values==null||values.Length==0){min=0;max=4;return;}
            float low=values[0],high=low;
            foreach(float value in values){low=Mathf.Min(low,value);high=Mathf.Max(high,value);}
            float step=Mathf.Max(1,Mathf.Ceil((high-low)/3));
            min=Mathf.Max(0,Mathf.Floor(low/step)*step-(low==high?step:0));max=min+4*step;
            while(max<=high){step++;min=Mathf.Max(0,Mathf.Floor(low/step)*step);max=min+4*step;}
        }
    }
}
