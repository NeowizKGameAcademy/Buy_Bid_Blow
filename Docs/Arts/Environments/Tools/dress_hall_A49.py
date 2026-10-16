import bpy, math, random, os, json
from mathutils import Vector, Matrix
from math import sin, cos, pi
random.seed(49)
S=bpy.context.scene
BASE='C:/course/unity/BuyBidBlow/Docs/Arts'
C=bpy.data.collections.new('A49_Concept_Furnishings');S.collection.children.link(C)
def mat(name,color,metal=0,rough=.45,emission=0):
 m=bpy.data.materials.new('A49_'+name);m.use_nodes=True;m.diffuse_color=(*color,1)
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=rough
 if emission:p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emission
 return m
wood=bpy.data.materials['BBB_Furniture_Hall_Walnut'];gold=bpy.data.materials['BBB_Column_Brass']
teal=mat('Teal_Velvet',(.025,.14,.125),rough=.8);red=mat('Burgundy_Velvet',(.22,.025,.042),rough=.82)
cream=mat('Lamp_Linen',(.9,.72,.42),rough=.75,emission=.45)
paper=mat('Book_Pages',(.65,.54,.35),rough=.9);dark=mat('Dark_Bronze',(.055,.036,.02),.65)
blue=mat('Book_Navy',(.025,.045,.1));green=mat('Book_Green',(.025,.09,.045));ruby=mat('Book_Red',(.17,.025,.025))
def root(name,loc,ang=0):
 o=bpy.data.objects.new('A49_'+name,None);C.objects.link(o);o.location=loc;o.rotation_euler.z=ang;o['placement_group']=name;return o
def mesh(name,vs,fs,material,parent=None):
 me=bpy.data.meshes.new('A49_'+name);me.from_pydata(vs,[],fs);me.update()
 o=bpy.data.objects.new('A49_'+name,me);C.objects.link(o)
 if material:me.materials.append(material)
 if parent:o.parent=parent
 return o
def box(name,loc,size,material,parent=None,bevel=.025):
 x,y,z=[v/2 for v in size]
 vs=[(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
 o=mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],material,parent);o.location=loc
 if bevel:
  b=o.modifiers.new('Soft edges','BEVEL');b.width=min(bevel,min(size)/3);b.segments=3
 return o
def lathe(name,loc,profile,material,parent=None,n=32):
 vs=[(r*cos(2*pi*j/n),r*sin(2*pi*j/n),z) for z,r in profile for j in range(n)]
 fs=[(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j) for i in range(len(profile)-1) for j in range(n)]
 fs += [tuple(reversed(range(n))),tuple((len(profile)-1)*n+j for j in range(n))]
 o=mesh(name,vs,fs,material,parent);o.location=loc
 for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
 return o
def bar(name,a,b,r,material,parent):
 a,b=Vector(a),Vector(b);o=lathe(name,a,[(0,r),((b-a).length,r)],material,parent,16);o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return o
def tube(name,points,r,material,parent):
 vs=[];N=10
 for i,p in enumerate(points):
  p=Vector(p);t=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)]);t.normalize()
  u=t.cross(Vector((0,0,1)))
  if u.length<.01:u=t.cross(Vector((0,1,0)))
  u.normalize();v=t.cross(u)
  vs.extend([tuple(p+r*(u*cos(2*pi*j/N)+v*sin(2*pi*j/N))) for j in range(N)])
 fs=[(i*N+j,i*N+(j+1)%N,(i+1)*N+(j+1)%N,(i+1)*N+j) for i in range(len(points)-1) for j in range(N)]
 return mesh(name,vs,fs,material,parent)
def texture(name,path):
 m=mat(name,(1,1,1),rough=.9);p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(path,check_existing=True);m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color']);return m
def panel(name,vs,material,parent):
 o=mesh(name,vs,[(0,1,2,3)],material,parent);uv=o.data.uv_layers.new()
 for i,xy in enumerate([(0,0),(1,0),(1,1),(0,1)]):uv.data[i].uv=xy
 return o
def rug(name,loc,w,d,file,ang=0):
 p=root(name,loc,ang);m=texture(name,BASE+'/Props/CarpetTextures/'+file+'.png')
 o=panel(name,[(-w/2,-d/2,0),(w/2,-d/2,0),(w/2,d/2,0),(-w/2,d/2,0)],m,p)
 b=o.modifiers.new('Carpet thickness','SOLIDIFY');b.thickness=.012
 return p
def lamp(name,loc,parent=None):
 p=root(name,loc)
 if parent:p.parent=parent
 lathe('Lamp_Base',(0,0,0),[(0,.19),(.035,.21),(.06,.15),(.1,.1)],gold,p)
 lathe('Lamp_Stem',(0,0,.1),[(0,.065),(.06,.045),(.2,.08),(.28,.045),(.32,.045)],wood,p)
 lathe('Lamp_Shade',(0,0,.39),[(0,.28),(.02,.28),(.30,.15),(.32,.15)],cream,p,48)
 lathe('Lamp_Finial',(0,0,.71),[(0,.035),(.06,.025),(.08,.005)],gold,p)
 return p
def table(name,loc,r=.8,h=.75):
 p=root(name,loc);lathe('Table_Plinth',(0,0,0),[(0,.42*r),(.06,.48*r),(.12,.32*r),(.18,.17*r),(h-.16,.15*r),(h-.1,.33*r)],wood,p)
 lathe('Table_Gold_Edge',(0,0,h-.09),[(0,r),(.025,r)],gold,p,48)
 lathe('Table_Top',(0,0,h-.065),[(0,r),(.065,r)],wood,p,48);return p
def sofa(name,loc,ang=0,width=2.6,material=teal,bench=False):
 p=root(name,loc,ang)
 for x in [-width/2+.18,width/2-.18]:
  for y in [-.36,.36]:box('Seat_Foot',(x,y,.14),(.12,.12,.28),wood,p);box('Foot_Cap',(x,y,.035),(.13,.13,.07),gold,p)
 box('Seat_Base',(0,0,.33),(width,.95,.19),wood,p,.07)
 count=max(2,round(width/.8))
 for j in range(count):box('Seat_Cushion',(-width/2+(j+.5)*width/count,-.025,.52),(width/count-.035,.87,.22),material,p,.09)
 if not bench:
  box('Sofa_Back',(0,.39,.88),(width,.24,.85),material,p,.1)
  for x in [-width/2+.07,width/2-.07]:box('Sofa_Arm',(x,0,.7),(.2,.98,.48),material,p,.09)
  for x in [-width*.32,width*.32]:
   o=box('Loose_Cushion',(x,.19,.83),(.44,.18,.43),red if material==teal else teal,p,.075);o.rotation_euler.y=.14 if x>0 else -.14
  for j in range(count*2):lathe('Upholstery_Button',(-width*.43+j*width*.86/(count*2-1),.257,.98),[(0,.018),(.015,.018)],gold,p,12).rotation_euler.x=pi/2
 return p
def book(parent,x,y,z,w=.12,h=.34,material=None):
 m=material or random.choice([green,ruby,blue,teal]);box('Book_Pages',(x,y,z+h/2),(w-.02,.25,h-.035),paper,parent,.004)
 for dx in [-w/2,w/2]:box('Book_Cover',(x+dx,y,z+h/2),(.012,.29,h),m,parent,.003)
 box('Book_Spine',(x,y-.145,z+h/2),(w,.025,h),m,parent,.006)
 for zz in [z+.055,z+h-.055]:box('Spine_Gilt',(x,y-.161,zz),(w*.8,.007,.012),gold,parent,.001)
def bookcase(name,loc,ang=0,w=1.65):
 p=root(name,loc,ang);box('Case_Back',(0,.23,1.25),(w,.08,2.5),wood,p)
 for x in [-w/2,w/2]:box('Case_Side',(x,0,1.25),(.12,.58,2.5),wood,p)
 for z in [.08,.64,1.2,1.76,2.32,2.52]:box('Case_Shelf',(0,0,z),(w+.16,.65,.09),wood,p)
 box('Case_Cornice',(0,-.015,2.58),(w+.28,.71,.1),wood,p);box('Cornice_Gold',(0,-.38,2.58),(w+.23,.018,.025),gold,p)
 for row,z in enumerate([.13,.69,1.25,1.81]):
  for j in range(9):book(p,-w/2+.15+j*(w-.3)/9,-.04,z,.115,random.uniform(.32,.43))
 return p
def frame(name,loc,ang,file,w=2,h=None):
 p=root(name,loc,ang);m=texture(name,BASE+'/Props/PaintingTextures/'+file+'.png')
 img=next(n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE');h=h or w*img.size[1]/img.size[0]
 panel('Painting',[(-w/2,-.075,-h/2),(w/2,-.075,-h/2),(w/2,-.075,h/2),(-w/2,-.075,h/2)],m,p)
 for thick,depth,extra,ma in [(.13,.14,.08,wood),(.035,.19,.025,gold),(.035,.16,.18,gold)]:
  for x in [-1,1]:box('Frame_Vertical',(x*(w/2+extra),-.045,0),(thick,depth,h+2*extra+thick),ma,p,.012)
  for z in [-1,1]:box('Frame_Horizontal',(0,-.045,z*(h/2+extra)),(w+2*extra+thick,depth,thick),ma,p,.012)
 box('Picture_Label',(w*.27,-.1,-h/2-.27),(.32,.035,.09),gold,p,.006)
 for x in [-w*.28,w*.28]:bar('Picture_Light_Arm',(x,0,h/2+.29),(x,-.32,h/2+.29),.018,gold,p)
 box('Picture_Light_Hood',(0,-.33,h/2+.29),(w*.7,.16,.09),gold,p,.035)
 box('Picture_Light_Diffuser',(0,-.34,h/2+.244),(w*.65,.12,.012),cream,p,.005)
 return p
def wallpoint(deg,inset=.4):
 a=math.radians(deg);lo,hi=0,40
 for _ in range(45):
  r=(lo+hi)/2;x=r*cos(a);y=-2.5+r*sin(a);qy=max(y-5.5,0) if y>=0 else max(-y-13.5,0);ry=12 if y>=0 else 9
  if (max(abs(x)-2,0)/12)**2+(qy/ry)**2<=1:lo=r
  else:hi=r
 x=(lo-inset)*cos(a);y=-2.5+(lo-inset)*sin(a)
 # true inward surface normal for rounded capsule wall
 nx=-(max(abs(x)-2,0)/144)*(1 if x>=0 else -1);ny=-(max(y-5.5,0)/144) if y>=0 else max(-y-13.5,0)/81
 if abs(ny)+abs(nx)<.001:nx=-cos(a);ny=-sin(a)
 return x,y,math.atan2(nx,-ny)
def clone_group(source,name,loc,scale=1,ang=0):
 src=bpy.data.objects[source];p=root(name,loc,ang);p.scale=(scale,)*3;inv=src.matrix_world.inverted()
 for o in [src]+list(src.children_recursive):
  if o.type not in ['MESH','CURVE']:continue
  n=o.copy();C.objects.link(n);n.name='A49_'+name+'_'+o.name;n.parent=p;n.matrix_parent_inverse=Matrix.Identity(4);n.matrix_basis=inv@o.matrix_world;n.hide_viewport=False;n.hide_render=False;n.hide_set(False)
 return p
def chair(name,loc,ang=0):return clone_group('A41_Front_Seat_1',name,loc,1,ang)
def sign(name,loc,ang=0,text='AUCTION PREVIEW',direction=False):
 p=root(name,loc,ang);lathe('Sign_Base',(0,0,0),[(0,.3),(.06,.33),(.1,.19)],gold,p)
 bar('Sign_Post',(0,0,.08),(0,0,1.35),.04,gold,p)
 if direction:
  for z,label,sgn in [(1.5,'GALLERY',-1),(1.85,'AUCTION',1)]:
   vs=[(-.6,-.04,z-.14),(.6,-.04,z-.14),(.78,-.04,z),(.6,-.04,z+.14),(-.6,-.04,z+.14)]
   if sgn<0:vs=[(-x,y,zz) for x,y,zz in reversed(vs)]
   mesh('Arrow_Plaque',vs,[tuple(range(5))],teal,p);label_text(p,label,(0,-.055,z-.055),.10)
 else:
  box('Sign_Frame',(0,0,1.35),(1.03,.1,.66),gold,p)
  box('Sign_Panel',(0,-.057,1.35),(.95,.025,.58),teal,p)
  label_text(p,text,(0,-.074,1.37),.1);label_text(p,'BUY  BID  BLOW',(0,-.075,1.17),.055)
 return p
def label_text(parent,text,loc,size):
 cu=bpy.data.curves.new('A49_SignText','FONT');cu.body=text;cu.size=size
 vals=[i.identifier for i in cu.bl_rna.properties['align_x'].enum_items];cu.align_x=next(v for v in vals if v=='CENTER')
 o=bpy.data.objects.new('A49_'+text,cu);C.objects.link(o);o.parent=parent;o.location=loc;o.rotation_euler.x=pi/2;cu.materials.append(gold)
def barrier(name,loc,w=2.6,ang=0):
 p=root(name,loc,ang)
 for x in [-w/2,w/2]:lathe('Stanchion',(x,0,0),[(0,.22),(.045,.25),(.09,.12),(.14,.05),(.85,.035),(.9,.08),(.97,.06),(1,.005)],gold,p,24)
 tube('Velvet_Rope',[(w*(t/24-.5),0,.89-.19*sin(pi*t/24)) for t in range(25)],.035,red,p);return p
def ornament(name,loc):
 p=root(name,loc);lathe('Ornament_Base',(0,0,0),[(0,.18),(.07,.18),(.11,.12),(.32,.04)],gold,p)
 for j in range(3):
  pts=[]
  for t in range(65):
   a=2*pi*t/64;v=Vector((.22*cos(a),0,.22*sin(a)));v=Matrix.Rotation(j*pi/3,3,'Z')@v;pts.append((v.x,v.y,v.z+.52))
  tube('Armillary_Ring',pts,.012,gold,p)
 lathe('Globe',(0,0,.52),[(-.1,0),(-.08,.06),(0,.1),(.08,.06),(.1,0)],gold,p)
 return p
def trolley(name,loc,ang=0):
 p=root(name,loc,ang)
 for z in [.18,.67]:
  box('Trolley_Tray',(0,0,z),(.85,.5,.06),wood,p)
  for y in [-.24,.24]:bar('Tray_Rail',(-.41,y,z+.11),(.41,y,z+.11),.018,gold,p)
 for x in [-.36,.36]:
  for y in [-.2,.2]:
   bar('Trolley_Post',(x,y,.08),(x,y,.82),.025,gold,p)
   o=lathe('Caster',(x,y-.035,.09),[(0,.075),(.07,.075)],dark,p,20);o.rotation_euler.x=pi/2
 bar('Push_Handle',(.36,-.2,.82),(.36,.2,.82),.028,gold,p);return p

# Existing carpets were already placed during the interrupted turn. Preserve and raise them above floor trim.
for o in bpy.data.collections['A48_Carpets'].objects:o.location.z=.025 if o.location.z<1 else 4.025
# Under-mezzanine gallery: a generous center aisle and two lounge islands.
rug('Lower_West_Lounge_Rug',(-7,10,.025),4.7,4.6,'02_Teal_Floral')
rug('Lower_East_Lounge_Rug',(7,10,.025),4.7,4.6,'09_Ivory_Burgundy')
for side in [-1,1]:
 x=side*7
 sofa('Lower_Sofa_'+str(side),(x,11.65,.03),material=teal if side<0 else red)
 table('Lower_Coffee_Table_'+str(side),(x,9.95,.03),.72,.52)
 chair('Lower_Chair_'+str(side),(x-side*1.6,9.1,.03),-side*.9)
 table('Lower_Side_Table_'+str(side),(x+side*1.85,11.55,.03),.36,.68)
 lamp('Lower_Table_Lamp_'+str(side),(x+side*1.85,11.55,.71))
 clone_group('BBB_Areca_Card_Planter_ROOT','Lower_Palm_'+str(side),(side*10.15,11.5,.03),.6)
 trolley('Lower_Service_Trolley_'+str(side),(side*4.4,13.2,.03))
 sofa('Lobby_Bench_'+str(side),(side*9,-9,.03),ang=side*pi/2,width=2,material=red,bench=True)
rug('Lower_Exhibition_Rug',(0,12,.025),4.2,3.7,'03_Ivory_Museum')
clone_group('02_Lot_Pedestal_ROOT','Lower_Exhibit_Pedestal',(0,12,.03),1.2)
ornament('Lower_Armillary',(0,12,1.23))
barrier('Exhibit_Front_Rope',(0,10.65,.03),2.8)
barrier('Exhibit_Left_Rope',(-1.4,12,.03),2.7,pi/2)
barrier('Exhibit_Right_Rope',(1.4,12,.03),2.7,pi/2)
sign('Gallery_Info',(2.3,10.5,.03),-.25)
sign('Entry_Directions',(11,-8.25,.03),-pi/2,direction=True)
sign('Stairs_Directions',(-3.0,3.4,.03),0,direction=True)
# Upper side wings keep x=-4..4 open for rostrum and the existing seating.
for side in [-1,1]:
 x=side*8
 rug('Upper_Lounge_Rug_'+str(side),(x,10,4.025),4.6,4.2,'05_Library_Persian')
 sofa('Upper_Sofa_'+str(side),(x,11.5,4.03),material=red if side<0 else teal,width=2.4)
 table('Upper_Round_Table_'+str(side),(x,9.65,4.03),.7,.74)
 chair('Upper_Chair_'+str(side),(x,8.35,4.03),pi)
 table('Upper_Side_Table_'+str(side),(x+side*1.65,11.3,4.03),.36,.65)
 lamp('Upper_Lamp_'+str(side),(x+side*1.65,11.3,4.68))
 ornament('Upper_Armillary_'+str(side),(x,9.65,4.77))
 clone_group('BBB_Areca_Card_Planter_ROOT','Upper_Palm_'+str(side),(side*11.7,7.5,4.03),.55)
 trolley('Upper_Trolley_'+str(side),(side*5.2,8.2,4.03),side*.2)
 sign('Upper_Info_'+str(side),(side*4.9,6.5,4.03),0,'READING LOUNGE')
 for deg in ([49,61] if side>0 else [119,131]):
  xw,yw,a=wallpoint(deg,.72);bookcase('Upper_Bookcase_'+str(deg),(xw,yw,4.03),a,1.7)
# Wall artwork: high enough to clear lower furniture; upper art sits above shelving and seats.
for i,deg in enumerate([32,49,65,90,115,131,148]):
 x,y,a=wallpoint(deg,.32);frame('Lower_Painting_'+str(i),(x,y,2.35),a,['01_River','07_Wolf_Portrait','02_Mountain','04_Palace','08_Fox_Portrait','03_Forest','06_Harbor'][i],1.7,1.45)
for i,deg in enumerate([34,75,90,105,146]):
 x,y,a=wallpoint(deg,.35);frame('Upper_Painting_'+str(i),(x,y,7.25),a,['12_Botanical','10_Horse','14_Constellations','11_Stag','13_Butterflies'][i],1.8,1.65)
for x,ang in [(-13.65,pi/2),(13.65,-pi/2)]:frame('Lobby_Painting_'+str(x),(x,-10,2.5),ang,'05_Garden' if x<0 else '06_Harbor',2.1,1.5)
# Small decorative wall shelves under the gallery pictures, away from columns.
for deg in [65,115]:
 x,y,a=wallpoint(deg,.48);p=root('Lower_Display_Shelf_'+str(deg),(x,y,.03),a)
 for z in [.4,.95]:box('Display_Shelf',(0,-.12,z),(1.6,.52,.08),wood,p)
 for xx in [-.68,.68]:box('Shelf_Support',(xx,0,.48),(.07,.08,.9),gold,p)
 ornament('Shelf_Armillary',(0,-.1,.99)).parent=p
 for j in range(7):book(p,-.55+j*.16,-.12,.44,.12,.32)
S.name='A49_Furnished_Concept_Layout'
S['A49_notes']='Concept placement pass: lower gallery lounges; upper library lounges; original seating/wolf/ceiling preserved. Built from existing assets plus editable concept furniture.'
bpy.context.view_layer.update()
print('A49_CREATED',len(C.objects),'objects',len([o for o in C.objects if o.type=='EMPTY']),'placement groups')
