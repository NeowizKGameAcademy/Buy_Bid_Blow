from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFont
P=Path(__file__).resolve().parents[1]/'ScenePlan'
a=np.fromfile(P/'unity_triangles.bin',dtype=np.dtype([('tri','<f4',(3,3)),('col','u1',(3,))]));tri=a['tri'];cols=a['col']
normals=np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]);lens=np.linalg.norm(normals,axis=1)
factors=.78+.22*np.abs(normals[:,1])/np.maximum(lens,1e-9)
shaded=(cols*factors[:,None]).astype(np.uint8)
height=tri[:,:,1].mean(1)
flat=(tri[:,:,1].max(1)-tri[:,:,1].min(1))<.005
floor=flat&((np.abs(height)<.005)|(np.abs(height-4)<.005))
shaded[floor]=[231,225,211]
carpet=flat&(((height>.005)&(height<.08))|((height>4.005)&(height<4.08)))&(cols[:,0]==176)
shaded[carpet]=[184,198,186]
BG='#f5f3ed';INK='#253c39'
im=Image.new('RGB',(3800,2400),BG);d=ImageDraw.Draw(im)
def text(x,y,s,size=26,col=INK):d.text((x,y),s,font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',size),fill=col)
text(80,45,'BUY · BID · BLOW  /  실제 씬 인테리어 평면도',48)
text(80,120,'SC_ExchangeFurnished · 저장된 메시의 월드 좌표를 직접 투영 · 2026.10.29',28)
scale=45
def clip(poly,h,above):
    out=[]
    for j,b in enumerate(poly):
        a=poly[j-1];ia=(a[1]>=h) if above else (a[1]<=h);ib=(b[1]>=h) if above else (b[1]<=h)
        if ia!=ib:out.append(a+(b-a)*((h-a[1])/(b[1]-a[1])))
        if ib:out.append(b)
    return out
for panel,(lo,hi,title) in enumerate([(-.5,3.3,'01  아래층 · 라운지 · 우측 로비'),(3.8,6.8,'02  중층 · 경매장')]):
    ox=760 if panel==0 else 3000;oy=1100
    tx=80 if panel==0 else 2300
    text(tx,205,title,34)
    text(tx,260,f'표시 높이 Y = {lo:g} ~ {hi:g} m  |  1 m = 45 px',23)
    def xy(v):return (ox+float(v[0])*scale,oy-float(v[2])*scale)
    # Reference grid outside and behind the actual model.
    xmax=29 if panel==0 else 15
    zmin=-23 if panel==0 else 2
    for x in range(-14,xmax,2):
        xx=ox+x*scale;d.line((xx,oy-18*scale,xx,oy-zmin*scale),fill='#e5e4dc');text(xx-12,oy-zmin*scale+12,str(x),18)
    for z in range(zmin+1,19,2):
        yy=oy-z*scale;d.line((ox-15*scale,yy,ox+xmax*scale,yy),fill='#e5e4dc');text(ox-15*scale-40,yy-10,str(z),18)
    mask=(tri[:,:,1].min(1)<=hi)&(tri[:,:,1].max(1)>=lo)&(np.abs(normals[:,1])>0.00001)
    if panel:mask&=(tri[:,:,2].min(1)>=2)&(tri[:,:,0].max(1)<=14.2)
    ids=np.flatnonzero(mask);ids=ids[np.argsort(tri[ids,:,1].mean(1),kind='stable')]
    shown=0
    for ix in ids:
        v=tri[ix];p=v
        if v[:,1].min()<lo:p=clip(p,lo,True)
        if len(p)<3:continue
        if np.max(np.asarray(p)[:,1])>hi:p=clip(p,hi,False)
        if len(p)<3:continue
        pts=[xy(q) for q in p]
        # Shade by actual face inclination, without adding guessed outlines.
        col=tuple(shaded[ix]);d.polygon(pts,fill=col);shown+=1
    text(tx,2180 if panel==0 else 1070,f'X →  /  Z ↑     투영한 면: {shown:,}',23)
text(1610,1390,'로비',32)
text(650,1190,'중앙 홀',30)
text(660,640,'매칭 공간',28)
text(2300,1240,'실제 배치에서 확인한 차이',34)
for j,s in enumerate(['오른쪽 로비와 연결 출입구 포함','아래층 좌우 라운지의 소파·벤치·테이블','후면 안내·수납 가구, 직원 좌석과 접수대','중층 개별 경매 좌석과 접근제한봉']):
    text(2300,1320+j*65,s,27)
text(2300,1660,'도면 기준',32)
for j,s in enumerate(['Unity가 불러온 활성 메시 581개','가구 프리팹의 위치·회전·축척 변경값 반영','평면 격자 숫자: Unity 월드 좌표(m)','재질 색상 단순화 / 높이 절단으로 천장 제거']):
    text(2300,1730+j*60,s,26)
text(80,2250,'위치·회전·축척: 씬의 Transform 계층 적용  |  윤곽: 실제 메시 삼각형  |  색상: 재질 종류별 단순화',26)
text(80,2300,'Unity에서 씬·프리팹·배치 변경값을 불러와 추출. 천장·상부 벽은 높이로 절단. 미저장·동적 배치 제외.',24)
im.save(P/'Actual_Scene_Interior_Plan.png')
print(P/'Actual_Scene_Interior_Plan.png')
