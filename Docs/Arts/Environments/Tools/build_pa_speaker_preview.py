import bpy,math,bmesh
from pathlib import Path
from mathutils import Vector
scene=bpy.data.scenes['A51_Staff_Portraits'];bpy.context.window.scene=scene
out=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Props/PA_Speaker');out.mkdir(parents=True,exist_ok=True)
col=bpy.data.collections.new('PA_Speaker_Green')
scene.collection.children.link(col)
root=bpy.data.objects.new('PA_Speaker_ROOT',None);col.objects.link(root)
def material(name,rgb):
 m=bpy.data.materials.new(name);m.diffuse_color=(*rgb,1);m.use_nodes=True
 bs=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(*rgb,1);bs.inputs['Roughness'].default_value=.83
 return m
bodymat=material('PA_Matte_GrayGreen',(.050,.066,.049))
dark=material('PA_Grille_Inner_Shadow',(.009,.013,.010))
def box(name,dims,center,mat,bevel):
 bm=bmesh.new();bmesh.ops.create_cube(bm,size=1)
 for v in bm.verts:v.co.x*=dims[0];v.co.y*=dims[1];v.co.z*=dims[2]
 mesh=bpy.data.meshes.new(name);bm.to_mesh(mesh);bm.free();o=bpy.data.objects.new(name,mesh);col.objects.link(o);o.parent=root;o.location=center;mesh.materials.append(mat)
 if bevel:
  mod=o.modifiers.new('Soft corners','BEVEL');mod.width=bevel;mod.segments=3
  o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
 return o
box('Speaker_Cabinet',(.25,.10,.34),(0,0,0),bodymat,.013)
box('Speaker_Grille_Dark_Back',(.224,.003,.314),(0,-.052,0),dark,.009)
box('Speaker_Wall_Bracket',(.14,.018,.21),(0,.057,0),dark,.004)
verts=[];faces=[]
step=.007;nx=30;nz=42;r=.00235
for ix in range(nx):
 for iz in range(nz):
  cx=(ix-(nx-1)/2)*step;cz=(iz-(nz-1)/2)*step;base=len(verts)
  for j in range(8):
   a=j*math.pi/4;c=math.cos(a);s=math.sin(a);fac=(step/2)/max(abs(c),abs(s));verts.append((cx+c*fac,-.055,cz+s*fac))
  for j in range(8):
   a=j*math.pi/4;verts.append((cx+r*math.cos(a),-.055,cz+r*math.sin(a)))
  for j in range(8):faces.append((base+j,base+(j+1)%8,base+8+(j+1)%8,base+8+j))
mesh=bpy.data.meshes.new('Perforated_Grille_Mesh');mesh.from_pydata(verts,[],faces);mesh.materials.append(bodymat)
o=bpy.data.objects.new('Speaker_Perforated_Grille',mesh);col.objects.link(o);o.parent=root
print('Geometry built',flush=True)
bpy.data.libraries.write(str(out/'PA_Speaker_GrayGreen.blend'),{col},fake_user=True)
print('Asset saved',flush=True)
hit,loc,normal,idx,obj,mat=scene.ray_cast(bpy.context.evaluated_depsgraph_get(),Vector((-8,10.6,2.7)),Vector((-1,0,0)))
root.location=loc+normal*.071;root.rotation_euler[2]=math.atan2(normal.x,-normal.y)
root['mount_height_m']=2.7;root['size_m']='0.25 x 0.10 x 0.34'
bpy.context.view_layer.update()

camdata=bpy.data.cameras.new('Speaker_Preview_Camera');cam=bpy.data.objects.new('Speaker_Preview_Camera',camdata);scene.collection.objects.link(cam);scene.camera=cam
def camera(pos,target,lens):
 cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();camdata.lens=lens
camera((-.2,5.5,1.65),(-10.4,11.1,1.65),35)
print('ENGINE',scene.render.engine,flush=True)
try:scene.render.engine='CYCLES'
except TypeError:pass
scene.cycles.samples=16
scene.cycles.use_denoising=True
world=bpy.data.worlds.new('Speaker_Preview_Ambient');world.use_nodes=True;scene.world=world
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.65,.68,.73,1);bg.inputs[1].default_value=.6
for name,power,size,pos in [('LoungeFill',650,5,(-7,9,3.1)),('WallFill',250,3,(-10,10.6,3.2))]:
 ld=bpy.data.lights.new(name,type=next(i.identifier for i in bpy.types.Light.bl_rna.properties['type'].enum_items if i.identifier=='AREA'));ld.energy=power;ld.shape=next(i.identifier for i in ld.bl_rna.properties['shape'].enum_items if i.identifier=='DISK');ld.size=size
 lo=bpy.data.objects.new(name,ld);scene.collection.objects.link(lo);lo.location=pos;lo.rotation_euler=(Vector((-12,10.6,1.7))-lo.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1600;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.image_settings.file_format=next(i.identifier for i in scene.render.image_settings.bl_rna.properties['file_format'].enum_items if i.identifier=='PNG')
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Hall_Speaker_Placement.blend'))
scene.render.filepath=str(out/'Speaker_Placement.png');bpy.ops.render.render(write_still=True)
camera(tuple(root.location+normal*1.2+Vector((.3,.15,.06))),tuple(root.location),55)
scene.render.resolution_x=1000;scene.render.resolution_y=1000
scene.render.filepath=str(out/'Speaker_Closeup.png');bpy.ops.render.render(write_still=True)
print('DONE',flush=True)

