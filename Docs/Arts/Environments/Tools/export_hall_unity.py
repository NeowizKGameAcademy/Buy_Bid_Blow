"""Export evaluated visible A88 geometry to native Unity mesh import data.
Coordinates: Blender (x,y,z) -> Unity (x,z,y); reverse triangle winding.
The .blend source is never changed. Run with Blender --background --python.
"""
import bpy, json, os, re, struct, math, hashlib
from collections import defaultdict
from mathutils import Vector

BASE=os.path.abspath('Assets/Project/Content/Levels/ExchangeFurnished')
DATA=os.path.abspath('Logs/HallImport')
os.makedirs(DATA,exist_ok=True);os.makedirs(BASE+'/Textures',exist_ok=True)
s=bpy.data.scenes['A51_Staff_Portraits'];bpy.context.window.scene=s
dg=bpy.context.evaluated_depsgraph_get()
visible=[o for o in s.objects if o.visible_get() and not o.hide_render]
convert=lambda v:[float(v[0]),float(v[2]),float(v[1])]
safe=lambda v:re.sub(r'[^a-zA-Z0-9_-]','_',v)
groups=[];owners={};materials=[];mat_ids={};image_paths={}
def group(name,kind,origin,**extra):
 d=dict(name=safe(name),kind=kind,position=convert(origin),parts=[],**extra)
 d['_origin']=Vector(origin);groups.append(d);return d
def assign(root,kind,**extra):
 g=group(root.name,kind,root.matrix_world.translation,**extra)
 for o in [root]+list(root.children_recursive):owners[o.name]=g
 return g
for o in visible:
 if o.type=='EMPTY' and ('Door_Pivot' in o.name or 'HingePivot' in o.name):
  assign(o,'door',angle=-95 if 'Right' in o.name else 95)
 elif o.type=='EMPTY' and (o.name.startswith('A41_') and '_Seat_' in o.name or 'Staff_Chair' in o.name and 'ROOT' in o.name or o.name.startswith(('A87_','A88_')) and any(k in o.name for k in ['Sofa_ROOT','Bench_ROOT'])):
  f=o.matrix_world.to_3x3()@Vector((0,-1,0));f.z=0;f.normalize()
  assign(o,'seat',forward=convert(f),slots=3 if 'Left_Sofa' in o.name else 2 if 'Sofa' in o.name or 'Bench' in o.name else 1)
for o in visible:
 if o.type=='EMPTY' and o.name.startswith('A73_') and o.parent is None:
  g=assign(o,'barrier',ropes=[],posts=[])
  for a in o.children:
   if a.type=='ARMATURE':
    pts=[a.matrix_world@b.head_local-g['_origin'] for b in sorted(a.data.bones,key=lambda b:b.name) if b.name.startswith('Rope_')]
    if pts:g['ropes'].append({'points':[convert(p) for p in pts],'length':sum((b-a).length for a,b in zip(pts,pts[1:]))})
   if a.name.endswith('_Base') and '_Post_' in a.name:g['posts'].append(convert(a.matrix_world.translation-g['_origin']))
banner=bpy.data.objects['A78_Auction_Rear_Banner'];assign(banner,'banner')
static={}
def upstream(socket,seen=None):
 seen=set() if seen is None else seen
 for l in socket.links:
  n=l.from_node
  if n in seen:continue
  seen.add(n)
  if n.type=='TEX_IMAGE' and n.image:return n
  for inp in n.inputs:
   r=upstream(inp,seen)
   if r:return r
 return None
def save_image(img):
 key=img.as_pointer()
 if key in image_paths:return image_paths[key]
 name=safe(img.name)+'_'+hashlib.sha1(img.name.encode()).hexdigest()[:6]+'.png'
 path=BASE+'/Textures/'+name
 oldpath=img.filepath_raw;oldfmt=img.file_format
 try:
  img.filepath_raw=path;img.file_format='PNG';img.save()
 except Exception as e:print('IMAGE_FAIL',img.name,str(e));return ''
 finally:img.filepath_raw=oldpath;img.file_format=oldfmt
 image_paths[key]='Textures/'+name;return image_paths[key]
def material(m):
 key=m.name if m else 'Default'
 if key in mat_ids:return mat_ids[key]
 d=dict(name=safe(key),color=[.5,.5,.5,1],metallic=0.,roughness=.65,baseMap='',normalMap='',emission=[0,0,0],alphaClip=False,scale=[1,1])
 nodes=list(m.node_tree.nodes) if m and m.use_nodes else []
 bs=next((n for n in nodes if n.type=='BSDF_PRINCIPLED'),None)
 if bs:
  d['color']=list(bs.inputs['Base Color'].default_value);d['metallic']=float(bs.inputs['Metallic'].default_value);d['roughness']=float(bs.inputs['Roughness'].default_value)
  for sock,key2 in [('Base Color','baseMap'),('Normal','normalMap')]:
   n=upstream(bs.inputs[sock])
   if n:
    d[key2]=save_image(n.image)
    if key2=='baseMap':
     d['color']=[1,1,1,1]
     maps=[x for x in nodes if x.type=='MAPPING']
     if maps:d['scale']=list(maps[0].inputs['Scale'].default_value)[:2]
  if 'Emission Color' in bs.inputs:
   d['emission']=[float(c)*float(bs.inputs['Emission Strength'].default_value) for c in bs.inputs['Emission Color'].default_value[:3]]
  d['alphaClip']=bs.inputs['Alpha'].is_linked or bs.inputs['Alpha'].default_value<.99
 if 'RB_Burgundy' in key:d.update(color=[.19,.008,.026,1],roughness=.85)
 if 'ON' in key and 'A63' in key:d['color']=[1,.86,.58,1];d['emission']=[1.2,.9,.5]
 if 'Bronze' in key:d.update(color=[.26,.15,.065,1],metallic=.85,roughness=.4)
 d['alphaClip'] |= any(k in key.lower() for k in ['foliage','leaf','areca','alpha','oval_auction'])
 i=len(materials);mat_ids[key]=i;materials.append(d);return i
material(bpy.data.materials.get('RB_Burgundy_Velvet_BK'))
buffers={};collision=[];source_count=0
def get_buffer(g,mid):
 key=(g['name'],mid)
 if key not in buffers:buffers[key]=[[],[],{}];g['parts'].append({'material':mid,'file':safe(g['name'])+'_'+str(mid)+'.bin'})
 return buffers[key]
for oi,o in enumerate(visible):
 if o.type not in {'MESH','CURVE','FONT','SURFACE'}:continue
 if o.name.endswith('_Velvet') and o.name.startswith('A73_'):continue
 ev=o.evaluated_get(dg)
 try:mesh=ev.to_mesh(preserve_all_data_layers=True,depsgraph=dg)
 except Exception:continue
 if not mesh or not mesh.polygons:continue
 mesh.calc_loop_triangles();source_count+=1
 g=owners.get(o.name)
 if g is None:
  # Spatial sections keep culling useful without thousands of tiny renderers.
  center=sum((o.matrix_world@Vector(c) for c in o.bound_box),Vector())/8
  cell=(math.floor(center.x/8),math.floor(center.y/8),math.floor(center.z/5))
  if cell not in static:static[cell]=group('Static_%s_%s_%s'%cell,'static',(0,0,0))
  g=static[cell]
 matrix=o.matrix_world;norm=matrix.to_3x3().inverted_safe().transposed()
 uv=mesh.uv_layers.active.data if mesh.uv_layers.active else None
 for tri in mesh.loop_triangles:
  mi=tri.material_index;m=mesh.materials[mi] if mi<len(mesh.materials) else None
  mid=material(m);vs,ts,cache=get_buffer(g,mid)
  ids=[]
  for li in tri.loops:
   loop=mesh.loops[li];v=matrix@mesh.vertices[loop.vertex_index].co-g['_origin'];n=(norm@mesh.corner_normals[li].vector).normalized()
   tex=tuple(uv[li].uv) if uv else (v.x*.5,v.y*.5)
   row=tuple(convert(v)+convert(n)+list(tex));key=tuple(round(x,6) for x in row)
   if key not in cache:cache[key]=len(vs);vs.append(row)
   ids.append(cache[key])
  ts.extend([ids[0],ids[2],ids[1]])
 # Main structural meshes, stairs, reception, tables and rails retain collision.
 if g['kind']=='static' and any(k in o.name for k in ['Hall_Steps','Hall_Landing','Hall_Mezzanine','A18_Floor_Ellipse','A18_Walls','Landing_Enclosed','A39_','A19_','Reception','Desk','Table','Column']):
  if any(k in o.name for k in ['Hall_','A18_','Landing_Enclosed','A39_']):
   cv=[convert(matrix@v.co) for v in mesh.vertices];ct=[i for t in mesh.loop_triangles for i in [t.vertices[0],t.vertices[2],t.vertices[1]]]
   if ct:collision.append((cv,ct))
  elif len(mesh.vertices)>0 and not any(k in o.name for k in ('Column_Lower_Band','Column_Upper_Band','Column_Capital_Lower_Moulding','A87_Left_Table_RA_Brass_Band')):
   pts=[matrix@Vector(c) for c in o.bound_box];lo=[min(v[i] for v in pts) for i in range(3)];hi=[max(v[i] for v in pts) for i in range(3)]
   if max(hi[i]-lo[i] for i in range(3))>.35:
    g.setdefault('boxes',[]).append({'center':convert([(a+b)/2 for a,b in zip(lo,hi)]),'size':convert([b-a for a,b in zip(lo,hi)])})
 ev.to_mesh_clear()
 if oi%500==0:print('EXPORT',oi,flush=True)
for g in groups:
 for part in g['parts']:
  vs,ts,_=buffers[(g['name'],part['material'])]
  with open(DATA+'/'+part['file'],'wb') as f:
   f.write(struct.pack('<ii',len(vs),len(ts)))
   for row in vs:f.write(struct.pack('<8f',*row))
   f.write(struct.pack('<%si'%len(ts),*ts))
 if g['kind']=='barrier':
  g['paths']=[{'points':[x for p in path['points'] for x in p],'length':path['length']} for path in g.pop('ropes')]
  g['postPositions']=[x for p in g.pop('posts') for x in p]
 g.pop('_origin')
cv=[];ct=[]
for vs,ts in collision:
 offset=len(cv);cv+=vs;ct += [i+offset for i in ts]
with open(DATA+'/collision.bin','wb') as f:
 f.write(struct.pack('<ii',len(cv),len(ct)))
 for row in cv:f.write(struct.pack('<3f',*row))
 f.write(struct.pack('<%si'%len(ct),*ct))
result={'source':bpy.data.filepath,'sourceObjects':source_count,'materials':materials,'groups':groups,'vertices':sum(len(b[0]) for b in buffers.values()),'triangles':sum(len(b[1])//3 for b in buffers.values())}
with open(DATA+'/manifest.json','w') as f:json.dump(result,f)
print('COMPLETE',source_count,len(groups),result['vertices'],result['triangles'],flush=True)
