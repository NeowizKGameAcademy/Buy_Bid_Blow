import bpy, json, math
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parent
out = root / 'optimized'
out.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=str(root / 'stylized pig 3d model.glb'), merge_vertices=True)
source = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
source.name = 'Pig_Black_Original'
scene = bpy.context.scene
stats = []

def record(obj, label):
    mesh = obj.data
    mesh.calc_loop_triangles()
    row = dict(variant=label, vertices=len(mesh.vertices), triangles=len(mesh.loop_triangles), uv_layers=len(mesh.uv_layers), materials=len(mesh.materials))
    stats.append(row)
    print('STATS ' + json.dumps(row), flush=True)

record(source, 'original')
variants = [source]
for target in (100000, 50000):
    obj = source.copy()
    obj.data = source.data.copy()
    scene.collection.objects.link(obj)
    obj.name = 'Pig_Black_' + str(target // 1000) + 'k'
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new('Polygon reduction', 'DECIMATE')
    mod.ratio = target / stats[0]['triangles']
    mod.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=mod.name)
    record(obj, str(target // 1000) + 'k')
    stem = out / obj.name
    bpy.ops.export_scene.gltf(filepath=str(stem.with_suffix('.glb')), use_selection=True, export_format='GLB')
    bpy.ops.export_scene.fbx(filepath=str(stem.with_suffix('.fbx')), use_selection=True, object_types={'MESH'}, path_mode='COPY', embed_textures=True, bake_anim=False, add_leaf_bones=False)
    variants.append(obj)
    print('EXPORTED ' + obj.name, flush=True)

# Identical cameras, lighting and materials for the visual comparison.
scene.render.engine = 'CYCLES'
scene.cycles.samples = 16
scene.cycles.use_denoising = True
scene.render.resolution_x = 640
scene.render.resolution_y = 640
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.world = bpy.data.worlds.new('Preview World')
scene.world.use_nodes = True
next(n for n in scene.world.node_tree.nodes if n.type == 'BACKGROUND').inputs[0].default_value = (0.22, 0.22, 0.22, 1)
bounds = [source.matrix_world @ Vector(c) for c in source.bound_box]
center = sum(bounds, Vector()) / 8
size = max(max(v[i] for v in bounds) - min(v[i] for v in bounds) for i in range(3))
camera_data = bpy.data.cameras.new('Comparison Camera')
camera = bpy.data.objects.new('Comparison Camera', camera_data)
scene.collection.objects.link(camera)
scene.camera = camera
camera_data.type = 'ORTHO'
camera_data.ortho_scale = size * 1.2
for name, position, power, scale in [('Key', (2,-3,4), 450, 3), ('Fill', (-3,-1,2), 220, 3), ('Rim', (1,3,3), 350, 2)]:
    data = bpy.data.lights.new(name, 'AREA')
    data.energy = power * size * size
    data.shape = 'DISK'
    data.size = size * scale
    light = bpy.data.objects.new(name, data)
    scene.collection.objects.link(light)
    light.location = center + Vector(position) * size
    light.rotation_euler = (center - light.location).to_track_quat('-Z', 'Y').to_euler()
for view, direction in [('front', (0,-3,0.4)), ('back', (1,3,0.5))]:
    camera.location = center + Vector(direction) * size
    camera.rotation_euler = (center - camera.location).to_track_quat('-Z','Y').to_euler()
    for obj in variants:
        for other in variants:
            other.hide_render = other != obj
        scene.render.filepath = str(out / (obj.name + '_' + view + '.png'))
        bpy.ops.render.render(write_still=True)
for row in stats[1:]:
    stem = out / ('Pig_Black_' + row['variant'])
    row['files'] = {ext: stem.with_suffix('.'+ext).stat().st_size for ext in ('fbx','glb')}
(out / 'statistics.json').write_text(json.dumps(stats, indent=2), encoding='utf-8')
print('COMPLETE', flush=True)

