import bpy, math, bmesh, os, json
from mathutils import Vector
from math import sin,cos,pi
s=bpy.data.scenes.new('BBB_Hall_A10');bpy.context.window.scene=s
s.unit_settings.system='METRIC'
groups={}
for name in ['A10_Floor','A10_Mezzanine','A10_Stairs','A10_Railings','A10_Columns','A10_Walls','A10_Roof_Panels','A10_Roof_Ribs','A10_Doors','A10_Views']:
 c=bpy.data.collections.new(name);s.collection.children.link(c);groups[name]=c
source=open(r'C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Tools/build_hall_geometry.py',encoding='utf-8').read()
exec(source[source.index('def mesh('):source.index('# Base and semicircular')])
def extrude(name,points,z0,z1):
 n=len(points);return mesh(name,[(x,y,z) for z in [z0,z1] for x,y in points],[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)])
def bound(a):
 lo,hi=0.,24.
 for _ in range(40):
  r=(lo+hi)/2;qx=max(abs(r*cos(a))-2,0);qy=max(abs(r*sin(a))-5.5,0)
  if qx*qx+qy*qy<=144:lo=r
  else:hi=r
 return (lo+hi)/2
outline=[]
for cx,cy,a0 in [(2,5.5,0),(-2,5.5,90),(-2,-5.5,180),(2,-5.5,270)]:
 for j in range(25):
  a=math.radians(a0+j*90/24);outline.append((cx+12*cos(a),cy+12*sin(a)))
current=groups['A10_Floor'];extrude('A10_Foundation',outline,-.3,0)
# North mezzanine and its central cubic bulge, precisely matching the plan.
north=outline[:50]
front=[(-3.5,5.5)]
for j in range(1,33):
 t=j/32;u=1-t
 x=u*u*u*(-3.5)+3*u*u*t*(-3.5)+3*u*t*t*3.5+t*t*t*3.5
 y=u*u*u*5.5+3*u*u*t*(5.5-2.8/.75)+3*u*t*t*(5.5-2.8/.75)+t*t*t*5.5
 front.append((x,y))
current=groups['A10_Mezzanine'];extrude('A10_Mezzanine_Bulged',north+front,3.65,4)
current=groups['A10_Columns']
for x,label in [(-7.5,'C4'),(7.5,'C6')]:box('A10_Column_'+label,(x,10.3,1.825),(.5,.5,3.65))
# Stairs along the front edge, rising from inside to outside.
current=groups['A10_Stairs']
for sign,label in [(-1,'West'),(1,'East')]:
 for i in range(24):
  h=(i+1)/6;box('A10_'+label+'_Step_'+str(i+1).zfill(2),(sign*(5.2+(i+.5)*7/24),4.5,h/2),(7/24,1.6,h))
 box('A10_'+label+'_Landing',(sign*13.1,4.5,3.825),(1.8,1.6,.35))
 box('A10_'+label+'_Landing_Link',(sign*13.1,5.4,3.825),(1.8,.2,.35))
 for yy in [3.64,5.36]:railing('A10_'+label+'_StairRail',(sign*5.2,yy,0),(sign*12.2,yy,4))
 railing('A10_'+label+'_LandingRail',(sign*12.2,3.64,4),(sign*13.9,3.64,4))
current=groups['A10_Railings']
railing('A10_FrontLeft',(-12.16,5.5,4),(-3.5,5.5,4));railing('A10_FrontRight',(3.5,5.5,4),(12.16,5.5,4))
for j in range(0,32,2):
 a=front[j];b=front[j+2];railing('A10_BulgeRail',(a[0],a[1],4),(b[0],b[1],4))
# Wall strips with real cut openings, not doors pasted onto solid walls.
current=groups['A10_Walls']
for i,p in enumerate(outline):
 q=outline[(i+1)%len(outline)]
 # Vertical straight side runs are separately segmented at door boundaries.
 if abs(p[0]-q[0])<1e-6 and abs(abs(p[0])-14)<1e-6:continue
 mid=((p[0]+q[0])/2,(p[1]+q[1])/2,4.5);length=math.dist(p,q)
 box('A10_Wall_Panel',mid,(length,.24,9),math.atan2(q[1]-p[1],q[0]-p[0]))
for x,doors in [(14,[(-1.5,2.1,3.5)]),(-14,[(-2.2,-.4,2.7),(1.3,3.1,2.7)])]:
 cuts=sorted(set([-5.5,5.5]+[v for a,b,h in doors for v in [a,b]]))
 for a,b in zip(cuts,cuts[1:]):
  z0=next((h for lo,hi,h in doors if lo<=(a+b)/2<=hi),0)
  box('A10_SideWall',(x,(a+b)/2,(z0+9)/2),(.24,b-a,9-z0))
current=groups['A10_Doors']
for x,cy,w,h,label in [(14,.3,3.6,3.5,'Entrance'),(-14,2.2,1.8,2.7,'Restroom_Men'),(-14,-1.3,1.8,2.7,'Restroom_Women')]:
 for yy in [cy-w/2,cy+w/2]:box('A10_'+label+'_Jamb',(x,yy,h/2),(.38,.12,h))
 box('A10_'+label+'_Header',(x,cy,h+.06),(.38,w+.12,.12))
 leaves=2 if label=='Entrance' else 1
 for j in range(leaves):
  center=cy-w/2+(j+.5)*w/leaves
  o=box('A10_'+label+'_Leaf_'+str(j),(x,center,h/2),(.10,w/leaves-.04,h-.04))
  hinge=Vector((x,cy-w/2+j*w/leaves,0));offset=o.location-hinge
  for v in o.data.vertices:v.co+=offset
  o.location=hinge;o['hinge_axis']='Z';o['opening_width_m']=w/leaves
# Continuous dome scaled to the capsule footprint: spring 9m, apex16m.
def rp(a,t,zoff=0):
 r=bound(a)*cos(t);return (r*cos(a),r*sin(a),9+7*sin(t)+zoff)
current=groups['A10_Roof_Panels']
for i in range(64):
 a=2*pi*i/64;b=2*pi*(i+1)/64
 for j in range(12):
  t0=j*pi/24;t1=(j+1)*pi/24
  vs=[rp(ang,t,z) for z in [0,.15] for ang,t in [(a,t0),(b,t0),(b,t1),(a,t1)]]
  mesh('A10_Dome_Panel',vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
current=groups['A10_Roof_Ribs']
for i in range(16):
 a=2*pi*i/16
 for j in range(12):beam('A10_Dome_Rib',rp(a,j*pi/24,-.18),rp(a,(j+1)*pi/24,-.18),.22,.28)
for j in [0,4,8]:
 for i in range(64):beam('A10_Dome_Ring',rp(2*pi*i/64,j*pi/24,-.18),rp(2*pi*(i+1)/64,j*pi/24,-.18),.19)
# Mesh cleanup and consistent normals.
for o in s.objects:
 bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001);bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.000001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
for name,eye,target,lens in [('Interior',(0,-12,1.75),(0,6,4.8),20),('Overview',(29,-38,32),(0,1,3),40),('Entry',(12,.3,1.75),(-4,5,3),20)]:
 d=bpy.data.cameras.new('A10_'+name);o=bpy.data.objects.new('A10_'+name,d);groups['A10_Views'].objects.link(o);o.location=eye;o.rotation_euler=(Vector(target)-Vector(eye)).to_track_quat('-Z','Y').to_euler();d.lens=lens
s.camera=bpy.data.objects['A10_Interior']
try:s.render.engine='BLENDER_WORKBENCH'
except TypeError as e:print(e)
s.display.shading.show_shadows=False;s.display.shading.show_cavity=True
s.render.resolution_x=1600;s.render.resolution_y=1100;s.render.resolution_percentage=100
s['plan_reference']='17_Hall_Doors_Closer_3m.svg / A-10'
s['design']='28 x35m; spring9 apex16; mezzanine4; 7m wide x2.8m deep bulge; columns C4 C6 only; east entrance, west separate male/female restroom doors; no restroom interiors.'
base=r'C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Models'
bpy.ops.wm.save_as_mainfile(filepath=base+'/BBB_Hall_A10.blend')
print('A10 created',len(s.objects))
