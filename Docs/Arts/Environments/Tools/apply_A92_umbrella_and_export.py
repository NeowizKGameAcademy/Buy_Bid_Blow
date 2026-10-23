"""Update shared umbrella meshes and export the approved lounge layout for Unity."""
import bpy, bmesh, ast, math, json, struct, re
from pathlib import Path
from mathutils import Vector, Matrix
from math import sin, cos, pi

BASE = Path('C:/course/unity/BuyBidBlow')
DATA = BASE / 'Logs/A92LoungeImport'
DATA.mkdir(parents=True, exist_ok=True)
hall = bpy.context.scene
source = (BASE/'Docs/Arts/Props/Tools/build_lounge_accessories.py').read_text(encoding='utf-8')
tree = ast.parse(source)
scope = dict(globals())
scope.update(scene=bpy.data.scenes['Lounge_Accessories_Game_Ready'], atlas=bpy.data.materials['Lounge_BK_Atlas'], roots=[], parts=[])
bpy.context.window.scene = scope['scene']
for node in tree.body:
    if isinstance(node, ast.FunctionDef) or isinstance(node, ast.Assign) and any(isinstance(t, ast.Name) and t.id=='regions' for t in node.targets):
        exec(compile(ast.Module(body=[node], type_ignores=[]), '<lounge_helpers>', 'exec'), scope)
block = source[source.index("start('UmbrellaStand')"):source.index('# Save/export metrically')]
block = block.replace("start('UmbrellaStand')", "start('UmbrellaStand_A92_Rebuild')")
exec(block, scope)
newroot = scope['root']
contacts = []
for i in range(3):
    # Each umbrella contributes ferrule, fabric, strap, shaft and handle.
    # The generator explicitly projects their minimum onto the tray at 37 mm.
    contacts.append({'umbrella':i+1, 'tip_contact_z_m':.037})
for lod in ['LOD0','LOD1']:
    old = bpy.data.objects['UmbrellaStand_'+lod]
    new = bpy.data.objects['UmbrellaStand_A92_Rebuild_'+lod]
    oldmesh = old.data
    for ob in list(bpy.data.objects):
        if ob.type=='MESH' and ob.data==oldmesh: ob.data=new.data
    new.data.name = 'UmbrellaStand_'+lod+'_SeatedInTray'
for ob in list(newroot.children_recursive)+[newroot]: bpy.data.objects.remove(ob, do_unlink=True)
root = bpy.data.objects['UmbrellaStand']
root['umbrella_tip_contacts'] = json.dumps(contacts)
oldpos=root.location.copy(); root.location=(0,0,0)
bpy.ops.object.select_all(action='DESELECT'); root.select_set(True)
for ob in root.children_recursive: ob.hide_set(False); ob.hide_render=False; ob.select_set(True)
bpy.context.view_layer.objects.active=root
bpy.ops.export_scene.fbx(filepath=str(BASE/'Assets/Project/Content/Props/LoungeAccessories/Models/UmbrellaStand.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,mesh_smooth_type='OFF',path_mode='STRIP')
root.location=oldpos
low=bpy.data.objects['UmbrellaStand_LOD1']; low.hide_set(True); low.hide_render=True
bpy.data.libraries.write(str(BASE/'Docs/Arts/Props/Models/BBB_Lounge_Accessories_BlenderKit_SeatedUmbrellas.blend'),{scope['scene']},fake_user=True)
bpy.context.window.scene=hall; bpy.context.view_layer.update()

convert=lambda v:[float(v[0]),float(v[2]),float(v[1])]
safe=lambda s:re.sub(r'[^a-zA-Z0-9_-]','_',s)
custom=[]
dg=bpy.context.evaluated_depsgraph_get()
for name,asset in [('A91_Left_Planter','LoungePlanter'),('A91_Left_Rug','LoungeRug')]:
    r=bpy.data.objects[name]; inv=r.matrix_world.inverted(); buffers={}
    for ob in [r]+list(r.children_recursive):
        if ob.type!='MESH':continue
        ev=ob.evaluated_get(dg); me=ev.to_mesh(preserve_all_data_layers=True,depsgraph=dg); me.calc_loop_triangles()
        matrix=inv@ob.matrix_world; normal=matrix.to_3x3().inverted_safe().transposed()
        uv=me.uv_layers.active.data if me.uv_layers.active else None
        for tri in me.loop_triangles:
            mat=safe(me.materials[tri.material_index].name)
            vs,ts,cache=buffers.setdefault(mat,([],[],{})); ids=[]
            for li in tri.loops:
                p=matrix@me.vertices[me.loops[li].vertex_index].co; n=(normal@me.corner_normals[li].vector).normalized()
                row=tuple(convert(p)+convert(n)+list(uv[li].uv if uv else (0,0))); key=tuple(round(x,6) for x in row)
                if key not in cache:cache[key]=len(vs);vs.append(row)
                ids.append(cache[key])
            ts.extend([ids[0],ids[2],ids[1]])
        ev.to_mesh_clear()
    parts=[]
    for mat,(vs,ts,_) in buffers.items():
        filename=asset+'_'+mat+'.bin'
        with (DATA/filename).open('wb') as f:
            f.write(struct.pack('<ii',len(vs),len(ts)))
            for row in vs:f.write(struct.pack('<8f',*row))
            f.write(struct.pack('<%si'%len(ts),*ts))
        parts.append({'file':filename,'material':mat})
    custom.append({'name':asset,'parts':parts})

layout=json.loads((BASE/'Docs/Arts/Environments/Models/A91_Furniture_Layout.json').read_text())
items=[]
sources={'A91_Left_Teal_Sofa':('A87_Left_Sofa_ROOT',-135), 'A91_Right_Red_Sofa':('A88_Red_Sofa_ROOT',-45), 'A91_Left_Bench':('A87_Left_Bench_ROOT',45), 'A91_Right_Bench':('A87_Left_Bench_ROOT',45), 'A91_Information_Desk_Chair':('A24_WOOD_05_Staff_Chair_ROOT',180)}
for placement in layout['placements']:
    name=placement['name']; ob=bpy.data.objects[name]; yaw=math.degrees(ob.rotation_euler.z); scale=convert(ob.scale)
    if name in sources:
        asset,oldyaw=sources[name]; prefab='Assets/Project/Content/Levels/ExchangeFurnished/Prefabs/'+asset+'.prefab'; unityyaw=oldyaw-yaw
        if 'Desk_Chair' in name:scale[0]/=.92
    elif 'Planter' in name or 'Rug' in name:
        asset='LoungePlanter' if 'Planter' in name else 'LoungeRug'; prefab='Assets/Project/Content/Levels/ExchangeFurnished/LoungeClusters/Prefabs/'+asset+'.prefab'; unityyaw=-yaw
    else:
        asset=ob.get('A91_source',name).replace('A90_','')
        group='LoungeAccessories' if asset in ['UmbrellaStand','DisplayCabinet','LeafletRack'] else 'FurnitureUtilityPack'
        prefab='Assets/Project/Content/Props/'+group+'/Prefabs/'+asset+'.prefab'; unityyaw=180-yaw
    items.append({'name':name,'prefab':prefab,'position':convert(ob.location),'yaw':unityyaw,'scale':scale})
(DATA/'layout.json').write_text(json.dumps({'placements':items,'customAssets':custom,'umbrellaContacts':contacts},indent=2))
hall['A92_umbrella_note']='Three metal tips contact the interior tray at z=0.037 m; restrained outward lean, open oval mouth.'
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'Docs/Arts/Environments/Models/BBB_Hall_A92_Unity_Lounge_Umbrellas.blend'))
print(json.dumps({'placements':len(items),'custom_assets':custom,'umbrella_contacts':contacts}))
