import bpy, math, bmesh, json
from mathutils import Vector, Quaternion
from math import sin,cos,pi,sqrt
src=bpy.context.scene
assert src.name=='BBB_Hall_A17_Smooth_6Columns',src.name
s=bpy.data.scenes.new('BBB_Hall_A18_Length40m')
s.world=src.world
copies={}
for old in src.objects:
 o=old.copy()
 if old.data:o.data=old.data.copy()
 s.collection.objects.link(o);copies[old]=o
for old,o in copies.items():
 o.parent=copies.get(old.parent)
 o.matrix_parent_inverse=old.matrix_parent_inverse.copy();o.matrix_basis=old.matrix_basis.copy()
bpy.context.window.scene=s
def remove(o):
    bpy.data.objects.remove(o,do_unlink=True)
def mesh(name,vs,fs,mat,smooth=False):
    me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=0.00001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    o=bpy.data.objects.new(name,me);s.collection.objects.link(o)
    if mat:me.materials.append(mat)
    uv=me.uv_layers.new(name='UVMap')
    for p in me.polygons:
        p.use_smooth=smooth and abs(p.normal.z)<.99
        for li in p.loop_indices:
            v=me.vertices[me.loops[li].vertex_index].co
            uv.data[li].uv=(v.x/4,v.y/4) if abs(p.normal.z)>.5 else ((v.x if abs(p.normal.y)>.5 else v.y)/4,v.z/4)
    return o

def inside(x,y,extended):
 ry=12 if y>=0 else 9
 qy=max(y-5.5,0) if y>=0 else max(-y-(13.5 if extended else 8.5),0)
 return (max(abs(x)-2,0)/12)**2+(qy/ry)**2<=1

def rad(a,extended):
 center=-2.5 if extended else 0
 lo,hi=0.,30.
 for _ in range(44):
  r=(lo+hi)/2
  if inside(r*cos(a),center+r*sin(a),extended):lo=r
  else:hi=r
 return (lo+hi)/2

def radius(a):return rad(a,True)
for old,o in list(copies.items()):
 if old.name.startswith(('A17_Floor_Ellipse','A17_Walls_Ellipse')):
  mw=o.matrix_world.copy();inv=mw.inverted()
  for v in o.data.vertices:
   p=mw@v.co
   if p.y<=-8.4999:p.y-=5
   v.co=inv@p
  o.name=old.name.replace('A17_','A18_').split('.')[0]
  o.data.update()
 elif old.name.startswith('Column04A_') and old.location.y<-10:o.location.y-=5
 elif old.name.startswith(('Hall_Roof_','A10_Dome_Ring')):
  mw=o.matrix_world.copy();inv=mw.inverted()
  for v in o.data.vertices:
   p=mw@v.co;a=math.atan2(p.y,p.x);k=rad(a,True)/rad(a,False)
   p.x*=k;p.y=p.y*k-2.5;v.co=inv@p
  o.data.update()
 elif old.name.startswith('Arch_Central_Hub'):o.location.y-=2.5
 elif old.name.startswith(('A17_RadialArch','A17_Capital_Saddle')):remove(o)
column_positions=[(6.694645,16.162304),(-6.694645,16.162304),(-13.52,-.4),(-6.694645,-21.162304),(6.694645,-21.162304),(13.52,-.4)]
for idx,(x,y) in enumerate(column_positions,1):
    a=math.atan2(y+2.5,x);r0=math.hypot(x,y+2.5);R=radius(a);v=[];f=[]
    for j in range(97):
        t=j/96;r=r0+(1.4-r0)*t
        roof=9+7*sqrt(max(0,1-(r/R)**2))
        startroof=9+7*sqrt(max(0,1-(r0/R)**2))
        z=roof-.08-(startroof-.08-9)*max(0,1-t/.13)**2
        for w,h in [(-.32,0),(.32,0),(.32,-.60),(-.32,-.60)]:
            v.append((r*cos(a)-w*sin(a),-2.5+r*sin(a)+w*cos(a),z+h))
    for j in range(96):
        for k in range(4):f.append((j*4+k,j*4+(k+1)%4,(j+1)*4+(k+1)%4,(j+1)*4+k))
    f += [(3,2,1,0),(384,385,386,387)]
    mesh('A18_RadialArch_P'+str(idx),v,f,bpy.data.materials['BBB_Column_Walnut'])
    # Small cap saddle between the column top and arch spring.
    mesh('A18_Capital_Saddle_P'+str(idx),[(x+dx,y+dy,z) for z in [8.84,9] for dx,dy in [(-.40,-.40),(.40,-.40),(.40,.40),(-.40,.40)]],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],bpy.data.materials['BBB_Column_Walnut'])

s['drawing_reference']='29_Hall_Length_40m_Proposal.svg / A18'
s['footprint_m']='28 x 40';s['lower_ellipse']='rx12 ry9 centers (+/-2,-13.5)'
s['column_count']=6;s['arch_count']=6;s['hub_y']=-2.5
bpy.context.view_layer.update()
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  r=area.spaces.active.region_3d;r.view_location=(0,-2.5,3);r.view_rotation=Quaternion((1,0,0),0);r.view_distance=48;r.view_perspective='ORTHO'
for o in s.objects:
 if o.name.startswith(('Hall_Roof_','A10_Dome_Ring','A18_RadialArch','Arch_Central_Hub')):o.hide_set(True)
print('Created',s.name,'floor',list(s.objects['A18_Floor_Ellipse'].dimensions))
