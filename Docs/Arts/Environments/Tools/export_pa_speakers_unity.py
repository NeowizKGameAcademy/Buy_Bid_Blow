import bpy,json,struct
from pathlib import Path
from mathutils import Vector
scene=bpy.data.scenes['A51_Staff_Portraits'];bpy.context.window.scene=scene
out=Path('C:/course/unity/BuyBidBlow/Assets/Project/Content/Props/PASpeaker/Source');out.mkdir(parents=True,exist_ok=True)
root=scene.objects['PA_01_Left_Lounge'];dg=bpy.context.evaluated_depsgraph_get()
convert=lambda v:[float(v.x),float(v.z),float(v.y)]
parts=[]
for ob in root.children_recursive:
 if ob.type!='MESH':continue
 ev=ob.evaluated_get(dg);me=ev.to_mesh(preserve_all_data_layers=True,depsgraph=dg);me.calc_loop_triangles()
 matrix=root.matrix_world.inverted()@ob.matrix_world;normal=matrix.to_3x3().inverted().transposed();vs=[];ts=[];cache={}
 for tri in me.loop_triangles:
  indices=[]
  for li in tri.loops:
   p=(matrix@me.vertices[me.loops[li].vertex_index].co)*1.5;n=(normal@me.corner_normals[li].vector).normalized();row=tuple(convert(p)+convert(n)+[0.,0.]);key=tuple(round(x,7) for x in row)
   if key not in cache:cache[key]=len(vs);vs.append(row)
   indices.append(cache[key])
  ts.extend([indices[0],indices[2],indices[1]])
 filename=ob.name+'.bytes'
 with (out/filename).open('wb') as f:
  f.write(struct.pack('<ii',len(vs),len(ts)))
  for row in vs:f.write(struct.pack('<8f',*row))
  f.write(struct.pack('<%si'%len(ts),*ts))
 parts.append({'file':filename,'name':ob.name,'dark':'Shadow' in me.materials[0].name,'vertices':len(vs),'triangles':len(ts)//3});ev.to_mesh_clear()
placements=[]
for r in root.users_collection[0].objects:
 if r.parent:continue
 rot=r.rotation_euler.to_matrix()
 placements.append({'name':r.name,'position':convert(r.location),'forward':convert(rot@Vector((0,1,0))),'up':convert(rot@Vector((0,0,1)))})
assert len(placements)==7
(out/'layout.json').write_text(json.dumps({'parts':parts,'placements':placements},indent=2));print('EXPORTED',len(placements),parts,flush=True)
