import bpy,math,bmesh
from mathutils import Vector,Matrix
S=bpy.context.scene
assert S.name=='BBB_Hall_A19_WideLandings_Railings'
S.name='BBB_Hall_A20_WallPanels'
with bpy.data.libraries.load('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Walls_03_Textured.blend',link=False) as (src,dst):
 dst.objects=[n for n in src.objects if n.startswith('03B_Curved_')]
sources=[o for o in dst.objects if o and o.type=='MESH']
col=bpy.data.collections.new('A20_Interior_WallPanels');S.collection.children.link(col)
columns=[(6.694645,16.162304),(-6.694645,16.162304),(-13.52,-.4),(13.52,-.4),(-6.694645,-21.162304),(6.694645,-21.162304)]
# Counter-clockwise outline; left normal is toward the room interior.
paths=[]
def arc(cx,cy,rx,ry,a,b):return [Vector((cx+rx*math.cos(a+(b-a)*i/240),cy+ry*math.sin(a+(b-a)*i/240))) for i in range(241)]
paths += [(arc(2,5.5,12,12,0,math.pi/2),4),( [Vector((2,17.5)),Vector((-2,17.5))],4),(arc(-2,5.5,12,12,math.pi/2,math.pi),4)]
paths += [([Vector((-14,5.5)),Vector((-14,-13.5))],0),(arc(-2,-13.5,12,9,math.pi,1.5*math.pi),0),([Vector((-2,-22.5)),Vector((2,-22.5))],0),(arc(2,-13.5,12,9,1.5*math.pi,2*math.pi),0),([Vector((14,-13.5)),Vector((14,5.5))],0)]
count=0
for points,base in paths:
 lengths=[0]
 for a,b in zip(points,points[1:]):lengths.append(lengths[-1]+(b-a).length)
 total=lengths[-1]
 def sample(d):
  d=max(0,min(total,d))
  import bisect
  i=min(len(points)-2,max(0,bisect.bisect_right(lengths,d)-1));v=(points[i+1]-points[i]).normalized()
  return points[i]+v*(d-lengths[i]),Vector((-v.y,v.x))
 def allowed(d):
  p,n=sample(d)
  if any((p-Vector(c)).length<.79 for c in columns):return False
  if p.x<-13.99 and any(abs(p.y-y)<1.02 for y in [-3.86,-7.36]):return False
  if p.x>13.99 and abs(p.y+5.44)<2.64:return False
  return True
 steps=math.ceil(total/.04);ranges=[];start=None
 for i in range(steps):
  a=total*i/steps;b=total*(i+1)/steps
  if allowed((a+b)/2):
   if start is None:start=a
  elif start is not None:ranges.append((start,a));start=None
 if start is not None:ranges.append((start,total))
 for lo,hi in ranges:
  if hi-lo<.18:continue
  bays=max(1,round((hi-lo)/2.8))
  for j in range(bays):
   l=lo+(hi-lo)*j/bays;w=(hi-lo)/bays;count+=1
   for src in sources:
    o=src.copy();o.data=src.data.copy();o.parent=None;o.matrix_world=Matrix.Identity(4);col.objects.link(o)
    o.name='A20_Wall_%02d_%s'%(count,src.name.replace('03B_Curved_',''))
    for v in o.data.vertices:
     old=v.co.copy();x=5*math.atan2(old.x,5-old.y);depth=5-math.hypot(old.x,5-old.y)
     p,n=sample(l+(x+1.4)/2.8*w)
     xy=p+n*(.145-depth)
     z=old.z
     if z>1.1:
      if z>=2.82:z+=(9-base)-3
      else:z=1.1+(z-1.1)*((9-base)-.18-1.1)/(2.82-1.1)
     v.co=(xy.x,xy.y,base+z)
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
# Match uncovered wall strips at columns and over doors to the approved teal.
teal=next(m for m in bpy.data.materials if m.name.startswith('BBB_Wall_Soft_Teal'))
wall=S.objects.get('A18_Walls_Ellipse_DoorOpenings')
if wall:
 wall.data=wall.data.copy();wall.data.materials.clear();wall.data.materials.append(teal)
 for p in wall.data.polygons:p.material_index=0
 uv=wall.data.uv_layers.active or wall.data.uv_layers.new(name='WallUV')
 for p in wall.data.polygons:
  for li in p.loop_indices:
   v=wall.data.vertices[wall.data.loops[li].vertex_index].co;uv.data[li].uv=((v.x+v.y)/.8,v.z/1.6)
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  r=a.spaces.active.region_3d;eye=Vector((7,-10,6));target=Vector((-7,5,4));r.view_rotation=(eye-target).to_track_quat('Z','Y');r.view_location=target;r.view_distance=(eye-target).length
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath='C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Models/BBB_Hall_A20_WallPanels.blend')
print('Applied',count,'wall bays;',len(col.objects),'parts')
