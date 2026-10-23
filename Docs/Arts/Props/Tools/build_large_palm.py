import bpy, math
from mathutils import Vector,Matrix
with bpy.data.libraries.load('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Auction_Exchange_Furniture_Textured.blend',link=False) as (a,b):b.objects=[n for n in a.objects if n.startswith('05A_Medium_Planter')]
s=bpy.context.scene;c=bpy.data.collections.new('A28_Large_Brass_Planters');s.collection.children.link(c)
r=bpy.data.objects.new('BBB_Large_Brass_Palm_ROOT',None);c.objects.link(r)
for src,label in [('05A_Medium_Planter_Hollow_Pot','Brass_Pot'),('05A_Medium_Planter_Soil','Soil')]:
 o=bpy.data.objects[src].copy();o.data=o.data.copy();c.objects.link(o);o.name='LargePalm_'+label;o.parent=r;o.matrix_parent_inverse=Matrix.Identity(4);o.scale=(1.25,1.25,1.3);o.location=(0,0,.767 if label=='Soil' else 0)
 if label=='Brass_Pot':o.data.materials.clear();o.data.materials.append(bpy.data.materials['BBB_Props_Brushed Brass Metal'])
leaf=bpy.data.objects['05A_Medium_Planter_NewLeaf_01_Blade']
def stem(name,points):
 vs=[];fs=[]
 for j,p in enumerate(points):
  tangent=points[min(j+1,len(points)-1)]-points[max(j-1,0)];q=tangent.to_track_quat('Z','Y')
  for k in range(8):vs.append(p+q@Vector((.008*math.cos(k*math.tau/8),.008*math.sin(k*math.tau/8),0)))
 for j in range(len(points)-1):
  for k in range(8):fs.append((j*8+k,j*8+(k+1)%8,(j+1)*8+(k+1)%8,(j+1)*8+k))
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.materials.append(bpy.data.materials['BBB_Leaf_Study_Vein']);o=bpy.data.objects.new(name,me);c.objects.link(o);o.parent=r
print('START_FRONDS',flush=True)
for i in range(12):
 a=i*2.39996;out=Vector((math.cos(a),math.sin(a),0));side=Vector((-math.sin(a),math.cos(a),0));base=out*.12+Vector((0,0,.76));height=1.35+.65*(i%4)/3;reach=.65+.45*((i+1)%4)/3
 def point(t):return base+out*(reach*t**1.55)+Vector((0,0,height*(1.48*t-.48*t*t)))
 stem('LargePalm_Frond%02d'%i,[point(j/16) for j in range(17)])
 for j in range(7):
  t=.32+j*.097
  for sign in [-1,1]:
   o=leaf.copy();c.objects.link(o);o.name='LargePalm_Leaf_%02d_%02d_%s'%(i,j,sign);o.parent=r;o.matrix_parent_inverse=Matrix.Identity(4);direction=(side*sign*.82+out*.5+Vector((0,0,.18-.32*t))).normalized();o.location=point(t);o.rotation_mode='QUATERNION';o.rotation_quaternion=direction.to_track_quat('Y','Z');o.scale=(.32,.45*(1-.65*(t-.32)/.65)/.85,.43)
bpy.context.view_layer.update()
print('READY_TO_PLACE',flush=True)
r.location=(4.3,-20.2,0)
obs=[r]+list(r.children_recursive);cp={o:o.copy() for o in obs}
for o,n in cp.items():
 c.objects.link(n);n.name=o.name+'_Left'
 if o!=r:n.parent=cp[o.parent];n.matrix_basis=o.matrix_basis.copy()
cp[r].location=(-4.3,-20.2,0);cp[r].rotation_euler.z=.7
bpy.context.view_layer.update()
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  sp=a.spaces.active;target=Vector((4.3,-20.2,1.4));eye=Vector((7,-16.5,2.3));sp.region_3d.view_rotation=(target-eye).to_track_quat('-Z','Y');sp.region_3d.view_location=target;sp.region_3d.view_distance=(target-eye).length;sp.lens=48
bpy.ops.wm.save_as_mainfile(filepath='C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Models/BBB_Hall_A28_LargePlanters.blend')
print('DONE_PLANTERS',len(c.objects),flush=True)



