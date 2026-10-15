"""Reference furniture geometry. Metric, -Y fronts, per-asset roots at floor level.
No tiered seating assembly; seven independent reusable assets.
"""
import bpy, math, bmesh, random
from mathutils import Vector
OUT='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Auction_Exchange_Furniture_Geometry.blend'
S=bpy.data.scenes.new('BBB_Auction_Exchange_Furniture_Geometry');bpy.context.window.scene=S
S.unit_settings.system='METRIC'
COL=None;ROOT=None
colors={'Wood':(.29,.12,.045,1),'Edge':(.46,.23,.09,1),'DarkWood':(.15,.055,.022,1),'Brass':(.68,.39,.09,1),'Red':(.34,.035,.045,1),'Teal':(.025,.13,.09,1),'Screen':(.012,.055,.028,1),'Green':(.12,.65,.2,1),'DimGreen':(.09,.27,.12,1),'Leaf':(.17,.32,.035,1),'LeafLight':(.27,.43,.07,1),'Soil':(.065,.035,.014,1),'Pot':(.46,.20,.09,1),'Iron':(.12,.11,.085,1),'Light':(.95,.72,.25,1),'Paper':(.68,.64,.49,1)}
M={}
for k,c in colors.items():
 m=bpy.data.materials.new('Furniture_Geo_'+k);m.diffuse_color=c;M[k]=m

def asset(name,loc):
 global COL,ROOT
 COL=bpy.data.collections.new(name);S.collection.children.link(COL)
 ROOT=bpy.data.objects.new(name+'_ROOT',None);COL.objects.link(ROOT);ROOT.location=loc
 ROOT['units']='meters';ROOT['front']='-Y';ROOT['reference']='02_Auction_Exchange_Furniture.png';return ROOT

def mesh(name,vs,fs,mat='Wood',bevel=.006):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(ROOT.name.replace('_ROOT','')+'_'+name,me);COL.objects.link(o);o.parent=ROOT;me.materials.append(M[mat])
 if bevel:
  mod=o.modifiers.new('Crafted edge','BEVEL');mod.width=bevel;mod.segments=2
 # Geometry-stage planar UVs, suitable for later material work.
 uv=me.uv_layers.new(name='GeometryUV')
 for p in me.polygons:
  axis=max(range(3),key=lambda i:abs(p.normal[i]));axes=[i for i in range(3) if i!=axis]
  for li in p.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(co[axes[0]],co[axes[1]])
 return o

def box(name,c,d,mat='Wood',bevel=.006):
 vs=[(x*d[0]/2,y*d[1]/2,z*d[2]/2) for x,y,z in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
 o=mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat,bevel);o.location=c;return o

def beam(name,a,b,w,d=None,mat='Wood'):
 a=Vector(a);b=Vector(b);o=box(name,(0,0,0),(w,d or w,(b-a).length),mat,min(.005,w*.1));o.location=(a+b)/2;o.rotation_mode='QUATERNION';o.rotation_quaternion=(b-a).to_track_quat('Z','Y');return o

def cylinder(name,c,r,h,mat='Brass',n=16,r2=None,axis=None):
 r2=r if r2 is None else r2
 vs=[(r0*math.cos(2*math.pi*i/n),r0*math.sin(2*math.pi*i/n),z) for z,r0 in [(-h/2,r),(h/2,r2)] for i in range(n)]
 fs=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
 o=mesh(name,vs,fs,mat,min(.004,h*.15));o.location=c
 if axis:o.rotation_mode='QUATERNION';o.rotation_quaternion=Vector(axis).to_track_quat('Z','Y')
 return o

def ball(name,c,r,mat='Brass'):
 n=12;vs=[(c[0]+r*math.sin(math.pi*j/6)*math.cos(i*2*math.pi/n),c[1]+r*math.sin(math.pi*j/6)*math.sin(i*2*math.pi/n),c[2]+r*math.cos(math.pi*j/6)) for j in range(7) for i in range(n)]
 return mesh(name,vs,[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(6) for i in range(n)],mat,0)

def path(name,pts,r=.012,mat='Brass'):
 for i,(a,b) in enumerate(zip(pts,pts[1:])):cylinder(name+str(i),(Vector(a)+Vector(b))/2,r,(Vector(b)-Vector(a)).length,mat,12,axis=Vector(b)-Vector(a))

def shade(name,c,r=.10,h=.12):
 # Open, double-wall tapered shade with visible rim, rather than a solid cone.
 n=16;vs=[]
 for rr,z in [(r,0),(r*.48,h),(r*.48-.008,h),(r-.008,0)]:
  vs += [(c[0]+rr*math.cos(i*2*math.pi/n),c[1]+rr*math.sin(i*2*math.pi/n),c[2]+z) for i in range(n)]
 fs=[(j*n+i,j*n+(i+1)%n,((j+1)%4)*n+(i+1)%n,((j+1)%4)*n+i) for j in range(4) for i in range(n)]
 mesh(name,vs,fs,'Brass',.002);ball(name+'_Bulb',(c[0],c[1],c[2]+.025),r*.40,'Light')

def desk_lamp(x,y,z):
 cylinder('Lamp_Foot',(x,y,z+.018),.075,.036)
 cylinder('Lamp_Stem',(x,y,z+.13),.014,.23)
 shade('Lamp_Shade',(x,y,z+.24),.115,.13)

def banner(name,y,z,w,h,mat,emblem):
 def flag(n,ww,hh,yy,zz,m):
  pts=[(-ww/2,zz),(ww/2,zz),(ww/2,zz-hh*.76),(0,zz-hh),(-ww/2,zz-hh*.76)]
  vs=[(x,yyy,zz0) for yyy in [yy,yy+.014] for x,zz0 in pts]
  mesh(n,vs,[(4,3,2,1,0),(5,6,7,8,9)]+[(i,(i+1)%5,(i+1)%5+5,i+5) for i in range(5)],m,.002)
 flag(name+'_Border',w,h,y,z,'Brass');flag(name+'_Cloth',w-.065,h-.065,y-.017,z-.018,mat)
 if emblem=='brick':
  for x,zz in [(-w*.17,z-h*.51),(w*.17,z-h*.51),(0,z-h*.31)]:box('Brick_Emblem',(x,y-.032,zz),(w*.30,.014,h*.16),'Brass',.007)
 else:
  for x,zz in [(-w*.15,z-h*.49),(w*.15,z-h*.49),(0,z-h*.29)]:
   cylinder('Log_Emblem',(x,y-.041,zz),w*.135,.026,'Brass',16,axis=(0,1,0));cylinder('Log_Pith',(x-.012,y-.058,zz),w*.025,.008,'DarkWood',12,axis=(0,1,0))

# 01: one podium; back reference becomes the open rear of this same object.
asset('01_Auction_Rostrum',(-3.7,1.4,0))
box('Plinth',(0,0,.09),(1.15,.83,.18),'Wood',.016);box('Plinth_Inlay',(0,0,.19),(1.09,.77,.04),'Brass');box('Plinth_Red',(0,0,.217),(1.03,.71,.018),'Red')
for x in [-.43,.43]:
 for y in [-.26,.26]:
  box('Post',(x,y,.70),(.13,.13,.96));box('Foot_Collar',(x,y,.27),(.17,.17,.09),'Brass');box('Capital',(x,y,1.11),(.18,.18,.09),'Edge')
for i in range(7):box('Front_Board',(-.35+i*.116,-.29,.70),(.111,.065,.83))
for x in [-.43,.43]:box('Side_Panel',(x,0,.69),(.06,.49,.82),'DarkWood')
box('Rear_Shelf',(0,.035,.60),(.77,.52,.045));box('Lower_Shelf',(0,.015,.30),(.77,.52,.045))
for i in range(3):
 box('Book_Pages',(-.19+i*.17,.09,.644),(.15,.23,.04),'Paper');box('Book_Cover',(-.19+i*.17,.09,.67),(.16,.24,.012),'Teal')
# slope falls toward the auctioneer at +Y
slope=-.12
for name,c,d,mat in [('Sloped_Desk',(0,0,1.20),(1.12,.82,.09),'Edge'),('Writing_Felt',(0,0,1.254),(.95,.65,.018),'Red')]:
 o=box(name,c,d,mat);o.rotation_euler.x=slope
for x in [-.535,.535]:
 o=box('Side_Lip',(x,0,1.29),(.055,.83,.08),'Edge');o.rotation_euler.x=slope
box('Front_Lip',(0,-.387,1.337),(1.1,.05,.055),'Edge')
banner('Banner',-.338,1.15,.62,.78,'Red','brick')
cylinder('Gavel_Block',(.28,-.08,1.31),.10,.045,'DarkWood',24)
beam('Gavel_Handle',(.23,-.02,1.345),(-.10,.13,1.345),.032,mat='Wood')
cylinder('Gavel_Head',(-.12,.14,1.38),.065,.17,'DarkWood',16,axis=(1,0,0))
for x in [-.18,-.06]:cylinder('Gavel_Band',(x,.14,1.38),.07,.018,'Brass',16,axis=(1,0,0))
ROOT['design_notes']='Single podium; open rear shelves, sloped writing top, stable base.'

# 02: pedestal, cushion, securely attached articulated lamp.
asset('02_Lot_Pedestal',(-1.7,1.4,0))
box('Base',(0,0,.07),(.94,.82,.14),'Wood',.015)
for i in range(6):box('Front_Board',(-.32+i*.128,-.30,.59),(.122,.07,.93))
box('Back',(0,.30,.59),(.76,.07,.93));box('Left',(-.36,0,.59),(.07,.56,.93));box('Right',(.36,0,.59),(.07,.56,.93))
for x in [-.39,.39]:
 for y in [-.31,.31]:
  box('Post',(x,y,.60),(.12,.12,1.04),'Edge');box('Corner_Cap',(x,y,1.10),(.15,.15,.11),'Brass')
box('Top',(0,0,1.08),(.94,.82,.13),'Edge');box('Velvet_Cushion',(0,-.015,1.17),(.68,.59,.095),'Red',.03)
banner('Banner',-.38,1.115,.56,.74,'Teal','logs')
x,y=.31,.27
cylinder('Lamp_Mount',(x,y,1.17),.075,.075);cylinder('Lamp_Upright',(x,y,1.48),.019,.58)
ball('Lamp_Joint',(x,y,1.77),.042);path('Lamp_Arm',[(x,y,1.77),(-.17,y,1.84),(-.20,y,1.75)],.017)
shade('Spot_Shade',(-.20,y,1.61),.14,.17)

# 03: wall-mounted monitor with an actual rear casing and mounting rails.
asset('03_Market_Monitor',(1.2,1.65,0))
box('Housing',(0,0,1.0),(2.65,.20,1.72),'DarkWood',.018)
box('Display',(0,-.112,1.0),(2.36,.028,1.40),'Screen',.006)
for x in [-1.27,1.27]:
 box('Upright',(x,-.07,1.0),(.15,.25,1.76),'Edge')
 for z in [.16,1.84]:box('Corner_Cap',(x,-.08,z),(.22,.27,.19),'Brass',.014)
for z in [.20,1.80]:box('Crossbar',(0,-.065,z),(2.60,.23,.13),'Edge')
for i in range(9):box('Rear_Plank',(0,.122,.28+i*.18),(2.45,.035,.172))
for x in [-.81,.81]:
 box('Rear_Strap',(x,.16,1),(.07,.035,1.57),'Iron')
 for z in [.30,.7,1.3,1.7]:cylinder('Strap_Rivet',(x,.185,z),.013,.016,'Brass',12,axis=(0,1,0))
 box('Wall_Mount',(x,.21,1.57),(.22,.08,.14),'Iron')
box('Electronics_Box',(0,.22,.56),(.36,.14,.36),'Iron')
path('Rear_Cable',[(.03,.24,.38),(.03,.26,.25),(.17,.26,.22),(.22,.23,.36)],.012,'Iron')
for x in [-.85,0,.85]:
 path('Gooseneck',[(x,.04,1.82),(x,.04,2.08),(x,-.05,2.18),(x,-.22,2.18),(x,-.31,2.08),(x,-.31,2.01)],.016)
 shade('Display_Lamp',(x,-.31,1.88),.105,.13)
def line(a,b,w=.008,mat='DimGreen'):beam('Display_Line',(a[0],-.134,a[1]),(b[0],-.134,b[1]),w,.008,mat)
for a,b in [((-1.10,.93),(1.10,.93)),((.25,.94),(.25,1.65)),((-1.1,.30),(-1.1,1.66)),((-1.1,1.66),(1.1,1.66))]:line(a,b)
chart=[(-1.02,1.02),(-.92,1.18),(-.84,1.10),(-.72,1.27),(-.63,1.15),(-.52,1.31),(-.42,1.28),(-.3,1.42),(-.17,1.19),(-.04,1.40),(.06,1.34),(.17,1.56)]
for a,b in zip(chart,chart[1:]):line(a,b,.018,'Green')
for j in range(4):
 z=1.54-j*.155;box('Quote_Bar',(.81,-.137,z),(.43,.01,.072),'Green' if j==0 else 'DimGreen',0)
 pts=[(.40,-.145,z-.05),(.58,-.145,z-.05),(.49,-.145,z+.055)]
 if j%2:pts=[(x,y,2*z-zz) for x,y,zz in pts]
 mesh('Trend_Arrow',pts,[(0,1,2)],'Green' if j%2==0 else 'Red',0)
for row in range(5):
 for j in range(5):box('Quote_Cell',(-.88+j*.43,-.137,.82-row*.105),(.26 if j!=2 else .10,.01,.048),'Green' if (row+j)%3==0 else 'DimGreen',0)
ROOT['mounting']='Wall-mounted; two rear brackets; use rear +Y attachment plane.'

# 04A: usable two-place desk, clear knee space at rear +Y.
asset('04A_Single_Desk',(-2.5,-1.4,0))
box('Desk_Top',(0,0,.755),(2.4,.74,.09),'Edge',.015);box('Desk_Felt',(0,0,.806),(2.20,.55,.018),'Red',.009)
for y in [-.34,.34]:box('Top_Inlay',(0,y,.804),(2.30,.025,.01),'Brass',.002)
for x in [-1.09,1.09]:
 for y in [-.25,.25]:box('Leg',(x,y,.375),(.11,.11,.75));box('Foot',(x,y,.06),(.15,.15,.12),'Edge')
box('Front_Backboard',(0,-.27,.39),(2.12,.055,.57),'DarkWood')
for z in [.13,.65]:box('Front_Rail',(0,-.31,z),(2.20,.09,.09),'Edge')
for x in [-1.09,0,1.09]:box('Front_Post',(x,-.31,.39),(.095,.10,.59),'Edge')
for cx in [-.55,.55]:
 beam('X_Brace',(cx-.43,-.35,.19),(cx+.43,-.35,.60),.065,.045)
 beam('X_Brace',(cx-.43,-.37,.60),(cx+.43,-.37,.19),.065,.045)
for x in [-1.09,1.09]:box('Side_Apron',(x,0,.64),(.09,.50,.12))
desk_lamp(.87,-.13,.815)
ROOT['design_notes']='0.80m top; open rear; no middle rear leg or knee-level shelf; two seats.'

# 04B: four independent legs and structural stretchers, 0.46m seat.
asset('04B_Single_Chair',(-.45,-1.55,0))
for x in [-.205,.205]:
 for y in [-.205,.205]:
  box('Leg',(x,y,.225),(.075,.075,.45),'Wood')
 for z in [.18,.39]:box('Side_Stretcher',(x,0,z),(.055,.43,.055))
box('Front_Apron',(0,-.205,.385),(.42,.06,.09));box('Rear_Stretcher',(0,.205,.19),(.42,.06,.06))
box('Seat_Frame',(0,0,.423),(.51,.51,.065),'Edge');box('Seat_Cushion',(0,-.012,.474),(.455,.46,.06),'Red',.022)
for x in [-.205,.205]:
 beam('Back_Post',(x,.205,.40),(x,.27,1.05),.065)
 cylinder('Finial_Seat',(x,.27,1.061),.039,.03);ball('Finial',(x,.27,1.10),.036)
box('Back_Top',(0,.26,1.016),(.41,.075,.066),'Edge')
o=box('Back_Frame',(0,.241,.84),(.39,.075,.31),'Edge');o.rotation_euler.x=-.10
o=box('Back_Pad',(0,.195,.84),(.325,.055,.265),'Red',.018);o.rotation_euler.x=-.10
ROOT['design_notes']='Four legs, side stretchers, 0.50m padded seat height; slightly reclined supported back.'

# 05: hollow containers, soil below lip, faceted thick leaves rooted in soil.
def plant(cx,cy,z,scale,seed):
 rng=random.Random(seed)
 for i in range(17):
  a=i*2.39996;length=scale*rng.uniform(.65,1.2);out=scale*(.25 if i<4 else rng.uniform(.45,.85));high=length*(1.25 if i<4 else .72)
  start=Vector((cx+rng.uniform(-.09,.09)*scale,cy+rng.uniform(-.09,.09)*scale,z-.04));end=start+Vector((out*math.cos(a),out*math.sin(a),high))
  mid=start.lerp(end,.58)+Vector((0,0,scale*.13));side=Vector((-math.sin(a),math.cos(a),0))*scale*rng.uniform(.10,.18)
  vs=[start,mid-side,end,mid+side,mid+Vector((0,0,.045*scale))]
  mesh('Leaf_%02d'%i,vs,[(0,1,4),(1,2,4),(2,3,4),(3,0,4)],'LeafLight' if i%3==0 else 'Leaf',0)
  beam('Leaf_Stem',start,start.lerp(end,.48),.016*scale,mat='Leaf')

def pot():
 n=10;vs=[]
 for r,z in [(.23,0),(.34,.57),(.38,.60),(.38,.69),(.31,.69),(.29,.57),(.20,.10)]:vs.extend([(r*math.cos(i*2*math.pi/n),r*math.sin(i*2*math.pi/n),z) for i in range(n)])
 fs=[tuple(reversed(range(n))),tuple(range(6*n,7*n))]+[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(6) for i in range(n)]
 mesh('Hollow_Pot',vs,fs,'Pot',.005);cylinder('Soil',(0,0,.59),.29,.025,'Soil',10)
asset('05A_Medium_Planter',(1.1,-1.55,0));pot();plant(0,0,.61,.73,45)
asset('05B_Large_Planter',(2.8,-1.3,0))
box('Bottom',(0,0,.10),(.82,.82,.12),'DarkWood')
for side in [-1,1]:
 for i in range(5):
  box('Front_Back_Plank',(-.32+i*.16,side*.40,.49),(.154,.075,.68))
  box('Side_Plank',(side*.40,-.32+i*.16,.49),(.075,.154,.68))
 for z in [.16,.82]:
  box('Rim_Front_Back',(0,side*.42,z),(.99,.13,.13),'Edge');box('Rim_Side',(side*.42,0,z),(.13,.73,.13),'Edge')
 for x in [-.40,.40]:box('Corner_Post',(x,side*.40,.43),(.12,.12,.86),'Edge')
 beam('Front_X',(-.32,side*.455,.23),(.32,side*.455,.75),.09,.045)
 beam('Front_X',(-.32,side*.48,.75),(.32,side*.48,.23),.09,.045)
 beam('Side_X',(side*.455,-.32,.23),(side*.455,.32,.75),.09,.045)
 beam('Side_X',(side*.48,-.32,.75),(side*.48,.32,.23),.09,.045)
box('Soil',(0,0,.735),(.71,.71,.055),'Soil');plant(0,0,.77,1.15,88)
# Review view: no hall geometry is included in saved asset library.
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  sp=a.spaces.active;sp.shading.type='SOLID';sp.shading.color_type='MATERIAL';sp.overlay.show_overlays=False
  r=sp.region_3d;eye=Vector((7,-14,10));target=Vector((-.4,.1,.8));r.view_rotation=(eye-target).to_track_quat('Z','Y');r.view_location=target;r.view_distance=7.5;r.view_perspective='ORTHO'
bpy.context.view_layer.update()
S['scope']='01 rostrum, 02 pedestal, 03 monitor, 04 individual desk/chair, 05 two planters. No tier assembly.'
bpy.data.libraries.write(OUT,{S},fake_user=True)
print('SAVED',OUT,'objects',len(S.objects),'meshes',sum(o.type=='MESH' for o in S.objects))

