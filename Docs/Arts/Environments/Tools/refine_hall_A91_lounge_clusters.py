"""Cluster the existing furniture around, rather than inside, the matching area."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector

scene = bpy.context.scene
folder = Path(r'C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Models')
assert scene.name == 'A51_Staff_Portraits'
assert not bpy.data.collections.get('A91_Lounge_Clusters')

def view(position, target, lens):
    position, target = Vector(position), Vector(target)
    for area in bpy.context.screen.areas:
        if area.type == 'VIEW_3D':
            space = area.spaces.active
            space.lens = lens
            space.region_3d.view_rotation = (target-position).to_track_quat('-Z', 'Y')
            space.region_3d.view_location = target
            space.region_3d.view_distance = (target-position).length
            space.region_3d.view_perspective = 'PERSP'
            space.overlay.show_overlays = False

def capture(filename):
    render = scene.render
    old = render.resolution_x, render.resolution_y, render.resolution_percentage, render.filepath, render.image_settings.file_format
    try:
        render.resolution_x, render.resolution_y, render.resolution_percentage = 1600, 900, 100
        render.filepath = str(folder / filename)
        render.image_settings.file_format = 'PNG'
        area = next(a for a in bpy.context.screen.areas if a.type == 'VIEW_3D')
        region = next(r for r in area.regions if r.type == 'WINDOW')
        with bpy.context.temp_override(area=area, region=region):
            bpy.ops.render.opengl(write_still=True, view_context=True)
    finally:
        render.resolution_x, render.resolution_y, render.resolution_percentage, render.filepath, render.image_settings.file_format = old

# Matched eye-height comparison: only the furniture changes between these images.
comparison = ((-.2, 5.5, 1.65), (-8.6, 11.6, 1.05), 35)
view(*comparison)
capture('A91_Left_Before_35mm.png')

collection = bpy.data.collections.new('A91_Lounge_Clusters')
scene.collection.children.link(collection)
added = []

def place(name, position, degrees):
    root = bpy.data.objects[name]
    root.location = position
    root.rotation_euler = (0, 0, math.radians(degrees))
    return root

def instance(source_name, name, position, degrees=0, scale=1):
    source = bpy.data.objects[source_name]
    mapping = {}
    for old in [source] + list(source.children_recursive):
        if 'LOD1' in old.name:
            continue
        new = old.copy()
        new.name = name if old == source else name + '_' + old.name
        collection.objects.link(new)
        mapping[old] = new
    for old, new in mapping.items():
        new.parent = mapping.get(old.parent)
        new.matrix_parent_inverse = old.matrix_parent_inverse.copy()
        new.matrix_basis = old.matrix_basis.copy()
        new.hide_viewport = False
        new.hide_render = False
        new.hide_set(False)
    root = mapping[source]
    root.location = position
    root.rotation_euler = (0, 0, math.radians(degrees))
    root.scale = (scale, scale, scale)
    root['A91_source'] = source_name
    added.append(root)
    return root

changes = [
    ('A90_InformationBoard_A', (-6.7, 6.9, 0), 25),
    ('A90_InformationBoard_B', (-.85, 15.35, 0), 0),
    ('A90_ServiceTrolley_A', (-10.4, 8.4, 0), 90),
    ('A90_ServiceTrolley_B', (10.4, 8.4, 0), -90),
    ('A90_Table_A', (-8.65, 10.6, 0), 90),
    ('A90_Table_B', (-2.2, 15.85, 0), 0),
    ('A90_Bookcase_A', (-9.9, 13.25, 0), 25),
    ('A90_Shelf_A', (-11.0, 12.35, 0), 55),
    ('A90_Shelf_B', (-2.2, 17.32, 1.15), 0),
    ('A90_LeafletRack', (-3.55, 15.45, 0), 12),
    ('A90_UmbrellaStand', (7.0, 6.95, 0), -25),
]
for spec in changes:
    place(*spec)

instance('A87_Left_Sofa_ROOT', 'A91_Left_Teal_Sofa', (-10.05, 10.6, -.01), 90)
instance('A88_Red_Sofa_ROOT', 'A91_Right_Red_Sofa', (10.05, 10.6, -.01), -90)
instance('A87_Left_Bench_ROOT', 'A91_Left_Bench', (-7.2, 10.6, -.007655), 90)
instance('A87_Left_Bench_ROOT', 'A91_Right_Bench', (7.2, 10.6, -.007655), 90)
instance('A90_Table_A', 'A91_Right_Coffee_Table', (8.65, 10.6, 0), 90)
instance('A90_Bookcase_A', 'A91_Rear_Library_Case', (-4.75, 16.4, 0), 14)
instance('A90_Bookcase_B', 'A91_Right_Library_Case', (9.4, 13.4, 0), -36)
instance('A90_DisplayCabinet', 'A91_Rear_Display_Cabinet', (1.9, 16.8, 0), 0)
instance('A24_WOOD_05_Staff_Chair_ROOT', 'A91_Information_Desk_Chair', (-2.2, 16.6, -.001), 180)
for name, position, angle in [
    ('A91_Left_Planter', (-11.3, 7.45, 0), 25),
    ('A91_Right_Planter', (11.3, 7.45, 0), -25),
    ('A91_Rear_Planter', (-5.6, 14.6, 0), 15),
]:
    instance('BBB_Areca_Card_Planter_ROOT', name, position, angle, .48)

# Two quiet textile rugs visually group the seats without competing with the matching carpet.
rug_material = bpy.data.materials['BBB_Seating_TealVelvet'].copy()
rug_material.name = 'A91_Lounge_Rug_BlenderKit_Fabric'
shader = next(n for n in rug_material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
shader.inputs['Roughness'].default_value = 1
for side in [-1, 1]:
    cx, cy, w, d, bevel = side*8.8, 10.6, 4.0, 4.1, .09
    xy = [(-w/2+bevel,-d/2), (w/2-bevel,-d/2), (w/2,-d/2+bevel), (w/2,d/2-bevel), (w/2-bevel,d/2), (-w/2+bevel,d/2), (-w/2,d/2-bevel), (-w/2,-d/2+bevel)]
    mesh = bpy.data.meshes.new('A91_Lounge_Rug_Mesh')
    mesh.from_pydata([(x,y,.012) for x,y in xy], [], [tuple(range(8))])
    mesh.materials.append(rug_material)
    uv = mesh.uv_layers.new(name='Fabric_UV')
    for loop in mesh.loops:
        p = mesh.vertices[loop.vertex_index].co
        uv.data[loop.index].uv = (p.x/2+.5, p.y/2+.5)
    rug = bpy.data.objects.new('A91_Left_Rug' if side < 0 else 'A91_Right_Rug', mesh)
    collection.objects.link(rug)
    rug.location = (cx,cy,0)
    added.append(rug)

bpy.context.view_layer.update()

def world_bounds(root):
    meshes = [o for o in [root]+list(root.children_recursive) if o.type == 'MESH']
    points = [o.matrix_world @ Vector(p) for o in meshes for p in o.bound_box]
    return Vector([min(p[i] for p in points) for i in range(3)]), Vector([max(p[i] for p in points) for i in range(3)])

roots = [o for o in scene.objects if o.type == 'EMPTY' and o.parent is None and o.name.startswith('A90_')]
roots += added
report = []
for root in roots:
    low, high = world_bounds(root)
    intrusion = low.x < 5.9 and high.x > -5.9 and low.y < 13.6 and high.y > 5.6
    assert not intrusion, 'Matching space obstruction: ' + root.name
    assert high.z < 3.5, 'Ceiling clearance: ' + root.name
    if 'Shelf_B' not in root.name:
        assert abs(low.z) < .025, 'Floor contact: ' + root.name
    report.append({'name': root.name, 'position': list(root.location), 'bounds': [list(low),list(high)]})

scene['A91_layout_note'] = 'Two furnished seating groups, a staffed information desk and a grouped rear display. Matching area kept clear, including 0.7 m buffer around the carpet. Existing shared-mesh furniture instances.'
(folder/'A91_Furniture_Layout.json').write_text(json.dumps({'review_lens_mm':35,'review_eye_height_m':1.65,'matching_clearance':{'x':[-5.9,5.9],'y':[5.6,13.6]},'placements':report},indent=2))

view(*comparison)
capture('A91_Left_After_35mm.png')
print(json.dumps({'placed_objects':len(roots),'added_instances':len(added),'matching_clearance_passed':True,'floor_and_ceiling_passed':True}))
