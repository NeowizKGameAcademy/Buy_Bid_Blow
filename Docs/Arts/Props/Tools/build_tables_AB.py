import bpy,math,os
from math import sin,cos,pi
from mathutils import Vector,Matrix
OUT='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/Tables'
os.makedirs(OUT,exist_ok=True)
s=bpy.data.scenes.new('Round_and_Side_Tables_AB');bpy.context.window.scene=s
source=open('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Tools/build_ornaments_AB.py',encoding='utf-8').read()
exec(source[source.index('def mesh('):source.index('def base(')])
with bpy.data.libraries.load('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/Ornaments/BBB_Ornaments_AB_Materials.blend',link=False) as (a,b):b.materials=['RB_Walnut_BK','Green marble']
wood=b.materials[0];green=b.materials[1];gold=bpy.data.materials['RB_Satin_Gold'].copy();gold.name='Tables_Satin_Brass'
def root(name,loc):
 o=bpy.data.objects.new(name,None);s.collection.objects.link(o);o.location=loc;return o
def assign(o,m):
 o.data.materials.append(m);uv=o.data.uv_layers.new(name='UVMap')
 for p in o.data.polygons:
  axis=max(range(3),key=lambda i:abs(p.normal[i]))
  for li in p.loop_indices:
   v=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=((v.y if axis==0 else v.x)*1.3,(v.y if axis==2 else v.z)*1.3)
 return o
def turn(name,prof,r,m):return assign(lathe(name,prof,r,n=96),m)
def block(name,loc,sz,r,m):return assign(box(name,loc,sz,r),m)
def sweep(name,pts,widths,depth,r,m):
 vs=[]
 for p,w in zip(pts,widths):
  vs.extend([(p[0]+a*w/2,p[1]+b*depth/2,p[2]) for a,b in [(-1,-1),(1,-1),(1,1),(-1,1)]])
 fs=[(i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j) for i in range(len(pts)-1) for j in range(4)]+[(3,2,1,0),tuple((len(pts)-1)*4+j for j in range(4))]
 o=assign(mesh(name,vs,fs,r,False),m);be=o.modifiers.new('Rounded_edges','BEVEL');be.width=.003;be.segments=3;return o
RA=root('Round_Table_A_ROOT',(-1.05,.85,0));RB=root('Round_Table_B_ROOT',(1.05,.85,0));SA=root('Side_Table_A_ROOT',(-.65,-1,0));SB=root('Side_Table_B_ROOT',(.65,-1,0))
turn('RA_Top_Edge',[(.715,.743),(.725,.755),(.755,.755),(.76,.743)],RA,gold)
# Radial veneer wedges preserve directional grain in each sector.
vs=[(0,0,.761)]+[(.741*cos(j*2*pi/96),.741*sin(j*2*pi/96),.761) for j in range(96)]
top=assign(mesh('RA_Radial_Walnut_Top',vs,[(0,j+1,(j+1)%96+1) for j in range(96)],RA,False),wood)
uv=top.data.uv_layers.active
for p in top.data.polygons:
 sector=p.index//6;theta=(sector+.5)*2*pi/16
 for li in p.loop_indices:
  v=top.data.vertices[top.data.loops[li].vertex_index].co;uv.data[li].uv=(v.x*cos(theta)+v.y*sin(theta),(v.y*cos(theta)-v.x*sin(theta))*.55+sector*.13)
turn('RA_Base',[(.008,.31),(.025,.33),(.045,.33),(.06,.31),(.075,.30),(.09,.26),(.105,.25),(.12,.20),(.135,.19),(.15,.17),(.19,.13),(.22,.11)],RA,wood)
for z,r in [(.026,.331),(.11,.24),(.17,.15),(.27,.105),(.53,.11),(.56,.115)]:turn('RA_Brass_Band',[(z,r),(z+.014,r),(z+.018,r-.006)],RA,gold)
prof=[(.22,.095),(.27,.10),(.30,.13),(.35,.175),(.41,.18),(.46,.16),(.50,.115),(.54,.095),(.58,.10),(.62,.13),(.68,.14),(.715,.19)]
body=turn('RA_Fluted_Pedestal',prof,RA,wood)
for v in body.data.vertices:
 if .285<v.co.z<.515:
  theta=math.atan2(v.co.y,v.co.x);fac=1+.065*cos(theta*16);v.co.x*=fac;v.co.y*=fac
for j in range(36):
 t=2*pi*j/36;assign(ell('RA_Base_Bead',(.174*cos(t),.174*sin(t),.145),(.014,.014,.022),RA,n=12,rings=8),gold)
turn('RB_Green_Marble_Top',[(.738,.742),(.745,.75),(.775,.75),(.78,.741)],RB,green)
turn('RB_Brass_Rim',[(.71,.745),(.715,.754),(.738,.754),(.742,.745)],RB,gold)
for j in range(4):
 t=pi/4+j*pi/2;x=.50*cos(t);y=.50*sin(t)
 sweep('RB_Tapered_Leg',[(x,y,.045),(x,y,.58)],[.045,.075],.065,RB,wood)
 block('RB_Capital',(x,y,.645),(.096,.096,.125),RB,wood)
 for z,w,h in [(.03,.057,.035),(.10,.065,.02),(.59,.085,.018),(.70,.103,.018)]:block('RB_Leg_Brass_Collar',(x,y,z),(w,w,h),RB,gold)
 for sign in [-1,1]:block('RB_Capital_Inlay',(x,y+sign*.049,.645),(.075,.004,.096),RB,gold);block('RB_Capital_Panel',(x,y+sign*.052,.645),(.057,.003,.078),RB,wood)
 pts=[(x*k/20,y*k/20,.18+.07*(k/20)**2) for k in range(21)]
 o=sweep('RB_Curved_Stretcher',pts,[.045]*21,.045,RB,wood)
turn('RB_Central_Rosette',[(.155,.06),(.185,.065),(.205,.06)],RB,gold)
turn('SA_Wood_Top',[(.59,.315),(.60,.325),(.64,.325),(.645,.315)],SA,wood)
turn('SA_Brass_Top_Trim',[(.582,.32),(.595,.325),(.602,.325)],SA,gold)
turn('SA_Lower_Shelf',[(.15,.218),(.165,.23),(.185,.23),(.19,.222)],SA,wood)
turn('SA_Shelf_Trim',[(.15,.23),(.165,.232)],SA,gold)
for j in range(3):
 t=-pi/2+j*2*pi/3
 pts=[(.29*cos(t),.29*sin(t),.02),(.20*cos(t),.20*sin(t),.59)]
 sweep('SA_Splayed_Leg',pts,[.035,.053],.045,SA,wood)
 pts=[(.29*cos(t),.29*sin(t),.008),(.281*cos(t),.281*sin(t),.075)]
 sweep('SA_Brass_Foot',pts,[.037,.04],.047,SA,gold)
white=bpy.data.materials.new('Tables_White_Marble_Pending');white.use_nodes=True
turn('SB_White_Marble_Top',[(.60,.315),(.605,.325),(.634,.325),(.64,.316)],SB,white)
turn('SB_Brass_Rim',[(.58,.318),(.585,.326),(.603,.326)],SB,gold)
assign(ring('SB_Lower_Ring',.215,.018,.024,(0,0,.17),Matrix.Identity(3),SB,n=96),gold)
for j in range(3):
 t=-pi/2+j*2*pi/3;pts=[]
 for k in range(25):
  u=k/24;rad=.275-.085*sin(pi*u)+.005*u;pts.append((rad*cos(t),rad*sin(t),.025+.555*u))
 sweep('SB_Curved_Brass_Leg',pts,[.028+.008*abs(2*k/24-1) for k in range(25)],.028,SB,gold)
 block('SB_Foot',pts[0],(.05,.05,.035),SB,gold);block('SB_Leg_Capital',pts[-1],(.055,.055,.035),SB,gold)
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  a.spaces.active.shading.type='MATERIAL';a.spaces.active.overlay.show_overlays=False;a.spaces.active.region_3d.view_location=(0,0,.35);a.spaces.active.region_3d.view_distance=4.7;a.spaces.active.region_3d.view_rotation=Vector((2,-6,4)).to_track_quat('Z','Y')
print('Built four tables',len(s.objects))
