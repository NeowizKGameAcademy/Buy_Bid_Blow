"""Place existing game-ready furniture around the under-mezzanine matching area."""
import bpy, math, json
from mathutils import Vector
from pathlib import Path

hall = bpy.data.scenes['A51_Staff_Portraits']
bpy.context.window.scene = hall
assert not bpy.data.collections.get('A90_UnderMezzanine_Furniture'), 'Layout already exists; edit it in place.'
collection = bpy.data.collections.new('A90_UnderMezzanine_Furniture')
hall.collection.children.link(collection)
zones = {}
for name in ['Left_Reading_Area', 'Rear_Information_Area', 'Right_Service_Area']:
    zones[name] = bpy.data.collections.new('A90_' + name)
    collection.children.link(zones[name])

specs = [
    ('InformationBoard_A', (-6.8, 7.2, 0), 35, 'Left_Reading_Area'),
    ('InformationBoard_B', (6.8, 7.6, 0), -30, 'Right_Service_Area'),
    ('ServiceTrolley_A', (-8.9, 12.6, 0), 35, 'Left_Reading_Area'),
    ('ServiceTrolley_B', (9.3, 12.6, 0), -35, 'Right_Service_Area'),
    ('Table_A', (-7.5, 13.8, 0), 30, 'Left_Reading_Area'),
    ('Table_B', (-1.8, 16.68, 0), 0, 'Rear_Information_Area'),
    ('Bookcase_A', (-4.4, 16.58, 0), 12, 'Left_Reading_Area'),
    ('Bookcase_B', (3.6, 16.70, 0), -9, 'Rear_Information_Area'),
    ('Shelf_A', (-9.5, 13.95, 0), 40, 'Left_Reading_Area'),
    ('Shelf_B', (-1.8, 17.35, 1.15), 0, 'Rear_Information_Area'),
    ('DisplayCabinet', (5.2, 16.10, 0), -16, 'Rear_Information_Area'),
    ('LeafletRack', (7.0, 13.8, 0), -25, 'Right_Service_Area'),
    ('UmbrellaStand', (10.7, 8.0, 0), -60, 'Right_Service_Area'),
]

placed = []
for name, position, angle, zone in specs:
    source = bpy.data.objects[name]
    mapping = {}
    for old in [source] + list(source.children_recursive):
        if 'LOD1' in old.name:
            continue
        new = old.copy()
        new.name = 'A90_' + old.name
        zones[zone].objects.link(new)
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
    root.rotation_euler = (0, 0, math.radians(angle))
    root.scale = (1, 1, 1)
    root['source_asset'] = name
    root['layout_zone'] = zone
    placed.append(root)

hall.view_layers.update()
bpy.context.view_layer.update()

def bounds(root):
    points = [obj.matrix_world @ Vector(corner) for obj in root.children_recursive if obj.type == 'MESH' for corner in obj.bound_box]
    return Vector([min(p[i] for p in points) for i in range(3)]), Vector([max(p[i] for p in points) for i in range(3)])

report = []
for root in placed:
    low, high = bounds(root)
    # Keep a buffer around the existing 10.4 x 6.6 metre matching carpet.
    intrudes = low.x < 5.9 and high.x > -5.9 and low.y < 13.6 and high.y > 5.6
    assert not intrudes, 'Matching clearance: ' + root.name
    if root['source_asset'] != 'Shelf_B':
        assert abs(low.z) < .025, 'Floor contact: ' + root.name
    assert high.z < 3.5, 'Mezzanine ceiling clearance: ' + root.name
    report.append({'name': root.name, 'position': list(root.location), 'yaw_degrees': round(math.degrees(root.rotation_euler.z), 2), 'bounds': [list(low), list(high)]})

review = bpy.data.collections.new('A90_Review_Cameras')
hall.collection.children.link(review)
camera_specs = [
    ('A90_Overview', (0, .5, 2.4), (0, 13.4, 1.3), 21),
    ('A90_Left_Furniture', (-4, 9.5, 2.0), (-6.8, 15, 1), 25),
    ('A90_Rear_Information', (0, 11.5, 1.9), (1.1, 16.9, 1.35), 21),
    ('A90_Right_Furniture', (5.2, 9.2, 1.9), (8.1, 13.5, .95), 26),
]
for name, position, target, lens in camera_specs:
    data = bpy.data.cameras.new(name)
    camera = bpy.data.objects.new(name, data)
    review.objects.link(camera)
    camera.location = position
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    data.lens = lens
    data.clip_end = 200

hall.camera = bpy.data.objects['A90_Overview']
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        space = area.spaces.active
        space.shading.type = 'MATERIAL'
        space.overlay.show_overlays = False
        camera = hall.camera
        target = Vector((0, 13.4, 1.3))
        space.region_3d.view_rotation = camera.rotation_euler.to_quaternion()
        space.region_3d.view_location = target
        space.region_3d.view_distance = (target - camera.location).length
        space.region_3d.view_perspective = 'PERSP'
        space.lens = camera.data.lens

hall['A90_layout_note'] = '13 furniture instances. Central matching carpet and stair access remain clear. Shared source meshes; LOD0 only for Blender review.'
path = Path(r'C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Models/A90_Furniture_Layout.json')
path.write_text(json.dumps({'matching_clearance': {'x': [-5.9, 5.9], 'y': [5.6, 13.6]}, 'placements': report}, indent=2))
print(json.dumps({'placed': len(placed), 'matching_clearance_passed': True, 'floor_contact_passed': True, 'ceiling_clearance_passed': True}))
