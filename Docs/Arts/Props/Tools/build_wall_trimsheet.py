import bpy,os
P='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/Wall_TrimSheet'
os.makedirs(P,exist_ok=True)
s=bpy.context.scene;s.name='BBB_Wall_TrimSheet_Review'
old=s.objects.get('Trim_Review')
if old:old.hide_set(True);old.hide_render=True
me=bpy.data.meshes.new('Wall_Trim_Bake');vs=[];fs=[]
bands=[(0,.125,'Plinth',1.12),(.125,.25,'Moulding',1.12),(.25,.75,'Boards',1.12),(.75,1,'X_Wood',1.008)]
for i,(lo,hi,name,value) in enumerate(bands):
 vs.extend([(-2,0,lo*4),(2,0,lo*4),(2,0,hi*4),(-2,0,hi*4)]);fs.append(tuple(i*4+j for j in range(4)))
me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new('Custom_TrimSheet',me);s.collection.objects.link(o)
u=me.uv_layers.new(name='AtlasUV');src=me.uv_layers.new(name='SourceUV')
for i,p in enumerate(me.polygons):
 lo,hi,name,val=bands[i]
 for j,li in enumerate(p.loop_indices):
  u.data[li].uv=[(0,lo),(1,lo),(1,hi),(0,hi)][j]
  src.data[li].uv=([(0,0),(0,2),(1,2),(1,0)] if name!='Boards' else [(0,0),(2,0),(2,1),(0,1)])[j]
me.uv_layers.active=u
base=bpy.data.materials['BBB_Wall_Light_Walnut'];mats=[]
for i,(lo,hi,name,value) in enumerate(bands):
 m=base.copy();m.name='Bake_'+name;nt=m.node_tree
 uv=nt.nodes.new('ShaderNodeUVMap');uv.uv_map='SourceUV'
 for n in list(nt.nodes):
  if n.type=='TEX_IMAGE':nt.links.new(uv.outputs['UV'],n.inputs['Vector'])
 for n in nt.nodes:
  if n.type=='HUE_SAT':n.inputs['Value'].default_value=value
 em=nt.nodes.new('ShaderNodeEmission');out=next(n for n in nt.nodes if n.type=='OUTPUT_MATERIAL');nt.links.new(em.outputs[0],out.inputs['Surface'])
 target=nt.nodes.new('ShaderNodeTexImage');target.name='BAKE_TARGET';nt.nodes.active=target
 me.materials.append(m);me.polygons[i].material_index=i;mats.append(m)
for ob in s.objects:ob.select_set(False)
o.hide_set(False);o.select_set(True);bpy.context.view_layer.objects.active=o
try:s.render.engine='CYCLES'
except TypeError as e:raise RuntimeError(str(e))
s.cycles.samples=1;s.cycles.device='CPU';s.render.bake.margin=0
for channel in ['BaseColor','Normal','Roughness']:
 im=bpy.data.images.new('BBB_WallTrim_'+channel,width=2048,height=2048,alpha=False)
 if channel!='BaseColor':im.colorspace_settings.name='Non-Color'
 for m in mats:
  nt=m.node_tree;target=nt.nodes.get('BAKE_TARGET');target.image=im;nt.nodes.active=target
  em=next(n for n in nt.nodes if n.type=='EMISSION')
  for link in list(em.inputs[0].links):nt.links.remove(link)
  if channel=='BaseColor':nt.links.new(next(n for n in nt.nodes if n.type=='HUE_SAT').outputs[0],em.inputs[0])
  elif channel=='Normal':
   tex=next(n for n in nt.nodes if n.type=='TEX_IMAGE' and n.image and 'Normal' in n.image.name and n!=target)
   mix=nt.nodes.new('ShaderNodeMixRGB');mix.inputs[0].default_value=.18;mix.inputs[1].default_value=(.5,.5,1,1);nt.links.new(tex.outputs['Color'],mix.inputs[2]);nt.links.new(mix.outputs[0],em.inputs[0])
  else:em.inputs[0].default_value=(.56,.56,.56,1)
 bpy.ops.object.bake(type='EMIT')
 im.filepath_raw=P+'/BBB_WallTrim_'+channel+'.png';im.file_format='PNG';im.save();im.pack()
# Finished image-based material.
m=bpy.data.materials.new('BBB_Wall_TrimSheet');m.use_nodes=True;nt=m.node_tree
bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
for channel in ['BaseColor','Normal','Roughness']:
 tex=nt.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images['BBB_WallTrim_'+channel]
 if channel=='Normal':
  n=nt.nodes.new('ShaderNodeNormalMap');nt.links.new(tex.outputs['Color'],n.inputs['Color']);nt.links.new(n.outputs[0],bs.inputs['Normal'])
 else:nt.links.new(tex.outputs['Color'],bs.inputs['Base Color' if channel=='BaseColor' else 'Roughness'])
me.materials.clear();me.materials.append(m)
for p in me.polygons:p.material_index=0
me.uv_layers.remove(src)
s.render.engine='BLENDER_EEVEE'
bpy.data.libraries.write(P+'/BBB_Wall_TrimSheet.blend',{s},fake_user=True)
print('SAVED 2K PBR maps and review blend:',P)
