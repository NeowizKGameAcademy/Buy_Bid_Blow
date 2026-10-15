import bpy,math,os,shutil
from mathutils import Vector
p=bpy.data.filepath;backup=p.replace('.blend','_BeforeFloorMedallion.blend')
if not os.path.exists(backup):shutil.copy2(p,backup)
col=bpy.data.collections.new('02B_Central_Floor_Medallion');bpy.context.scene.collection.children.link(col)
base=bpy.data.materials['Hall_Dark_Walnut_Border'];mats=[]
for name,val,sat in [('Walnut',.42,.95),('Walnut_Alternate',.52,.95),('Honey_Wood',1.2,1.15),('Light_Inlay',1.65,.85),('Dark_Edge',.22,.95)]:
 m=base.copy();m.name='BBB_Medallion_'+name
 n=next(n for n in m.node_tree.nodes if n.type=='HUE_SAT');n.inputs['Value'].default_value=val;n.inputs['Saturation'].default_value=sat
 for n in m.node_tree.nodes:
  if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.18
 mats.append(m)
vs=[];fs=[];mi=[]
def poly(points,mat,z=.012):
 k=len(vs);vs.extend([(x,y-7,z) for x,y in points]);fs.append(tuple(range(k,len(vs))));mi.append(mat)
def ring(r0,r1,mat,n=192):
 for i in range(n):
  a=2*math.pi*i/n;b=2*math.pi*(i+1)/n
  poly([(r0*math.cos(a),r0*math.sin(a)),(r1*math.cos(a),r1*math.sin(a)),(r1*math.cos(b),r1*math.sin(b)),(r0*math.cos(b),r0*math.sin(b))],mat if isinstance(mat,int) else mat[i%len(mat)])
def bar(a,b,w,mat=3,z=.014):
 dx=b[0]-a[0];dy=b[1]-a[1];d=math.hypot(dx,dy);nx=-dy/d*w/2;ny=dx/d*w/2
 poly([(a[0]-nx,a[1]-ny),(b[0]-nx,b[1]-ny),(b[0]+nx,b[1]+ny),(a[0]+nx,a[1]+ny)],mat,z)
def disc(x,y,r,mat,z=.015):poly([(x+r*math.cos(i*2*math.pi/64),y+r*math.sin(i*2*math.pi/64)) for i in range(64)],mat,z)
ring(3.92,4,4);ring(3.86,3.92,3);ring(3.61,3.86,0);ring(3.03,3.61,[2,2,3,2],96);ring(2.98,3.03,4)
for i in range(72):
 a=i*2*math.pi/72;b=(i+1)*2*math.pi/72
 poly([(0,0),(2.98*math.cos(a),2.98*math.sin(a)),(2.98*math.cos(b),2.98*math.sin(b))],i%2)
for angle in [90,210,330]:
 a=math.radians(angle);bar((0,0),(2.98*math.cos(a),2.98*math.sin(a)),.115)
# Three timber symbols, arranged like reference: wheat left, logs right, bricks below.
x,y=-1.42,.65
bar((x,y-.62),(x,y+.63),.075)
poly([(x-.23,y-.72),(x+.23,y-.72),(x+.12,y-.28),(x-.12,y-.28)],3,.015)
for side in [-1,1]:
 for h in [-.06,.26,.58]:
  poly([(x+side*.04,y+h-.15),(x+side*.29,y+h-.06),(x+side*.43,y+h+.18),(x+side*.21,y+h+.15)],3,.015)
poly([(x,y+.66),(x-.12,y+.82),(x,y+1.02),(x+.12,y+.82)],3,.015)
for x,y in [(1.15,.42),(1.88,.42),(1.51,1.04)]:
 disc(x,y,.36,4);disc(x,y,.325,2,.016)
 # End-grain circular inlay, kept subtle.
 for i in range(48):
  a=i*2*math.pi/48;b=(i+1)*2*math.pi/48
  poly([(x+.22*math.cos(a),y+.22*math.sin(a)),(x+.233*math.cos(a),y+.233*math.sin(a)),(x+.233*math.cos(b),y+.233*math.sin(b)),(x+.22*math.cos(b),y+.22*math.sin(b))],0,.017)
 disc(x-.065,y+.04,.038,0,.017)
for x,y in [(-.43,-1.98),(.43,-1.98),(0,-1.52)]:
 poly([(x-.4,y-.2),(x+.4,y-.2),(x+.4,y+.2),(x-.4,y+.2)],3,.015)
me=bpy.data.meshes.new('02B_Wood_Inlay');me.from_pydata(vs,[],fs);me.update()
o=bpy.data.objects.new('02B_Central_Medallion_8m',me);col.objects.link(o)
for m in mats:me.materials.append(m)
u=me.uv_layers.new(name='WoodGrainUV')
for f,idx in zip(me.polygons,mi):
 f.material_index=idx
 for li in f.loop_indices:
  v=me.vertices[me.loops[li].vertex_index].co;u.data[li].uv=(v.x/2,(v.y+7)/2)
o['diameter_m']=8.0;o['reference']='03_Architecture_Kit.png / 02B';o['design']='Walnut and honey wood marquetry: wheat, logs, bricks';o['center_xy']=[0,-7]
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  r=a.spaces.active.region_3d;eye=Vector((9,-18,15));target=Vector((0,-5,0));r.view_rotation=(eye-target).to_track_quat('Z','Y');r.view_location=target;r.view_distance=(eye-target).length
print('Created 8m medallion centered (0,-7),',len(me.polygons),'faces')
