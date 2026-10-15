import bpy
from mathutils import Vector
def matof(n):return bpy.data.objects[n].active_material
brass=matof('12_Service_Bell_Dome');wood=matof('06_Document_Trays_TrayFloor');plastic=matof('01_Employee_Monitor_RearHousing');leather=matof('05_Staff_Chair_SeatCushion');paper=matof('08_Order_Mat_Clipboard_OrderSheet')
for m in [brass,wood,plastic,leather,paper]:
 m.name='BBB_Props_'+m.name
 for l in list(m.node_tree.links):
  if l.to_node.type=='OUTPUT_MATERIAL' and l.to_socket.name=='Displacement':m.node_tree.links.remove(l)
 for n in m.node_tree.nodes:
  if n.type=='NORMAL_MAP':n.inputs['Strength'].default_value=.18
def tint(src,name,color,rough=None,metal=None):
 m=src.copy();m.name=name
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
 for key,val in [('Base Color',(*color,1)),('Roughness',rough),('Metallic',metal)]:
  if val is None:continue
  for l in list(p.inputs[key].links):m.node_tree.links.remove(l)
  p.inputs[key].default_value=val
 return m
colors={'STRAW':(.46,.29,.065),'WOOD':(.045,.115,.055),'BRICK':(.30,.072,.043)}
leathers={k:tint(leather,'BBB_Leather_'+k,c,.53) for k,c in colors.items()}
enamels={k:tint(plastic,'BBB_Enamel_'+k,c,.34) for k,c in colors.items()}
black=tint(plastic,'BBB_Black_Screen',(.002,.003,.004),.19)
rubber=tint(plastic,'BBB_Soft_Rubber',(.009,.011,.012),.83)
ivory=tint(paper,'BBB_Ivory_Paper',(.82,.78,.66),.8)
ink=tint(plastic,'BBB_Printed_Ink',(.065,.048,.031),.9)
def assign(o,m):
 o.data.materials.clear();o.data.materials.append(m)
def choose(n):
 if n.startswith('01_'):
  return black if '_Screen' in n else rubber if any(s in n for s in ['Cable','Vent','Ports']) else plastic
 if n.startswith(('02_','03_')):return rubber if any(s in n for s in ['Cable','Scroll','Seam']) else plastic
 if n.startswith('04_'):return ivory if n.endswith('_Receipt') else rubber if any(s in n for s in ['Cable','Exit','Socket','Foot']) else plastic
 if n.startswith('05_'):
  return leathers['WOOD'] if 'Cushion' in n else brass if any(s in n for s in ['SeatFrame','BackFrame']) else plastic
 if n.startswith('06_'):return ivory if 'Paper' in n else brass if 'Post' in n else wood
 if n.startswith('07_'):return ivory if 'Pages' in n else ink if 'PageEdge' in n else brass if 'Corner' in n else leathers['WOOD']
 if n.startswith('08_'):return ivory if 'OrderSheet' in n else ink if 'FormRule' in n else brass if any(s in n for s in ['ClipHinge','_Clip.']) or n.endswith('_Clip') else wood if 'Clipboard_Clipboard' in n else leathers['WOOD']
 if n.startswith('09_'):return wood if 'PenBarrel' in n else brass
 if n.startswith('10_'):return brass
 if n.startswith('11_'):return wood if n.endswith('_Stamp') else rubber if 'InkSurface' in n else brass if 'Hinge' in n else plastic
 if n.startswith('12_'):return rubber if 'Base' in n else brass
 if n.startswith('13_'):
  c=next(k for k in colors if k in n)
  return enamels[c] if 'FrontPlate' in n else wood if 'RearSupport' in n else brass
 return brass
for o in bpy.context.scene.objects:
 if o.type!='MESH':continue
 assign(o,choose(o.name))
 uv=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
 for face in o.data.polygons:
  ax=max(range(3),key=lambda k:abs(face.normal[k]));axes=[k for k in range(3) if k!=ax]
  for li in face.loop_indices:
   v=o.data.vertices[o.data.loops[li].vertex_index].co
   uv.data[li].uv=(v[axes[0]]*4,v[axes[1]]*4)
# Material zoning on a single lathed stamp and pen.
o=bpy.data.objects['11_Stamp_InkPad_Stamp'];o.data.materials.append(brass);o.data.materials.append(rubber)
for f in o.data.polygons:
 z=sum(o.data.vertices[i].co.z for i in f.vertices)/len(f.vertices)
 f.material_index=2 if z<.009 else 1 if z<.033 else 0
o=bpy.data.objects['10_Pen_Pen'];o.data.materials.append(wood)
for f in o.data.polygons:
 z=sum(o.data.vertices[i].co.z for i in f.vertices)/len(f.vertices)
 f.material_index=1 if .025<z<.114 else 0
# Reusable complete color variants, keeping originals as WOOD.
def variant(rootname,key,pos):
 src=bpy.data.objects[rootname];col=bpy.data.collections.new(rootname[:-5]+'_'+key);bpy.context.scene.collection.children.link(col)
 r=src.copy();r.name=rootname[:-5]+'_'+key+'_ROOT';col.objects.link(r);r.location=pos
 mapping={src:r}
 for old in src.children_recursive:
  n=old.copy();n.data=old.data.copy();n.name=old.name+'_'+key;col.objects.link(n);mapping[old]=n
  for i,m in enumerate(n.data.materials):
   if m==leathers['WOOD']:n.data.materials[i]=leathers[key]
 for old,n in mapping.items():
  if old!=src:n.parent=mapping[old.parent]
 r['color_variant']=key
bpy.data.objects['05_Staff_Chair_ROOT'].location=(0,3.6,0)
variant('05_Staff_Chair_ROOT','STRAW',(-1.3,3.6,0))
variant('05_Staff_Chair_ROOT','BRICK',(1.3,3.6,0))
variant('08_Order_Mat_Clipboard_ROOT','STRAW',(-1.35,-2,0))
variant('08_Order_Mat_Clipboard_ROOT','BRICK',(-.45,-2,0))
variant('07_Bound_Ledger_ROOT','STRAW',(.5,-2,0))
variant('07_Bound_Ledger_ROOT','BRICK',(1.1,-2,0))
# Pack all downloaded textures used by these materials.
packed=0
for image in bpy.data.images:
 if image.source=='FILE' and not image.packed_file:
  try:image.pack();packed+=1
  except Exception as e:print('PACK ERROR',image.name,str(e))
bpy.context.scene.name='BBB_Reception_Props_Textured'
bpy.context.view_layer.update()
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  s=a.spaces.active;s.shading.type='MATERIAL'
  target=Vector((0,.9,.4));eye=Vector((2,-6,6));r=s.region_3d
  r.view_rotation=(target-eye).to_track_quat('-Z','Y');r.view_location=target;r.view_distance=7.3;s.lens=50
print('packed',packed,'unassigned',[o.name for o in bpy.context.scene.objects if o.type=='MESH' and not len(o.data.materials)])
bpy.ops.wm.save_as_mainfile(filepath='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Reception_Props_Textured.blend')

