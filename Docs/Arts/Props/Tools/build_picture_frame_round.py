import bpy,bmesh,math
from mathutils import Vector
from math import sin,cos,pi
OUT='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Picture_Frame_Round.blend'
if bpy.data.is_dirty:bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
bpy.ops.wm.save_as_mainfile(filepath=OUT)
s=bpy.context.scene;s.name='Picture_Frame_Round_Studio'
root=bpy.data.objects['Frame_A_ROOT'];root.name='Frame_Round_ROOT'
root['outer_size_m']='Diameter 1.78';root['opening_m']='Diameter 1.40';root['pivot']='bottom center';root['front_axis']='-Y'
bpy.data.collections['Picture_Frame_A'].name='Picture_Frame_Round'
def rebuild(o,vs,fs):
 old=o.data;me=bpy.data.meshes.new(o.name+'_RoundMesh');me.from_pydata(vs,[],fs);me.update()
 for m in old.materials:me.materials.append(m)
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.000001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();o.data=me
 if old.users==0:bpy.data.meshes.remove(old)
 return me
N=192
for name in ['01_Walnut_Profile','02_Inner_Gilt_Lip','03_Outer_Gilt_Contour','04_Bead_Channel']:
 o=bpy.data.objects[name]
 profile=[(-o.data.vertices[j].co.x-.6,o.data.vertices[j].co.y) for j in range(0,len(o.data.vertices),4)]
 vs=[((.7+d)*cos(2*pi*i/N),y,.9+(.7+d)*sin(2*pi*i/N)) for d,y in profile for i in range(N)]
 fs=[(j*N+i,j*N+(i+1)%N,((j+1)%len(profile))*N+(i+1)%N,((j+1)%len(profile))*N+i) for j in range(len(profile)) for i in range(N)]
 me=rebuild(o,vs,fs);uv=me.uv_layers.new(name='Round_Frame_UV')
 for p in me.polygons:
  p.use_smooth=True
  for li in p.loop_indices:
   co=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(co.x*.6,(co.z-.9)*.6)
# Evenly distributed pearl ring, with genuine three-dimensional beads.
vs=[];fs=[]
for k in range(176):
 a=2*pi*k/176;cx=.738*cos(a);cz=.9+.738*sin(a);start=len(vs);n=10;m=6
 for j in range(m+1):
  t=pi*j/m
  for i in range(n):
   b=2*pi*i/n;vs.append((cx+.009*sin(t)*cos(b),-.068+.008*sin(t)*sin(b),cz+.009*cos(t)))
 for j in range(m):
  for i in range(n):fs.append((start+j*n+i,start+j*n+(i+1)%n,start+(j+1)*n+(i+1)%n,start+(j+1)*n+i))
me=rebuild(bpy.data.objects['05_Pearl_Border'],vs,fs)
for p in me.polygons:p.use_smooth=True
# Reuse the crafted leaf reliefs, rotating each corner motif onto a cardinal position.
o=bpy.data.objects['06_Corner_Acanthus_Ornaments'];o.name='06_Cardinal_Acanthus_Ornaments'
for v in o.data.vertices:
 x,y,z=v.co;sx=1 if x>0 else -1;sz=1 if z>1 else -1
 angle=math.atan2(sz,sx)+pi/4
 cx=.805*cos(angle);cz=.9+.805*sin(angle)
 dx=x-sx*.695;dz=z-(1+sz*.895)
 v.co=(cx+(dx-dz)/math.sqrt(2),y,cz+(dx+dz)/math.sqrt(2))
o.data.update()
# Separate circular artwork with planar 0..1 UVs; concealed for frame-only inspection.
for name,radius,y in [('Artwork_Surface_Replaceable',.7,-.037),('Backboard_Optional',.718,.035)]:
 o=bpy.data.objects[name];vs=[(0,y,.9)]+[(radius*cos(2*pi*i/N),y,.9+radius*sin(2*pi*i/N)) for i in range(N)]
 fs=[(0,i+1,(i+1)%N+1) for i in range(N)];me=rebuild(o,vs,fs);uv=me.uv_layers.new(name='Artwork_0_1')
 for p in me.polygons:
  for li in p.loop_indices:
   v=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(v.x/(2*radius)+.5,(v.z-.9)/(2*radius)+.5)
 o.hide_viewport=True;o.hide_render=True
cam=s.camera;cam.location=(.8,-5.5,1.8);target=Vector((0,0,.9));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.22
s.render.resolution_x=1400;s.render.resolution_y=1400;s.render.resolution_percentage=100;s.render.filepath=OUT.replace('.blend','_Preview.png')
for ar in bpy.context.screen.areas:
 if ar.type=='VIEW_3D':
  sp=ar.spaces.active;sp.lens=50;sp.region_3d.view_rotation=cam.rotation_euler.to_quaternion();sp.region_3d.view_location=target;sp.region_3d.view_distance=3;sp.overlay.show_overlays=False
bpy.context.view_layer.update()
print('ROUND_DIAMETER',tuple(round(v,3) for v in bpy.data.objects['01_Walnut_Profile'].dimensions))
bpy.ops.wm.save_as_mainfile(filepath=OUT)
bpy.ops.render.render(write_still=True)
