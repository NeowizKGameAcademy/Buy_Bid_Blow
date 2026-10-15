import bpy, json, math
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

base=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Environments')
assert Path(bpy.data.filepath).name == 'BBB_Hall_A30_StaffMedallion.blend'
assert bpy.data.collections.get('A31_Wall_Sconces') is None
plan=json.loads((base/'A31_Wall_Sconce_Placement_Plan.json').read_text(encoding='utf-8'))
scene=bpy.context.scene
existing=list(scene.objects)
dg=bpy.context.evaluated_depsgraph_get()
wall=bpy.data.objects['A18_Walls_Ellipse_DoorOpenings']
wall_bvh=BVHTree.FromObject(wall,dg)
wall_inv=wall.matrix_world.inverted()
with bpy.data.libraries.load(str(base.parent/'Props/Models/BBB_Vertical_Wall_Sconce.blend'),link=False) as (src,dst):
    dst.objects=list(src.objects)
sources=[o for o in dst.objects if o]
assert len(sources)==28
root_source=next(o for o in sources if o.name.startswith('Vertical_Sconce_ROOT'))
col=bpy.data.collections.new('A31_Wall_Sconces');scene.collection.children.link(col)
records=[]
for entry in plan['placements']:
    x,y=entry['wall_reference_x'],entry['wall_reference_y']
    if entry['id'] in ('L07','L08'):
        x=math.copysign(4.8,x)
        y=-13.5-9*math.sqrt(1-((abs(x)-2)/12)**2)
    if entry['floor']==2:
        n=Vector((-(x-math.copysign(2,x)),-(y-5.5),0)).normalized()
    elif abs(x)>13.9:
        n=Vector((-math.copysign(1,x),0,0))
    else:
        n=Vector((-(x-math.copysign(2,x))/144,-(y+13.5)/81,0)).normalized()
    target=Vector((x,y,entry['center_world_z']))
    origin=target+n*.8
    hit,normal,face,dist=wall_bvh.ray_cast(wall_inv@origin,wall_inv.to_3x3()@(-n),2)
    assert hit is not None, entry['id']
    hit=wall.matrix_world@hit
    inward=(wall.matrix_world.to_3x3()@normal).normalized()
    if inward.dot(n)<0:inward=-inward
    inward.z=0;inward.normalize()
    # Backplate rests 2mm in front of the wall; the small rear fasteners penetrate it.
    pos=hit+inward*.002;pos.z=entry['root_world_z']
    angle=math.atan2(inward.x,-inward.y)
    transform=Matrix.Translation(pos)@Matrix.Rotation(angle,4,'Z')
    copies={}
    sub=bpy.data.collections.new('A31_'+entry['id']);col.children.link(sub)
    for src in sources:
        ob=src.copy();ob.name='A31_'+entry['id']+'_'+src.name
        sub.objects.link(ob);copies[src]=ob
    for src,ob in copies.items():
        ob.parent=copies.get(src.parent)
        if src==root_source:ob.matrix_world=transform
        else:
            ob.matrix_parent_inverse=src.matrix_parent_inverse.copy()
            ob.matrix_basis=src.matrix_basis.copy()
    root=copies[root_source]
    root['plan_id']=entry['id'];root['floor']=entry['floor']
    root['mount_center_world_z']=entry['center_world_z']
    root['source_asset']='BBB_Vertical_Wall_Sconce.blend'
    records.append({'id':entry['id'],'root':root.name,'location':list(pos),'rotation_z_deg':math.degrees(angle),'wall_face':face,'inward_normal':list(inward),'collection':sub.name})
for src in sources:bpy.data.objects.remove(src,do_unlink=True)
scene.name='BBB_Hall_A31_WallSconces'
bpy.context.view_layer.update()
assert len(col.all_objects)==14*28
assert all(o.name in scene.objects for o in existing)
# Check actual meshes against nearby scene geometry, excluding the intended wall contact.
collisions=[];bounds=[]
def bb(ob):
    pts=[ob.matrix_world@Vector(p) for p in ob.bound_box]
    return Vector([min(p[i] for p in pts) for i in range(3)]),Vector([max(p[i] for p in pts) for i in range(3)])
def overlap(a,b):return all(a[0][i]<=b[1][i] and a[1][i]>=b[0][i] for i in range(3))
def world_bvh(obs):
    verts=[];faces=[]
    for ob in obs:
        ev=ob.evaluated_get(dg);mesh=ev.to_mesh();off=len(verts)
        verts.extend(ob.matrix_world@v.co for v in mesh.vertices)
        faces.extend(tuple(off+i for i in p.vertices) for p in mesh.polygons)
        ev.to_mesh_clear()
    return BVHTree.FromPolygons(verts,faces)
existing_bounds=[(ob,bb(ob)) for ob in existing if ob.type=='MESH' and ob!=wall]
for r in records:
    obs=[o for o in bpy.data.collections[r['collection']].objects if o.type=='MESH']
    boxes=[bb(o) for o in obs]
    whole=(Vector([min(b[0][i] for b in boxes) for i in range(3)]),Vector([max(b[1][i] for b in boxes) for i in range(3)]))
    bounds.append({'id':r['id'],'min':list(whole[0]),'max':list(whole[1])})
    fixture=world_bvh(obs)
    for ob,b in existing_bounds:
        if overlap(whole,b) and fixture.overlap(world_bvh([ob])):
            collisions.append({'id':r['id'],'object':ob.name})
report={'count':14,'first_floor':8,'second_floor':6,'existing_objects_preserved':len(existing),'placements':records,'bounds':bounds,'mesh_intersections':collisions}
(base/'A31_Wall_Sconce_Applied.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for ob in bpy.context.selected_objects:ob.select_set(False)
print(json.dumps({'count':len(records),'objects':len(col.all_objects),'collisions':collisions,'bounds':bounds}))
