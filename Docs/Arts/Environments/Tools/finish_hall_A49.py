import ast,bpy,math,random,json,os
from mathutils import Vector,Matrix
from math import sin,cos,pi
src='C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Tools/dress_hall_A49.py'
tree=ast.parse(open(src,encoding='utf-8').read())
S=bpy.context.scene;C=bpy.data.collections['A49_Concept_Furnishings'];BASE='C:/course/unity/BuyBidBlow/Docs/Arts'
wood=bpy.data.materials['BBB_Furniture_Hall_Walnut'];gold=bpy.data.materials['BBB_Column_Brass']
teal=bpy.data.materials['A49_Teal_Velvet'];red=bpy.data.materials['A49_Burgundy_Velvet'];cream=bpy.data.materials['A49_Lamp_Linen'];paper=bpy.data.materials['A49_Book_Pages'];dark=bpy.data.materials['A49_Dark_Bronze']
blue=bpy.data.materials['A49_Book_Navy'];green=bpy.data.materials['A49_Book_Green'];ruby=bpy.data.materials['A49_Book_Red']
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef)],type_ignores=[]),src,'exec'))
random.seed(50)
# Catalog desk in the rear gallery, with one meter behind its chairs.
p=root('Gallery_Catalog_Table',(0,15,.03))
box('Catalog_Top',(0,0,.79),(2.5,.9,.1),wood,p,.04)
box('Catalog_Felt',(0,0,.845),(2.3,.72,.012),teal,p,.012)
box('Catalog_Apron',(0,0,.65),(2.35,.73,.22),wood,p)
for x in [-1.08,1.08]:
 for y in [-.32,.32]:
  box('Catalog_Leg',(x,y,.36),(.12,.12,.72),wood,p)
  box('Catalog_Foot',(x,y,.06),(.13,.13,.12),gold,p)
for x in [-.6,.6]:chair('Catalog_Chair_'+str(x),(x,16.05,.03))
lamp('Catalog_Lamp',(-.83,15,.875))
for j in range(5):book(p,.25+j*.14,.08,.853,.11,.3)
for j in range(3):
 box('Catalog_Stack_Cover',(-.1,-.08,.87+j*.055),(.43,.31,.025),ruby,p,.006)
 box('Catalog_Stack_Paper',(-.1,-.08,.892+j*.055),(.41,.29,.023),paper,p,.004)
for side in [-1,1]:
 table('Lobby_Side_Table_'+str(side),(side*8.8,-10.55,.03),.37,.66)
 lamp('Lobby_Lamp_'+str(side),(side*8.8,-10.55,.69))
 rug('Lobby_Bench_Rug_'+str(side),(side*8.5,-9,.025),2.4,3.7,'15_Burgundy_SimpleRunner')
# Decorative drapes frame the rear artwork, with fabric entirely above the seating furniture.
p=root('Upper_Rear_Drapery',(0,17.03,6.55))
bar('Curtain_Rod',(-2.1,0,2.1),(2.1,0,2.1),.045,gold,p)
for side in [-1,1]:
 vs=[];fs=[];nu,nv=32,24
 for j in range(nv+1):
  t=j/nv;z=2.02*(1-t);width=.66-.27*sin(pi*t);center=side*(1.55+.2*sin(pi*t))
  for i in range(nu+1):
   u=i/nu;x=center+(u-.5)*width;y=-.12+.055*cos(u*10*pi)*(1-.35*sin(pi*t))
   vs.append((x,y,z))
 for j in range(nv):
  for i in range(nu):a=j*(nu+1)+i;fs.append((a,a+1,a+nu+2,a+nu+1))
 o=mesh('Velvet_Drapery',vs,fs,red,p)
 for f in o.data.polygons:f.use_smooth=True
 so=o.modifiers.new('Fabric thickness','SOLIDIFY');so.thickness=.008
 tube('Gold_Tieback',[(side*1.75+.23*cos(2*pi*i/32),-.12+.075*sin(2*pi*i/32),.9) for i in range(33)],.017,gold,p)
 bar('Curtain_Tassel',(side*1.75,-.22,.88),(side*1.75,-.22,.63),.026,gold,p)
box('Curtain_Pelmet',(0,-.08,2.02),(4.1,.15,.24),red,p,.025)
box('Pelmet_Gold_Edge',(0,-.165,1.9),(4.12,.025,.025),gold,p,.006)
# Validate placement-group bounds and keep an editable inventory alongside the scene.
bpy.context.view_layer.update()
inventory=[]
for o in C.objects:
 if o.type=='EMPTY' and not o.parent:
  pts=[ch.matrix_world@Vector(v) for ch in o.children_recursive if ch.type=='MESH' for v in ch.bound_box]
  if not pts:continue
  b=[[round(fn(v[k] for v in pts),3) for k in range(3)] for fn in [min,max]]
  inventory.append({'name':o.name,'bounds':b,'position':[round(v,3) for v in o.location]})
out=BASE+'/Environments/Models/A49_Placement_Inventory.json'
with open(out,'w',encoding='utf-8') as f:json.dump(inventory,f,ensure_ascii=False,indent=2)
print('GROUPS',len(inventory),'OBJECTS',len(C.objects))
print('BELOW_FLOOR',[(i['name'],i['bounds'][0][2]) for i in inventory if i['bounds'][0][2]<-.02])
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=BASE+'/Environments/Models/BBB_Hall_A49_Furnished_Concept.blend')
