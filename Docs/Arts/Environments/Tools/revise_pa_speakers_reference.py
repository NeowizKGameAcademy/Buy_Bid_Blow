import bpy,math,json
from pathlib import Path
from mathutils import Vector
scene=bpy.data.scenes['A51_Staff_Portraits'];bpy.context.window.scene=scene
out=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Props/PA_Speaker/Reference_Revision');out.mkdir(exist_ok=True)
old=bpy.data.objects['PA_03_Rear_Information']
for obj in list(old.children_recursive)+[old]:bpy.data.objects.remove(obj,do_unlink=True)
light=bpy.data.objects.get('PA_03_Rear_Information_PreviewFill')
if light:bpy.data.objects.remove(light,do_unlink=True)
source=bpy.data.objects['PA_01_Left_Lounge'];col=source.users_collection[0]
specs=[('Reference_01_Lobby_TV',(10,-11,9.25),(1,0,0),(7,-7,5.3)),('Reference_02_Opposite_TV',(-10,-11,9.25),(-1,0,0),(-7,-7,5.3)),('Reference_03_Banner_Right',(8,12.4,9.25),(1,0,0),(4.7,7.5,5.4)),('Reference_04_Banner_Left',(-8,12.4,9.25),(-1,0,0),(-4.7,7.5,5.4))]
records=[];roots=[]
for name,origin,direction,eye in specs:
 origin=Vector(origin);direction=Vector(direction);hits=[]
 for wall in scene.objects:
  if wall.type!='MESH' or not wall.name.startswith(('A18_Walls','A20_Wall_','Hall_Roof_')):continue
  inv=wall.matrix_world.inverted()
  try:hit,loc,n,face=wall.ray_cast(inv@origin,inv.to_3x3()@direction)
  except RuntimeError:continue
  if hit:
   loc=wall.matrix_world@loc;n=(wall.matrix_world.to_3x3().inverted().transposed()@n).normalized()
   if n.dot(direction)>0:n=-n
   hits.append(((loc-origin).length,loc,n,wall.name))
 assert hits,name
 dist,loc,n,wallname=min(hits,key=lambda h:h[0]);mapping={}
 for old in [source]+list(source.children_recursive):
  new=old.copy();new.name=name+'_'+old.name;col.objects.link(new);mapping[old]=new
 for old,new in mapping.items():new.parent=mapping.get(old.parent);new.matrix_parent_inverse=old.matrix_parent_inverse.copy();new.matrix_basis=old.matrix_basis.copy()
 root=mapping[source];root.name='PA_'+name;root.location=loc+n*.074;root.rotation_euler=n.to_track_quat('-Y','Z').to_euler();root['mount_height_m']=loc.z;root['zone']=name
 roots.append(root);records.append({'reference':name,'position':list(root.location),'surface':wallname,'normal':list(n)})
 ld=bpy.data.lights.new(name+'_PreviewLight',type='AREA');ld.energy=260;ld.size=3
 lo=bpy.data.objects.new(ld.name,ld);scene.collection.objects.link(lo);lo.location=loc+n*3+Vector((0,0,-.3));lo.rotation_euler=(loc-lo.location).to_track_quat('-Z','Y').to_euler()
 bpy.context.view_layer.update()
assert len([o for o in col.objects if o.parent is None])==7
assert not bpy.data.objects.get('PA_03_Rear_Information')
(out/'Placements.json').write_text(json.dumps({'total':7,'removed':'Rear information speaker','added':records},indent=2))
scene.cycles.samples=16;scene.render.resolution_x=1440;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
cam=scene.camera
for root,(name,origin,direction,eye) in zip(roots,specs):
 cam.location=eye;target=root.location.copy();target.z=7.55;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=27
 scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
 print('CAPTURED',name,flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Hall_Seven_Speakers.blend'))
print('VERIFIED',json.dumps(records),flush=True)
