import bpy, numpy as np, math,json
from mathutils import Vector,Matrix
from pathlib import Path
out=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/Areca_Frond_Card');out.mkdir(exist_ok=True)
s=bpy.context.scene
src=bpy.data.objects['Plane.1730'];src.parent=None;src.matrix_world=Matrix.Identity(4)
for o in list(bpy.data.objects):
 if o!=src:bpy.data.objects.remove(o,do_unlink=True)
src.name='SOURCE_Frond_39852_faces'
a=np.array([v.co[:] for v in src.data.vertices]);xmin,ymin,zmin=a.min(0);xmax,ymax,zmax=a.max(0)
W=(xmax-xmin)*1.10;H=(ymax-ymin)*1.05;cx=(xmin+xmax)/2;cy=(ymin+ymax)/2
# Unlit color plus alpha projection, preserving source shader mixing and front/back colors.
original=list(src.data.materials)
for idx,m in enumerate(original):
 m=m.copy();src.data.materials[idx]=m;nt=m.node_tree
 for p in list(nt.nodes):
  if p.type!='BSDF_PRINCIPLED':continue
  em=nt.nodes.new('ShaderNodeEmission');tr=nt.nodes.new('ShaderNodeBsdfTransparent');mix=nt.nodes.new('ShaderNodeMixShader')
  bc=p.inputs['Base Color'];al=p.inputs['Alpha']
  if bc.is_linked:nt.links.new(bc.links[0].from_socket,em.inputs['Color'])
  else:em.inputs['Color'].default_value=bc.default_value
  if al.is_linked:nt.links.new(al.links[0].from_socket,mix.inputs[0])
  else:mix.inputs[0].default_value=al.default_value
  nt.links.new(tr.outputs[0],mix.inputs[1]);nt.links.new(em.outputs[0],mix.inputs[2])
  for link in list(p.outputs[0].links):nt.links.new(mix.outputs[0],link.to_socket)
camd=bpy.data.cameras.new('BakeCamera');cam=bpy.data.objects.new('BakeCamera',camd);s.collection.objects.link(cam);camd.type='ORTHO';camd.ortho_scale=H;cam.location=(cx,cy,zmax+2);cam.rotation_euler=(0,0,0);s.camera=cam
s.render.engine='CYCLES';s.cycles.samples=32;s.cycles.transparent_max_bounces=32
s.render.resolution_x=512;s.render.resolution_y=2048;s.render.resolution_percentage=100
# Camera portrait framing width fixed by aspect ratio.
W=H*512/2048
s.render.film_transparent=True;s.render.image_settings.file_format='PNG';s.render.image_settings.color_mode='RGBA';s.view_settings.view_transform='Standard';s.render.filepath=str(out/'Areca_Frond_BaseColor_Alpha.png')
bpy.ops.render.render(write_still=True)
for idx,m in enumerate(original):src.data.materials[idx]=m
# 12 rows, three vertices across. Median original depth retains droop; mild central crease.
vs=[];uvs=[]
for j in range(13):
 v=j/12;y=cy-H/2+v*H;near=a[np.abs(a[:,1]-y)<H/24]
 z=float(np.median(near[:,2])) if len(near) else 0
 for u in [0,.5,1]:vs.append((cx+(u-.5)*W,y,z-(abs(u-.5)*.018)));uvs.append((u,v))
fs=[]
for j in range(12):
 for k in range(2):i=j*3+k;fs.append((i,i+1,i+4,i+3))
me=bpy.data.meshes.new('FrondCard_48tris');me.from_pydata(vs,[],fs);me.update();uv=me.uv_layers.new(name='UVMap')
for p in me.polygons:
 for li in p.loop_indices:uv.data[li].uv=uvs[me.loops[li].vertex_index]
card=bpy.data.objects.new('CARD_Frond_48_triangles',me);s.collection.objects.link(card)
mat=bpy.data.materials.new('BBB_Areca_Frond_Card');mat.use_nodes=True;nt=mat.node_tree;p=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED');tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(out/'Areca_Frond_BaseColor_Alpha.png'));tex.image.pack();nt.links.new(tex.outputs['Color'],p.inputs['Base Color']);nt.links.new(tex.outputs['Alpha'],p.inputs['Alpha']);p.inputs['Roughness'].default_value=.55;mat.surface_render_method='DITHERED';mat.use_backface_culling=False;me.materials.append(mat)
src.location.x=-.22;card.location.x=.22
bpy.data.objects.remove(cam,do_unlink=True)
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  sp=area.spaces.active;target=Vector((cx,cy,0));eye=target+Vector((0,-.3,1.8));sp.region_3d.view_rotation=(target-eye).to_track_quat('-Z','Y');sp.region_3d.view_location=target;sp.region_3d.view_distance=1.65;sp.lens=45
s['README']='Left: BlenderKit original frond 39852 polygons. Right: 24 quads / 48 triangles. Unlit RGBA projection; curved card. Source Samuel Slavik / Artificial palm Areca 180 cm / BlenderKit royalty_free. Not a complete plant.'
bpy.ops.wm.save_as_mainfile(filepath=str(out/'BBB_Areca_Frond_Comparison.blend'))
print('BAKE_COMPLETE',flush=True)
