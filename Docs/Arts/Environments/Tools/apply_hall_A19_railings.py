import bpy,math,bmesh
from mathutils import Vector,Matrix
S=bpy.context.scene
assert S.name=='BBB_Hall_A18_Length40m'
S.name='BBB_Hall_A19_WideLandings_Railings'
# Unique mesh data preserves earlier scenes stored in the same file.
for o in list(S.objects):
 if o.name.startswith('Hall_Rails_'):
  for c in list(o.users_collection):
   if c in list(S.collection.children_recursive) or c==S.collection:c.objects.unlink(o)
for o in S.objects:
 if o.name.startswith(('Hall_Steps_','COL_StairRamp_')):
  sign=-1 if o.location.x<0 or '_-1.' in o.name else 1
  o.location.x-=sign*1.4
 if o.name.startswith(('Hall_Landing_','Landing_Enclosed_Base_')):
  o.data=o.data.copy();M=o.matrix_world.copy();inv=M.inverted()
  for v in o.data.vertices:
   p=M@v.co;sgn=1 if p.x>0 else -1
   p.x=sgn*(14-(14-abs(p.x))*3.2/1.8);v.co=inv@p
 if o.name.startswith('COL_'):o.hide_set(True);o.hide_render=True
bpy.context.view_layer.update()
with bpy.data.libraries.load('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Railings_Textured.blend',link=False) as (src,dst):
 dst.objects=[n for n in src.objects if n.startswith('01A_Straight_L_')]
parts=[o for o in dst.objects if o]
wood=next(m for m in bpy.data.materials if m.name.startswith('BBB_Railing_Walnut'))
col=bpy.data.collections.new('A19_New_Railings');S.collection.children.link(col)
posts={}
def post(p,angle=0):
 key=tuple(round(v,3) for v in p)
 if key in posts:return
 posts[key]=True
 for source in parts:
  o=source.copy();o.data=source.data.copy();o.parent=None;col.objects.link(o)
  o.name='A19_Post_%03d_%s'%(len(posts),source.name.split('_L_')[-1])
  for v in o.data.vertices:v.co.x+=1
  o.location=p;o.rotation_euler=(0,0,angle)
def mesh(name,vs,fs):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(name,me);col.objects.link(o);me.materials.append(wood)
 uv=me.uv_layers.new(name='WoodUV')
 for p in me.polygons:
  for li in p.loop_indices:
   v=me.vertices[me.loops[li].vertex_index].co
   uv.data[li].uv=(v.z/.8,(v.x+v.y)/1.6)
 mod=o.modifiers.new('Soft edges','BEVEL');mod.width=.006;mod.segments=2
 return o
def rail(name,points,z,depth,height):
 vs=[]
 for i,p in enumerate(points):
  d=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)]);d.z=0;d.normalize();n=Vector((-d.y,d.x,0))
  for side,up in [(-1,-1),(1,-1),(1,1),(-1,1)]:
   v=Vector(p)+n*(side*depth/2)+Vector((0,0,z+up*height/2));vs.append(v)
 fs=[(3,2,1,0),tuple(4*(len(points)-1)+i for i in range(4))]
 fs += [(4*j+k,4*j+(k+1)%4,4*(j+1)+(k+1)%4,4*(j+1)+k) for j in range(len(points)-1) for k in range(4)]
 return mesh(name,vs,fs)
def sample(points,t):
 lens=[0]
 for a,b in zip(points,points[1:]):lens.append(lens[-1]+(Vector(b)-Vector(a)).length)
 dist=t*lens[-1]
 for i in range(len(points)-1):
  if dist<=lens[i+1]+1e-7:return Vector(points[i]).lerp(Vector(points[i+1]),(dist-lens[i])/(lens[i+1]-lens[i]))
 return Vector(points[-1])
def run(name,points,bays):
 rail(name+'_Top',points,1.035,.20,.13);rail(name+'_Bottom',points,.25,.16,.14)
 for i in range(bays+1):
  p=sample(points,i/bays);d=sample(points,min(1,i/bays+.001))-sample(points,max(0,i/bays-.001));post(p,math.atan2(d.y,d.x))
 length=sum((Vector(b)-Vector(a)).length for a,b in zip(points,points[1:]))
 for b in range(bays):
  n=max(2,round(length/bays/.24))
  for j in range(1,n):
   t=(b+j/n)/bays;p=sample(points,t)
   # Vertical balusters, even on inclined runs.
   q=p+Vector((.075,0,0));rail(name+'_Spindle_%02d_%02d'%(b,j),[p,q],.635,.10,.67)
for sign in [-1,1]:
 for y in [2.26,5.14]:
  run('A19_Stair_%s_%s'%(sign,y),[(sign*3.95,y,1/6),(sign*10.66,y,4)],4)
 # Front landing edge. Shared terminal post with inclined front rail.
 run('A19_Landing_%s'%sign,[(sign*10.66,2.26,4),(sign*13.82,2.26,4)],2)
 # Rear of landing remains clear for the turn into the mezzanine.
 run('A19_MezzStraight_%s'%sign,[(sign*10.66,5.67,4),(sign*3.50,5.67,4)],4)
o=next(o for o in S.objects if o.name.startswith('Hall_Mezzanine_6.') and 'Border' not in o.name)
p=max((p for p in o.data.polygons if p.normal.z>.5),key=lambda p:p.area)
outline=[o.matrix_world@o.data.vertices[i].co for i in p.vertices]
start=next(i for i,p in enumerate(outline) if abs(p.x+3.5)<.01 and abs(p.y-5.5)<.01)
curve=outline[start:]
# Offset toward the mezzanine interior; vertical posts remain entirely on the slab.
offset=[]
for i,p in enumerate(curve):
 d=curve[min(i+1,len(curve)-1)]-curve[max(i-1,0)];d.z=0;d.normalize();n=Vector((-d.y,d.x,0));offset.append(p+n*.17)
# Smooth short transition to the straight guards, with no doubled corner posts.
offset[0]=Vector((-3.5,5.67,4));offset[-1]=Vector((3.5,5.67,4))
run('A19_MezzCurve',offset,6)
for im in bpy.data.images:
 if im.source=='FILE' and im.has_data and not im.packed_file:
  try:im.pack()
  except:pass
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  a.spaces.active.shading.type='MATERIAL';a.spaces.active.overlay.show_overlays=False
  r=a.spaces.active.region_3d;eye=Vector((0,-15,8));target=Vector((0,5,3));r.view_rotation=(eye-target).to_track_quat('Z','Y');r.view_location=target;r.view_distance=(eye-target).length;r.view_perspective='PERSP'
bpy.ops.wm.save_as_mainfile(filepath='C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Models/BBB_Hall_A19_WideLandings_Railings.blend')
print('COMPLETE',len(posts),'posts;',len(col.objects),'railing parts')
