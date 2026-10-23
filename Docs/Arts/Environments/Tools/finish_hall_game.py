import bpy,math,os,json
from mathutils import Vector
root=r'C:/course/unity/BuyBidBlow/Assets/Project/Content/Levels/Exchange'
tex=root+'/Textures';os.makedirs(root+'/Models',exist_ok=True)
source=bpy.context.scene
s=bpy.data.scenes.new('BBB_Hall_GameReady');bpy.context.window.scene=s
colls={};copies={}
for c in source.collection.children:
 nc=bpy.data.collections.new('Game_'+c.name);s.collection.children.link(nc);colls[c.name]=nc
 for old in c.objects:
  o=old.copy();o.data=old.data.copy();nc.objects.link(o);copies[old.name]=o
for oldname,o in copies.items():
 old=source.objects[oldname]
 if old.parent:o.parent=copies.get(old.parent.name)
materials={}
spec=[('Hall_Floor','Floor',.65,0,.12,(.45,.24,.1,1)),('Hall_Wood','Wood',.72,0,.10,(.2,.08,.03,1)),('Hall_Teal','Plaster',.9,0,.05,(.02,.08,.07,1)),('Hall_Ceiling','Plaster',.9,0,.05,(.01,.04,.04,1)),('Hall_Brass','Brass',.48,.8,.08,(.45,.25,.07,1))]
for name,normal,rough,metal,strength,color in spec:
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=color
 nt=m.node_tree;bs=next(n for n in nt.nodes if n.type=='BSDF_PRINCIPLED')
 bs.inputs['Roughness'].default_value=rough;bs.inputs['Metallic'].default_value=metal
 al=nt.nodes.new('ShaderNodeTexImage');al.image=bpy.data.images.load(tex+'/'+name+'.png',check_existing=True)
 nt.links.new(al.outputs['Color'],bs.inputs['Base Color'])
 normalimg=nt.nodes.new('ShaderNodeTexImage');normalimg.image=bpy.data.images.load(tex+'/Hall_'+normal+'_Normal.png',check_existing=True);normalimg.image.colorspace_settings.name='Non-Color'
 n=nt.nodes.new('ShaderNodeNormalMap');n.inputs['Strength'].default_value=strength
 nt.links.new(normalimg.outputs['Color'],n.inputs['Color']);nt.links.new(n.outputs['Normal'],bs.inputs['Normal']);materials[name]=m
# Single-material chunks; preserve door leaves as independent objects/pivots.
buckets={}
for oldname,o in copies.items():
 if o.type!='MESH':continue
 if oldname in ['A10_Foundation','A10_Mezzanine_Bulged']:mat='Hall_Floor';cat='Floor' if 'Foundation' in oldname else 'Mezzanine'
 elif 'Wall' in oldname:mat='Hall_Teal';cat='Walls'
 elif 'Dome_Panel' in oldname:mat='Hall_Ceiling';cat='Roof'
 elif any(k in oldname for k in ['Header','Jamb']):mat='Hall_Brass';cat='DoorFrames'
 else:mat='Hall_Wood';cat='Wood'
 if 'Step_' in oldname:cat='Steps'
 elif 'Landing' in oldname and 'Rail' not in oldname:cat='Landing'
 elif 'Rail' in oldname or 'FrontLeft' in oldname or 'FrontRight' in oldname:cat='Rails'
 elif 'Column' in oldname:cat='Columns'
 elif 'Dome_Rib' in oldname or 'Dome_Ring' in oldname:cat='RoofWood'
 o.data.materials.clear();o.data.materials.append(materials[mat])
 # Use metre-based planar UVs; floor tile 4m, wood grain longitudinal on beams.
 uv=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
 span=[max(v.co[i] for v in o.data.vertices)-min(v.co[i] for v in o.data.vertices) for i in range(3)]
 for p in o.data.polygons:
  drop=max(range(3),key=lambda i:abs(p.normal[i]));axes=[i for i in range(3) if i!=drop]
  if mat=='Hall_Wood':axes.sort(key=lambda i:span[i])
  for li in p.loop_indices:
   v=o.data.vertices[o.data.loops[li].vertex_index].co
   uv.data[li].uv=(v[axes[0]]/4,v[axes[1]]/4)
 if '_Leaf_' in oldname:
  o.name=oldname;continue
 # 8 sectors retain culling granularity rather than one building-wide mesh.
 center=sum((o.matrix_world@v.co for v in o.data.vertices),Vector())/len(o.data.vertices)
 sector=int((math.atan2(center.y,center.x)+math.pi)/(2*math.pi)*8)%8
 buckets.setdefault((cat,mat,sector),[]).append(o)
for (cat,mat,sector),obs in buckets.items():
 bpy.ops.object.select_all(action='DESELECT')
 for o in obs:o.select_set(True)
 bpy.context.view_layer.objects.active=obs[0]
 if len(obs)>1:bpy.ops.object.join()
 o=obs[0];o.name='Hall_'+cat+'_'+str(sector)
 # Consolidate identical imported material slots after join.
 for p in o.data.polygons:p.material_index=0
 o.data.materials.clear();o.data.materials.append(materials[mat])
# Smooth ramp colliders for stairs, export-only hidden render geometry.
cc=bpy.data.collections.new('Game_Collision');s.collection.children.link(cc)
for side in [-1,1]:
 x0=side*5.2;x1=side*12.2
 vs=[(x0,3.7,0),(x0,5.3,0),(x1,5.3,0),(x1,3.7,0),(x1,3.7,4),(x1,5.3,4)]
 fs=[(0,3,2,1),(0,1,5,4),(3,4,5,2),(0,4,3),(1,2,5)]
 if side>0:fs=[tuple(reversed(f)) for f in fs]
 m=bpy.data.meshes.new('Ramp');m.from_pydata(vs,[],fs);m.update()
 o=bpy.data.objects.new('COL_StairRamp_'+str(side),m);cc.objects.link(o);o.hide_render=True
# Reference lighting, no added visible props.
lc=bpy.data.collections.new('Game_Preview_Lighting');s.collection.children.link(lc)
for i,(x,y,z) in enumerate([(-7,-7,7),(7,-7,7),(-7,7,10),(7,7,10),(0,0,12),(0,12,7),(0,8,2.4)]):
 d=bpy.data.lights.new('PreviewLight','POINT');d.energy=2300 if z>5 else 550;d.color=(1,.78,.52);d.shadow_soft_size=3
 o=bpy.data.objects.new('PreviewLight_'+str(i),d);lc.objects.link(o);o.location=(x,y,z)
s.world=bpy.data.worlds.new('HallWorld');s.world.use_nodes=True
bg=next(n for n in s.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.18,.21,.23,1);bg.inputs['Strength'].default_value=.5
s.camera=copies['A10_Interior'];s.camera.data.lens=20
try:s.render.engine='BLENDER_EEVEE'
except TypeError as e:print(e)
s.render.resolution_x=1400;s.render.resolution_y=960;s.render.resolution_percentage=100
s.render.filepath=r'C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Models/Hall_Materials_Preview.png'
for o in s.objects:o.select_set(o.type=='MESH')
bpy.ops.export_scene.fbx(filepath=root+'/Models/AuctionHall.fbx',use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',bake_anim=False)
for o in s.objects:o.select_set(False)
bpy.ops.wm.save_as_mainfile(filepath=r'C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Models/BBB_Hall_Textured.blend')
print(json.dumps({'mesh_count':sum(o.type=='MESH' for o in s.objects),'materials':list(materials),'fbx':root+'/Models/AuctionHall.fbx'}))
