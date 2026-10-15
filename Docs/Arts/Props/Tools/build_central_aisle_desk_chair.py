import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector, Matrix

OUT=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Central_Aisle_Desk_Chair_Geometry.blend')
assert not OUT.exists()
original_scene=bpy.context.scene
S=bpy.data.scenes.new('BBB_Central_Aisle_Desk_Chair_Geometry')
with bpy.data.libraries.load('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Auction_Exchange_Furniture_Geometry.blend',link=False) as (src,dst):
    dst.objects=[n for n in src.objects if n.startswith(('04A_Single_Desk','04B_Single_Chair')) and '_Lamp_' not in n]
desk=bpy.data.collections.new('01_Curved_Desk');chair=bpy.data.collections.new('02_Chair')
S.collection.children.link(desk);S.collection.children.link(chair)
for ob in dst.objects:(desk if ob.name.startswith('04A') else chair).objects.link(ob)
bpy.context.window.scene=S
bpy.context.view_layer.update()
roots={c:next(o for o in c.objects if o.type=='EMPTY') for c in [desk,chair]}
clays=[]
for name,value in [('Structure',.48),('Trim',.65),('Upholstery',.32)]:
    mat=bpy.data.materials.new('Geometry_'+name);mat.diffuse_color=(value,value,value,1);clays.append(mat)
R=5.5
def bend(p):
    t=p.x/R
    return Vector(((R+p.y)*math.sin(t),-R+(R+p.y)*math.cos(t),p.z))
for c in [desk,chair]:
    root=roots[c];inv=root.matrix_world.inverted()
    for ob in list(c.objects):
        if ob.type!='MESH':continue
        local=inv@ob.matrix_world
        ob.data=ob.data.copy()
        for v in ob.data.vertices:v.co=local@v.co
        ob.matrix_parent_inverse=Matrix.Identity(4);ob.matrix_basis=Matrix.Identity(4)
        if c==desk:
            # Refine the existing desk geometry so rails, felt and diagonal braces curve smoothly.
            bm=bmesh.new();bm.from_mesh(ob.data)
            for v in bm.verts:v.co.x*=1.5
            for _ in range(5):
                edges=[e for e in bm.edges if abs(e.verts[0].co.x-e.verts[1].co.x)>.14]
                if not edges:break
                bmesh.ops.subdivide_edges(bm,edges=edges,cuts=1,use_grid_fill=True)
            for v in bm.verts:v.co=bend(v.co)
            bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(ob.data);bm.free()
        name=ob.name.lower()
        idx=2 if any(k in name for k in ['felt','cushion','pad']) else 1 if any(k in name for k in ['inlay','finial','frame','rail','foot']) else 0
        ob.data.materials.clear();ob.data.materials.append(clays[idx])
        ob.name=('Desk_' if c==desk else 'Chair_')+ob.name.split('_',3)[-1]
    root.location=(0,0,0) if c==desk else (2.65,-.1,0)
    root.name='Curved_Desk_ROOT' if c==desk else 'Chair_ROOT'
    root['front']='-Y';root['stage']='Geometry only; no final textures'

# Simple end-column caps from the approved component sheet.
def cap(name,x,y,z,dx,dy,dz):
    vs=[bend(Vector((x+sx*dx/2,y+sy*dy/2,z+sz*dz/2))) for sx,sy,sz in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    me=bpy.data.meshes.new(name);me.from_pydata(vs,[],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]);me.update()
    ob=bpy.data.objects.new(name,me);desk.objects.link(ob);ob.parent=roots[desk];me.materials.append(clays[1])
    bevel=ob.modifiers.new('Soft edges','BEVEL');bevel.width=.008;bevel.segments=3
for x in [-1.635,1.635]:
    cap('Desk_End_Cap',x,-.25,.81,.24,.23,.12)
    cap('Desk_Cap_Crown',x,-.25,.895,.16,.16,.05)

roots[desk]['design']='Three-seat curved half-row desk; open rear; X-braced front'
roots[desk]['curve_radius_m']=R
roots[chair]['design']='Reusable padded chair with four legs, rear frame and ball finials'
bpy.context.view_layer.update()
report={}
for c in [desk,chair]:
    meshes=[o for o in c.objects if o.type=='MESH'];pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    bad=[]
    for ob in meshes:
        bm=bmesh.new();bm.from_mesh(ob.data)
        if any(not e.is_manifold for e in bm.edges):bad.append(ob.name)
        bm.free()
    report[c.name]={'parts':len(meshes),'dimensions':[round(max(v[i] for v in pts)-min(v[i] for v in pts),3) for i in range(3)],'vertices':sum(len(o.data.vertices) for o in meshes),'non_manifold_parts':bad}
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        sp=area.spaces.active;sp.shading.type='SOLID';sp.shading.color_type='MATERIAL';sp.overlay.show_overlays=False
        rv=sp.region_3d;eye=Vector((5,-8,4.5));target=Vector((.5,0,.5));rv.view_rotation=(eye-target).to_track_quat('Z','Y');rv.view_location=target;rv.view_distance=6.5;rv.view_perspective='ORTHO'
OUT.with_suffix('.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
# Write only the new asset scene; the hall stays in its original file.
bpy.data.libraries.write(str(OUT),{S},fake_user=True)
print(json.dumps({'file':str(OUT),'geometry':report}))
