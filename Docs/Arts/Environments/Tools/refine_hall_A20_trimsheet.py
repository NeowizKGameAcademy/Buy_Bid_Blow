import bpy, math, bmesh, bisect, shutil, os
from mathutils import Vector, Matrix
P='C:/course/unity/BuyBidBlow/Docs/Arts/'
TARGET=P+'Environments/Models/BBB_Hall_A20_WallPanels.blend'
BACKUP=TARGET.replace('.blend','_BeforeTrimSheet.blend')
assert bpy.data.filepath.replace('\\','/')==TARGET
if not os.path.exists(BACKUP): shutil.copy2(TARGET,BACKUP)
S=bpy.context.scene
with bpy.data.libraries.load(P+'Props/Models/Wall_TrimSheet/BBB_Wall_TrimSheet.blend',link=False) as (src,dst):
 dst.materials=['BBB_Wall_TrimSheet']
trim=dst.materials[0]
with bpy.data.libraries.load(P+'Props/Models/BBB_Walls_03_Textured.blend',link=False) as (src,dst):
 dst.objects=[n for n in src.objects if n.startswith('03B_Curved_') and 'Crown' not in n]
sources=[o for o in dst.objects if o and o.type=='MESH']
removed=[]
for o in list(S.objects):
 if o.name.startswith(('A20_Wall_','A10_Dome_Ring')):
  removed.append(o.name);bpy.data.objects.remove(o,do_unlink=True)
col=bpy.data.collections.get('A20_Interior_WallPanels')
def arc(cx,cy,rx,ry,a,b):
 return [Vector((cx+rx*math.cos(a+(b-a)*i/480),cy+ry*math.sin(a+(b-a)*i/480))) for i in range(481)]
paths=[arc(2,5.5,12,12,0,math.pi/2),[Vector((2,17.5)),Vector((-2,17.5))],arc(-2,5.5,12,12,math.pi/2,math.pi),[Vector((-14,5.5)),Vector((-14,-13.5))],arc(-2,-13.5,12,9,math.pi,1.5*math.pi),[Vector((-2,-22.5)),Vector((2,-22.5))],arc(2,-13.5,12,9,1.5*math.pi,2*math.pi),[Vector((14,-13.5)),Vector((14,5.5))]]
count=0
for pi,points in enumerate(paths):
 lengths=[0.0]
 for a,b in zip(points,points[1:]):lengths.append(lengths[-1]+(b-a).length)
 total=lengths[-1]
 def sample(d):
  d=max(0,min(total,d));i=min(len(points)-2,max(0,bisect.bisect_right(lengths,d)-1))
  t=(d-lengths[i])/(lengths[i+1]-lengths[i]);p=points[i].lerp(points[i+1],t)
  if pi in [0,2]: n=Vector((-(p.x-(2 if pi==0 else -2)),-(p.y-5.5))).normalized()
  elif pi in [4,6]:n=Vector((-(p.x-(-2 if pi==4 else 2))/144,-(p.y+13.5)/81)).normalized()
  else:
   v=(points[-1]-points[0]).normalized();n=Vector((-v.y,v.x))
  return p,n
 ranges=[(0,total)]
 if pi==3:ranges=[(0,8.4),(10.32,11.9),(13.82,19)]
 if pi==7:ranges=[(0,5.52),(10.6,19)]
 for ri,(lo,hi) in enumerate(ranges):
  bays=max(1,round((hi-lo)/2.8))
  for j in range(bays):
   l=lo+(hi-lo)*j/bays;w=(hi-lo)/bays;count+=1
   for src in sources:
    tag=src.name.split('03B_Curved_')[1].split('.')[0]
    o=src.copy();o.data=src.data.copy();o.parent=None;o.matrix_world=Matrix.Identity(4);col.objects.link(o);o.name='A20_Wall_%02d_%s'%(count,tag)
    raw=[]
    for v in src.data.vertices:
     a=v.co;raw.append((5*math.atan2(a.x,5-a.y),5-math.hypot(a.x,5-a.y),a.z))
    zmin=min(v[2] for v in raw);zmax=max(v[2] for v in raw)
    for v,(x,depth,z) in zip(o.data.vertices,raw):
     d=l+(x+1.4)/2.8*w
     # Higher wood components meet the entry shaft; lower plinth meets its wider foot.
     if pi==7 and zmax>.56 and tag!='Upper_Panel':
      if ri==0 and j==bays-1:d+=.13*max(0,(x+1.4)/2.8)
      if ri==1 and j==0:d-=.13*max(0,1-(x+1.4)/2.8)
     p,n=sample(d);xy=p+n*(.145-depth)
     if tag=='Upper_Panel':z=.98+(z-.98)/(2.86-.98)*(9-.98)
     v.co=(xy.x,xy.y,z)
    if tag!='Upper_Panel':
     o.data.materials.clear();o.data.materials.append(trim)
     uv=o.data.uv_layers.active or o.data.uv_layers.new(name='TrimSheetUV')
     if tag.startswith('Plinth'):vlo,vhi=0,.125
     elif tag in ['Dado_Rail','Dado_Lip']:vlo,vhi=.125,.25
     elif tag.startswith('X_') or tag.startswith('Dado_Divider'):vlo,vhi=.75,1
     else:vlo,vhi=.25,.75
     pad=2/2048
     for f in o.data.polygons:
      f.material_index=0
      for li in f.loop_indices:
       x,depth,z=raw[o.data.loops[li].vertex_index]
       uv.data[li].uv=((l+(x+1.4)/2.8*w)/2.8,vlo+pad+(vhi-vlo-2*pad)*(z-zmin)/max(.00001,zmax-zmin))
     o['trim_band']=[vlo,vhi];o['floor_base_m']=0.0
    o['wall_path']=pi;o['wall_range']=[lo,hi];o['bay_range']=[l,l+w]
    bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
for o in sources:bpy.data.objects.remove(o,do_unlink=True)
bpy.context.view_layer.update()
print('Rebuilt',count,'bays',len(col.objects),'parts. Removed',len(removed),'old parts. Backup',BACKUP)
