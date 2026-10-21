import bpy,math,json
from mathutils import Vector
from pathlib import Path
scene=bpy.data.scenes['A51_Staff_Portraits'];bpy.context.window.scene=scene
out=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Props/PA_Speaker')
root=bpy.data.objects['PA_04_Auction_Hall']
origin=Vector((10,-12,3.6));direction=Vector((1,0,0));hits=[]
for wall in scene.objects:
 if wall.type!='MESH' or not wall.name.startswith(('A18_Walls','A20_Wall_')):continue
 inv=wall.matrix_world.inverted();hit,loc,n,face=wall.ray_cast(inv@origin,inv.to_3x3()@direction)
 if hit:
  loc=wall.matrix_world@loc;n=(wall.matrix_world.to_3x3()@n).normalized();hits.append(((loc-origin).length,loc,n,wall.name))
dist,loc,n,name=min(hits,key=lambda h:h[0]);root.location=loc+n*.071;root.rotation_euler=(0,0,math.atan2(n.x,-n.y));root['mount_height_m']=3.6
cam=scene.camera;cam.location=(7,-8,1.8);target=root.location.copy();target.z=2.7;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=32
light=bpy.data.objects['PA_04_Auction_Hall_PreviewFill'];light.location=root.location-Vector((3,0,-.4));light.rotation_euler=(root.location-light.location).to_track_quat('-Z','Y').to_euler()
records=json.loads((out/'Four_Speaker_Positions.json').read_text());records[3].update(position=list(root.location),rotation=list(root.rotation_euler),wall=name,height=3.6);(out/'Four_Speaker_Positions.json').write_text(json.dumps(records,indent=2))
scene.render.filepath=str(out/'04_Auction_Hall.png');bpy.ops.wm.save_as_mainfile(filepath=str(out/'Hall_Four_Speakers.blend'));bpy.ops.render.render(write_still=True)
print('PLACEMENT',records[3],flush=True)
