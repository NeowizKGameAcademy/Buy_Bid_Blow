import bpy, math
from mathutils import Vector
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
src='C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Models/BBB_Hall_A30_StaffMedallion.blend'
with bpy.data.libraries.load(src,link=False) as (a,b):
 b.materials=['BBB_Chandelier_BlenderKit_BrushedBrass','BBB_Chandelier_BlenderKit_FrostedGlass']
brass,glass=b.materials
# Retain the hall brass; use a dedicated ivory diffuser material.
glass=glass.copy();glass.name='BBB_Sconce_Ivory_FrostedGlass'
glass.use_nodes=True
p=next((n for n in glass.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
if p:
 for key in ['Base Color','Transmission Weight','Roughness','Emission Color','Emission Strength']:
  if key in p.inputs:
   for link in list(p.inputs[key].links): glass.node_tree.links.remove(link)
 p.inputs['Base Color'].default_value=(0.88,0.77,0.53,1)
 p.inputs['Roughness'].default_value=.38
 p.inputs['Transmission Weight'].default_value=.16
 p.inputs['Emission Color'].default_value=(1,.78,.42,1)
 p.inputs['Emission Strength'].default_value=.18
c=bpy.data.collections.new('BBB_Vertical_Wall_Sconce');bpy.context.scene.collection.children.link(c)
r=bpy.data.objects.new('Vertical_Sconce_ROOT',None);c.objects.link(r)
r['dimensions']='H 0.80m W 0.22m D 0.18m';r['mounting']='Backplate rear plane Y=0; fixture projects toward -Y; origin at lower tip'
def finish(o,name,mat,bevel=.001):
 o.name=name
 for cc in list(o.users_collection):cc.objects.unlink(o)
 c.objects.link(o);o.parent=r;o.data.materials.append(mat)
 if bevel:
  m=o.modifiers.new('Edge softening','BEVEL');m.width=bevel;m.segments=2
  m=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
 return o
def box(name,loc,dims,mat=brass,bev=.001):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=dims
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 return finish(o,name,mat,bev)
def cyl(name,z,rad,depth,mat=brass,y=-.10):
 bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=rad,depth=depth,location=(0,y,z));o=bpy.context.object
 for f in o.data.polygons:f.use_smooth=len(f.vertices)==4
 return finish(o,name,mat,.0008)
box('Mounting_Backplate',(0,-.009,.4),(.16,.018,.64),bev=.003)
box('Backplate_Raised_Inset',(0,-.020,.4),(.14,.006,.62),bev=.002)
for z in [.145,.655]:box('Wall_StandOff',(0,-.052,z),(.048,.074,.032))
cyl('Ivory_Glass_Diffuser',.4,.067,.50,glass)
# Mirror the stepped caps around the center; tip-to-tip 80cm.
for s in [-1,1]:
 for i,(offset,rad,depth) in enumerate([(.255,.079,.025),(.280,.087,.025),(.305,.110,.024),(.327,.088,.020),(.348,.065,.022),(.369,.044,.020),(.390,.018,.020)]):
  cyl(('Lower' if s<0 else 'Upper')+'_Cap_'+str(i),.4+s*offset,rad,depth)
for i,angle in enumerate([25,155,245,295]):
 a=math.radians(angle);x=.074*math.cos(a);y=-.10+.074*math.sin(a)
 box('Vertical_Brass_Rib_'+str(i),(x,y,.4),(.008,.008,.49),bev=.0015)
for x in [-.058,.058]:
 for z in [.12,.68]:
  bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=6,radius=1,location=(x,-.026,z))
  o=bpy.context.object;o.scale=(.006,.003,.006);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
  finish(o,'Backplate_Fastener',brass,0)
for im in bpy.data.images:
 if im.source=='FILE' and not im.packed_file:
  try:im.pack()
  except:pass
for o in bpy.context.selected_objects:o.select_set(False)
r.select_set(True);bpy.context.view_layer.objects.active=r
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  a.spaces.active.shading.type='MATERIAL'
  a.spaces.active.region_3d.view_location=(0,-.08,.4)
  a.spaces.active.region_3d.view_distance=1.25
  a.spaces.active.region_3d.view_rotation=Vector((.6,-1,.20)).to_track_quat('Z','Y')
bpy.ops.wm.save_as_mainfile(filepath='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Vertical_Wall_Sconce.blend')
print('SAVED_SCONCE',len(c.objects))
