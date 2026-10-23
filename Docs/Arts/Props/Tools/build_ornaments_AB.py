import bpy, math, os
from mathutils import Vector, Matrix
from math import sin,cos,pi
OUT='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/Ornaments'
os.makedirs(OUT,exist_ok=True)
s=bpy.data.scenes.new('Ornaments_AB_Geometry')
bpy.context.window.scene=s
def empty(name,x):
 o=bpy.data.objects.new(name,None);s.collection.objects.link(o);o.location.x=x;return o
A=empty('Ornament_A_Armillary_ROOT',-1.1);B=empty('Ornament_B_Rearing_Horse_ROOT',1.1)
def mesh(name,vs,fs,parent,smooth=True):
 m=bpy.data.meshes.new(name);m.from_pydata(vs,[],fs);m.update();o=bpy.data.objects.new(name,m);s.collection.objects.link(o);o.parent=parent
 for p in m.polygons:p.use_smooth=smooth
 return o
def box(name,loc,size,parent):
 vs=[tuple(Vector(loc)+Vector((a*size[0]/2,b*size[1]/2,c*size[2]/2))) for a,b,c in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
 o=mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],parent,False);mod=o.modifiers.new('Edge highlights','BEVEL');mod.width=.008;mod.segments=3;return o
def ell(name,loc,scale,parent,rot=None,n=32,rings=20):
 vs=[]
 for i in range(rings+1):
  ph=pi*i/rings
  for j in range(n):
   t=2*pi*j/n;v=Vector((scale[0]*sin(ph)*cos(t),scale[1]*sin(ph)*sin(t),scale[2]*cos(ph)))
   if rot:v=rot@v
   vs.append(tuple(Vector(loc)+v))
 fs=[(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j) for i in range(rings) for j in range(n)]
 return mesh(name,vs,fs,parent)
def lathe(name,profile,parent,loc=(0,0,0),n=64):
 vs=[(loc[0]+r*cos(2*pi*j/n),loc[1]+r*sin(2*pi*j/n),loc[2]+z) for z,r in profile for j in range(n)]
 fs=[(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j) for i in range(len(profile)-1) for j in range(n)]
 fs.extend([tuple(reversed(range(n))),tuple((len(profile)-1)*n+j for j in range(n))]);return mesh(name,vs,fs,parent)
def tube(name,pts,radii,parent,n=12):
 vs=[]
 for i,p in enumerate(pts):
  p=Vector(p);t=(Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(i-1,0)])).normalized();u=t.cross(Vector((0,1,0)))
  if u.length<.01:u=t.cross(Vector((1,0,0)))
  u.normalize();v=t.cross(u).normalized();r=radii[i] if isinstance(radii,list) else radii
  vs.extend([tuple(p+r*(u*cos(2*pi*j/n)+v*sin(2*pi*j/n))) for j in range(n)])
 fs=[(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j) for i in range(len(pts)-1) for j in range(n)];fs.extend([tuple(reversed(range(n))),tuple((len(pts)-1)*n+j for j in range(n))]);return mesh(name,vs,fs,parent)
def spline(points,per=8):
 p=[Vector(points[0])]+[Vector(v) for v in points]+[Vector(points[-1])];out=[]
 for i in range(1,len(p)-2):
  for j in range(per):
   t=j/per;out.append(.5*((2*p[i])+(-p[i-1]+p[i+1])*t+(2*p[i-1]-5*p[i]+4*p[i+1]-p[i+2])*t*t+(-p[i-1]+3*p[i]-3*p[i+1]+p[i+2])*t*t*t))
 return out+[Vector(points[-1])]
def ring(name,r,w,depth,center,rot,parent,n=128):
 vs=[]
 for j in range(n):
  t=2*pi*j/n
  for rr,zz in [(r-w/2,-depth/2),(r+w/2,-depth/2),(r+w/2,depth/2),(r-w/2,depth/2)]:vs.append(tuple(Vector(center)+rot@Vector((rr*cos(t),rr*sin(t),zz))))
 fs=[(j*4+k,((j+1)%n)*4+k,((j+1)%n)*4+(k+1)%4,j*4+(k+1)%4) for j in range(n) for k in range(4)]
 return mesh(name,vs,fs,parent)
def base(parent,prefix,width=.9,depth=.65):
 for i,(z,h,w,d) in enumerate([(.08,.06,width,depth),(.13,.04,width-.05,depth-.05),(.27,.24,width-.10,depth-.10),(.41,.035,width-.04,depth-.04),(.44,.035,width-.09,depth-.09)]):box(prefix+'_Plinth_%02d'%i,(0,0,z),(w,d,h),parent)
 for x in [-width*.38,width*.38]:
  for y in [-depth*.35,depth*.35]:ell(prefix+'_Foot',(x,y,.045),(.065,.065,.045),parent)
base(A,'A')
for x in [-.365,.365]:box('A_Front_Stile',(x,-.281,.275),(.024,.016,.2),A)
for z in [.175,.375]:box('A_Front_Rail',(0,-.281,z),(.74,.016,.022),A)
lathe('A_Pedestal',[(.45,.22),(.48,.22),(.49,.19),(.52,.19),(.54,.14),(.59,.10),(.65,.075),(.68,.12),(.71,.12),(.73,.065)],A)
center=(0,0,1.34)
rot=Matrix.Rotation(pi/2,3,'X')
ring('A_Outer_Meridian',.59,.032,.024,center,rot,A)
ring('A_Inner_Meridian',.51,.018,.018,center,Matrix.Rotation(.46,3,'Y')@rot,A)
ring('A_Equator',.49,.024,.018,center,Matrix.Rotation(.27,3,'X'),A)
ring('A_Ecliptic',.54,.055,.018,center,Matrix.Rotation(.85,3,'X')@Matrix.Rotation(.3,3,'Y'),A)
ell('A_Celestial_Globe',center,(.18,.18,.18),A)
for deg in [-60,-30,0,30,60]:
 z=.181*sin(math.radians(deg));r=.181*cos(math.radians(deg));pts=[(r*cos(2*pi*j/96),r*sin(2*pi*j/96),1.34+z) for j in range(97)];tube('A_Globe_Latitude',pts,.0018,A,6)
for j in range(6):
 t=j*pi/6;pts=[(.181*sin(2*pi*k/96)*cos(t),.181*sin(2*pi*k/96)*sin(t),1.34+.181*cos(2*pi*k/96)) for k in range(97)];tube('A_Globe_Longitude',pts,.0018,A,6)
axis=Vector((.42,0,.907));ctr=Vector(center)
tube('A_Polar_Axis',[ctr-axis*.65,ctr+axis*.65],.015,A,20)
tube('A_Cross_Axis',[(-.65,0,1.34),(.65,0,1.34)],.012,A,20)
for sign in [-1,1]:
 ell('A_Axis_Finial',ctr+axis*.68*sign,(.035,.035,.035),A)
 ell('A_Cross_Finial',(sign*.66,0,1.34),(.024,.024,.024),A)
 for k in [.51,.55,.59]:ell('A_Axis_Collar',ctr+axis*k*sign,(.036,.036,.026),A,axis.to_track_quat('Z','Y').to_matrix())
ring('A_Compass_Rim',.071,.007,.012,(0,-.302,.275),rot,A,64)
for j in range(8):
 t=j*pi/4;tip=Vector((.063*cos(t),-.311,.275+.063*sin(t)));q=Vector((-.012*sin(t),0,.012*cos(t)));c=Vector((0,-.318,.275));mesh('A_Compass_Point',[tuple(c+q),tuple(tip),tuple(c-q),tuple(c+Vector((0,-.012,0)))],[(0,1,3),(1,2,3),(2,0,3),(2,1,0)],A,False)
# Horse sculpture: union the anatomical masses, leaving facial and hair details separate.
base(B,'B',1.05,.66)
ell('B_Rock',(0,0,.48),(.47,.27,.07),B)
parts=[]
def mass(name,loc,sc,ang=0):
 o=ell(name,loc,sc,B,Matrix.Rotation(ang,3,'Y'));parts.append(o);return o
mass('B_Haunch',(.17,0,.98),(.235,.175,.31),-.3)
mass('B_Barrel',(-.015,0,1.2),(.25,.19,.34),-.5)
mass('B_Chest',(-.19,0,1.43),(.195,.17,.255),-.3)
mass('B_Neck',(-.16,0,1.66),(.145,.12,.3),-.12)
mass('B_Poll',(-.2,0,1.88),(.125,.105,.18),-.3)
mass('B_Cheek',(-.30,0,1.88),(.135,.105,.13),.45)
mass('B_Face',(-.395,0,1.84),(.195,.075,.085),-.48)
mass('B_Muzzle',(-.53,0,1.76),(.08,.087,.063),-.4)
mass('B_Jaw',(-.355,0,1.79),(.14,.07,.065),-.4)
for side in [-1,1]:
 y=side*.115
 mass('B_Thigh',(.21,y,.91),(.14,.105,.23),.35)
 pts=[(.22,y,1.02),(.29,y,.83),(.25,y,.70),(.10,y,.57),(.08,y,.51)] if side==-1 else [(.16,y,1.0),(.13,y,.8),(-.02,y,.68),(-.18,y,.57),(-.21,y,.51)]
 pp=spline(pts);rr=[.07*(1-i/(len(pp)-1))+.028*i/(len(pp)-1) for i in range(len(pp))];parts.append(tube('B_Hind_Leg',pp,rr,B,16))
 mass('B_Hock',pts[2],(.05,.047,.065))
 ell('B_Hind_Hoof',pts[-1],(.065,.052,.04),B)
 y=side*.15
 pts=[(-.18,y,1.43),(-.32,y,1.25),(-.48,y,1.35),(-.61,y,1.21),(-.64,y,1.10)] if side==-1 else [(-.2,y,1.47),(-.39,y,1.44),(-.54,y,1.56),(-.68,y,1.38),(-.71,y,1.27)]
 pp=spline(pts);rr=[.075*(1-i/(len(pp)-1))+.025*i/(len(pp)-1) for i in range(len(pp))];parts.append(tube('B_Foreleg',pp,rr,B,16))
 mass('B_Knee',pts[2],(.055,.045,.052))
 ell('B_Front_Hoof',(pts[-1][0]+.02,y,pts[-1][2]-.035),(.055,.045,.065),B,Matrix.Rotation(-.4,3,'Y'))
 # Pointed ears with a separate recessed inner surface.
 pp=spline([(-.22,side*.065,1.96),(-.23,side*.075,2.065),(-.28,side*.07,2.14)])
 parts.append(tube('B_Ear',pp,[.032*(1-i/(len(pp)-1))+.002 for i in range(len(pp))],B,12))
 ell('B_Eye',(-.35,side*.092,1.91),(.024,.012,.017),B)
 pp=spline([(-.38,side*.091,1.92),(-.35,side*.106,1.938),(-.315,side*.095,1.925)])
 tube('B_Eyelid',pp,.008,B,8)
 ring('B_Nostril',.02,.008,.008,(-.555,side*.075,1.78),Matrix.Rotation(pi/2,3,'X'),B,32)
 pp=spline([(-.585,side*.05,1.735),(-.53,side*.082,1.721),(-.46,side*.065,1.74)])
 tube('B_Lip',pp,.005,B,6)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();body=bpy.context.object;body.name='B_Horse_Sculpt_Body'
rem=body.modifiers.new('Anatomy_Union','REMESH');rem.mode='VOXEL';rem.voxel_size=.009;rem.use_smooth_shade=True;bpy.ops.object.modifier_apply(modifier=rem.name)
sm=body.modifiers.new('Sculpt_Smooth','SMOOTH');sm.factor=.75;sm.iterations=5;bpy.ops.object.modifier_apply(modifier=sm.name)
# Flowing locks, multiple tapered strands create modeled ridges.
for j in range(13):
 t=j/12;z=1.97-.74*t;x=-.12+.36*t
 pp=spline([(x,0,z),(x+.12,.015,z+.025),(x+.24,.035,z-.055),(x+.26,.02,z-.13),(x+.32,.01,z-.15)])
 r=[.035*(sin(pi*(i/(len(pp)-1)*.85+.1))**.7)*(1-i/(len(pp)-1))+.001 for i in range(len(pp))]
 tube('B_Mane_Lock_%02d'%j,pp,r,B,10)
 for side in [-1,1]:tube('B_Mane_Ridge',[(p.x,p.y+side*.018,p.z+.008) for p in pp],[v*.2 for v in r],B,6)
for j in range(10):
 off=(j-4.5)*.012
 pp=spline([(.33,off,1.02),(.52,off,.95),(.57,off,.76),(.49,off,.57),(.61,off,.43),(.78,off,.45),(.83,off,.52)])
 tube('B_Tail_Lock_%02d'%j,pp,[.025*(1-i/(len(pp)-1))+.002 for i in range(len(pp))],B,10)
bpy.ops.object.select_all(action='DESELECT')
for o in s.objects:
 if o.type=='MESH':o.select_set(True)
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  area.spaces.active.shading.type='SOLID';area.spaces.active.overlay.show_overlays=False
  area.spaces.active.region_3d.view_location=(0,0,1);area.spaces.active.region_3d.view_distance=5.6;area.spaces.active.region_3d.view_rotation=Vector((2,-7,2)).to_track_quat('Z','Y')
bpy.data.libraries.write(OUT+'/BBB_Ornaments_AB_Geometry.blend',{s},fake_user=True)
print('Geometry saved',len(s.objects),'objects',sum(len(o.data.polygons) for o in s.objects if o.type=='MESH'),'polygons')
