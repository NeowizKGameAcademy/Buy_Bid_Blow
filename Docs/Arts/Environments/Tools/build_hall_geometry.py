import bpy, math, bmesh, os, json
from mathutils import Vector
from math import sin, cos, pi

OUT = r'C:/course/unity/BuyBidBlow/Docs/Arts/Environments/Models'
os.makedirs(OUT, exist_ok=True)
scene = bpy.data.scenes.new('BBB_Auction_Exchange_Geometry')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0
groups = {}
for name in ['01_Floor','02_Mezzanine','03_Stairs','04_Auction_Seating','05_Auction_Stage','06_Exchange','07_Lounge','08_Walls','09_Columns','10_Roof_Panels','11_Roof_Ribs','12_Doors','13_Railings','14_Props']:
    c=bpy.data.collections.new(name); scene.collection.children.link(c); groups[name]=c
current=groups['01_Floor']

def mesh(name, verts, faces):
    m=bpy.data.meshes.new(name); m.from_pydata(verts, [], faces); m.update()
    bm=bmesh.new(); bm.from_mesh(m); bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(m); bm.free()
    uv=m.uv_layers.new(name='UVMap')
    for p in m.polygons:
        n=p.normal; drop=max(range(3),key=lambda i:abs(n[i])); axes=[i for i in range(3) if i!=drop]
        for li in p.loop_indices:
            v=m.vertices[m.loops[li].vertex_index].co; uv.data[li].uv=(v[axes[0]],v[axes[1]])
    o=bpy.data.objects.new(name,m); current.objects.link(o); return o

def box(name,loc,size,angle=0):
    x,y,z=[v/2 for v in size]
    vs=[(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    o=mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
    o.location=loc; o.rotation_euler.z=angle; return o

def beam(name,a,b,width,depth=None):
    a,b=Vector(a),Vector(b); o=box(name,(a+b)/2,(width,depth or width,(b-a).length)); o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler(); return o

def arc(name,r0,r1,a0,a1,z0,z1,n=32,center=(0,0)):
    vs=[]
    for z in [z0,z1]:
        for r in [r0,r1]:
            for i in range(n+1):
                a=a0+(a1-a0)*i/n; vs.append((center[0]+r*cos(a),center[1]+r*sin(a),z))
    k=n+1; fs=[]
    for i in range(n):
        fs += [(i,i+1,k+i+1,k+i),(2*k+i,3*k+i,3*k+i+1,2*k+i+1),(i,2*k+i,2*k+i+1,i+1),(k+i,k+i+1,3*k+i+1,3*k+i)]
    fs += [(0,k,3*k,2*k),(n,2*k+n,3*k+n,k+n)]
    return mesh(name,vs,fs)

def cylinder(name,loc,r,depth,n=16):
    vs=[(r*cos(2*pi*i/n),r*sin(2*pi*i/n),z) for z in [-depth/2,depth/2] for i in range(n)]
    fs=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    o=mesh(name,vs,fs);o.location=loc;return o

def railing(name,a,b):
    a,b=Vector(a),Vector(b)
    beam(name+'_Handrail',a+Vector((0,0,1.05)),b+Vector((0,0,1.05)),.12,.16)
    beam(name+'_LowerRail',a+Vector((0,0,.18)),b+Vector((0,0,.18)),.09)
    n=max(1,math.ceil((b-a).length/.8))
    for i in range(n+1):
        p=a.lerp(b,i/n);beam(name+'_Baluster',p,p+Vector((0,0,1.1)),.09)

# Base and semicircular mezzanine: north half only.
cylinder('Hall_Foundation',(0,0,-.22),14.3,.44,96)
cylinder('Floor_Finish',(0,0,-.015),14,.03,96)
arc('Floor_Center_Inlay',2.8,3.0,0,2*pi,.002,.018,64)
current=groups['02_Mezzanine']
arc('Mezzanine_Slab',0,13.65,0,pi,3.65,4,64)
box('Front_Fascia',(0,0,3.72),(27.3,.32,.55))
for x in [-9,-4.5,0,4.5,9]:
    box('Underfloor_Beam',(x,4.8,3.52),(.24,9.4,.26))
current=groups['09_Columns']
for x,y in [(-9,.4),(-4.5,.4),(4.5,.4),(9,.4),(-7,7),(0,9),(7,7)]:
    box('Mezzanine_Column',(x,y,1.8),(.38,.38,3.6));box('Column_Foot',(x,y,.13),(.62,.62,.26));box('Column_Capital',(x,y,3.42),(.7,.7,.25))

current=groups['03_Stairs']
for side in [-1,1]:
    x=side*10.8
    for i in range(24):
        h=4*(i+1)/24;box('Main_Stair_Tread',(x,-6.72+(i+.5)*.28,h/2),(2.2,.28,h))
    for dx in [-1.14,1.14]:railing('Stair_Rail',(x+dx,-6.72,0),(x+dx,0,4))
current=groups['13_Railings']
railing('Balcony_Front',(-9.55,-.03,4),(9.55,-.03,4))
for side in [-1,1]:
    railing('Balcony_End',(side*12.05,0,4),(side*13.5,0,4))

# Three curved seating terraces split by a central circulation aisle.
current=groups['04_Auction_Seating']
for row,r in enumerate([5.1,7.55,10.0]):
    level=4+row*.42
    for a0,a1 in [(20,84),(96,160)]:
        aa,bb=math.radians(a0),math.radians(a1)
        if row:arc('Seating_Terrace',r-.4,r+1.85,aa,bb,4,level,24)
        arc('Desk_Curved_Top',r-.1,r+.65,aa,bb,level+.72,level+.82,24)
        arc('Desk_Front_Panel',r-.1,r+.04,aa,bb,level+.08,level+.72,24)
        count=4+row*2
        for j in range(count):
            a=aa+(bb-aa)*(j+.5)/count; px=(r+1.15)*cos(a);py=(r+1.15)*sin(a);ang=a-pi/2
            def local(dx,dy,z):return (px+dx*cos(ang)-dy*sin(ang),py+dx*sin(ang)+dy*cos(ang),z)
            box('Chair_Seat',local(0,0,level+.45),(.62,.62,.12),ang)
            box('Chair_Cushion',local(0,0,level+.53),(.55,.53,.09),ang)
            box('Chair_Back',local(0,.27,level+.92),(.62,.1,.78),ang)
            for dx in [-.24,.24]:
                for dy in [-.23,.23]:box('Chair_Leg',local(dx,dy,level+.21),(.07,.07,.42),ang)
    if row:
        for s in range(3):box('Tier_Aisle_Step',(0,r-.4+(s+.5)*.32,4+(row-1)*.42+(s+1)*.14/2),(1.05,.32,(s+1)*.14))
current=groups['05_Auction_Stage']
box('Auctioneer_Platform',(0,1.65,4.1),(3.6,2.1,.2))
box('Lectern_Body',(0,1.65,4.73),(1.15,.75,1.06))
o=box('Lectern_WritingTop',(0,1.65,5.31),(1.3,.9,.12));o.rotation_euler.x=math.radians(-8)
box('Lectern_Front_Inset',(0,2.035,4.83),(.78,.035,.7))
box('Lot_Pedestal',(2.75,1.6,4.55),(.9,.9,1.1));box('Lot_Trim',(2.75,1.6,5.13),(1.05,1.05,.1));box('Lot_Cushion',(2.75,1.6,5.21),(.84,.84,.09))

# Exchange stations stand inside perimeter and face the open floor.
current=groups['06_Exchange']
for idx,deg in enumerate([211,250,326]):
    a=math.radians(deg);r=11.35;cx,cy=r*cos(a),r*sin(a);ang=a+pi/2
    def at(dx,dy,z):return (cx+dx*cos(ang)-dy*sin(ang),cy+dx*sin(ang)+dy*cos(ang),z)
    box('Exchange_Counter_'+str(idx),at(0,0,.5),(3.7,.85,1),ang)
    box('Counter_Worktop',at(0,0,1.05),(3.95,1.05,.12),ang)
    # local negative y points out to the wall, leaving staff standing gap.
    box('Display_Frame',at(0,-1.55,3.05),(4,.2,2.05),ang)
    box('Display_Screen',at(0,-1.425,3.05),(3.68,.04,1.73),ang)
    for dx in [-1.5,1.5]:box('Display_Support',at(dx,-1.55,1.52),(.13,.13,3.04),ang)
    for j in range(3):box('Counter_Ledger',at(-1.05+j*.25,0,1.17+j*.03),(.42,.52,.07),ang)

current=groups['07_Lounge']
for x in [-6,6]:
    box('Lounge_Sofa_Base',(x,6,.28),(3.4,1,.4));box('Sofa_Back',(x,6.48,.8),(3.4,.2,1.25))
    for dx in [-1.6,1.6]:box('Sofa_Arm',(x+dx,6,.64),(.2,1,.6))
    for j in [-1,0,1]:box('Sofa_Cushion',(x+j,6,.54),(.93,.83,.17))
    box('Lounge_TableTop',(x,4.3,.56),(2.3,1.05,.12))
    for dx in [-.9,.9]:
        for dy in [-.35,.35]:box('Lounge_TableLeg',(x+dx,4.3+dy,.25),(.1,.1,.5))
box('Preview_Display_Base',(0,8.5,.5),(3,.9,1))
box('Preview_Display_Top',(0,8.5,1.05),(3.15,1.05,.1))

# Faceted perimeter wall with an actual southern entrance opening.
current=groups['08_Walls']
for i in range(48):
    a0=2*pi*i/48;a1=2*pi*(i+1)/48;mid=(a0+a1)/2
    door=abs(mid-1.5*pi)<math.radians(9)
    arc('Wall_Panel',14,14.22,a0,a1,4.5 if door else 0,7,1)
    if not door:arc('Wall_Wainscot',13.91,14,a0,a1,.12,1.25,1)
current=groups['09_Columns']
for i in range(16):
    a=2*pi*i/16
    if i==12:continue
    x,y=13.82*cos(a),13.82*sin(a)
    box('Perimeter_Column',(x,y,3.5),(.34,.34,7),a)
    box('Perimeter_Base',(x,y,.22),(.62,.62,.44),a)

# Dome shell uses separate wedge panels; ribs follow exact roof profile.
def roofpoint(a,t,r_offset=0,z_offset=0):
    r=14*cos(t)+r_offset;return (r*cos(a),r*sin(a),7+6*sin(t)+z_offset)
current=groups['10_Roof_Panels']
for i in range(32):
    aa=2*pi*i/32;bb=2*pi*(i+1)/32
    for j in range(8):
        t0=j*(pi/2)/9;t1=(j+1)*(pi/2)/9
        vs=[roofpoint(a,t,0,z) for z in [0,.16] for a,t in [(aa,t0),(bb,t0),(bb,t1),(aa,t1)]]
        mesh('Dome_Panel',vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)])
cylinder('Dome_Crown',(0,0,12.97),14*cos(8*pi/18),.2,32)
current=groups['11_Roof_Ribs']
for i in range(16):
    a=2*pi*i/16
    for j in range(8):beam('Dome_Radial_Beam',roofpoint(a,j*pi/18,0,-.2),roofpoint(a,(j+1)*pi/18,0,-.2),.25,.34)
for j in [0,3,6,8]:
    t=j*pi/18;r=14*cos(t);z=7+6*sin(t)-.18
    arc('Dome_Ring_Beam',r-.12,r+.12,0,2*pi,z-.14,z+.14,64)

current=groups['12_Doors']
# Two leaves are separate meshes, origins at their hinges.
for side in [-1,1]:
    hinge=side*1.8
    o=box('Entrance_Door_Left' if side<0 else 'Entrance_Door_Right',(hinge,-14,0),(1,1,1))
    # replace helper unit cube coordinates with leaf geometry about hinge.
    for v in o.data.vertices:
        v.co.x=(v.co.x+.5)*1.78*(-side);v.co.y*=.18;v.co.z=(v.co.z+.5)*4.35
    o['hinge_axis']='local Z';o['open_angle_degrees']=side*85
    box('Door_Inset',(side*.9,-13.89,2.25),(1.45,.055,3.5))
    beam('Door_Handle',(side*.22,-13.65,1.55),(side*.22,-13.65,2.2),.07)
for x in [-1.97,1.97]:box('Door_Jamb',(x,-14,2.3),(.25,.42,4.6))
box('Door_Header',(0,-14,4.57),(4.18,.42,.24))

current=groups['14_Props']
for i in range(8):
    a=2*pi*i/8+pi/8;r=8;x,y=r*cos(a),r*sin(a)
    beam('Lantern_Hanger',(x,y,9),(x,y,11.4),.025)
    cylinder('Lantern_Core',(x,y,8.65),.28,.7,6)
    cylinder('Lantern_Top',(x,y,9.04),.4,.12,6);cylinder('Lantern_Base',(x,y,8.25),.34,.12,6)
    for k in range(6):
        b=2*pi*k/6;beam('Lantern_Frame',(x+.29*cos(b),y+.29*sin(b),8.3),(x+.29*cos(b),y+.29*sin(b),9),.045)
for x,y in [(-8,2),(8,2),(-4,8),(4,8),(-9,-8),(9,-8)]:
    cylinder('Planter',(x,y,.35),.4,.7,8)
    for k in range(7):
        a=2*pi*k/7;start=Vector((x,y,.65));tip=Vector((x+.65*cos(a),y+.65*sin(a),1.7+(k%3)*.15));mid=start.lerp(tip,.55);side=Vector((-.19*sin(a),.19*cos(a),0))
        mesh('Plant_Leaf',[start,mid+side,tip,mid-side,mid+Vector((0,0,.12))],[(0,1,4),(1,2,4),(2,3,4),(3,0,4)])

scene['design_notes']='28m diameter; north half mezzanine at 4m; twin 2.2m stairs; south entrance. All meshes unmaterialed. UVMap uses local planar projection at 1 unit per metre. Roof collections may be hidden for editing.'
scene['materials']='No materials assigned; named separate objects for later assignment.'
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.shading.type='SOLID'
        rv=area.spaces.active.region_3d
        eye=Vector((0,-10,2.1));target=Vector((0,3,4.7));rv.view_rotation=(target-eye).to_track_quat('-Z','Y');rv.view_distance=(target-eye).length;rv.view_location=target;rv.view_perspective='PERSP'
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'BBB_Auction_Exchange_Geometry.blend'))
print(json.dumps({'scene':scene.name,'objects':len(scene.objects),'mesh_objects':sum(o.type=='MESH' for o in scene.objects),'assigned_materials':sum(len(o.data.materials) for o in scene.objects if o.type=='MESH'),'file':bpy.data.filepath}))
