import bpy, math, os, json
import numpy as np
from mathutils import Vector

BASE='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/Seating'
OUT=BASE+'/Unity/Textures'
os.makedirs(OUT,exist_ok=True)

def principled(m):
    return next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')

def set_input(m,key,value):
    s=principled(m).inputs[key]
    for l in list(s.links):m.node_tree.links.remove(l)
    s.default_value=value

def tint(m,h,s,v):
    tree=m.node_tree;p=principled(m);socket=p.inputs['Base Color']
    src=socket.links[0].from_socket
    hs=tree.nodes.new('ShaderNodeHueSaturation')
    hs.inputs['Hue'].default_value=h;hs.inputs['Saturation'].default_value=s;hs.inputs['Value'].default_value=v
    tree.links.new(src,hs.inputs['Color']);tree.links.new(hs.outputs['Color'],socket)

def copy_mat(src,name):
    m=bpy.data.materials[src].copy();m.name=name;return m

def projection_uv(o):
    mesh=o.data;uv=mesh.uv_layers.get('UVMap') or mesh.uv_layers.new(name='UVMap')
    parent=list(range(len(mesh.vertices)))
    def find(i):
        while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
        return i
    for e in mesh.edges:
        a,b=(find(i) for i in e.vertices)
        if a!=b:parent[b]=a
    parts={}
    for i in range(len(parent)):parts.setdefault(find(i),[]).append(i)
    wood=o.name.endswith('Woodwork');frames={}
    if wood:
        for key,ids in parts.items():
            arr=np.array([mesh.vertices[i].co[:] for i in ids]);center=arr.mean(axis=0)
            _,axes=np.linalg.eigh((arr-center).T@(arr-center))
            axis=Vector(axes[:,-1]);axis*=1 if axis[max(range(3),key=lambda i:abs(axis[i]))]>=0 else -1
            frames[key]=axis
    for p in mesh.polygons:
        n=p.normal
        if wood:
            vaxis=frames[find(p.vertices[0])]
            # Grain follows each separate timber member; end caps use a planar cut.
            uaxis=n.cross(vaxis)
            if uaxis.length<.15:
                k=min(range(3),key=lambda i:abs(n[i]));a=Vector((int(k==0),int(k==1),int(k==2)))
                uaxis=n.cross(a).normalized();vaxis=uaxis.cross(n).normalized()
            else:
                # Quantize the across-grain axis so adjacent bevel triangles share a coherent mapping.
                k=max(range(3),key=lambda i:abs(uaxis[i]));uaxis=Vector((int(k==0),int(k==1),int(k==2)))
            scale=1.2
        else:
            axis=max(range(3),key=lambda i:abs(n[i]))
            uaxis=Vector((0,1,0) if axis==0 else (1,0,0));vaxis=Vector((0,1,0) if axis==2 else (0,0,1))
            scale=2.2 if 'Upholstery' in o.name else 1.0
        for li in p.loop_indices:
            co=mesh.vertices[mesh.loops[li].vertex_index].co
            uv.data[li].uv=(co.dot(uaxis)*scale,co.dot(vaxis)*scale)
    o['uv_mapping']='Metric tiling; wood grain follows disconnected timber components'

def prepare():
    global scene,mats
    scene=bpy.context.scene
    mats={}
    mats['Walnut']=copy_mat('BBB_Furniture_Hall_Walnut','BBB_Seating_Walnut')
    set_input(mats['Walnut'],'Roughness',.36);set_input(mats['Walnut'],'Coat Weight',.16)
    for n in mats['Walnut'].node_tree.nodes:
        if n.type=='HUE_SAT':n.inputs['Value'].default_value=.30;n.inputs['Saturation'].default_value=.8
    next(n for n in mats['Walnut'].node_tree.nodes if n.type=='NORMAL_MAP').inputs['Strength'].default_value=.18
    mats['Brass']=copy_mat('Tables_Satin_Brass','BBB_Seating_Brass')
    set_input(mats['Brass'],'Roughness',.27)
    for name,hue,sat,val in [('BurgundyVelvet',.52,1.7,.8),('TealVelvet',.08,1.8,.65)]:
        m=copy_mat('Red Wine Velvet Fabric','BBB_Seating_'+name);mats[name]=m
        tint(m,hue,sat,val)
        tree=m.node_tree;p=principled(m)
        tex=next(n for n in tree.nodes if n.type=='TEX_IMAGE' and n.image and 'Basecolor' in n.image.name)
        ramp=tree.nodes.new('ShaderNodeValToRGB')
        lo,hi=((.035,.0025,.005,1),(.21,.024,.036,1)) if name=='BurgundyVelvet' else ((.006,.024,.026,1),(.025,.12,.13,1))
        ramp.color_ramp.elements[0].position=0;ramp.color_ramp.elements[0].color=lo
        ramp.color_ramp.elements[1].position=.27;ramp.color_ramp.elements[1].color=hi
        tree.links.new(tex.outputs['Color'],ramp.inputs[0]);tree.links.new(ramp.outputs['Color'],p.inputs['Base Color'])
        set_input(m,'Metallic',0);set_input(m,'Roughness',.72);set_input(m,'Sheen Weight',.12)
        next(n for n in m.node_tree.nodes if n.type=='NORMAL_MAP').inputs['Strength'].default_value=.12
        out=next(n for n in m.node_tree.nodes if n.type=='OUTPUT_MATERIAL')
        for l in list(out.inputs['Displacement'].links):m.node_tree.links.remove(l)
    m=copy_mat('Leather Brown','BBB_Seating_OxbloodLeather');mats['OxbloodLeather']=m
    p=principled(m);nodes=m.node_tree.nodes;links=m.node_tree.links
    base=next(n for n in nodes if n.type=='TEX_IMAGE' and n.image and n.image.name.startswith('leather-mix-2a_diffuse'))
    links.new(base.outputs['Color'],p.inputs['Base Color']);tint(m,.47,1.25,1.1)
    rough=next(n for n in nodes if n.type=='TEX_IMAGE' and n.image and n.image.name.startswith('leather-mix-2a_roughess'))
    # Map source roughness into a polished, supple leather range.
    remap=nodes.new('ShaderNodeMapRange');remap.inputs['To Min'].default_value=.14;remap.inputs['To Max'].default_value=.60
    links.new(rough.outputs['Color'],remap.inputs['Value']);links.new(remap.outputs[0],p.inputs['Roughness'])
    normal=next(n for n in nodes if n.type=='TEX_IMAGE' and n.image and n.image.name.startswith('leather-mix-2a_normal'))
    nm=next(n for n in nodes if n.type=='NORMAL_MAP');links.new(normal.outputs['Color'],nm.inputs['Color']);nm.inputs['Strength'].default_value=.24;links.new(nm.outputs[0],p.inputs['Normal'])
    set_input(m,'Metallic',0);set_input(m,'Subsurface Weight',0);set_input(m,'Coat Weight',.13);set_input(m,'Coat Roughness',.25)
    for o in scene.objects:
        if o.type!='MESH':continue
        role=o.name.rsplit('_',1)[-1]
        key='Walnut' if role=='Woodwork' else 'Brass' if role=='Metalwork' else 'BurgundyVelvet' if o.name.startswith(('Bench_A','Chair_A')) else 'OxbloodLeather' if o.name.startswith('Sofa_B') else 'TealVelvet'
        o.data.materials.clear();o.data.materials.append(mats[key]);o['material_key']=key
        for p in o.data.polygons:p.material_index=0
        projection_uv(o)
    for a in bpy.context.screen.areas:
        if a.type=='VIEW_3D':
            a.spaces.active.shading.type='MATERIAL';a.spaces.active.overlay.show_overlays=False
    print('Assigned five material families and UVs to 18 meshes')

def bake_color(key,channel='BaseColor'):
    source=mats[key];mat=source.copy();mat.name='TEMP_BAKE_'+key
    tree=mat.node_tree;p=principled(mat);output=next(n for n in tree.nodes if n.type=='OUTPUT_MATERIAL')
    socket=p.inputs['Base Color' if channel=='BaseColor' else 'Roughness']
    em=tree.nodes.new('ShaderNodeEmission')
    if socket.is_linked:tree.links.new(socket.links[0].from_socket,em.inputs['Color'])
    else:
        value=socket.default_value
        em.inputs['Color'].default_value=value if channel=='BaseColor' else (value,value,value,1)
    tree.links.new(em.outputs[0],output.inputs['Surface'])
    temp=bpy.data.scenes.new('Material_Tile_Bake_TEMP');old=bpy.context.window.scene;bpy.context.window.scene=temp
    try:
        temp.render.engine='CYCLES';temp.cycles.samples=1
        data=bpy.data.meshes.new('Material_Tile_TEMP');data.from_pydata([(0,0,0),(1,0,0),(1,1,0),(0,1,0)],[],[(0,1,2,3)]);data.update()
        uv=data.uv_layers.new(name='UVMap')
        for i,co in enumerate([(0,0),(1,0),(1,1),(0,1)]):uv.data[i].uv=co
        obj=bpy.data.objects.new('Material_Tile_TEMP',data);temp.collection.objects.link(obj);data.materials.append(mat)
        bpy.context.view_layer.objects.active=obj;obj.select_set(True)
        size=1024 if key=='Walnut' else 2048
        im=bpy.data.images.new('BBB_'+key+'_'+channel,width=size,height=size,alpha=False)
        im.colorspace_settings.name='sRGB' if channel=='BaseColor' else 'Non-Color'
        node=tree.nodes.new('ShaderNodeTexImage');node.image=im;tree.nodes.active=node
        bpy.ops.object.bake(type='EMIT',margin=0)
        im.filepath_raw=OUT+'/'+key+'_'+channel+'.png';im.file_format='PNG';im.save()
        print('Baked',im.filepath_raw)
    finally:
        bpy.context.window.scene=old
        for ob in list(temp.objects):bpy.data.objects.remove(ob,do_unlink=True)
        bpy.data.scenes.remove(temp);bpy.data.materials.remove(mat)

def portable():
    spec={
        'Walnut':('BBB_Door_Wood_Normal.004',.18,.36,0),
        'Brass':(None,0,.27,.88),
        'BurgundyVelvet':('Velvet_Fabric_Normal.jpg.005',.12,.72,0),
        'TealVelvet':('Velvet_Fabric_Normal.jpg.005',.12,.72,0),
        'OxbloodLeather':('leather-mix-2a_normal.jpg',.24,None,0)}
    manifest=[]
    for key,m in mats.items():
        p=principled(m);normal_name,strength,roughness,metallic=spec[key]
        base_color=list(p.inputs['Base Color'].default_value)
        tree=m.node_tree;tree.nodes.clear();p=tree.nodes.new('ShaderNodeBsdfPrincipled');out=tree.nodes.new('ShaderNodeOutputMaterial');tree.links.new(p.outputs['BSDF'],out.inputs['Surface'])
        p.inputs['Metallic'].default_value=metallic
        maps={}
        def add_image(path,cs,target):
            im=bpy.data.images.load(path,check_existing=True);im.colorspace_settings.name=cs;im.reload()
            n=tree.nodes.new('ShaderNodeTexImage');n.image=im
            if target:tree.links.new(n.outputs['Color'],p.inputs[target])
            return n
        if key!='Brass':
            path=OUT+'/'+key+'_BaseColor.png';add_image(path,'sRGB','Base Color');maps['baseColor']=os.path.basename(path)
        else:p.inputs['Base Color'].default_value=base_color
        if normal_name:
            im=bpy.data.images[normal_name]
            filename=('Velvet' if 'Velvet' in key else key)+'_Normal.png';path=OUT+'/'+filename
            im.filepath_raw=path;im.file_format='PNG';im.save()
            n=add_image(path,'Non-Color',None);nm=tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=strength
            tree.links.new(n.outputs['Color'],nm.inputs['Color']);tree.links.new(nm.outputs['Normal'],p.inputs['Normal']);maps['normal']=filename
        if roughness is None:
            path=OUT+'/'+key+'_Roughness.png';add_image(path,'Non-Color','Roughness');maps['roughness']=os.path.basename(path)
        else:p.inputs['Roughness'].default_value=roughness
        if 'Velvet' in key:p.inputs['Sheen Weight'].default_value=.12
        if key in ('Walnut','OxbloodLeather'):
            p.inputs['Coat Weight'].default_value=.13;p.inputs['Coat Roughness'].default_value=.25
        m['unity_texture_material']=True
        manifest.append({'key':key,'material':m.name,'textures':maps,'baseColorLinear':base_color if key=='Brass' else [1,1,1,1],'metallic':metallic,'roughness':roughness,'normalScale':strength})
    with open(BASE+'/Unity/Material_Manifest.json','w',encoding='utf-8') as f:json.dump(manifest,f,indent=2)
    scene['materials_pending']=False;scene['material_texture_workflow']='Shared tiling PBR images; no displacement geometry'
    print(json.dumps(manifest,indent=2))
