import bpy,math
from mathutils import Vector
S=bpy.context.scene
assert 'Furniture' in S.name
P='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/'
# Preserve the geometry source and create a dedicated material-ready deliverable.
wood=bpy.data.materials['BBB_Column_Walnut'];brass=bpy.data.materials['BBB_Column_Brass']
wood.name='BBB_Furniture_Hall_Walnut';brass.name='BBB_Furniture_Hall_Brass'
def wood_variant(name,val):
 m=wood.copy();m.name=name
 n=next(n for n in m.node_tree.nodes if n.type=='HUE_SAT');n.inputs['Value'].default_value=val
 return m
edge=wood_variant('BBB_Furniture_Walnut_Edge',.60)
dark=wood_variant('BBB_Furniture_Walnut_Recess',.34)
def material(name,color,rough=.5,metal=0,texture=0,sheen=0,emission=0):
 m=bpy.data.materials.new('BBB_Furniture_'+name);m.use_nodes=True;m.diffuse_color=(*color,1)
 nt=m.node_tree;b=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
 b.inputs['Base Color'].default_value=(*color,1);b.inputs['Roughness'].default_value=rough;b.inputs['Metallic'].default_value=metal
 if 'Sheen Weight' in b.inputs:b.inputs['Sheen Weight'].default_value=sheen
 if emission:
  b.inputs['Emission Color'].default_value=(*color,1);b.inputs['Emission Strength'].default_value=emission
 if texture:
  n=nt.nodes.new('ShaderNodeTexNoise');n.inputs['Scale'].default_value=texture;n.inputs['Detail'].default_value=2
  bump=nt.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.16;bump.inputs['Distance'].default_value=.001
  nt.links.new(n.outputs['Fac'],bump.inputs['Height']);nt.links.new(bump.outputs['Normal'],b.inputs['Normal'])
 return m
red=material('Crimson_Woven_Cloth',(.22,.005,.012),.78,texture=220,sheen=.10)
green=material('Emerald_Woven_Cloth',(.008,.065,.025),.78,texture=220,sheen=.10)
paint=material('Deep_Green_Painted_Wood',(.012,.075,.043),.40,texture=100)
matmap={'Wood':wood,'Edge':edge,'DarkWood':dark,'Brass':brass,'Red':red,'Teal':green,
 'Screen':material('Monitor_Green_Glass',(.006,.034,.018),.23),
 'Green':material('Monitor_Bright_Green',(.10,.65,.18),.40,emission=.6),
 'DimGreen':material('Monitor_Dim_Green',(.035,.18,.065),.5,emission=.15),
 'Leaf':material('Leaf_Green',(.055,.19,.018),.48,texture=30),
 'LeafLight':material('Leaf_Young_Green',(.12,.29,.03),.46,texture=30),
 'Soil':material('Dark_Soil',(.035,.017,.007),.96,texture=85),
 'Pot':material('Terracotta',(.37,.105,.041),.80,texture=110),
 'Iron':material('Dark_Iron',(.085,.073,.056),.50,.75,texture=140),
 'Light':material('Warm_Lamp',(.95,.56,.16),.3,emission=2.5),
 'Paper':material('Ivory_Paper',(.64,.57,.40),.85,texture=180)}
count=0
for o in S.objects:
 if o.type!='MESH':continue
 old=o.data.materials[0].name;key=old.split('Furniture_Geo_')[-1].split('.')[0]
 if key not in matmap:raise RuntimeError((o.name,old))
 m=matmap[key]
 if key=='Red' and 'Trend_Arrow' in o.name:m=material('Monitor_Red_'+o.name,(.65,.025,.015),.4,emission=.4)
 if key=='Teal' and 'Book' in o.name:m=paint
 o.data.materials.clear();o.data.materials.append(m)
 for f in o.data.polygons:f.material_index=0
 # Wood maps follow the longest local board axis, including diagonal bracing.
 if key in ['Wood','Edge','DarkWood','Brass']:
  verts=o.data.vertices
  dims=[max(v.co[i] for v in verts)-min(v.co[i] for v in verts) for i in range(3)]
  length_axis=max(range(3),key=lambda i:dims[i])
  uv=o.data.uv_layers.active or o.data.uv_layers.new(name='FurnitureUV')
  for f in o.data.polygons:
   normal_axis=max(range(3),key=lambda i:abs(f.normal[i]))
   axes=[i for i in range(3) if i!=normal_axis]
   along=length_axis if length_axis in axes else axes[1];across=next(i for i in axes if i!=along)
   for li in f.loop_indices:
    v=verts[o.data.loops[li].vertex_index].co
    uv.data[li].uv=(v[across]/.45,v[along]/1.4)
 count+=1
S.name='BBB_Auction_Exchange_Furniture_Textured'
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  sp=a.spaces.active;sp.shading.type='MATERIAL';sp.shading.use_scene_world=False;sp.shading.use_scene_lights=False
  sp.overlay.show_overlays=False;r=sp.region_3d;eye=Vector((7,-14,10));target=Vector((-.4,.1,.8));r.view_rotation=(eye-target).to_track_quat('Z','Y');r.view_location=target;r.view_distance=8.5
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=P+'BBB_Auction_Exchange_Furniture_Textured.blend')
print('Assigned materials to',count,'meshes; packed images',[(i.name,tuple(i.size),bool(i.packed_file)) for i in bpy.data.images if i.source=='FILE' and i.users])

