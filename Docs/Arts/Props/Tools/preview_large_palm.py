import bpy
from mathutils import Vector
s=bpy.context.scene
s.render.engine='CYCLES';s.cycles.samples=8
s.render.resolution_x=700;s.render.resolution_y=800;s.render.resolution_percentage=100
c=bpy.data.collections['A28_Large_Brass_Planters']
for o in s.objects:o.hide_render=o.name not in c.objects
camd=bpy.data.cameras.new('Preview');cam=bpy.data.objects.new('Preview',camd);s.collection.objects.link(cam);cam.location=(7.4,-15.9,3.0);cam.rotation_euler=(Vector((4.3,-20.2,1.4))-cam.location).to_track_quat('-Z','Y').to_euler();camd.lens=50;s.camera=cam
for pos,power,size in [((4,-16,5),1200,5),((1,-21,4),900,3)]:
 d=bpy.data.lights.new('PreviewArea','AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new('PreviewArea',d);s.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((4.3,-20.2,1.3))-o.location).to_track_quat('-Z','Y').to_euler()
s.render.film_transparent=True;s.render.filepath='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Large_Brass_Palm_Preview.png'
bpy.ops.render.render(write_still=True)
