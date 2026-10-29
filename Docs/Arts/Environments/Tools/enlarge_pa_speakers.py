import bpy,json
from pathlib import Path
from mathutils import Vector
scene=bpy.data.scenes['A51_Staff_Portraits'];bpy.context.window.scene=scene
out=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Props/PA_Speaker/Reference_Revision/Larger_150');out.mkdir(exist_ok=True)
col=bpy.data.objects['PA_01_Left_Lounge'].users_collection[0]
roots=[o for o in col.objects if o.parent is None]
assert len(roots)==7
records=[]
for root in roots:
 normal=root.rotation_euler.to_matrix()@Vector((0,-1,0))
 # Keep the back of the mounting bracket at the same distance from the wall.
 old_back=root.matrix_world@Vector((0,.066,0))
 root.scale*=1.5;root.location+=normal*.033
 bpy.context.view_layer.update()
 new_back=root.matrix_world@Vector((0,.066,0))
 assert (old_back-new_back).length<.0001
 root['size_m']='0.375 x 0.150 x 0.510';root['size_multiplier']=1.5
 records.append({'name':root.name,'position':list(root.location),'scale':list(root.scale),'mount_preserved':True})
assert not bpy.data.objects.get('PA_03_Rear_Information')
(out/'Placements.json').write_text(json.dumps({'count':7,'width_m':.375,'height_m':.510,'speakers':records},indent=2))
cam=scene.camera
scene.cycles.samples=16;scene.render.resolution_x=1440;scene.render.resolution_y=1000
shots=[('Upper_Lobby_TV','PA_Reference_01_Lobby_TV',(7,-7,5.3),7.55,27),('Left_Lounge','PA_01_Left_Lounge',(-5.5,7.1,1.7),1.7,32)]
for name,rootname,eye,height,lens in shots:
 root=bpy.data.objects[rootname];cam.location=eye;target=root.location.copy();target.z=height
 cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=lens
 scene.render.filepath=str(out/(name+'.png'));bpy.ops.render.render(write_still=True)
 print('CAPTURED',name,flush=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Hall_Seven_Speakers_150.blend'))
print('VERIFIED',json.dumps(records),flush=True)
