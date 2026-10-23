import bpy,math,random
from mathutils import Vector,Matrix
from pathlib import Path
random.seed(29)
P=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models');s=bpy.context.scene
card=bpy.data.objects['CARD_Frond_48_triangles'];mat=card.data.materials[0]
for o in list(s.objects):bpy.data.objects.remove(o,do_unlink=True)
with bpy.data.libraries.load(str(P.parent.parent/'Environments/Models/BBB_Hall_A28_LargePlanters.blend'),link=False) as (a,b):b.objects=['LargePalm_Brass_Pot','LargePalm_Soil']
c=bpy.data.collections.new('BBB_Areca_Card_Planter');s.collection.children.link(c);root=bpy.data.objects.new('BBB_Areca_Card_Planter_ROOT',None);c.objects.link(root)
for o in b.objects:
 c.objects.link(o);o.parent=root;o.matrix_parent_inverse=Matrix.Identity(4)
 if 'Brass' in o.name:o.location=(0,0,0);o.scale=(1.46,1.46,1.52)
 else:o.location=(0,0,.90);o.scale=(1.46,1.46,1.52)
stemmat=bpy.data.materials.new('BBB_Areca_Cane');stemmat.diffuse_color=(.17,.22,.065,1);stemmat.use_nodes=True;p=next(n for n in stemmat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(.17,.22,.065,1);p.inputs['Roughness'].default_value=.62
leafmat=mat
# Reuse the baked leaf card texture; trim away the long empty petiole area using UV range.
def tube(name,pts,radius):
 vs=[];fs=[]
 for j,p in enumerate(pts):
  q=(pts[min(j+1,len(pts)-1)]-pts[max(0,j-1)]).to_track_quat('Z','Y');rad=radius*(1-.6*j/(len(pts)-1))
  for k in range(8):vs.append(p+q@Vector((rad*math.cos(k*math.tau/8),rad*math.sin(k*math.tau/8),0)))
 for j in range(len(pts)-1):
  for k in range(8):i=j*8+k;fs.append((i,j*8+(k+1)%8,(j+1)*8+(k+1)%8,i+8))
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.materials.append(stemmat);o=bpy.data.objects.new(name,me);c.objects.link(o);o.parent=root
for i in range(7):
 a=i*2.39996;v=Vector((math.cos(a),math.sin(a),0));base=v*.16+Vector((0,0,.88));crown=v*(.25+.10*(i%3))+Vector((0,0,2.25+.18*i))
 tube('Cane_%02d'%i,[base.lerp(crown,t/8)+v*(.04*math.sin(math.pi*t/8)) for t in range(9)],.023)
 for j in range(5):
  ang=a+j*math.tau/5+random.uniform(-.15,.15);out=Vector((math.cos(ang),math.sin(ang),0));side=Vector((-math.sin(ang),math.cos(ang),0));length=random.uniform(1.0,1.35);width=random.uniform(.58,.78);rise=random.uniform(.55,.85);drop=random.uniform(.32,.62)
  def center(t):return crown+out*(length*t)+Vector((0,0,rise*math.sin(math.pi*t*.85)-drop*t))
  vs=[];uv=[];fs=[]
  for k in range(17):
   t=k/16
   for u in [0,.5,1]:vs.append(center(t)+side*((u-.5)*width)+Vector((0,0,-.07*abs(u-.5))));uv.append((u,.43+.57*t))
  for k in range(16):
   for x in range(2):n=k*3+x;fs.append((n,n+1,n+4,n+3))
  me=bpy.data.meshes.new('FrondCard');me.from_pydata(vs,[],fs);me.materials.append(leafmat);uvl=me.uv_layers.new(name='UVMap')
  for p in me.polygons:
   for li in p.loop_indices:uvl.data[li].uv=uv[me.loops[li].vertex_index]
  o=bpy.data.objects.new('Frond_%02d_%02d'%(i,j),me);c.objects.link(o);o.parent=root
  tube('Rachis_%02d_%02d'%(i,j),[center(k/12) for k in range(13)],.007)
# Save asset with packed texture and source attribution.
s['source']='Samuel Slavik, Artificial palm Areca 180 cm, BlenderKit royalty_free. Frond texture baked from licensed model; custom card arrangement and canes.'
bpy.context.view_layer.update()
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
  sp=area.spaces.active;target=Vector((0,0,2));eye=Vector((5,-7,4));sp.region_3d.view_rotation=(target-eye).to_track_quat('-Z','Y');sp.region_3d.view_location=target;sp.region_3d.view_distance=7;sp.lens=48
bpy.ops.wm.save_as_mainfile(filepath=str(P/'BBB_Areca_Card_Planter.blend'))
print('ASSET_DONE',flush=True)
# Render asset preview without changing saved asset.
camd=bpy.data.cameras.new('Preview');cam=bpy.data.objects.new('Preview',camd);s.collection.objects.link(cam);cam.location=(5,-7,4);cam.rotation_euler=(Vector((0,0,2))-cam.location).to_track_quat('-Z','Y').to_euler();camd.lens=48;s.camera=cam
for pos,energy in [((3,-4,6),1700),((-4,-1,4),1300)]:
 d=bpy.data.lights.new('Studio','AREA');d.energy=energy;d.size=5;o=bpy.data.objects.new('Studio',d);s.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,2))-o.location).to_track_quat('-Z','Y').to_euler()
s.render.engine='CYCLES';s.cycles.samples=24;s.cycles.transparent_max_bounces=32;s.render.resolution_x=900;s.render.resolution_y=1000;s.render.resolution_percentage=100;s.render.film_transparent=True;s.render.filepath=str(P/'BBB_Areca_Card_Planter_Preview.png');bpy.ops.render.render(write_still=True)
