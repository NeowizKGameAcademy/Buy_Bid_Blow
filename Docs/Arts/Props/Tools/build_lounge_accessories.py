"""Run in Blender. Three reusable metre-scale props; one atlas + separate glass."""
import bpy, bmesh, math, os, json
from mathutils import Vector, Matrix
from math import sin, cos, pi

BASE=r'C:/course/unity/BuyBidBlow'
ASSET=BASE+'/Assets/Project/Content/Props/LoungeAccessories'
SOURCE=BASE+'/Docs/Arts/Props/Models'
os.makedirs(ASSET+'/Models',exist_ok=True)
scene=bpy.data.scenes.new('Lounge_Accessories_Game_Ready')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
scene.unit_settings.scale_length=1
regions={'wood':(8,8,504,1016),'brass':(520,8,760,248),'velvet':(776,8,1016,248),
 'charcoal':(520,264,760,504),'ceramic':(776,264,1016,504),'paper':(512,512,768,896),
 'clock':(768,512,1024,768),'cream':(776,776,1016,888),'dark':(520,904,760,1016)}
atlas=bpy.data.materials.new('Lounge_Atlas');atlas.use_nodes=True
bs=next(n for n in atlas.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
base_img=bpy.data.images.load(ASSET+'/Textures/Lounge_Atlas_BaseColor.png',check_existing=True)
mask_img=bpy.data.images.load(ASSET+'/Textures/Lounge_Atlas_MetallicSmoothness.png',check_existing=True)
mask_img.colorspace_settings.name='Non-Color'
t=atlas.node_tree.nodes.new('ShaderNodeTexImage');t.image=base_img
m=atlas.node_tree.nodes.new('ShaderNodeTexImage');m.image=mask_img
sep=atlas.node_tree.nodes.new('ShaderNodeSeparateColor')
inv=atlas.node_tree.nodes.new('ShaderNodeMath');inv.operation='SUBTRACT';inv.inputs[0].default_value=1
links=atlas.node_tree.links
links.new(t.outputs['Color'],bs.inputs['Base Color']);links.new(m.outputs['Color'],sep.inputs[0]);links.new(sep.outputs[0],bs.inputs['Metallic'])
links.new(m.outputs['Alpha'],inv.inputs[1]);links.new(inv.outputs[0],bs.inputs['Roughness'])
glass=bpy.data.materials.new('Lounge_Glass');glass.use_nodes=True
g=next(n for n in glass.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
g.inputs['Base Color'].default_value=(.54,.75,.72,1);g.inputs['Roughness'].default_value=.12;g.inputs['Alpha'].default_value=.16
glass.diffuse_color=(.54,.75,.72,.16)
glass.surface_render_method='BLENDED'
parts=[];root=None

def uvrect(region,u,v):
    x0,y0,x1,y1=regions[region]
    return ((x0+u*(x1-x0))/1024,1-(y1-v*(y1-y0))/1024)

def mesh(name,verts,faces,region='wood',smooth=False,uvs=None):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    ob=bpy.data.objects.new(name,me);scene.collection.objects.link(ob);ob.parent=root
    me.materials.append(atlas)
    uv=me.uv_layers.new(name='UVMap')
    for poly in me.polygons:
        poly.use_smooth=smooth
        n=poly.normal;axis=max(range(3),key=lambda i:abs(n[i]));axes=[i for i in range(3) if i!=axis]
        for li in poly.loop_indices:
            idx=me.loops[li].vertex_index;p=me.vertices[idx].co
            if uvs: u,v=uvs[idx]
            else: u=(p[axes[0]]*1.3+.5)%1;v=(p[axes[1]]*.65+.5)%1
            uv.data[li].uv=uvrect(region,u,v)
    parts.append(ob);return ob

def activate(ob):
    bpy.ops.object.select_all(action='DESELECT');ob.select_set(True);bpy.context.view_layer.objects.active=ob

def box(name,pos,size,region='wood',bevel=0,rot=None):
    verts=[Vector((x*size[0]/2,y*size[1]/2,z*size[2]/2)) for z in [-1,1] for y in [-1,1] for x in [-1,1]]
    if rot:verts=[rot@v for v in verts]
    ob=mesh(name,[Vector(pos)+v for v in verts],[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],region)
    if bevel:
        activate(ob);mod=ob.modifiers.new('Edge highlights','BEVEL');mod.width=bevel;mod.segments=1;mod.limit_method='ANGLE';bpy.ops.object.modifier_apply(modifier=mod.name)
        # Face normals stay flat: single chamfer strips supply a stable low-poly silhouette.
    return ob

def beam(name,a,b,width,depth,region='wood',bevel=0):
    a=Vector(a);b=Vector(b);rot=Vector((0,0,1)).rotation_difference(b-a).to_matrix()
    return box(name,(a+b)/2,(width,depth,(b-a).length),region,bevel,rot)

def lathe(name,profile,pos=(0,0,0),region='brass',n=16,ellipse=1):
    verts=[(pos[0]+r*cos(i*2*pi/n),pos[1]+ellipse*r*sin(i*2*pi/n),pos[2]+z) for r,z in profile for i in range(n)]
    faces=[]
    for j in range(len(profile)-1):
        for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    return mesh(name,verts,faces,region,True)

def tube(name,points,r,region='brass',n=8):
    pts=[Vector(p) for p in points];verts=[]
    for i,p in enumerate(pts):
        tangent=pts[min(i+1,len(pts)-1)]-pts[max(i-1,0)]
        q=Vector((0,0,1)).rotation_difference(tangent)
        verts.extend(p+q@Vector((r*cos(k*2*pi/n),r*sin(k*2*pi/n),0)) for k in range(n))
    faces=[tuple(reversed(range(n))),tuple((len(pts)-1)*n+k for k in range(n))]
    for j in range(len(pts)-1):
        for k in range(n):faces.append((j*n+k,j*n+(k+1)%n,(j+1)*n+(k+1)%n,(j+1)*n+k))
    return mesh(name,verts,faces,region,True)

def front_card(name,c,w,h,region='paper',slope=0):
    x,y,z=c
    return mesh(name,[(x-w/2,y-slope*h/2,z-h/2),(x+w/2,y-slope*h/2,z-h/2),(x+w/2,y+slope*h/2,z+h/2),(x-w/2,y+slope*h/2,z+h/2)],[(0,1,2,3)],region,False,[(0,0),(1,0),(1,1),(0,1)])

roots=[]
def start(name):
    global root,parts
    root=bpy.data.objects.new(name,None);scene.collection.objects.link(root);roots.append(root);parts=[]

def finish():
    opaque=[o for o in parts if o.data.materials[0]==atlas]
    bpy.ops.object.select_all(action='DESELECT')
    for o in opaque:o.select_set(True)
    bpy.context.view_layer.objects.active=opaque[0];bpy.ops.object.join()
    body=bpy.context.object;body.name=root.name+'_LOD0';body.data.name=body.name
    # Recalculate consistently outward normals, remove only degenerate edges.
    bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.dissolve_degenerate(bm,dist=.0000001,edges=list(bm.edges));bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    # Open printed cards must face outward (-Y); volume-based normal repair may flip them.
    layer=bm.loops.layers.uv.active
    for f in bm.faces:
        uv=[loop[layer].uv for loop in f.loops]
        if (root.name=='LeafletRack' and min(v.x for v in uv)>=.499 and max(v.x for v in uv)<=.751
            and min(v.y for v in uv)>=.124 and max(v.y for v in uv)<=.501
            and max(v.y for v in uv)-min(v.y for v in uv)>.15 and f.normal.y>0):f.normal_flip()
    bm.to_mesh(body.data);bm.free()
    activate(body);bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    low=body.copy();low.data=body.data.copy();low.name=root.name+'_LOD1';scene.collection.objects.link(low);low.parent=root
    activate(low);d=low.modifiers.new('Distance simplification','DECIMATE');d.decimate_type='COLLAPSE';d.ratio=.48;d.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=d.name)
    low.hide_render=True;low.hide_set(True)
    return body

start('DisplayCabinet')
box('Carcass',(0,0,.35),(1.12,.5,.48),bevel=.008)
box('Lower plinth',(0,0,.12),(1.2,.56,.07),bevel=.009)
box('Top cornice',(0,0,.625),(1.22,.58,.065),bevel=.008)
for x in [-.53,.53]:
 for y in [-.22,.22]:
    lathe('Short turned foot',[(.034,0),(.043,.025),(.033,.055),(.034,.095)],(x,y,0),'brass',12)
for x in [-.28,.28]:
    box('Door panel',(x,-.259,.365),(.51,.028,.39),bevel=.006)
    for xx in [x-.235,x+.235]:box('Door brass inlay',(xx,-.277,.365),(.006,.005,.36),'brass')
    for zz in [.185,.545]:box('Door brass inlay',(x,-.277,zz),(.476,.005,.006),'brass')
    tube('Door knob',[(x+(.18 if x<0 else -.18),-.277,.37),(x+(.18 if x<0 else -.18),-.296,.37)],.013,'brass',8)
for x in [-.562,.562]:
    box('Corner pilaster',(x,-.253,.365),(.032,.036,.43),bevel=.004)
box('Burgundy display bed',(0,0,.667),(1.1,.47,.025),'velvet')
for x in [-.575,.575]:
 for y in [-.255,.255]:
    tube('Vitrine brass upright',[(x,y,.66),(x,y,1.125)],.012,'brass',8)
    lathe('Post cap',[(.018,0),(.018,.014),(.009,.024)],(x,y,1.125),'brass',8)
for z in [.675,1.117]:
 for y in [-.255,.255]:tube('Vitrine horizontal frame',[(-.575,y,z),(.575,y,z)],.012,'brass',8)
 for x in [-.575,.575]:tube('Vitrine side frame',[(x,-.255,z),(x,.255,z)],.012,'brass',8)
gv=[(x,y,z) for z in [.686,1.107] for y in [-.248,.248] for x in [-.565,.565]]
gl=mesh('DisplayCabinet_Glass',gv,[(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5),(4,5,7,6)])
gl.data.materials.clear();gl.data.materials.append(glass)
for x in [-.29,.28]:box('Velvet riser',(x,.025,.709),(.30,.27,.058),'velvet',.005)
# Pocket watch: 16-sided body and printed face instead of modeled numerals.
tube('Watch shell',[(-.29,.015,.835),(-.29,-.022,.835)],.085,'brass',24)
face=[(-.29+.077*cos(i*2*pi/24),-.024,.835+.077*sin(i*2*pi/24)) for i in range(24)]
mesh('Printed watch face',face,[tuple(range(24))],'clock',False,[(.5+.5*cos(i*2*pi/24),.5+.5*sin(i*2*pi/24)) for i in range(24)])
lathe('Watch crown',[(.012,0),(.012,.018)],(-.29,0,.919),'brass',8)
tube('Watch loop',[(-.29+.017*cos(i*pi/6),0,.95+.017*sin(i*pi/6)) for i in range(13)],.004,'brass',6)
lathe('Porcelain auction vase',[(.039,0),(.047,.012),(.065,.055),(.071,.095),(.056,.14),(.031,.164),(.029,.19),(.036,.2),(.027,.2),(.024,.18)],(.28,.025,.739),'ceramic',16)
for z,r in [(.751,.048),(.88,.057),(.933,.034)]:lathe('Vase gilding',[(r,0),(r,.005)],(.28,.025,z),'brass',16)
for x in [-.09,.49]:
    box('Lot card',(x,-.146,.713),(.105,.014,.065),'cream',rot=Matrix.Rotation(math.radians(-18),3,'X'))
finish()

start('LeafletRack')
slope=.24
for x in [-.207,.207]:
    beam('Front side rail',(x,-.25,.025),(x,.033,1.205),.042,.046,'wood',.004)
    beam('Rear folding leg',(x,.29,.025),(x,.017,1.13),.035,.038,'wood',.003)
    tube('Folding brace',[(x,-.17,.34),(x,.21,.34)],.007,'brass',8)
beam('Back panel',(0,-.211,.19),(0,.02,1.15),.36,.022,'wood',.002)
rot=Matrix.Rotation(-math.atan(slope),3,'X')
box('Lower plaque',(0,-.224,.16),(.36,.024,.17),'wood',.004,rot)
for z in [.085,.23]:box('Plaque edge',(0,-.251+slope*(z-.16),z),(.32,.005,.006),'brass',rot=rot)
for z in [.34,.65,.96]:
    y=-.25+slope*z
    box('Pocket shelf',(0,y-.031,z-.07),(.36,.102,.022),'wood',.002)
    for offset in [0,.007,.014]:
        box('Catalogue stack',(offset/2,y-.023+offset,z+.047+offset),(.29,.009,.255),'cream',rot=rot)
    front_card('Auction catalogue',(0,y-.032,z+.047),.29,.255,'paper',slope)
    tube('Brass retaining rail',[(-.184,y-.093,z),(.184,y-.093,z)],.0075,'brass',8)
    for x in [-.183,.183]:tube('Rail attachment',[(x,y-.093,z),(x,y+.004,z)],.006,'brass',8)
tube('Top carrying bar',[(-.2,.024,1.177),(.2,.024,1.177)],.009,'brass',8)
finish()

start('UmbrellaStand')
for z in [.05,.51]:lathe('Open brass band',[(.2,0),(.2,.024),(.184,.024),(.184,0),(.2,0)],(0,0,z),'brass',24,.79)
lathe('Removable drip tray',[(0,.025),(.183,.025),(.183,.055),(.171,.055),(.171,.037),(0,.037)],region='dark',n=24,ellipse=.79)
for i in range(16):
    a=i*2*pi/16;pos=(.19*cos(a),.15*sin(a),.29)
    box('Walnut slat',pos,(.033,.018,.442),'wood',0,Matrix.Rotation(a+pi/2,3,'Z'))
for x in [-.13,.13]:
 for y in [-.09,.09]:lathe('Stand foot',[(.02,0),(.026,.012),(.021,.035)],(x,y,0),'brass',8)
for k,(x,y,height,col) in enumerate([(-.07,-.025,.9,'charcoal'),(0,.052,.99,'velvet'),(.075,-.025,.89,'charcoal')]):
    umbrella_start=len(parts)
    # Metal tips touch the interior drip tray; the folded fabric starts above it.
    z0=.075;z1=height-.18;n=16
    tube('Umbrella ferrule',[(x,y,.037),(x,y,z0+.018)],.006,'brass',8)
    profile=[(.012,z0),(.045,z0+.16),(.052,z1-.17),(.036,z1-.035),(.017,z1)]
    verts=[]
    for radius,z in profile:
        for i in range(n):
            a=i*2*pi/n;r=radius*(1 if i%2==0 else .72)
            verts.append((x+r*cos(a),y+r*sin(a),z))
    faces=[]
    for j in range(len(profile)-1):
        for i in range(n):faces.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    faces.extend([tuple(reversed(range(n))),tuple((len(profile)-1)*n+i for i in range(n))])
    mesh('Folded umbrella canopy',verts,faces,col,False)
    lathe('Fabric fastening strap',[(.046,0),(.046,.018)],(x,y,z1-.115),'dark',16)
    tube('Umbrella shaft',[(x,y,z1-.015),(x,y,height-.095)],.008,'brass',8)
    # Hook continues from the shaft into a semicircle, with a short returned tip.
    pts=[(x,y,height-.13),(x,y,height-.075)]
    pts += [(x-.042+.042*cos(a),y,height-.075+.042*sin(a)) for a in [j*pi/12 for j in range(1,13)]]
    pts.append((x-.084,y,height-.105))
    tube('Curved walnut handle',pts,.012,'wood',8)
    # Small, distinct outward leans fit through the oval mouth without cutting its rim.
    tilt=Matrix.Rotation(math.radians([-6,0,6][k]),3,'Y') @ Matrix.Rotation(math.radians([0,-4,0][k]),3,'X')
    pivot=Vector((x,y,.037))
    umbrella_parts=parts[umbrella_start:]
    for part in umbrella_parts:
        for vertex in part.data.vertices:vertex.co=pivot+tilt@(vertex.co-pivot)
    contact=min(v.co.z for part in umbrella_parts for v in part.data.vertices)
    for part in umbrella_parts:
        for vertex in part.data.vertices:vertex.co.z+=.037-contact
finish()

# Save/export metrically correct assets with the floor-center pivot at the origin.
report=[]
for r in roots:
    bpy.ops.object.select_all(action='DESELECT');r.select_set(True)
    for o in r.children_recursive:o.hide_set(False);o.hide_render=False;o.select_set(True)
    bpy.context.view_layer.objects.active=r
    bpy.ops.export_scene.fbx(filepath=ASSET+'/Models/'+r.name+'.fbx',use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,mesh_smooth_type='OFF',path_mode='STRIP')
    info={'asset':r.name,'lods':{}}
    for lod in ['LOD0','LOD1']:
        o=next(o for o in r.children if o.name.endswith(lod));o.data.calc_loop_triangles()
        info['lods'][lod]={'vertices':len(o.data.vertices),'triangles':len(o.data.loop_triangles)}
        if lod=='LOD1':o.hide_set(True);o.hide_render=True
    report.append(info)

# Display the independent props side by side; the hall scene remains untouched.
for r,x in zip(roots,[-1.25,.22,1.21]):r.location.x=x
floor_mat=bpy.data.materials.new('Preview_Backdrop');floor_mat.diffuse_color=(.19,.22,.20,1);floor_mat.use_nodes=True
next(n for n in floor_mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED').inputs['Base Color'].default_value=(.19,.22,.20,1)
root=None
floor=box('Preview floor',(0,0,-.026),(200,200,.04));floor.data.materials.clear();floor.data.materials.append(floor_mat)
def aim(ob,target):ob.rotation_euler=(Vector(target)-ob.location).to_track_quat('-Z','Y').to_euler()
camera=bpy.data.cameras.new('Lounge preview');cam=bpy.data.objects.new('Lounge preview',camera);scene.collection.objects.link(cam);cam.location=(1.7,-6,2.6);aim(cam,(-.18,0,.57));camera.type='ORTHO';camera.ortho_scale=3.8;scene.camera=cam
for name,pos,energy,size in [('Key',(-2,-3,4),500,4),('Fill',(3,-1,2.8),300,3),('Rim',(0,3,3.5),600,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.shape='DISK';data.size=size
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=pos;aim(o,(0,0,.6))
world=bpy.data.worlds.new('Lounge preview world');world.use_nodes=True;next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs[0].default_value=(.18,.22,.24,1);scene.world=world
scene.render.engine='CYCLES';scene.cycles.samples=48
scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=SOURCE+'/BBB_Lounge_Accessories_Preview.png'
for area in bpy.context.screen.areas:
 if area.type=='VIEW_3D':
    space=area.spaces.active;space.shading.type='MATERIAL';space.overlay.show_overlays=False
    space.region_3d.view_rotation=cam.rotation_euler.to_quaternion();space.region_3d.view_location=Vector((-.18,0,.57));space.region_3d.view_distance=2.7;space.region_3d.view_perspective='ORTHO'
base_img.pack();mask_img.pack()
scene['asset_report']=json.dumps(report)
bpy.data.libraries.write(SOURCE+'/BBB_Lounge_Accessories_GameReady.blend',{scene},fake_user=True)
open(SOURCE+'/BBB_Lounge_Accessories_Budget.json','w').write(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
