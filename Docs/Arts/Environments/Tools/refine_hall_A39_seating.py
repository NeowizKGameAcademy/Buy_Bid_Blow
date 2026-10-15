"""Refine the approved six-seat mezzanine without changing the rest of the hall."""
import bpy, bmesh, json, math, textwrap
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(r'C:\course\unity\BuyBidBlow\Docs\Arts\Environments')
TOP = bpy.data.collections['A38_Compact_6Seat_Seating']
REPORT = {'original_meshes': sum(o.type == 'MESH' for o in TOP.all_objects)}
SEATS = {o.name: o.matrix_world.copy() for o in TOP.all_objects if o.type == 'EMPTY'}


def widen_stairs():
    ns = bpy.app.driver_namespace['compact_seating_A38']
    g = ns['build'].__globals__
    src = (ROOT / 'Tools/apply_hall_A38_compact_seating.py').read_text(encoding='utf-8')
    functions = src[src.index('def stair_geometry('):src.index('def rail_post(')]
    functions = functions.replace('(.95,', '(1.425,').replace('(.69,', '(1.035,').replace('(.91,', '(1.365,').replace('*.475', '*.7125')
    exec(functions, g)
    for suffix in ['01_Floors', '03_Stairs', '04_Rails', '07_Lamps']:
        c = bpy.data.collections['A38_' + suffix]
        for o in list(c.objects):
            bpy.data.objects.remove(o, do_unlink=True)
    g['STEPS'].clear()
    section = src[src.index('    front=[from_original'):src.index('    # Rigid chairs')]
    section = textwrap.dedent(section)
    section = section.replace('    le=stair_edges', '    le=stair_edges')
    section = section.replace('le=stair_edges(la,lb);ue=stair_edges(ua,ub)',
        'def shift_out(a,b):\n'
        '    a,b=Vector(a),Vector(b);d=(b-a).normalized();n=Vector((d.y,-d.x))\n'
        '    return tuple(a+n*.2375),tuple(b+n*.2375)\n'
        'la,lb=shift_out(la,lb);ua,ub=shift_out(ua,ub)\n'
        'le=stair_edges(la,lb);ue=stair_edges(ua,ub)')
    section = section.replace('rear_halfwidth=3.276', 'rear_halfwidth=3.751')
    exec(section, g)
    bpy.context.view_layer.update()
    REPORT['stair_width_before_m'] = .95
    REPORT['stair_width_after_m'] = 1.425
    REPORT['lamps_removed'] = 6
    REPORT['stair_flights'] = [(n, list(a), list(b), z0, z1) for n,a,b,z0,z1 in g['STEPS']]
    print('Rebuilt four widened flights, matching floor edges and side rails.')


def shader(material):
    return next((n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)


def prepare_materials():
    sources = {
        'Walnut': bpy.data.objects['A38_Front_Seat_1_Leg'].data.materials[0],
        'Velvet': bpy.data.objects['A38_Front_Seat_1_Back_Pad'].data.materials[0],
        'Brass': bpy.data.objects['A38_Front_Seat_1_Finial'].data.materials[0],
    }
    mapping = {}
    specs = [('Walnut','Walnut',(1,1,1)), ('DarkWalnut','Walnut',(.55,.55,.55)),
             ('HoneyWalnut','Walnut',(1.16,1.08,.93)), ('PanelWalnut','Walnut',(.85,.85,.85)),
             ('Velvet','Velvet',(.48,.28,.42)), ('Runner','Velvet',(.35,.20,.30)), ('Brass','Brass',(1,.72,.32))]
    for key,source,tint in specs:
        m=sources[source].copy();m.name='A39_BK_'+key
        bs=shader(m)
        if bs and bs.inputs['Base Color'].is_linked and tint!=(1,1,1):
            socket=bs.inputs['Base Color']; upstream=socket.links[0].from_socket
            mix=m.node_tree.nodes.new('ShaderNodeMixRGB')
            # The enum is checked on this installed Blender before use.
            assert 'MULTIPLY' in [i.identifier for i in mix.bl_rna.properties['blend_type'].enum_items]
            mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1
            mix.inputs[2].default_value=(*tint,1)
            m.node_tree.links.new(upstream,mix.inputs[1]);m.node_tree.links.new(mix.outputs[0],socket)
        for node in m.node_tree.nodes:
            if node.type=='NORMAL_MAP': node.inputs['Strength'].default_value=.22 if source=='Walnut' else .3
            if node.type=='OUTPUT_MATERIAL':
                for link in list(node.inputs['Displacement'].links):m.node_tree.links.remove(link)
            if node.type=='TEX_IMAGE' and node.image and not node.image.packed_file:
                node.image.pack()
        m.diffuse_color = {'Walnut':(.27,.12,.045,1),'Velvet':(.22,.008,.02,1),'Brass':(.54,.29,.07,1)}[source]
        mapping[key]=m
    old_map={'ASR_Wood':'Walnut','ASR_DarkWood':'DarkWalnut','ASR_Honey':'HoneyWalnut',
             'ASR_Panel':'PanelWalnut','ASR_Red':'Velvet','ASR_Carpet':'Runner','ASR_Brass':'Brass'}
    for i in range(4):old_map['ASR_Plank'+str(i)]=['Walnut','HoneyWalnut','DarkWalnut','PanelWalnut'][i]
    for o in TOP.all_objects:
        if o.type!='MESH':continue
        for i,m in enumerate(o.data.materials):
            key=old_map.get(m.name)
            if not key:
                key=next((k for k,v in sources.items() if m==v),None)
            if key:o.data.materials[i]=mapping[key]
    REPORT['materials']={k:{'name':v.name,'source':sources[s].name} for k,s,t in specs for v in [mapping[k]]}
    print('BlenderKit materials assigned only to seating.')


def finish_geometry():
    groups={}; removed_vertices=0
    for o in list(TOP.all_objects):
        if o.type!='MESH':continue
        # Metric projection per component keeps texel density consistent after assembly.
        me=o.data
        uv=me.uv_layers.active or me.uv_layers.new(name='UVMap')
        spans=[max(v.co[i] for v in me.vertices)-min(v.co[i] for v in me.vertices) for i in range(3)]
        for p in me.polygons:
            drop=max(range(3),key=lambda i:abs(p.normal[i])); axes=[i for i in range(3) if i!=drop]
            axes.sort(key=lambda i:spans[i],reverse=True)
            for li in p.loop_indices:
                v=me.vertices[me.loops[li].vertex_index].co
                uv.data[li].uv=(v[axes[0]]/1.5,v[axes[1]]/1.5)
        ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get())
        clean=bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True, depsgraph=bpy.context.evaluated_depsgraph_get())
        o.modifiers.clear();o.data=clean
        bm=bmesh.new();bm.from_mesh(clean);before=len(bm.verts)
        bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
        bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=.000001)
        loose=[v for v in bm.verts if not v.link_faces]
        if loose:bmesh.ops.delete(bm,geom=loose,context='VERTS')
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        removed_vertices+=before-len(bm.verts);bm.to_mesh(clean);bm.free();clean.validate();clean.update()
        col=o.users_collection[0].name
        if 'Chairs' in col:key=o.parent.name.replace('A38_','A39_')+'_Mesh'
        elif 'Desks' in col:key='A39_'+('Front' if '_Front_' in o.name else 'Rear')+'_Desk'
        elif 'Stairs' in col:key='A39_'+o.name.split('A38_')[1].split('_Step_')[0].split('_Runner_')[0].split('_Nosing_')[0]+'_Stair'
        elif 'Floors' in col:key='A39_'+('Upper' if 'Upper' in o.name else 'Lower')+('_Deck' if 'Plank' in o.name else '_Base')
        elif 'Rails' in col:
            center=sum((o.matrix_world@Vector(v) for v in o.bound_box),Vector())/8
            key='A39_'+('Left' if center.x<0 else 'Right')+'_Side_Rail'
        else:continue
        groups.setdefault(key,[]).append(o)
    for name,obs in groups.items():
        for o in bpy.context.selected_objects:o.select_set(False)
        for o in obs:o.select_set(True)
        bpy.context.view_layer.objects.active=obs[0]
        if len(obs)>1:bpy.ops.object.join()
        o=obs[0];o.name=name
        # Compact duplicate material slots left by joining many components.
        mats=[];remap={}
        for i,m in enumerate(o.data.materials):
            if m not in mats:mats.append(m)
            remap[i]=mats.index(m)
        indices=[remap[p.material_index] for p in o.data.polygons]
        o.data.materials.clear()
        for m in mats:o.data.materials.append(m)
        for p,i in zip(o.data.polygons,indices):p.material_index=i
    for o in TOP.all_objects:
        if o.type=='EMPTY':o.name=o.name.replace('A38_','A39_')
        o.select_set(False)
    REPORT['removed_duplicate_or_loose_vertices']=removed_vertices
    REPORT['final_meshes']=sum(o.type=='MESH' for o in TOP.all_objects)
    TOP.name='A39_Refined_6Seat_Seating'
    for c in TOP.children:c.name=c.name.replace('A38_','A39_')
    bpy.context.scene.name='A39_Refined_6Seats_BlenderKit'
    bpy.context.view_layer.update()
    print(json.dumps(REPORT,ensure_ascii=False))


def validate():
    s=bpy.context.scene;dg=bpy.context.evaluated_depsgraph_get();checks=[]
    for name,aa,bb,z0,z1 in REPORT['stair_flights']:
        a,b=Vector(aa),Vector(bb);d=(b-a).normalized();w=Vector((d.y,-d.x))
        for label,p,z in [('entry',a-d*.10,z0),('exit',b+d*.10,z1)]:
            hit,loc,n,i,obj,mat=s.ray_cast(dg,Vector((p.x,p.y,7)),Vector((0,0,-1)))
            checks.append({'flight':name,'point':label,'ok':bool(hit and abs(loc.z-z)<.04),'actual':loc.z if hit else None})
        for t in [.125,.375,.625,.875]:
            for side in [-.52,0,.52]:
                p=a.lerp(b,t)+w*side
                hit,loc,n,i,obj,mat=s.ray_cast(dg,Vector((p.x,p.y,7)),Vector((0,0,-1)))
                z=z0+(z1-z0)*(math.floor(t*4)+1)/4
                checks.append({'flight':name,'point':'tread','ok':bool(hit and abs(loc.z-z)<.05)})
    REPORT['walk_surface_checks']=checks
    REPORT['chairs']=sum(o.type=='EMPTY' and '_Seat_' in o.name for o in TOP.all_objects)
    REPORT['remaining_table_lamps']=sum('Lamp' in o.name for o in TOP.all_objects)
    REPORT['seat_positions_unchanged']=all((bpy.data.objects[n.replace('A38_','A39_')].matrix_world.translation-m.translation).length<.00001 for n,m in SEATS.items())
    assert all(c['ok'] for c in checks),checks
    assert REPORT['chairs']==6 and REPORT['remaining_table_lamps']==0
    assert REPORT['seat_positions_unchanged']
    (ROOT/'Models/A39_Seating_Validation.json').write_text(json.dumps(REPORT,indent=2),encoding='utf-8')
    print('Validated',len(checks),'stair surface checks; six unchanged seats; no table lamps.')
