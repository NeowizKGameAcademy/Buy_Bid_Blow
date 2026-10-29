using UnityEngine;
using UnityEngine.UI;

namespace BuyBidBlow.UI.Ticker
{
    public sealed class TickerIcon : TickerGeometry
    {
        [Range(0,2)] public int company;
        // 회사 번호를 바꾸고 메시 갱신 요청
        public void SetCompany(int value){company=value;SetVerticesDirty();}
        // 회사별 아이콘 모양을 선분으로 그려 메시 생성
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=rectTransform.rect;
            Vector2 P(float x,float y)=>new Vector2(r.xMin+x*r.width/100,r.yMin+y*r.height/100);
            void L(float x,float y,float xx,float yy)=>Stroke(vh,P(x,y),P(xx,yy),r.width*.055f,color);
            void Rect(float x,float y,float w,float h){L(x,y,x+w,y);L(x+w,y,x+w,y+h);L(x+w,y+h,x,y+h);L(x,y+h,x,y);}
            if(company==0)
            {
                for(int i=0;i<5;i++){float x=18+i*16;L(x,88,38+i*6,58);L(38+i*6,42,x,12);}
                L(30,54,70,54);L(30,46,70,46);
            }
            else if(company==1)
            {
                void Log(float x,float y)
                {
                    Vector2[] p={P(x,y+6),P(x+6,y),P(x+18,y),P(x+24,y+6),P(x+24,y+18),P(x+18,y+24),P(x+6,y+24),P(x,y+18)};
                    for(int i=0;i<p.Length;i++)Stroke(vh,p[i],p[(i+1)%p.Length],r.width*.045f,color);
                    L(x+8,y+8,x+16,y+16);L(x+6,y+24,x+40,y+40);L(x+24,y+18,x+52,y+32);L(x+24,y+6,x+52,y+20);L(x+52,y+20,x+52,y+32);
                }
                Log(8,12);Log(40,12);Log(24,47);
            }
            else
            {
                Rect(8,12,38,25);Rect(52,12,38,25);Rect(28,43,40,25);
                L(28,68,48,82);L(48,82,88,82);L(88,82,68,68);L(88,82,88,57);L(88,57,68,43);
            }
        }
    }
}
