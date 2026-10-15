import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

base=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Environments')
scene=bpy.context.scene
assert Path(bpy.data.filepath).name=='BBB_Hall_A31_WallSconces.blend'
assert not bpy.data.collections.get('A33_Wall_Sconces')
backup=base/'Models/BBB_Hall_A31_Before_Rev03.blend'
if not backup.exists():bpy.ops.wm.save_as_mainfile(filepath=str(backup),copy=True)
plan=json.loads((base/'A33_Wall_Sconce_Placement_Plan_Rev03.json').read_text())
col=bpy.data.collections['A31_Wall_Sconces']
original_nonfixtures={o.name:o.matrix_world.copy() for o in scene.objects if o.name not in col.all_objects}
dg=bpy.context.evaluated_depsgraph_get()
def mount(obj,center,toward):
    inv=obj.matrix_world.inverted();bvh=BVHTree.FromObject(obj,dg)
    p,no,idx,dist=bvh.ray_cast(inv@(center+toward*2),inv.to_3x3()@(-toward),4)
    assert p is not None,(obj.name,list(center))
    n=(obj.matrix_world.to_3x3().inverted().transposed()@no).normalized()
    if n.dot(toward)<0:n=-n
    n.z=0;n.normalize()
    point=obj.matrix_world@p
    # Sample the backplate footprint so grooves cannot intersect its flat rear face.
    tangent=Vector((-n.y,n.x,0));samples=[]
    for u in [-.08,-.04,0,.04,.08]:
        for v in [-.32,0,.32]:
            q=point+tangent*u+Vector((0,0,v))
            hit,_,_,_=bvh.ray_cast(inv@(q+n*.3),inv.to_3x3()@(-n),.6)
            if hit is not None:samples.append((obj.matrix_world@hit-point).dot(n))
    point+=n*(max(samples,default=0)+.002)
    point.z=center.z-.4
    return point,n

# Reuse the ten fixtures removed in the annotated plan, preserving their materials.
reuse=dict(zip(['N01','N02','N03','N04','N05','N06','N07','N08','S01','S02'],plan['remove_ids']))
records=[]
for entry in plan['placements']:
    id=entry['id'];old=reuse.get(id,id)
    sub=bpy.data.collections['A31_'+old]
    root=next(o for o in sub.objects if o.type=='EMPTY')
    host=None
    if id.startswith('N'):
        x,y=entry['column_center_x'],entry['column_center_y'];z=entry['center_world_z']
        shaft=min([o for o in scene.objects if 'Column_Grooved_Shaft' in o.name],key=lambda o:(o.matrix_world.translation-Vector((x,y,0))).length)
        # Geometry may be stored in world coordinates: choose using world bounding center instead.
        def distance(ob):
            p=[ob.matrix_world@Vector(v) for v in ob.bound_box];c=sum(p,Vector())/8
            return (c.x-x)**2+(c.y-y)**2
        shaft=min([o for o in scene.objects if 'Column_Grooved_Shaft' in o.name],key=distance)
        center=Vector((x,y,z))
        toward=Vector((-x,-y if abs(y)>1 else 0,0)).normalized()
        pos,n=mount(shaft,center,toward);host=shaft.name
    elif id.startswith('S'):
        x=entry['schematic_reference_x'];y=5.5
        host='Landing_Enclosed_Base_'+('-1.002' if x<0 else '1.002')
        pos,n=mount(bpy.data.objects[host],Vector((x,y,2.5)),Vector((0,1,0)))
    if host:
        root.matrix_world=Matrix.Translation(pos)@Matrix.Rotation(math.atan2(n.x,-n.y),4,'Z')
    for ob in sub.objects:ob.name=ob.name.replace('A31_'+old+'_','A33_'+id+'_',1)
    sub.name='A33_'+id
    root['plan_id']=id;root['floor']=entry['floor'];root['revision']='Rev03'
    root['mount_center_world_z']=root.location.z+.4
    records.append({'id':id,'root':root.name,'collection':sub.name,'host':host,'location':list(root.location),'center_world_z':root.location.z+.4,'rotation_z_deg':math.degrees(root.rotation_euler.z)})
col.name='A33_Wall_Sconces';scene.name='BBB_Hall_A33_WallSconces_Rev03'
bpy.context.view_layer.update()
assert len(col.all_objects)==392
assert all(name in scene.objects and scene.objects[name].matrix_world==matrix for name,matrix in original_nonfixtures.items())
for ob in bpy.context.selected_objects:ob.select_set(False)
(base/'A33_Wall_Sconce_Applied.json').write_text(json.dumps({'count':14,'first_floor':10,'second_floor':4,'revision':'Rev03','placements':records},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(records))
