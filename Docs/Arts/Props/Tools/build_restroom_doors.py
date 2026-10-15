import bpy,bmesh,math,json
from mathutils import Vector
from pathlib import Path
s=bpy.data.scenes.new('BBB_Restroom_Doors');bpy.context.window.scene=s
mats=[]
for src,name,rough in [('BBB_Door_Wood','Restroom_Walnut',.52),('BBB_Door_Teal','Restroom_Teal',.55),('BBB_Door_Brass','Restroom_Brass',.43)]:
 m=bpy.data.materials[src].copy();m.name=name
 n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');n.inputs['Roughness'].default_value=rough
 for node in m.node_tree.nodes:
  if node.type=='NORMAL_MAP':node.inputs['Strength'].default_value=.18
 mats.append(m)
wood,teal,brass=mats
def empty(name,loc,parent=None):
 o=bpy.data.objects.new(name,None);s.collection.objects.link(o);o.parent=parent;o.location=loc;return o
def geo(name,verts,faces,mat,parent,bevel=.004):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
 bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(name,me);s.collection.objects.link(o);o.parent=parent;me.materials.append(mat)
 uv=me.uv_layers.new(name='UVMap')
 for p in me.polygons:
  for li in p.loop_indices:
   v=me.vertices[me.loops[li].vertex_index].co
   uv.data[li].uv=(v.x/1.7,v.z/2.7) if abs(p.normal.y)>.5 else ((v.y/.3,v.z/2.7) if abs(p.normal.x)>.5 else (v.x/1.7,v.y/.3))
 if bevel:
  mod=o.modifiers.new('Soft edges','BEVEL');mod.width=bevel;mod.segments=2
 return o
def box(name,c,d,mat,parent,bev=.004):
 x,y,z=c;X,Y,Z=[v/2 for v in d]
 return geo(name,[(x+dx*X,y+dy*Y,z+dz*Z) for dx,dy,dz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],mat,parent,bev)
def extrude(name,points,y0,y1,mat,parent,bevel=.001):
 n=len(points);return geo(name,[(x,y,z) for y in [y0,y1] for x,z in points],[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],mat,parent,bevel)
def disc(name,x,y,z,r,depth,mat,parent):
 return extrude(name,[(x+r*math.cos(i*math.tau/16),z+r*math.sin(i*math.tau/16)) for i in range(16)],y-depth/2,y+depth/2,mat,parent)
def rod(name,p,q,width,mat,parent):
 p=Vector(p);q=Vector(q);o=box(name,(0,0,0),(width,width,(q-p).length),mat,parent,.002)
 o.location=(p+q)/2;o.rotation_euler=(q-p).to_track_quat('Z','Y').to_euler();return o
for gender,offset in [('Men',-1.25),('Women',1.25)]:
 root=empty('Restroom_'+gender,(offset,0,0));root['opening_m']='1.80 x 2.70';root['frame_outer_m']='1.92 x 2.82'
 frame=empty(gender+'_Frame',(0,0,0),root)
 pivot=empty(gender+'_HingePivot',(-.83,0,0),root)
 leaf=empty(gender+'_LeafGeometry',(.83,0,0),pivot)
 for x in [-.90,.90]:
  box(gender+'_Jamb',(x,0,1.35),(.12,.24,2.7),wood,frame,.008)
  box(gender+'_Foot',(x,-.02,.15),(.12,.28,.30),wood,frame,.006)
  for sign in [-1,1]:
   box(gender+'_FrameBead',(x,sign*.135,1.38),(.04,.03,2.36),wood,frame)
   box(gender+'_Capital',(x,sign*.03,2.70),(.12,.25,.24),wood,frame)
   box(gender+'_CornerBrass',(x,sign*.164,2.70),(.065,.012,.15),brass,frame,.003)
 box(gender+'_Lintel',(0,0,2.76),(1.8,.24,.12),wood,frame,.008)
 box(gender+'_LeafCore',(0,0,1.34),(1.66,.08,2.64),wood,leaf,.007)
 for sign in [-1,1]:
  y=sign*.047
  box(gender+'_InsetPanel',(0,y,1.47),(1.30,.018,2.10),teal,leaf,.012)
  for x in [-.665,.665]:box(gender+'_PanelMoulding',(x,sign*.061,1.47),(.045,.025,2.16),wood,leaf)
  for z in [.39,2.55]:box(gender+'_PanelRail',(0,sign*.061,z),(1.37,.025,.045),wood,leaf)
  for x in [-.595,.595]:box(gender+'_TealBead',(x,sign*.065,1.47),(.025,.018,1.98),teal,leaf,.002)
  for z in [.48,2.46]:box(gender+'_TealBead',(0,sign*.065,z),(1.19,.018,.025),teal,leaf,.002)
  for x in [-1,1]:
   for z,dz in [(.48,1),(2.46,-1)]:rod(gender+'_CornerChevron',(x*.595,sign*.068,z),(x*.47,sign*.068,z+dz*.125),.018,teal,leaf)
  box(gender+'_KickPlate',(0,sign*.049,.175),(1.54,.018,.25),brass,leaf,.006)
  for x in [-.73,.73]:
   for z in [.075,.275]:disc(gender+'_KickRivet',x,sign*.062,z,.011,.008,brass,leaf)
  box(gender+'_HandlePlate',(.715,sign*.059,1.05),(.10,.025,.20),brass,leaf,.009)
  disc(gender+'_HandleBoss',.715,sign*.088,1.05,.036,.04,brass,leaf)
  box(gender+'_Lever',(.64,sign*.132,1.05),(.19,.045,.035),brass,leaf,.012)
 for z in [.36,1.35,2.35]:
  # Barrel is vertical; rings follow the cylinder silhouette.
  verts=[(-.84+.023*math.cos(i*math.tau/16),-.059+.023*math.sin(i*math.tau/16),zz) for zz in [z-.075,z+.075] for i in range(16)]
  geo(gender+'_Hinge',verts,[tuple(reversed(range(16))),tuple(range(16,32))]+[(i,(i+1)%16,(i+1)%16+16,i+16) for i in range(16)],brass,frame,.002)
 box(gender+'_SymbolPlaque',(0,-.077,1.90),(.22,.018,.32),brass,leaf,.012)
 for x in [-.088,.088]:
  for z in [1.77,2.03]:disc(gender+'_PlaqueRivet',x,-.091,z,.008,.006,brass,leaf)
 disc(gender+'_SymbolHead',0,-.093,1.984,.024,.009,teal,leaf)
 if gender=='Men':
  box(gender+'_SymbolTorso',(0,-.094,1.913),(.049,.010,.088),teal,leaf,.006)
  for x in [-.045,.045]:box(gender+'_SymbolArm',(x,-.094,1.911),(.014,.010,.079),teal,leaf,.005)
 else:
  extrude(gender+'_SymbolDress',[(-.026,1.953),(.026,1.953),(.060,1.853),(-.060,1.853)],-.101,-.090,teal,leaf,.003)
  for sign in [-1,1]:rod(gender+'_SymbolArm',(sign*.037,-.098,1.94),(sign*.069,-.098,1.88),.012,teal,leaf)
 for x in [-.018,.018]:box(gender+'_SymbolLeg',(x,-.094,1.821),(.015,.010,.075),teal,leaf,.005)
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  r=area.spaces.active.region_3d;r.view_location=(0,0,1.4);r.view_rotation=Vector((0,-1,.08)).to_track_quat('Z','Y');r.view_distance=6.8;r.view_perspective='ORTHO';area.spaces.active.shading.type='MATERIAL'
bpy.context.view_layer.update()
print('Built',len(s.objects),'objects')
