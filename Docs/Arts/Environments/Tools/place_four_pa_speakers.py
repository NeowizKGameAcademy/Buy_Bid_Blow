import bpy, math, json
from mathutils import Vector
from pathlib import Path
scene=bpy.data.scenes['A51_Staff_Portraits'];bpy.context.window.scene=scene
out=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Props/PA_Speaker')
source=bpy.data.objects['PA_Speaker_ROOT'];col=source.users_collection[0]
specs=[('01_Left_Lounge',(-8,10.6,2.7),(-1,0,0),(-5.5,7.1,1.7)),('02_Right_Lounge',(8,10.6,2.7),(1,0,0),(5.5,7.1,1.7)),('03_Rear_Information',(-2.2,14,2.7),(0,1,0),(1.8,11.5,1.7)),('04_Auction_Hall',(10,-10,3.2),(1,0,0),(7,-5.5,1.7))]
roots=[];records=[]
for index,(name,origin,direction,eye) in enumerate(specs):
 if index==0: root=source
 else:
  mapping={}
  for old in [source]+list(source.children_recursive):
   new=old.copy();new.name=name+'_'+old.name;col.objects.link(new);mapping[old]=new
  for old,new in mapping.items():new.parent=mapping.get(old.parent);new.matrix_parent_inverse=old.matrix_parent_inverse.copy();new.matrix_basis=old.matrix_basis.copy()
  root=mapping[source]
 wall=scene.objects['A18_Walls_Ellipse_DoorOpenings'];inv=wall.matrix_world.inverted()
 hit,loc,normal,face=wall.ray_cast(inv@Vector(origin),inv.to_3x3()@Vector(direction))
 loc=wall.matrix_world@loc;normal=(wall.matrix_world.to_3x3()@normal).normalized()
 assert hit and ('Wall' in wall.name or 'wall' in wall.name), (name,wall.name if wall else None)
 root.name='PA_'+name;root.location=loc+normal*.071;root.rotation_euler=(0,0,math.atan2(normal.x,-normal.y))
 root['mount_height_m']=origin[2];root['zone']=name
 roots.append(root);records.append({'zone':name,'position':list(root.location),'rotation':list(root.rotation_euler),'wall':wall.name,'height':origin[2]})
 bpy.context.view_layer.update()
cam=scene.camera
scene.cycles.samples=24
scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
for light in list(scene.objects):
 if light.name in ['LoungeFill','WallFill']:bpy.data.objects.remove(light,do_unlink=True)
for root,(_,origin,direction,eye) in zip(roots,specs):
 ld=bpy.data.lights.new(root.name+'_PreviewFill',type=next(i.identifier for i in bpy.types.Light.bl_rna.properties['type'].enum_items if i.identifier=='AREA'));ld.energy=420;ld.size=4
 lo=bpy.data.objects.new(ld.name,ld);scene.collection.objects.link(lo);lo.location=root.location-Vector(direction)*3+Vector((0,0,.4));lo.rotation_euler=(root.location-lo.location).to_track_quat('-Z','Y').to_euler()
assert len([o for o in col.objects if o.parent is None])==4
(out/'Four_Speaker_Positions.json').write_text(json.dumps(records,indent=2))
cam.location=specs[0][3];cam.rotation_euler=(Vector((-11,10.6,1.7))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=32
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Hall_Four_Speakers.blend'))
for root,(name,origin,direction,eye) in zip(roots,specs):
 cam.location=eye
 target=root.location.copy();target.z=1.7 if origin[2]<3 else 2.3
 cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=32
 scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
 print('CAPTURED',name,flush=True)
print('FOUR_SPEAKERS_VERIFIED',json.dumps(records),flush=True)
