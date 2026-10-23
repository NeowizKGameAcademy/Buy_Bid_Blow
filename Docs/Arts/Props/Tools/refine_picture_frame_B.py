import bpy,math,bmesh
from mathutils import Vector
from math import sin,cos,pi
OUT='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Picture_Frame_B_Ornate_Oval.blend'
if bpy.data.is_dirty:bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
bpy.ops.wm.save_as_mainfile(filepath=OUT)
s=bpy.context.scene;s.name='Frame_B_Ornate_Oval_Studio'
root=bpy.data.objects['Frame_Round_ROOT'];root.name='Frame_B_Oval_ROOT'
coll=bpy.data.collections['Picture_Frame_Round'];coll.name='Frame_B_Ornate_Oval'
gold=bpy.data.materials['Frame_A_Antique_Brass'].copy();gold.name='Frame_B_Antique_Gold'
p=next(n for n in gold.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
for link in list(gold.node_tree.links):
 if link.to_node==p and link.to_socket==p.inputs['Base Color']:gold.node_tree.links.remove(link)
p.inputs['Base Color'].default_value=(.48,.29,.105,1);p.inputs['Metallic'].default_value=.82;p.inputs['Roughness'].default_value=.34
gold.diffuse_color=(.48,.29,.105,1)
# Oval silhouette and completely gilded frame, including the broad concave molding.
for o in list(s.objects):
 if o.type=='MESH':
  if o.name=='06_Cardinal_Acanthus_Ornaments':bpy.data.objects.remove(o,do_unlink=True);continue
  for v in o.data.vertices:v.co.x*=.82;v.co.z=1.15+(v.co.z-.9)*1.12
  if not o.name.startswith(('Artwork','Backboard')):o.data.materials.clear();o.data.materials.append(gold)
  o.data.update()
bpy.data.objects['01_Walnut_Profile'].name='01_Oval_Gilded_Coved_Molding'
def mesh(name,vs,fs):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(name,me);coll.objects.link(o);o.parent=root;me.materials.append(gold)
 for p in me.polygons:p.use_smooth=True
 return o
def tube(name,pts,radii):
 vs=[];fs=[];N=12
 for j,pt in enumerate(pts):
  t=Vector(pts[min(j+1,len(pts)-1)])-Vector(pts[max(0,j-1)]);t.normalize();u=Vector((0,1,0));v=t.cross(u).normalized();rad=radii[j]
  for k in range(N):
   a=2*pi*k/N;vs.append(tuple(Vector(pt)+u*(rad*.7*cos(a))+v*(rad*sin(a))))
 for j in range(len(pts)-1):
  for k in range(N):fs.append((j*N+k,j*N+(k+1)%N,(j+1)*N+(k+1)%N,(j+1)*N+k))
 fs.extend([tuple(reversed(range(N))),tuple((len(pts)-1)*N+k for k in range(N))]);return mesh(name,vs,fs)
def leaf(name,x,z,dx,dz,L,W,y=-.12):
 d=math.hypot(dx,dz);dx/=d;dz/=d;vs=[];fs=[];N=24;M=12
 for j in range(N+1):
  t=j/N;half=W*sin(pi*t)**.65*(.83+.17*cos(6*pi*t));bend=.025*sin(pi*t)
  for k in range(M+1):
   u=2*k/M-1
   vs.append((x+dx*L*t-dz*(u*half+bend),y-.035*sin(pi*t)*(1-u*u)-.009*sin(pi*t)*cos(3*pi*u),z+dz*L*t+dx*(u*half+bend)))
 for j in range(N):
  for k in range(M):a=j*(M+1)+k;fs.append((a,a+1,a+M+2,a+M+1))
 o=mesh(name,vs,fs);m=o.modifiers.new('Cast_leaf_thickness','SOLIDIFY');m.thickness=.006
 pts=[(x+dx*L*t-dz*.025*sin(pi*t),y-.042*sin(pi*t)-.006,z+dz*L*t+dx*.025*sin(pi*t)) for t in [j/32 for j in range(33)]]
 tube(name+'_Midrib',pts,[.004+.004*sin(pi*j/32) for j in range(33)])
def scroll(name,cx,cz,sx,sz,R=.12):
 pts=[];rs=[]
 for j in range(81):
  t=j/80;a=-pi/2+t*pi*2.0;r=R*(1-.85*t)
  pts.append((cx+sx*r*cos(a),-.133-.01*sin(pi*t),cz+sz*r*sin(a)));rs.append(.007+.014*(1-t))
 tube(name,pts,rs)
# Large symmetric crown of acanthus leaves and curling volutes.
leaf('Crown_Central_Spear',0,2.05,0,1,.40,.055)
for side in [-1,1]:
 leaf('Crown_Inner_Frond',side*.015,2.065,side*.36,1,.35,.052)
 leaf('Crown_Middle_Frond',side*.035,2.06,side*.75,1,.30,.053)
 leaf('Crown_Outer_Frond',side*.04,2.065,side*1,.35,.29,.049)
 leaf('Crown_Drooping_Leaf',side*.05,2.045,side*.3,-1,.15,.041)
 scroll('Crown_Volute',side*.19,2.10,side,1,.115)
 scroll('Crown_Outer_Curl',side*.30,2.105,side,1,.068)
# Lower ornament mirrors the carved flourish, not the entire crown.
leaf('Bottom_Central_Leaf',0,.23,0,1,.22,.052)
for side in [-1,1]:
 leaf('Bottom_Ascending_Leaf',side*.02,.23,side*.6,1,.24,.049)
 leaf('Bottom_Swept_Leaf',side*.03,.24,side*1,.25,.23,.044)
 scroll('Bottom_Volute',side*.17,.24,side,-1,.105)
 leaf('Bottom_Tip',side*.014,.22,side*.3,-1,.13,.035)
# Tall side sprigs following the oval's long sides.
for side in [-1,1]:
 x=side*.675
 pts=[(x+side*.035*sin(pi*j/40),-.13,.91+.47*j/40) for j in range(41)]
 tube('Side_Curved_Stem',pts,[.011]*41)
 for j in range(4):
  z=.94+j*.095
  leaf('Side_Outer_Leaf',x,z,side*.45,1,.18,.037)
  leaf('Side_Inner_Leaf',x,z,-side*.55,1,.15,.035)
 scroll('Side_Lower_Curl',x-side*.012,.935,side,-1,.075)
 scroll('Side_Upper_Curl',x+side*.005,1.39,side,1,.068)
root['reference']='FurnitureConcepts_AB/15_Picture_Frame.png, variant B';root['shape']='Vertical oval, fully gilded, sculpted crown / base / side ornaments';root['opening_m']='1.148 wide x 1.568 high'
cam=s.camera;cam.location=(.55,-6,1.65);target=Vector((0,0,1.25));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=2.75
s.render.resolution_x=1100;s.render.resolution_y=1400;s.render.filepath=OUT.replace('.blend','_Preview.png')
for ar in bpy.context.screen.areas:
 if ar.type=='VIEW_3D':
  sp=ar.spaces.active;sp.region_3d.view_rotation=cam.rotation_euler.to_quaternion();sp.region_3d.view_location=target;sp.region_3d.view_distance=3.8
bpy.context.view_layer.update()
bpy.ops.wm.save_as_mainfile(filepath=OUT)
bpy.ops.render.render(write_still=True)
print('Saved ornate oval reference revision')
