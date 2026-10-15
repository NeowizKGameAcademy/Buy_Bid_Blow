import bpy,math,bmesh,os
from mathutils import Vector
from math import sin,cos,pi
OUT='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Picture_Frame_A.blend'
# Preserve the current editable level before opening a separate prop file.
if bpy.data.is_dirty:bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
s=bpy.data.scenes.new('Picture_Frame_A_Studio')
c=bpy.data.collections.new('Picture_Frame_A');s.collection.children.link(c)
studio=bpy.data.collections.new('Studio_Preview');s.collection.children.link(studio)
optional=bpy.data.collections.new('Optional_Artwork_and_Backboard');s.collection.children.link(optional)
wood=bpy.data.materials['BBB_Furniture_Hall_Walnut'].copy();wood.name='Frame_A_Walnut'
gold=bpy.data.materials['BBB_Column_Brass'].copy();gold.name='Frame_A_Antique_Brass'
root=bpy.data.objects.new('Frame_A_ROOT',None);c.objects.link(root)
root['outer_size_m']='1.58 x 1.98';root['opening_m']='1.20 x 1.60';root['front_axis']='-Y';root['pivot']='bottom center';root['artwork']='Optional_Artwork_and_Backboard collection; UV 0..1'
def mesh(name,v,f,ma,collection=c):
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(name,me);collection.objects.link(o);me.materials.append(ma);o.parent=root
 return o
def soften(o,w=.002):
 b=o.modifiers.new('Fine_edge_bevel','BEVEL');b.width=w;b.segments=2
def ring(name,profile,ma):
 # Profile is radial offset from artwork opening, depth toward viewer (negative Y).
 v=[]
 for d,y in profile:
  x=.6+d;z=.8+d
  v.extend([(-x,y,1-z),(x,y,1-z),(x,y,1+z),(-x,y,1+z)])
 f=[]
 for j in range(len(profile)):
  k=(j+1)%len(profile)
  for i in range(4):f.append((j*4+i,j*4+(i+1)%4,k*4+(i+1)%4,k*4+i))
 o=mesh(name,v,f,ma);soften(o,.0015)
 uv=o.data.uv_layers.new(name='Frame_Wood_UV')
 for p in o.data.polygons:
  for li in p.loop_indices:
   co=o.data.vertices[o.data.loops[li].vertex_index].co
   uv.data[li].uv=(co.x*.6,(co.z if abs(p.normal.y)>.4 else co.y)*.6)
 return o
# Deeply profiled continuous mitred walnut rails with a recessed central field.
ring('01_Walnut_Profile',[(0,.018),(0,-.045),(.014,-.058),(.028,-.058),(.038,-.045),(.047,-.036),(.063,-.035),(.076,-.047),(.085,-.061),(.096,-.064),(.108,-.053),(.12,-.048),(.145,-.048),(.158,-.06),(.168,-.074),(.18,-.079),(.19,-.067),(.19,.055),(.17,.065),(.015,.065)],wood)
ring('02_Inner_Gilt_Lip',[(-.004,-.045),(-.004,-.057),(.006,-.065),(.014,-.065),(.021,-.054),(.021,-.043)],gold)
ring('03_Outer_Gilt_Contour',[(.169,-.073),(.172,-.082),(.178,-.085),(.184,-.08),(.186,-.072),(.179,-.069)],gold)
ring('04_Bead_Channel',[(.027,-.051),(.027,-.061),(.048,-.061),(.048,-.048)],gold)
# Combine repeated beads into one mesh, with an evenly spaced pearl border.
vv=[];ff=[]
def ellipsoid(center,scale,vs,fs,n=12,m=8):
 start=len(vs)
 for j in range(m+1):
  a=pi*j/m
  for k in range(n):
   b=2*pi*k/n;vs.append((center[0]+scale[0]*sin(a)*cos(b),center[1]+scale[1]*sin(a)*sin(b),center[2]+scale[2]*cos(a)))
 for j in range(m):
  for k in range(n):fs.append((start+j*n+k,start+j*n+(k+1)%n,start+(j+1)*n+(k+1)%n,start+(j+1)*n+k))
for z in [.162,1.838]:
 for i in range(43):ellipsoid((-.63+i*1.26/42,-.068,z),(.009,.008,.009),vv,ff)
for x in [-.638,.638]:
 for i in range(1,56):ellipsoid((x,-.068,.162+i*1.676/56),(.009,.008,.009),vv,ff)
o=mesh('05_Pearl_Border',vv,ff,gold)
for p in o.data.polygons:p.use_smooth=True
# Four corner ornaments. Sculpted leaf lobes taper to tips and lift above the wood.
v=[];f=[]
def leaf(cx,cz,dx,dz,L,W):
 start=len(v);N=14;M=8;norm=math.hypot(dx,dz);dx/=norm;dz/=norm
 for j in range(N+1):
  t=j/N;curve=.022*sin(pi*t);half=W*sin(pi*t)**.7
  for k in range(M+1):
   u=2*k/M-1
   x=cx+dx*L*t-dz*(u*half+curve);z=cz+dz*L*t+dx*(u*half+curve)
   y=-.079-.022*sin(pi*t)*(1-u*u)-.007*sin(pi*t)*cos(u*pi*2)
   v.append((x,y,z))
 for j in range(N):
  for k in range(M):
   a=start+j*(M+1)+k;f.append((a,a+1,a+M+2,a+M+1))
for sx in [-1,1]:
 for sz in [-1,1]:
  x=sx*.695;z=1+sz*.895
  for dx,dz,L,W in [(-sx,0,.13,.024),(0,-sz,.13,.024),(-sx,-sz,.12,.028),(-sx,sz*.35,.075,.021),(sx*.35,-sz,.075,.021)]:leaf(x,z,dx,dz,L,W)
  ellipsoid((x,-.091,z),(.028,.015,.034),v,f,16,10)
o=mesh('06_Corner_Acanthus_Ornaments',v,f,gold)
for p in o.data.polygons:p.use_smooth=True
solid=o.modifiers.new('Cast_Ornament_Thickness','SOLIDIFY');solid.thickness=.004
# Optional replaceable painting surface and back panel, not part of decorative frame.
art=bpy.data.materials.new('Frame_A_Artwork_Replace_Image');art.use_nodes=True
bs=next(n for n in art.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Roughness'].default_value=.85
tex=art.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load('C:/course/unity/BuyBidBlow/Docs/Arts/Props/PaintingTextures/07_Wolf_Portrait.png',check_existing=True);tex.image.pack();art.node_tree.links.new(tex.outputs['Color'],bs.inputs['Base Color'])
o=mesh('Artwork_Surface_Replaceable',[(-.6,-.037,.2),(.6,-.037,.2),(.6,-.037,1.8),(-.6,-.037,1.8)],[(0,1,2,3)],art,optional)
uv=o.data.uv_layers.new(name='Artwork_0_1')
for i,p in enumerate([(0,0),(1,0),(1,1),(0,1)]):uv.data[i].uv=p
o.hide_render=True;o.hide_viewport=True
o=mesh('Backboard_Optional',[(-.62,.035,.18),(.62,.035,.18),(.62,.035,1.82),(-.62,.035,1.82)],[(0,1,2,3)],wood,optional)
so=o.modifiers.new('Backboard_Thickness','SOLIDIFY');so.thickness=.012;o.hide_render=True;o.hide_viewport=True
# Neutral studio with camera and large soft lights.
world=bpy.data.worlds.new('Frame_Studio_World');world.use_nodes=True;s.world=world
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.13,.13,.13,1);bg.inputs[1].default_value=.45
cam=bpy.data.cameras.new('Frame_Review_Camera');co=bpy.data.objects.new('Frame_Review_Camera',cam);studio.objects.link(co);co.location=(1.5,-5.5,2.2);target=Vector((0,0,1));co.rotation_euler=(target-co.location).to_track_quat('-Z','Y').to_euler();cam.type='ORTHO';cam.ortho_scale=2.6;s.camera=co
for name,loc,power,size in [('Key',(-3,-4,5),450,4),('Fill',(3,-2,2),250,3),('Rim',(0,2,4),350,3)]:
 ld=bpy.data.lights.new('Frame_'+name,'AREA');ld.energy=power;ld.shape='DISK' if 'DISK' in [i.identifier for i in ld.bl_rna.properties['shape'].enum_items] else ld.shape;ld.size=size
 lo=bpy.data.objects.new('Frame_'+name,ld);studio.objects.link(lo);lo.location=loc;lo.rotation_euler=(target-lo.location).to_track_quat('-Z','Y').to_euler()
try:s.render.engine='BLENDER_EEVEE'
except TypeError:pass
s.render.resolution_x=1200;s.render.resolution_y=1400;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG'
s.render.filepath=OUT.replace('.blend','_Preview.png')
bpy.context.window.scene=s
for ar in bpy.context.screen.areas:
 if ar.type=='VIEW_3D':
  sp=ar.spaces.active;sp.region_3d.view_rotation=co.rotation_euler.to_quaternion();sp.region_3d.view_location=(0,0,1);sp.region_3d.view_distance=3.3;sp.region_3d.view_perspective='ORTHO';sp.overlay.show_overlays=False
bpy.context.view_layer.update()
print('FRAME',len(c.objects),'objects',sum(len(o.data.polygons) for o in c.objects if o.type=='MESH'),'polygons')
# Export only this scene and its dependencies, keeping the level file independent.
bpy.data.libraries.write(OUT,{s},path_remap='ABSOLUTE',fake_user=True,compress=True)
print('SAVED',OUT)
