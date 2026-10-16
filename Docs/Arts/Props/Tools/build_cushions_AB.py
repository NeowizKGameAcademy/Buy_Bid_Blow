import bpy, math, os, json, bmesh
from mathutils import Vector
from math import sin, cos, pi, exp, sqrt

BASE='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/Cushions'
os.makedirs(BASE+'/Unity/Textures',exist_ok=True)
s=bpy.data.scenes.new('Cushions_AB');bpy.context.window.scene=s
s.unit_settings.system='METRIC'
teal=bpy.data.materials['BBB_Seating_TealVelvet'].copy();teal.name='Cushion_Teal_Velvet'
red=bpy.data.materials['BBB_Seating_BurgundyVelvet'].copy();red.name='Cushion_Burgundy_Velvet'
gold=bpy.data.materials['BBB_Seating_Brass'].copy();gold.name='Cushion_Gold_Embroidery'
bs=next(n for n in gold.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
bs.inputs['Metallic'].default_value=.45;bs.inputs['Roughness'].default_value=.43;bs.inputs['Sheen Weight'].default_value=.12
dark=gold.copy();dark.name='Cushion_Gold_Cord_Shadow'
next(n for n in dark.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'].default_value=(.32,.16,.035,1)
thread=red.copy();thread.name='Cushion_Burgundy_Tassel_Thread'
next(n for n in thread.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Roughness'].default_value=.56

def root(name,x):
 o=bpy.data.objects.new(name,None);s.collection.objects.link(o);o.location.x=x
 o['source_reference']='10_Cushion.png';o['independent_prop']=True;return o

def mesh(name,verts,faces,r,mat,uvs=None):
 d=bpy.data.meshes.new(name);d.from_pydata(verts,[],faces);d.update()
 o=bpy.data.objects.new(name,d);s.collection.objects.link(o);o.parent=r;d.materials.append(mat)
 for p in d.polygons:p.use_smooth=True
 uv=d.uv_layers.new(name='UVMap')
 for p in d.polygons:
  for li in p.loop_indices:
   idx=d.loops[li].vertex_index;v=d.vertices[idx].co
   uv.data[li].uv=uvs[idx] if uvs else (v.x*3,v.z*3)
 return o

def tube(name,points,r,mat,radius=.0008,sides=5,closed=False,radii=None):
 pts=[Vector(p) for p in points];vs=[];N=len(pts)
 for i,p in enumerate(pts):
  t=(pts[(i+1)%N]-pts[(i-1)%N]) if closed else pts[min(i+1,N-1)]-pts[max(i-1,0)]
  t.normalize();a=t.cross(Vector((0,1,0)))
  if a.length<.01:a=t.cross(Vector((1,0,0)))
  a.normalize();b=t.cross(a).normalized();rad=radius if radii is None else radii[i]
  for j in range(sides):vs.append(tuple(p+rad*(a*cos(2*pi*j/sides)+b*sin(2*pi*j/sides))))
 fs=[(i*sides+j,i*sides+(j+1)%sides,((i+1)%N)*sides+(j+1)%sides,((i+1)%N)*sides+j) for i in range(N if closed else N-1) for j in range(sides)]
 if not closed:fs.extend([tuple(range(sides-1,-1,-1)),tuple((N-1)*sides+j for j in range(sides))])
 return mesh(name,vs,fs,r,mat)

class Pillow:
 def __init__(self,name,w,h,d,z,x,mat,nx,ny):
  self.r=root(name+'_ROOT',x);self.name=name;self.w=w;self.h=h;self.d=d;self.z=z
  verts=[];uv=[]
  for side in [1,-1]:
   for j in range(ny+1):
    for i in range(nx+1):
     u=2*i/nx-1;v=2*j/ny-1;verts.append(self.point(u,v,side));uv.append((i/nx*1.5,j/ny*1.5))
  stride=nx+1;off=stride*(ny+1);faces=[]
  for j in range(ny):
   for i in range(nx):
    a=j*stride+i;faces.extend([(a,a+1,a+stride+1,a+stride),(off+a+stride,off+a+stride+1,off+a+1,off+a)])
  edge=list(range(nx+1))+[j*stride+nx for j in range(1,ny+1)]+[ny*stride+i for i in range(nx-1,-1,-1)]+[j*stride for j in range(ny-1,0,-1)]
  for i,a in enumerate(edge):b=edge[(i+1)%len(edge)];faces.append((a,off+a,off+b,b))
  mesh(name+'_Velvet_Body',verts,faces,self.r,mat,uv)
 def point(self,u,v,side=1,lift=0):
  rounded=1-.07*abs(u*v)**8
  x=self.w/2*u*(.90+.10*abs(v)**5)*rounded;z=self.h/2*v*(.90+.10*abs(u)**5)*rounded+self.z
  puff=max(0,(1-u*u)*(1-v*v))**.48
  folds=(sin(35*v+3*u)+.5*sin(57*v))*exp(-((1-abs(u))/.15)**2)+(sin(36*u-4*v)+.45*sin(61*u))*exp(-((1-abs(v))/.14)**2)
  # Pinched corners, full center, and small gathered folds by the stitched edges.
  edge=.0045+.0012*sin(4*u+3*v)
  depth=edge+self.d*.5*puff+.0035*folds*min(1,puff*5)
  y=-side*depth-lift if side==1 else -side*depth*.88
  return (x,y,z)
 def seam(self,N=256):
  pts=[]
  for j in range(N):
   a=2*pi*j/N;den=max(abs(cos(a)),abs(sin(a)));u=cos(a)/den;v=sin(a)/den
   # Small rounding of the four square tips.
   fac=1-.018*abs(u*v)**10;u*=fac;v*=fac
   x,_,z=self.point(u,v);pts.append(Vector((x,0,z)))
  return pts
 def embroider(self,name,uv,radius=.0007,mat=None):
  return tube(self.name+'_'+name,[self.point(u,v,lift=.0008) for u,v in uv],self.r,mat or gold,radius,4)
 def leaf(self,name,start,end,width=.06,bend=.02):
  a=Vector(start);b=Vector(end);t=(b-a).normalized();n=Vector((-t.y,t.x));vs=[];faces=[];rows=6;cols=3
  for i in range(rows):
   q=i/(rows-1);center=a.lerp(b,q)+n*bend*sin(pi*q);w=width*sin(pi*q)**.75+.0005
   for j in range(cols):
    lateral=j-1;p=center+n*w*lateral
    vs.append(self.point(p.x,p.y,lift=.0006+.0009*sin(pi*q)*(1-abs(lateral))))
  for i in range(rows-1):
   for j in range(cols-1):k=i*cols+j;faces.append((k,k+cols,k+cols+1,k+1))
  return mesh(self.name+'_'+name,vs,faces,self.r,gold)

def bezier(a,b,c,n=18):
 return [tuple(Vector(a)*(1-t)**2+Vector(b)*2*t*(1-t)+Vector(c)*t*t) for t in [i/(n-1) for i in range(n)]]

A=Pillow('Cushion_A',.52,.52,.14,.275,-.38,teal,28,28)
B=Pillow('Cushion_B',.62,.36,.13,.235,.36,red,30,22)

# A: two intertwined gold cords follow the sewn perimeter.
path=A.seam(256)
for strand in range(2):
 pts=[]
 for i,p in enumerate(path):
  tangent=(path[(i+1)%len(path)]-path[(i-1)%len(path)]).normalized();normal=Vector((0,-1,0));across=tangent.cross(normal).normalized()
  phase=2*pi*64*i/len(path)+strand*pi
  pts.append(p+.0015*(normal*cos(phase)+across*sin(phase)))
 tube('Cushion_A_Twisted_Gold_Cord_'+str(strand),pts,A.r,gold if strand==0 else dark,.0016,4,True)

# Elliptical double embroidered border, four fleur accents, and acanthus foliage.
for scale in [1,.955]:
 uv=[(.68*scale*cos(2*pi*j/96),.74*scale*sin(2*pi*j/96)) for j in range(97)]
 A.embroider('Oval_Embroidery',uv,.00085)
def fleur(pillow,u,v,size):
 for du,dv,w in [(0,1,.20),(-.55,.55,.18),(.55,.55,.18),(-.65,-.10,.14),(.65,-.10,.14)]:
  pillow.leaf('Fleur_Leaf',(u,v),(u+du*size,v+dv*size),size*w)
 pillow.embroider('Fleur_Tie',[(u-size*.35,v),(u+size*.35,v)],.0008)
for u,v in [(0,.74),(0,-.78),(-.69,0),(.69,0)]:fleur(A,u,v,.09)
A.embroider('Central_Stem',bezier((0,-.56),(-.025,.05),(0,.61),30),.00115)
A.leaf('Crown_Leaf',(0,.45),(0,.66),.052)
for side in [-1,1]:
 for idx,v in enumerate([-.40,-.22,-.04,.15,.32]):
  reach=[.34,.40,.32,.23,.13][idx];rise=[.27,.25,.23,.19,.15][idx]
  A.embroider('Acanthus_Stem',bezier((0,v),(side*reach*.55,v+rise),(side*reach,v+rise*.7),17),.00085)
  A.leaf('Acanthus_Blade',(side*.035,v+.025),(side*reach,v+rise*.7),.035 if idx<3 else .025,side*.04)
  A.leaf('Acanthus_Tip',(side*reach*.6,v+rise*.75),(side*(reach+.015),v+rise+.055),.028,-side*.025)
 # The smaller outer spray repeats the reference's open, airy branching silhouette.
 A.embroider('Outer_Spray',bezier((0,-.50),(side*.53,-.12),(side*.50,.41),28),.0008)
 for v,reach in [(-.25,.39),(-.04,.48),(.16,.51),(.34,.50)]:
  A.leaf('Outer_Leaf',(side*reach,v),(side*(reach+.10),v+.10),.027,side*.01)
  A.leaf('Outer_Leaf',(side*reach,v+.03),(side*(reach-.07),v+.16),.025,-side*.01)
for v,w in [(-.55,.06),(-.59,.045)]:A.embroider('Base_Knot',[(-w,v),(0,v-.015),(w,v)],.001)

# B: diagonals and small repeated embroidered fleur motifs, on the front face.
tube('Cushion_B_Piped_Perimeter',B.seam(192),B.r,thread,.0017,6,True)
for sign in [-1,1]:
 for offset in [-1.5,-.9,-.3,.3,.9,1.5]:
  uv=[]
  for i in range(121):
   u=-.91+1.82*i/120;v=sign*1.25*u+offset
   if -.90<=v<=.90:uv.append((u,v))
  if len(uv)>1:B.embroider('Diamond_Stitch',uv[::3]+[uv[-1]],.00055)
for u in [-.72,-.24,.24,.72]:
 for v in [-.60,0,.60]:fleur(B,u,v,.065)
for u in [-.48,0,.48]:
 for v in [-.30,.30]:fleur(B,u,v,.065)

def tassel(pillow,side,top):
 # A softly ribbed tassel skirt plus a restrained number of curved outer strands.
 u=side*.99;v=.99 if top else -.99;x,_,z=pillow.point(u,v)
 anchor=Vector((x,0,z));cap=anchor+Vector((side*.019,-.002,-.012 if top else -.002))
 tube('Cushion_B_Tassel_Loop',[anchor,anchor+Vector((side*.014,0,.012)),cap],B.r,thread,.0038,7)
 axis=Vector((side*.16,-.08,-1)).normalized();length=.074 if top else .042
 points=[cap-axis*.006,cap,cap+axis*.007,cap+axis*.013]
 tube('Cushion_B_Tassel_Gold_Collar',points,B.r,gold,sides=10,radii=[.005,.007,.007,.005])
 start=cap+axis*.012;a=Vector((1,0,0));a=(a-axis*a.dot(axis)).normalized();b=axis.cross(a)
 verts=[];faces=[];N=20;levels=6
 for j in range(levels):
  t=j/(levels-1);center=start+axis*(length*t);rad=.004+.014*t**.8
  for k in range(N):
   angle=2*pi*k/N;rr=rad*(1+.055*cos(angle*12));end=.003*sin(k*2.7)*t**6
   verts.append(tuple(center+axis*end+rr*(a*cos(angle)+b*sin(angle))))
 for j in range(levels-1):
  for k in range(N):faces.append((j*N+k,j*N+(k+1)%N,(j+1)*N+(k+1)%N,(j+1)*N+k))
 faces.extend([tuple(range(N-1,-1,-1)),tuple((levels-1)*N+k for k in range(N))])
 mesh('Cushion_B_Tassel_Ribbed_Skirt',verts,faces,B.r,thread)
 for k in range(12):
  angle=2*pi*k/12;pts=[]
  for j in range(6):
   t=j/5;rad=.0048+.0145*t**.8;pos=start+axis*(length*t+.004*sin(k*2.7)*t**6)+rad*(a*cos(angle)+b*sin(angle));pts.append(pos)
  tube('Cushion_B_Tassel_Free_Thread',pts,B.r,thread,.0007,4)
for side in [-1,1]:
 for top in [True,False]:tassel(B,side,top)

# Consolidate by material for a compact, editable prop hierarchy.
report=[]
for pillow in [A,B]:
 groups={}
 for ob in list(pillow.r.children):groups.setdefault(ob.data.materials[0],[]).append(ob)
 for mat,parts in groups.items():
  bpy.ops.object.select_all(action='DESELECT')
  for ob in parts:ob.select_set(True)
  bpy.context.view_layer.objects.active=parts[0]
  if len(parts)>1:bpy.ops.object.join()
  ob=bpy.context.object;ob.name=pillow.name+'_'+mat.name.replace('Cushion_','')
  bm=bmesh.new();bm.from_mesh(ob.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free();ob.data.update()
  ob['material_role']=mat.name
 meshes=list(pillow.r.children);vs=ts=0
 for ob in meshes:ob.data.calc_loop_triangles();vs+=len(ob.data.vertices);ts+=len(ob.data.loop_triangles)
 report.append({'asset':pillow.name,'vertices':vs,'triangles':ts,'meshes':len(meshes)})
s['geometry_report']=json.dumps(report)
bpy.ops.object.select_all(action='DESELECT')
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  a.spaces.active.shading.type='MATERIAL';a.spaces.active.shading.studio_light='forest.exr';a.spaces.active.overlay.show_overlays=False
  v=a.spaces.active.region_3d;v.view_location=(0,0,.26);v.view_distance=1.15;v.view_rotation=Vector((.12,-1,.20)).to_track_quat('Z','Y')
print(json.dumps(report,indent=2))
