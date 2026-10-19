"""Apply packed BlenderKit materials to the lounge scene without changing meshes."""
import bpy, json
from pathlib import Path

BASE=Path(r'C:/course/unity/BuyBidBlow')
TEX=BASE/'Assets/Project/Content/Props/LoungeAccessories/Textures'
SCENE=bpy.data.scenes['Lounge_Accessories_Game_Ready']
def image(name,linear=False):
    img=bpy.data.images.load(str(TEX/(name+'.png')),check_existing=True)
    img.colorspace_settings.name='Non-Color' if linear else 'sRGB'
    img.pack();return img

mat=bpy.data.materials.get('Lounge_BK_Atlas')
if not mat:
    mat=bpy.data.materials['Lounge_Atlas'].copy();mat.name='Lounge_BK_Atlas'
nodes=mat.node_tree.nodes;links=mat.node_tree.links
bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
for node in nodes:
    if node.type=='TEX_IMAGE' and node.image:
        if 'BaseColor' in node.image.name:node.image=image('Lounge_BK_Atlas_BaseColor')
        elif 'MetallicSmoothness' in node.image.name:node.image=image('Lounge_BK_Atlas_MetallicSmoothness',True)
normal=next((n for n in nodes if n.type=='NORMAL_MAP'),None)
if normal is None:
    normal=nodes.new('ShaderNodeNormalMap');tex=nodes.new('ShaderNodeTexImage');tex.image=image('Lounge_BK_Atlas_Normal',True)
    links.new(tex.outputs['Color'],normal.inputs['Color']);links.new(normal.outputs[0],bs.inputs['Normal'])
normal.inputs['Strength'].default_value=1
sources=json.loads((BASE/'Docs/Arts/Props/MaterialSources/LoungeAccessories/BlenderKit_Sources.json').read_text())
mat['material_sources']=json.dumps(sources[:2])

glass=bpy.data.materials.get('Lounge_BK_Glass')
if not glass:
    glass=bpy.data.materials['Lounge_Glass'].copy();glass.name='Lounge_BK_Glass'
nodes=glass.node_tree.nodes;links=glass.node_tree.links
bs=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
bs.inputs['Base Color'].default_value=(.93,.98,.97,1)
bs.inputs['Alpha'].default_value=.075;bs.inputs['IOR'].default_value=1.49
bs.inputs['Transmission Weight'].default_value=0
glass.surface_render_method='BLENDED';glass.diffuse_color=(.93,.98,.97,.075)
tex=next((n for n in nodes if n.type=='TEX_IMAGE'),None) or nodes.new('ShaderNodeTexImage')
tex.image=image('Lounge_BK_Glass_MetallicSmoothness',True)
inv=next((n for n in nodes if n.type=='MATH'),None) or nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
links.new(tex.outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bs.inputs['Roughness'])
glass['material_source']=json.dumps(sources[2])
glass['game_adaptation']='Original smudge roughness compressed to .06-.24; alpha glass approximates transmission for URP.'

for ob in SCENE.objects:
    if ob.type=='MESH' and ob.name.startswith(('DisplayCabinet','LeafletRack','UmbrellaStand')):
        ob.data.materials[0]=glass if 'Glass' in ob.name else mat
SCENE['material_sources']=json.dumps(sources)
SCENE['texture_pipeline']='BlenderKit wood/brass maps packed into 1K atlas, normal map added; glass uses 512px smudge mask.'
bpy.data.libraries.write(str(BASE/'Docs/Arts/Props/Models/BBB_Lounge_Accessories_BlenderKit.blend'),{SCENE},fake_user=True)
print('BlenderKit materials applied; mesh geometry unchanged.')
