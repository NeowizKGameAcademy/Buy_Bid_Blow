"""Two curved auction rows, following the user's live blockout and outline sketch.

Run stages in the connected Blender; the original scene is preserved separately.
"""
import bpy
import bmesh
import math
import os
import random
from mathutils import Vector, Matrix
from mathutils.geometry import tessellate_polygon

BASE = r'C:\course\unity\BuyBidBlow\Docs\Arts\Props\Models'
PREFIX = 'ASR_'
M = {}
C = {}


def collection(label):
    name = PREFIX + label
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    C[label] = c
    return c


def mat(label, color, metallic=0, roughness=.45):
    m = bpy.data.materials.get(PREFIX + label) or bpy.data.materials.new(PREFIX + label)
    m.use_nodes = True
    node = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    node.inputs['Base Color'].default_value = (*color, 1)
    node.inputs['Metallic'].default_value = metallic
    node.inputs['Roughness'].default_value = roughness
    m.diffuse_color = (*color, 1)
    M[label] = m
    return m


def mesh(name, verts, faces, material, group, bevel=0):
    data = bpy.data.meshes.new(PREFIX + name)
    data.from_pydata(verts, [], faces)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(PREFIX + name, data)
    C[group].objects.link(obj)
    data.materials.append(M[material])
    if bevel:
        mod = obj.modifiers.new('Soft crafted edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        mod.limit_method = 'ANGLE'
    return obj


def slab(name, poly, bottom, top, material, group, bevel=.02):
    # Triangulation preserves concave stair cutouts in the plan.
    poly = [(float(p[0]), float(p[1])) for p in poly]
    n = len(poly)
    points = [Vector((x, y, 0)) for x, y in poly]
    lookup = {(round(v.x, 6), round(v.y, 6)): i for i, v in enumerate(points)}
    triangles = tessellate_polygon([points])
    faces = []
    for tri in triangles:
        idx = [v if isinstance(v, int) else lookup[(round(v.x, 6), round(v.y, 6))] for v in tri]
        faces.extend([tuple(reversed(idx)), tuple(i+n for i in idx)])
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    verts = [(x, y, bottom) for x, y in poly] + [(x, y, top) for x, y in poly]
    return mesh(name, verts, faces, material, group, bevel)


def box(name, loc, dims, material, group, angle=0, bevel=.025):
    x, y, z = [d/2 for d in dims]
    verts = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
             (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    obj = mesh(name, verts, faces, material, group, bevel)
    obj.location = loc
    obj.rotation_euler.z = angle
    return obj


def beam(name, a, b, width, depth, material, group, bevel=.015):
    a, b = Vector(a), Vector(b)
    o = box(name, (a+b)/2, (width, depth, (b-a).length), material, group, bevel=bevel)
    o.rotation_euler = (b-a).to_track_quat('Z', 'Y').to_euler()
    return o


def offset(path, distance):
    result = []
    for i, p in enumerate(path):
        a = Vector(path[max(0, i-1)])
        b = Vector(path[min(len(path)-1, i+1)])
        d = (b-a).normalized()
        result.append((p[0]-d.y*distance, p[1]+d.x*distance))
    return result


def ribbon(name, path, front, back, bottom, top, material, group, bevel=.018):
    a, b = offset(path, front), offset(path, back)
    n = len(a)
    verts = [(x,y,z) for z in (bottom,top) for line in (a,b) for x,y in line]
    faces = []
    for i in range(n-1):
        faces += [(i,i+1,n+i+1,n+i), (2*n+i,3*n+i,3*n+i+1,2*n+i+1),
                  (i,2*n+i,2*n+i+1,i+1),(n+i,n+i+1,3*n+i+1,3*n+i)]
    faces += [(0,n,3*n,2*n),(n-1,3*n-1,4*n-1,2*n-1)]
    return mesh(name, verts, faces, material, group, bevel)


def rowpath(width, y, n=64):
    return [(width*(2*i/n-1), y+1.84*(2*i/n-1)**2) for i in range(n+1)]


def clipped(poly, axis, bound, above):
    out = []
    for a, b in zip(poly, poly[1:]+poly[:1]):
        ia = a[axis] >= bound if above else a[axis] <= bound
        ib = b[axis] >= bound if above else b[axis] <= bound
        if ia: out.append(a)
        if ia != ib:
            t = (bound-a[axis])/(b[axis]-a[axis])
            out.append(tuple(a[j]+t*(b[j]-a[j]) for j in (0,1)))
    return out


def flooring(name, poly, z):
    # Subtle, staggered plank seams follow each shaped platform.
    rng = random.Random(24)
    ymin = min(p[1] for p in poly)
    ymax = max(p[1] for p in poly)
    xmin = min(p[0] for p in poly)
    xmax = max(p[0] for p in poly)
    row = 0
    y = ymin
    while y < ymax:
        band = clipped(clipped(poly,1,y+.008,True),1,y+.42-.008,False)
        x = xmin - (row%2)*1.2
        while x < xmax:
            p = clipped(clipped(band,0,x+.008,True),0,x+2.4-.008,False) if band else []
            if len(p)>2:
                area = abs(sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(p,p[1:]+p[:1])))/2
                if area > .006:
                    slab(name, p, z-.018, z, 'Plank'+str(rng.randrange(4)), '01_Floors', 0)
            x += 2.4
        y += .42
        row += 1


def post(name, xy, z, height=1.26, group='04_Rails'):
    x, y = xy
    box(name+'_Foot', (x,y,z+.08), (.51,.51,.16), 'DarkWood', group)
    box(name+'_Shaft', (x,y,z+height/2), (.32,.32,height), 'Wood', group)
    box(name+'_Collar', (x,y,z+height-.12), (.43,.43,.16), 'Honey', group)
    box(name+'_Cap', (x,y,z+height+.015), (.49,.49,.11), 'Brass', group)
    box(name+'_Crown', (x,y,z+height+.10), (.30,.30,.10), 'Brass', group)


def init():
    assert not any(c.name.startswith(PREFIX) for c in bpy.data.collections), 'Revision already exists'
    original = bpy.data.collections.new('00_ORIGINAL_Blockout_Hidden')
    bpy.context.scene.collection.children.link(original)
    for obj in list(bpy.context.scene.objects):
        original.objects.link(obj)
        for c in list(obj.users_collection):
            if c != original: c.objects.unlink(obj)
    original.hide_render = True
    original.hide_viewport = True
    for label in ['01_Floors','02_Curved_Desks','03_Stairs','04_Rails','05_Rear_Wall','06_Chairs','07_Lamps','08_Presentation']:
        collection(label)
    mat('Wood',(.30,.125,.042),roughness=.38)
    mat('Honey',(.46,.235,.078),roughness=.34)
    mat('DarkWood',(.115,.048,.020),roughness=.52)
    mat('Panel',(.235,.087,.030),roughness=.47)
    mat('Brass',(.66,.365,.087),metallic=.72,roughness=.27)
    mat('Red',(.20,.007,.016),roughness=.76)
    mat('Carpet',(.15,.006,.011),roughness=.92)
    mat('Cream',(.86,.64,.28),roughness=.4)
    mat('Backdrop',(.63,.58,.49),roughness=.85)
    for i,c in enumerate([(.38,.20,.084),(.43,.23,.103),(.34,.17,.067),(.47,.26,.12)]):
        mat('Plank'+str(i),c,roughness=.5)
    red = next(n for n in M['Red'].node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    red.inputs['Sheen Weight'].default_value = .035
    red.inputs['Sheen Tint'].default_value = (.32,.012,.023,1)
    red.inputs['Specular IOR Level'].default_value = .28
    glow = next(n for n in M['Cream'].node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    glow.inputs['Emission Color'].default_value = (1,.58,.14,1)
    glow.inputs['Emission Strength'].default_value = .35


def architecture(floors_only=False):
    front = rowpath(7.514,-1)
    rear = rowpath(5.766,6.171)
    wall = [(8.4*(2*i/80-1),10.7+3.25*(1-(2*i/80-1)**2)) for i in range(81)]
    basepoly = front + [(9.45,1.2),(8.80,4.3),(8.15,7.9),(7.25,10.6),(8.4,10.7)] + list(reversed(wall[:-1])) + [(-7.25,10.6),(-8.15,7.9),(-8.80,4.3),(-9.45,1.2)]
    slab('Shaped_Foundation',basepoly,-.23,0,'DarkWood','01_Floors',.045)
    lowpoly = front + [(7.665,1.091),(6.965,3.891),(8.635,4.309),(8.015,7.909)] + list(reversed(rear)) + [(-8.015,7.909),(-8.635,4.309),(-6.965,3.891),(-7.665,1.091)]
    slab('Tier_01_Curved_Floor',lowpoly,0,1.025,'DarkWood','01_Floors',.025)
    flooring('Tier_01_Plank',lowpoly,1.05)
    highpoly = rear + [(6.359,7.395),(5.459,10.295),(7.101,10.805),(8.4,10.7)] + list(reversed(wall[:-1])) + [(-7.101,10.805),(-5.459,10.295),(-6.359,7.395)]
    slab('Tier_02_Curved_Floor',highpoly,0,2.075,'DarkWood','01_Floors',.025)
    flooring('Tier_02_Plank',highpoly,2.10)
    if floors_only:
        return
    for side in [-1,1]:
        stair('Lower_'+str(side),(side*8.50,1.30),(side*7.80,4.10),0,1.05)
        stair('Upper_'+str(side),(side*7.18,7.65),(side*6.28,10.55),1.05,2.10)
        # Rail heights follow the accessible route and leave its inner edge open.
        route=[(side*9.40,1.53,0),(side*8.70,4.33,1.05),(side*8.08,7.93,1.05),(side*7.18,10.83,2.10),(side*8.4,10.7,2.10)]
        for i,p in enumerate(route):
            post('Side_'+str(side)+'_Post_'+str(i),p[:2],p[2])
        for i,(a,b) in enumerate(zip(route,route[1:])):
            beam('Side_Rail_Cap', (a[0],a[1],a[2]+1.12),(b[0],b[1],b[2]+1.12), .20,.24,'Honey','04_Rails')
            beam('Side_Rail_Base',(a[0],a[1],a[2]+.20),(b[0],b[1],b[2]+.20), .17,.20,'Wood','04_Rails')
            beam('Side_Rail_Brass',(a[0],a[1],a[2]+1.23),(b[0],b[1],b[2]+1.23), .06,.13,'Brass','04_Rails',.01)
            for t in [.25,.5,.75]:
                p=Vector(a).lerp(Vector(b),t)
                box('Side_Baluster',(p.x,p.y,p.z+.65),(.12,.12,.92),'Wood','04_Rails')
            side_panel(a,b)
    desk('Front',front,1.05,10)
    desk('Rear',rear,2.1,8)
    # Rear wall bows away from the auction item, as in the drawn outline.
    ribbon('Rear_Enclosing_Wall',wall,0,.40,2.10,4.65,'Panel','05_Rear_Wall',.035)
    for z,w,h,ma in [(2.20,.48,.18,'DarkWood'),(2.43,.45,.10,'Honey'),(3.35,.46,.13,'Honey'),(4.55,.48,.17,'Honey'),(4.72,.57,.14,'Wood'),(4.80,.60,.045,'Brass')]:
        ribbon('Rear_Wall_Moulding',wall,-.05,w, z-h/2,z+h/2,ma,'05_Rear_Wall')
    for i in range(0,81,8):
        p=wall[i]
        box('Rear_Wall_Pilaster',(p[0],p[1]-.02,3.43),(.20,.30,2.45),'Wood','05_Rear_Wall',angle=math.atan2(-6.5*p[0]/8.4**2,1))
    bpy.context.scene['Design Notes']='Two curved rows. Front and rear desk curves preserve original blockout spans and positions. Six steps per flight; side access; curved rear boundary. Original blockout is hidden and separately backed up.'


def stair(name, a, b, z0, z1):
    a,b=Vector(a),Vector(b)
    direction=(b-a).normalized()
    length=(b-a).length
    angle=math.atan2(-direction.x,direction.y)
    for i in range(6):
        p=a.lerp(b,(i+.5)/6)
        top=z0+(z1-z0)*(i+1)/6
        box(name+'_Step_'+str(i+1),(p.x,p.y,(z0+top)/2),(1.72,length/6+.008,top-z0),'Wood','03_Stairs',angle,.018)
        box(name+'_Carpet_'+str(i+1),(p.x,p.y,top+.012),(1.22,length/6-.025,.024),'Carpet','03_Stairs',angle,.008)
        edge=p-direction*(length/12-.045)
        box(name+'_Nosing_'+str(i+1),(edge.x,edge.y,top+.025),(1.58,.045,.025),'Brass','03_Stairs',angle,.008)


def side_panel(a,b):
    a,b=Vector(a),Vector(b)
    direction=b-a
    n=Vector((-direction.y,direction.x,0)).normalized()
    verts=[tuple(p+n*d+Vector((0,0,h))) for d in [-.055,.055] for p,h in [(a,.27),(b,.27),(b,1.02),(a,1.02)]]
    mesh('Side_Wood_Infill',verts,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],'Panel','04_Rails',.012)
    for sign in [-1,1]:
        p=a.lerp(b,.07)+n*.075*sign
        q=a.lerp(b,.93)+n*.075*sign
        beam('Side_X_Brace',p+Vector((0,0,.30)),q+Vector((0,0,.99)),.085,.085,'Wood','04_Rails')
        beam('Side_X_Brace',p+Vector((0,0,.99)),q+Vector((0,0,.30)),.085,.085,'Wood','04_Rails')


def desk(name,path,level,panels):
    g='02_Curved_Desks'
    ribbon(name+'_Desk_Front',path,.025,.24,level+.08,level+1.00,'Panel',g)
    ribbon(name+'_Desk_Plinth',path,-.06,.34,level+.02,level+.16,'Wood',g)
    ribbon(name+'_Desk_Lower_Rail',path,-.045,.30,level+.18,level+.28,'Honey',g)
    ribbon(name+'_Desk_Upper_Rail',path,-.045,.30,level+.87,level+1.01,'Honey',g)
    ribbon(name+'_Desktop',path,-.12,1.02,level+1.00,level+1.16,'Honey',g,.035)
    ribbon(name+'_Red_Desktop_Inlay',path,.05,.87,level+1.16,level+1.18,'Red',g,.015)
    ribbon(name+'_Front_Brass_Edge',path,-.13,-.09,level+1.10,level+1.16,'Brass',g,.008)
    ribbon(name+'_Back_Brass_Edge',path,.98,1.02,level+1.10,level+1.16,'Brass',g,.008)
    ribbon(name+'_Tier_Fascia',path,-.035,.05,level-.88,level-.06,'Panel',g)
    ribbon(name+'_Tier_Base_Trim',path,-.08,.14,level-.95,level-.82,'Honey',g)
    for i in range(panels+1):
        idx=round(i*(len(path)-1)/panels)
        p=path[idx]
        tangent=Vector(path[min(idx+1,len(path)-1)])-Vector(path[max(idx-1,0)])
        angle=math.atan2(tangent.y,tangent.x)
        box(name+'_Panel_Stile',(p[0],p[1]-.015,level+.56),(.14,.12,.92),'Wood',g,angle)
        box(name+'_Tier_Stile',(p[0],p[1]-.025,level-.47),(.11,.10,.80),'Wood',g,angle)
        if i<panels:
            nxt=path[round((i+1)*(len(path)-1)/panels)]
            a=Vector(p).lerp(Vector(nxt),.10)
            b=Vector(p).lerp(Vector(nxt),.90)
            beam(name+'_X_Brace',(a.x,a.y-.035,level+.31),(b.x,b.y-.035,level+.85),.07,.07,'Wood',g,.01)
            beam(name+'_X_Brace',(a.x,a.y-.035,level+.85),(b.x,b.y-.035,level+.31),.07,.07,'Wood',g,.01)
    for p in (path[0],path[-1]):
        post(name+'_Desk_End',p,level,1.27,g)


def chairs():
    source=bpy.data.collections['REFERENCE_Existing_Chair']
    # The existing chair's mesh coordinates are centered locally and face -Y.
    for row,w,y,level,count in [('Front',7.514,-1,1.05,9),('Rear',5.766,6.171,2.1,7)]:
        for seat in range(count):
            x=(-.86+1.72*seat/(count-1))*w
            curve_y=y+1.84*(x/w)**2
            angle=math.atan2(3.68*x/w**2,1)
            normal=Vector((-math.sin(angle),math.cos(angle),0))
            origin=Vector((x,curve_y,level))+normal*1.65
            facing=math.atan2(-origin.x,origin.y+7)
            transform=Matrix.Translation(origin)@Matrix.Rotation(facing,4,'Z')@Matrix.Scale(1.6,4)
            root=bpy.data.objects.new(PREFIX+row+'_Seat_'+str(seat+1).zfill(2),None)
            C['06_Chairs'].objects.link(root)
            root.matrix_world=transform
            for src in source.all_objects:
                if src.type!='MESH': continue
                obj=src.copy()
                obj.data=src.data.copy()
                obj.parent=root
                obj.matrix_parent_inverse=Matrix.Identity(4)
                obj.matrix_basis=Matrix.Identity(4)
                obj.name=PREFIX+row+'_Seat_'+str(seat+1)+'_'+src.name
                C['06_Chairs'].objects.link(obj)
                label='Red' if ('Pad' in src.name or 'Cushion' in src.name) else ('Brass' if 'Finial' in src.name else 'Wood')
                obj.data.materials.clear()
                obj.data.materials.append(M[label])


def turned(name,x,y,z,rings,material,group='07_Lamps',sides=16):
    verts=[]
    for radius,height in rings:
        verts.extend([(x+radius*math.cos(2*math.pi*i/sides),y+radius*math.sin(2*math.pi*i/sides),z+height) for i in range(sides)])
    faces=[tuple(reversed(range(sides)))]
    for j in range(len(rings)-1):
        faces.extend([(j*sides+i,j*sides+(i+1)%sides,(j+1)*sides+(i+1)%sides,(j+1)*sides+i) for i in range(sides)])
    faces.append(tuple(range((len(rings)-1)*sides,len(rings)*sides)))
    return mesh(name,verts,faces,material,group,.008)


def lamps():
    for label,w,y,level in [('Front',7.514,-1,1.05),('Rear',5.766,6.171,2.1)]:
        for fraction in [-.68,0,.68]:
            x=w*fraction
            yy=y+1.84*fraction*fraction+.49
            z=level+1.18
            turned(label+'_Lamp_Base',x,yy,z,[(.19,0),(.20,.035),(.15,.07),(.075,.10),(.055,.52),(.085,.56)],'Brass')
            turned(label+'_Lamp_Shade',x,yy,z,[(.28,.45),(.29,.49),(.17,.78),(.14,.80)],'Cream')
            turned(label+'_Lamp_Rim',x,yy,z,[(.292,.45),(.292,.48)],'Brass')


def presentation():
    g=C['08_Presentation']
    ground=box('Studio_Ground',(0,5,-.36),(200,200,.20),'Backdrop','08_Presentation',bevel=0)
    ground.hide_set(True)
    camera=bpy.data.cameras.new(PREFIX+'Camera')
    camera.type='ORTHO'
    camera.ortho_scale=25.8
    obj=bpy.data.objects.new(PREFIX+'Camera',camera)
    g.objects.link(obj)
    obj.location=(20,-25,24)
    target=Vector((0,6,1.7))
    obj.rotation_euler=(target-obj.location).to_track_quat('-Z','Y').to_euler()
    bpy.context.scene.camera=obj
    for name,loc,power,size in [('Key',(-8,-5,18),2700,10),('Fill',(12,3,14),1900,9),('Rear',(-1,17,15),2300,8)]:
        data=bpy.data.lights.new(PREFIX+name,'AREA')
        data.energy=power*1.4
        data.shape=next(i.identifier for i in data.bl_rna.properties['shape'].enum_items if i.identifier=='DISK')
        data.size=size
        light=bpy.data.objects.new(PREFIX+name,data)
        g.objects.link(light)
        light.location=loc
        light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
    scene=bpy.context.scene
    background=next(n for n in scene.world.node_tree.nodes if n.type=='BACKGROUND')
    background.inputs['Color'].default_value=(.32,.32,.32,1)
    background.inputs['Strength'].default_value=.35
    scene.view_settings.exposure=-.10
    scene.eevee.taa_render_samples=64
    scene.render.resolution_x=1440
    scene.render.resolution_y=1120
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=os.path.join(BASE,'BBB_Auction_Seating_Sketch_Revision_Perspective.png')
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                space=area.spaces.active
                space.shading.type='SOLID'
                space.shading.color_type='MATERIAL'
                space.shading.show_shadows=True
                space.shading.show_cavity=True
                space.overlay.show_floor=False
                space.overlay.show_axis_x=False
                space.overlay.show_axis_y=False
                space.overlay.show_extras=False
                rv=space.region_3d
                rv.view_rotation=obj.rotation_euler.to_quaternion()
                rv.view_location=target
                rv.view_distance=29
                rv.view_perspective=next(i.identifier for i in rv.bl_rna.properties['view_perspective'].enum_items if i.identifier=='ORTHO')
    for o in bpy.context.selected_objects: o.select_set(False)
    bpy.context.view_layer.objects.active=None
    bpy.context.view_layer.update()


def save():
    path=os.path.join(BASE,'BBB_Auction_Seating_Sketch_Revision.blend')
    bpy.ops.wm.save_as_mainfile(filepath=path)
    print('Saved:',path)

