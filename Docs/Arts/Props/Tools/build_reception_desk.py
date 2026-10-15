import bpy,math,bmesh
from mathutils import Vector
from pathlib import Path
OUT=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BBB_Reception_Props_BeforeDesk.blend'),copy=True)
scene=bpy.data.scenes.new('BBB_Reception_Desk');bpy.context.window.scene=scene
col=bpy.data.collections.new('Reception_Desk_Modules');scene.collection.children.link(col)
master=bpy.data.objects.new('Reception_Desk_ROOT',None);col.objects.link(master)
wood=bpy.data.materials['BBB_Props_Polished Walnut Wood'];brass=bpy.data.materials['BBB_Props_Brushed Brass Metal']
def group(n):
 global root
 root=bpy.data.objects.new(n+'_ROOT',None);col.objects.link(root);root.parent=master
 return root
def mesh(n,v,f,mat):
 m=bpy.data.meshes.new(n);m.from_pydata(v,[],f);m.update()
 o=bpy.data.objects.new(n,m);col.objects.link(o);o.parent=root
 if mat:m.materials.append(mat)
 b=o.modifiers.new('Edge softness','BEVEL');b.width=.007;b.segments=2
 return o
def box(n,c,s,mat=wood):
 x,y,z=[v/2 for v in s]
 o=mesh(n,[(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat);o.location=c;return o
def f(x):return .020*x*x
def ribbon(n,a,b,front,back,z0,z1,mat=wood):
 N=max(3,int((b-a)*10));v=[]
 for i in range(N+1):
  x=a+(b-a)*i/N
  for y,z in [(f(x)+front,z0),(f(x)+back,z0),(f(x)+back,z1),(f(x)+front,z1)]:v.append((x,y,z))
 faces=[(3,2,1,0)]
 for i in range(N):
  for j in range(4):faces.append((4*i+j,4*i+(j+1)%4,4*(i+1)+(j+1)%4,4*(i+1)+j))
 faces.append(tuple(4*N+j for j in range(4)))
 return mesh(n,v,faces,mat)
def rod(n,a,b,r,mat=brass):
 a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y');N=16
 v=[tuple(a+q@Vector((r*math.cos(i*2*math.pi/N),r*math.sin(i*2*math.pi/N),z))) for z in [0,(b-a).length] for i in range(N)]
 faces=[tuple(reversed(range(N))),tuple(N+i for i in range(N))]+[(i,(i+1)%N,N+(i+1)%N,N+i) for i in range(N)]
 return mesh(n,v,faces,mat)
for label,a,b in [('STRAW',-11,-4),('WOOD',-4,4),('BRICK',4,11)]:
 group('Desk_'+label)
 ribbon(label+'_FrontCore',a,b,0,.10,.12,1.04)
 ribbon(label+'_ColorPanel',a+.16,b-.16,-.014,0,.26,.91,bpy.data.materials['BBB_Enamel_'+label])
 for z in [.235,.93]:ribbon(label+'_IvoryBorder',a+.12,b-.12,-.03,-.008,z,z+.035,None)
 for x in [a+.12,b-.16]:
  o=box(label+'_IvorySide',(x,f(x)-.02,.599),(.035,.026,.68),None);o.rotation_euler.z=math.atan(.04*x)
 for z in [.19,.985]:ribbon(label+'_BrassRail',a,b,-.035,-.018,z,z+.025,brass)
 ribbon(label+'_Plinth',a,b,-.025,.14,.02,.18)
 ribbon(label+'_CustomerLedge',a,b,-.10,.35,1.035,1.10)
 ribbon(label+'_StaffWorktop',a,b,.11,1.10,.705,.75)
 # Decorative front posts at module boundaries.
 for x in [a+.06,b-.06]:
  o=box(label+'_Stile',(x,f(x)-.025,.61),(.12,.16,.88));o.rotation_euler.z=math.atan(.04*x)
  for z in [.16,1.0]:
   o=box(label+'_PostCap',(x,f(x)-.027,z),(.15,.175,.07),brass);o.rotation_euler.z=math.atan(.04*x)
 # Two pedestals per bay preserve very generous central knee space.
 for x in [a+.5,b-.5]:
  y=f(x)+.63
  box(label+'_Cabinet',(x,y,.365),(.70,.80,.68))
  for j,(z,h) in enumerate([(.17,.22),(.375,.15),(.55,.15)]):
   box(label+'_Drawer',(x,y+.412,z),(.63,.03,h))
   rod(label+'_Handle',(x-.055,y+.447,z),(x+.055,y+.447,z),.009)
  box(label+'_CabinetFoot',(x,y,.04),(.74,.84,.08))
 # Reuse approved commodity relief meshes, scaled to 0.55m tall.
 source=bpy.data.objects[{'STRAW':'14_Emblem_Straw_ROOT','WOOD':'15_Emblem_Wood_ROOT','BRICK':'16_Emblem_Brick_ROOT'}[label]]
 cx=(a+b)/2;scale={'STRAW':1.35,'WOOD':2.05,'BRICK':2.55}[label]
 er=bpy.data.objects.new(label+'_Emblem_ROOT',None);col.objects.link(er);er.parent=root;er.location=(cx,f(cx)-.065,.30);er.rotation_euler.z=math.atan(.04*cx);er.scale=(scale,scale,scale)
 for old in source.children:
  o=old.copy();col.objects.link(o);o.parent=er;o.name='Desk_'+label+'_'+old.name
# End modules and functional pivoted gates.
for side in [-1,1]:
 group('End_'+str(side))
 a,b=sorted([side*11,side*12])
 ribbon('End_IvoryPanel',a,b,0,.08,.18,1.03,None)
 ribbon('End_Top',a,b,-.10,.35,1.035,1.1)
 ribbon('End_Plinth',a,b,-.025,.14,.02,.18)
 rod('End_XBrace',(a+.07,f(a+.07)-.04,.22),(b-.07,f(b-.07)-.04,.96),.035,wood)
 rod('End_XBrace',(a+.07,f(a+.07)-.04,.96),(b-.07,f(b-.07)-.04,.22),.035,wood)
 x=side*12;y=f(x);angle=math.atan(.04*x)
 tangent=Vector((math.cos(angle)*side,math.sin(angle)*side,0))
 for t in [0,1.35]:
  p=Vector((x,y,0))+tangent*t
  box('GatePost',(p.x,p.y,.56),(.14,.14,1.12))
  box('GatePostFoot',(p.x,p.y,.07),(.18,.18,.14),brass)
  box('GatePostCap',(p.x,p.y,1.10),(.18,.18,.07),brass)
  rod('GateFinial',(p.x,p.y,1.135),(p.x,p.y,1.22),.065)
 pivot=bpy.data.objects.new('Staff_Gate_'+str(side)+'_HINGE',None);col.objects.link(pivot);pivot.parent=root;pivot.location=(x,y,0);pivot.rotation_euler.z=angle if side==1 else angle+math.pi
 oldroot=root;root=pivot
 box('GateLeaf',(.675,0,.56),(1.17,.065,.82))
 for z in [.18,.94]:box('GateRail',(.675,-.045,z),(1.17,.055,.07))
 for xx in [.15,1.2]:box('GateStile',(xx,-.045,.56),(.075,.055,.79))
 box('StaffOnlyPlaque',(.675,-.052,.69),(.47,.02,.14),bpy.data.materials['BBB_Enamel_WOOD'])
 for z in [.30,.83]:rod('GateHinge',(0,0,z-.07),(0,0,z+.07),.028)
 box('GateLatch',(1.20,-.06,.56),(.10,.04,.035),brass)
 root=oldroot
master['layout']='Three bays; front left STRAW, center WOOD, right BRICK. 24m chord plus 1.35m gates.'
master['customer_height']=1.1;master['staff_height']=.75
for o in scene.objects:
 if o.type=='MESH':
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
  uv=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
  for p in o.data.polygons:
   ax=max(range(3),key=lambda i:abs(p.normal[i]));axes=[i for i in range(3) if i!=ax]
   for li in p.loop_indices:
    v=o.data.vertices[o.data.loops[li].vertex_index].co;uv.data[li].uv=(v[axes[0]],v[axes[1]])
bpy.context.view_layer.update()
print('created',len(scene.objects),'objects; paint target STRAW_IvoryBorder')

