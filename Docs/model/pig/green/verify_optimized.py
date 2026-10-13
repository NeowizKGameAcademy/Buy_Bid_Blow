import bpy, json
from pathlib import Path
root = Path(__file__).resolve().parent / 'optimized'
rows = []
for path in sorted(root.iterdir()):
    if path.suffix not in ('.glb', '.fbx'):
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    if path.suffix == '.glb':
        bpy.ops.import_scene.gltf(filepath=str(path))
    else:
        bpy.ops.import_scene.fbx(filepath=str(path))
    meshes = [o.data for o in bpy.context.scene.objects if o.type == 'MESH']
    row = dict(file=path.name, vertices=sum(len(m.vertices) for m in meshes), triangles=sum(sum(len(p.vertices)-2 for p in m.polygons) for m in meshes), uv_layers=[len(m.uv_layers) for m in meshes], textures=[dict(name=i.name, width=i.size[0], height=i.size[1], loaded=i.has_data) for i in bpy.data.images])
    assert meshes and all(len(m.uv_layers) for m in meshes)
    assert all(i['loaded'] and i['width'] > 0 for i in row['textures']) and row['textures']
    rows.append(row)
    print('VERIFIED ' + json.dumps(row), flush=True)
(root / 'verification.json').write_text(json.dumps(rows, indent=2), encoding='utf-8')
