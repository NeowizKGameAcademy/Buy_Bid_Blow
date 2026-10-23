import bpy,json
s=bpy.data.scenes['A51_Staff_Portraits'];bpy.context.window.scene=s
visible=[o for o in s.objects if o.visible_get()]
print('SCENE',s.name,len(s.objects),'VISIBLE',len(visible))
for o in visible:
 if any(k in o.name.lower() for k in ['banner','rope','barrier','anchor','a75','a76','a77']):
  print('DYNAMIC',o.name,o.type,'parent',o.parent.name if o.parent else '', 'loc',list(o.matrix_world.translation),'dims',list(o.dimensions))
print('COLLECTIONS',[(c.name,len(c.all_objects),c.hide_viewport,c.hide_render) for c in bpy.data.collections])
mats={m for o in visible if o.type=='MESH' for m in o.data.materials if m}
for m in sorted(mats,key=lambda m:m.name):
 print('MAT',m.name,[(n.type,n.image.name if n.type=='TEX_IMAGE' and n.image else '') for n in m.node_tree.nodes] if m.use_nodes else '')
print('ENUM', [i.identifier for i in bpy.ops.export_scene.fbx.get_rna_type().properties['axis_forward'].enum_items])
