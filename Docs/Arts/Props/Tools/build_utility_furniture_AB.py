"""Ten game props from the supplied A/B concept sheets. Run inside Blender."""
import bpy,bmesh,math,json,os,random
from pathlib import Path
from mathutils import Vector,Matrix
from math import sin,cos,pi
BASE=Path(r'C:/course/unity/BuyBidBlow')
OUT=BASE/'Assets/Project/Content/Props/FurnitureUtilityPack'
DOC=BASE/'Docs/Arts/Props/Models/FurnitureUtilityPack'
(OUT/'Models').mkdir(parents=True,exist_ok=True);DOC.mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes.new('Furniture_Utility_AB_GameReady');bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'
atlas=bpy.data.materials['Lounge_BK_Atlas'];glass=bpy.data.materials['Lounge_BK_Glass']
regions={'wood':(8,8,504,1016),'brass':(520,8,760,248),'velvet':(776,8,1016,248),'charcoal':(520,264,760,504),'ceramic':(776,264,1016,504),'cream':(776,776,1016,888),'dark':(520,904,760,1016)}
accent_regions={**{f'book{i}':(i*64+2,2,i*64+62,254) for i in range(6)},'leaf':(386,2,510,254),'marble':(2,258,254,510),'leather':(258,258,510,510)}
def texture_material(name,file,roughness=.65):
    mat=bpy.data.materials.new(name);mat.use_nodes=True
    node=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');node.inputs['Roughness'].default_value=roughness
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(OUT/'Textures'/file),check_existing=True);tex.image.pack()
    mat.node_tree.links.new(tex.outputs['Color'],node.inputs['Base Color']);return mat
accents=texture_material('Furniture_Accents','Furniture_Accents_BaseColor.png')
signs={k:texture_material('InformationBoard_'+k,'InformationBoard_'+k+'_Text.png',.8) for k in ['A','B']}
parts=[];roots=[];root=None;role='Body';colliders=[]
def uvrect(rect,u,v,size):
    x0,y0,x1,y1=rect;return ((x0+u*(x1-x0))/size,1-(y1-v*(y1-y0))/size)
def mesh(name,verts,faces,region='wood',smooth=False,uvs=None,material=None):
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update()
    ob=bpy.data.objects.new(name,me);scene.collection.objects.link(ob);ob.parent=root;ob['role']=role
    mat=material or (accents if region in accent_regions else atlas);me.materials.append(mat)
    uv=me.uv_layers.new(name='UVMap')
    mins=[min(v.co[i] for v in me.vertices) for i in range(3)];maxs=[max(v.co[i] for v in me.vertices) for i in range(3)]
    for p in me.polygons:
        p.use_smooth=smooth;axis=max(range(3),key=lambda i:abs(p.normal[i]));axes=[i for i in range(3) if i!=axis]
        for li in p.loop_indices:
            idx=me.loops[li].vertex_index;co=me.vertices[idx].co
            u,v=uvs[idx] if uvs else tuple(.06+.88*(co[i]-mins[i])/max(.0001,maxs[i]-mins[i]) for i in axes)
            uv.data[li].uv=(u,v) if material else uvrect(accent_regions[region] if region in accent_regions else regions[region],u,v,512 if region in accent_regions else 1024)
    parts.append(ob);return ob
def active(o):
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
def bevel(o,w):
    if w:
        active(o);m=o.modifiers.new('Small edge chamfer','BEVEL');m.width=w;m.segments=1;m.limit_method='ANGLE';bpy.ops.object.modifier_apply(modifier=m.name)
    return o
def box(name,c,size,region='wood',bev=0,rot=None):
    vs=[Vector((x*size[0]/2,y*size[1]/2,z*size[2]/2)) for z in [-1,1] for y in [-1,1] for x in [-1,1]]
    if rot:vs=[rot@v for v in vs]
    return bevel(mesh(name,[Vector(c)+v for v in vs],[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)],region),bev)
def beam(name,a,b,w,d,region='wood',bev=0):
    a,b=Vector(a),Vector(b);return box(name,(a+b)/2,(w,d,(b-a).length),region,bev,Vector((0,0,1)).rotation_difference(b-a).to_matrix())
def lathe(name,profile,c=(0,0,0),region='brass',n=16,rot=None):
    vs=[Vector((r*cos(i*2*pi/n),r*sin(i*2*pi/n),z)) for r,z in profile for i in range(n)]
    if rot:vs=[rot@v for v in vs]
    fs=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(profile)-1) for i in range(n)]
    fs.extend([tuple(reversed(range(n))),tuple((len(profile)-1)*n+i for i in range(n))])
    return mesh(name,[Vector(c)+v for v in vs],fs,region,True)
def tube(name,points,r,region='brass',n=8):
    pts=[Vector(p) for p in points];vs=[]
    for j,p in enumerate(pts):
        q=Vector((0,0,1)).rotation_difference(pts[min(j+1,len(pts)-1)]-pts[max(j-1,0)])
        vs.extend(p+q@Vector((r*cos(i*2*pi/n),r*sin(i*2*pi/n),0)) for i in range(n))
    fs=[tuple(reversed(range(n))),tuple((len(pts)-1)*n+i for i in range(n))]
    fs += [(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(pts)-1) for i in range(n)]
    return mesh(name,vs,fs,region,True)
def ring(name,c,r,t=.006,region='brass',rot=None,n=20):
    pts=[]
    for i in range(n+1):
        p=Vector((r*cos(i*2*pi/n),r*sin(i*2*pi/n),0));pts.append(Vector(c)+(rot@p if rot else p))
    return tube(name,pts,t,region,6)
def rounded(name,c,w,d,h,r=.06,region='wood',n=4):
    outline=[]
    for x,y,a in [(w/2-r,d/2-r,0),(-w/2+r,d/2-r,90),(-w/2+r,-d/2+r,180),(w/2-r,-d/2+r,270)]:
        for i in range(n+1):
            theta=math.radians(a+i*90/n);outline.append((x+r*cos(theta),y+r*sin(theta)))
    N=len(outline);vs=[(c[0]+x,c[1]+y,c[2]+z) for z in [-h/2,h/2] for x,y in outline]
    fs=[tuple(reversed(range(N))),tuple(N+i for i in range(N))]+[(i,(i+1)%N,(i+1)%N+N,i+N) for i in range(N)]
    return mesh(name,vs,fs,region)
def panel(name,c,w,h,region=None,material=None,rot=None):
    vs=[Vector((-w/2,0,-h/2)),Vector((w/2,0,-h/2)),Vector((w/2,0,h/2)),Vector((-w/2,0,h/2))]
    if rot:vs=[rot@v for v in vs]
    ob=mesh(name,[Vector(c)+v for v in vs],[(0,1,2,3)],region or 'cream',False,[(0,0),(1,0),(1,1),(0,1)],material);ob['open_face']=True;return ob
def col(c,size):colliders.append({'center':[c[0],c[2],c[1]],'size':[size[0],size[2],size[1]]})
def start(name):
    global root,parts,role,colliders
    root=bpy.data.objects.new(name,None);scene.collection.objects.link(root);roots.append(root);parts=[];role='Body';colliders=[]
def finish():
    # Join each material/role separately; signage stays a four-vertex replaceable face.
    groups={}
    for o in parts:groups.setdefault((o['role'],o.data.materials[0].name),[]).append(o)
    for (tag,mname),objs in groups.items():
        bpy.ops.object.select_all(action='DESELECT')
        for o in objs:o.select_set(True)
        bpy.context.view_layer.objects.active=objs[0];bpy.ops.object.join();o=bpy.context.object
        suffix='SignFace' if tag=='SignFace' else 'Glass' if mname==glass.name else tag+'_'+('Accents' if mname==accents.name else 'PBR')
        o.name=root.name+'_'+suffix+('_LOD0' if tag!='SignFace' and mname!=glass.name else '')
        # Preserve intentionally oriented open faces. Creation helpers already wind closed shapes correctly.
        bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.dissolve_degenerate(bm,dist=1e-7,edges=list(bm.edges));bm.to_mesh(o.data);bm.free();o.data.update()
        active(o);scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
        if tag=='SignFace' or mname==glass.name:continue
        low=o.copy();low.data=o.data.copy();low.name=o.name.replace('LOD0','LOD1');scene.collection.objects.link(low);low.parent=root;active(low)
        dec=low.modifiers.new('Distance LOD','DECIMATE');dec.decimate_type='COLLAPSE';dec.ratio=.5;dec.use_collapse_triangulate=True;bpy.ops.object.modifier_apply(modifier=dec.name)
        low.hide_set(True);low.hide_render=True
    root['colliders']=json.dumps(colliders)

def framed_panel(c,w,h,wood=True,rot=None):
    rot=rot or Matrix.Identity(3)
    def at(v):return Vector(c)+rot@Vector(v)
    box('Panel backing',c,(w,.035,h),'wood' if wood else 'brass',.004,rot)
    for x in [-w/2,w/2]:box('Raised frame',at((x,-.024,0)),(.032,.024,h+.032),'wood' if wood else 'brass',.003,rot)
    for z in [-h/2,h/2]:box('Raised frame',at((0,-.024,z)),(w+.032,.024,.032),'wood' if wood else 'brass',.003,rot)
    for x in [-w/2+.025,w/2-.025]:box('Inner brass line',at((x,-.04,0)),(.006,.008,h-.038),'brass',0,rot)
    for z in [-h/2+.025,h/2-.025]:box('Inner brass line',at((0,-.04,z)),(w-.044,.008,.006),'brass',0,rot)

start('InformationBoard_A')
for x in [-.395,.395]:
    box('Wide stable foot',(x,0,.045),(.19,.43,.09),'wood',.012)
    box('Foot brass rim',(x,0,.02),(.194,.434,.015),'brass',.003)
    box('Stepped base',(x,0,.11),(.14,.26,.055),'wood',.008)
    box('Upright',(x,0,.86),(.075,.075,1.45),'wood',.003)
    for z in [.18,1.44,1.57]:box('Post collar',(x,0,z),(.1,.1,.025),'brass',.003)
    lathe('Finial',[(.028,0),(.032,.012),(.018,.024),(.032,.055),(.021,.086),(.001,.12)],(x,0,1.59),'brass',12)
    col((x,0,.86),(.10,.12,1.71))
    col((x,0,.055),(.20,.44,.11))
box('Base crossbar',(0,0,.19),(.75,.065,.065),'wood',.004)
framed_panel((0,0,.99),.65,1.08)
role='SignFace';panel('Information artwork',(0,-.043,.99),.59,1.02,material=signs['A']);role='Body'
col((0,0,.99),(.70,.10,1.13));finish()

start('InformationBoard_B')
lathe('Weighted brass base',[(.29,0),(.3,.02),(.3,.045),(.272,.065),(.265,.082),(.24,.09)],n=24)
lathe('Marble inset',[(.235,.086),(.235,.096)],region='marble',n=24)
lathe('Turned brass stem',[(.11,.096),(.11,.12),(.08,.14),(.075,.17),(.046,.20),(.027,.25),(.027,.83),(.052,.85),(.06,.875),(.04,.9),(.035,1.04)],n=16)
rot=Matrix.Rotation(math.radians(-40),3,'X');center=Vector((0,0,1.08))
framed_panel(center,.64,.43,False,rot)
role='SignFace';panel('Information artwork',center+rot@Vector((0,-.043,0)),.58,.37,material=signs['B'],rot=rot);role='Body'
for x in [-.345,.345]:tube('Tilt joint',[(x-.014,0,1.08),(x+.014,0,1.08)],.036,'brass',12)
col((0,0,.045),(.60,.60,.09));col((0,0,.55),(.12,.12,1.02));col((0,0,1.08),(.72,.34,.4));finish()

def wheel(x,y):
    # Twelve-sided tires and simple forks, baked into the static furniture mesh.
    rot=Matrix.Rotation(pi/2,3,'X')
    lathe('Caster tire',[(.044,-.023),(.052,-.015),(.052,.015),(.044,.023)],(x,y,.052),'charcoal',12,rot)
    tube('Brass axle',[(x,y-.03,.052),(x,y+.03,.052)],.019,'brass',10)
    for dy in [-.029,.029]:beam('Caster fork',(x,y+dy,.053),(x+.012,y+dy,.12),.016,.009,'brass')
    tube('Caster spindle',[(x+.012,y,.11),(x+.012,y,.145)],.016,'brass',8)
def tray(w,d,z,rounded_style=False):
    if rounded_style:
        rounded('Walnut tray',(0,0,z),w,d,.038,.06)
        rounded('Tray brass edge',(0,0,z-.014),w+.006,d+.006,.008,.06,'brass')
    else:box('Walnut tray',(0,0,z),(w,d,.04),'wood',.005)
    for y in [-d/2+.012,d/2-.012]:box('Tray lip',(0,y,z+.025),(w-.02,.02,.045),'wood',.003)
    for x in [-w/2+.012,w/2-.012]:box('Tray lip',(x,0,z+.025),(.02,d-.02,.045),'wood',.003)
    for x in [-w/2+.045,w/2-.045]:
        for y in [-d/2+.045,d/2-.045]:tube('Guard support',[(x,y,z+.04),(x,y,z+.095)],.006,'brass',6)
    pts=[(-w/2+.045,-d/2+.045,z+.095),(w/2-.045,-d/2+.045,z+.095),(w/2-.045,d/2-.045,z+.095),(-w/2+.045,d/2-.045,z+.095),(-w/2+.045,-d/2+.045,z+.095)]
    tube('Brass tray guard',pts,.005,'brass',6)
    col((0,0,z),(w,d,.07))
for variant in ['A','B']:
    start('ServiceTrolley_'+variant);w,d=(.78,.46) if variant=='A' else (.62,.46)
    levels=[.20,.73] if variant=='A' else [.20,.48,.76]
    for z in levels:tray(w,d,z,variant=='A')
    for x in [-w/2+.04,w/2-.04]:
        for y in [-d/2+.04,d/2-.04]:
            wheel(x,y)
            if variant=='A':tube('Brass pillar',[(x,y,.135),(x,y,.82)],.013,'brass',10)
            else:box('Walnut pillar',(x,y,.485),(.031,.031,.7),'wood',.003)
            for z in [.16]+[z+.01 for z in levels]:lathe('Pillar collar',[(.021,0),(.021,.022)],(x,y,z),'brass',8)
            lathe('Guard finial',[(.014,0),(.018,.01),(.011,.027)],(x,y,levels[-1]+.09),'brass',8)
    if variant=='A':
        for y in [-.17,.17]:tube('Curved handle support',[(-.35,y,.74),(-.39,y,.88),(-.46,y,.96),(-.51,y,.98)],.013,'brass',8)
        tube('Walnut hand grip',[(-.51,-.17,.98),(-.51,.17,.98)],.02,'wood',12)
    else:
        for y in [-.16,.16]:tube('Push handle bracket',[(.3,y,.73),(.41,y,.73)],.012,'brass',8)
        tube('Side push handle',[(.41,-.19,.73),(.41,.19,.73)],.017,'brass',10)
    col((0,0,.43),(w,d,.8));finish()

def tapered_leg(x,y,height):
    vs=[(x+sx*w/2,y+sy*w/2,z) for z,w in [(0,.037),(height,.065)] for sy in [-1,1] for sx in [-1,1]]
    mesh('Tapered walnut leg',vs,[(0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)])
    box('Brass foot',(x,y,.025),(.041,.041,.05),'brass',.002)
    col((x,y,height/2),(.07,.07,height))
start('Table_A')
rounded('Rounded brass rim',(0,0,.435),1.14,.66,.042,.07,'brass')
rounded('Walnut tabletop',(0,0,.442),1.13,.65,.04,.065)
for x in [-.51,.51]:
    for y in [-.26,.26]:tapered_leg(x,y,.42)
for y in [-.275,.275]:box('Apron',(0,y,.387),(1.04,.035,.07),'wood',.005)
for x in [-.525,.525]:box('Apron',(x,0,.387),(.035,.55,.07),'wood',.005)
rounded('Low brass shelf',(0,0,.12),1.06,.58,.025,.04,'brass')
col((0,0,.437),(1.14,.66,.06));col((0,0,.12),(1.06,.58,.025));finish()
start('Table_B')
box('Writing desk top',(0,0,.755),(1.35,.66,.04),'wood',.007)
box('Green leather inset',(0,0,.778),(1.23,.54,.006),'leather',.003)
for y in [-.276,.276]:box('Desktop brass inlay',(0,y,.779),(1.25,.005,.004),'brass')
for x in [-.626,.626]:box('Desktop brass inlay',(x,0,.779),(.005,.556,.004),'brass')
for x in [-.615,.615]:
    for y in [-.274,.274]:
        tapered_leg(x,y,.66);box('Leg capital',(x,y,.672),(.075,.075,.14),'wood',.004)
        box('Capital collar',(x,y,.603),(.082,.082,.014),'brass',.002)
for y in [-.28,.28]:box('Desk apron',(0,y,.68),(1.24,.036,.14),'wood',.003)
for x in [-.63,.63]:box('Side apron',(x,0,.68),(.035,.58,.14),'wood',.003)
for x in [-.302,.302]:
    box('Drawer front',(x,-.308,.678),(.58,.024,.116),'wood',.004)
    for z in [.63,.724]:box('Drawer inlay',(x,-.323,z),(.55,.003,.004),'brass')
    for xx in [x-.086,x+.086]:box('Handle mounting plate',(xx,-.329,.677),(.032,.008,.027),'brass',.002)
    tube('Drawer handle',[(x-.086,-.333,.677),(x-.086,-.355,.677),(x+.086,-.355,.677),(x+.086,-.333,.677)],.006,'brass',8)
col((0,0,.706),(1.35,.66,.15));finish()

# Small contents use texture spines and existing reduced sculpture geometry.
def book(x,y,z,h=.24,w=.04,index=0,lying=False):
    rot=Matrix.Rotation(pi/2,3,'Y') if lying else None
    # For lying books dimensions are supplied directly, keeping stable shelf contact.
    if lying:
        box('Stacked book',(x,y,z+w/2),(h,.16,w),'book'+str(index%6),.001)
    else:
        box('Book volume',(x,y,z+h/2),(w,.16,h),'book'+str(index%6),.001)
        panel('Printed book spine',(x,y-.081,z+h/2),w*.92,h*.94,'book'+str(index%6))
def books(x,y,z,count):
    for i in range(count):book(x+i*.047,y,z,.22+.025*sin(i*1.7),.042,i)
def urn(x,y,z):
    lathe('Decorative urn',[(.036,0),(.045,.015),(.06,.055),(.061,.09),(.046,.12),(.035,.13),(.054,.14),(.047,.15),(.015,.174)],(x,y,z),'brass',12)
def armillary(x,y,z):
    lathe('Armillary pedestal',[(.06,0),(.055,.015),(.025,.03),(.02,.065)],(x,y,z),'brass',12)
    c=(x,y,z+.155)
    for rot in [Matrix.Rotation(pi/2,3,'X'),Matrix.Rotation(.6,3,'X'),Matrix.Rotation(pi/2,3,'Y')]:ring('Armillary ring',c,.092,.004,'brass',rot,16)
    tube('Armillary axis',[(x,y,z+.055),(x,y,z+.255)],.004,'brass',6)
def plant(x,y,z):
    lathe('Plant pot',[(.045,0),(.059,.075),(.065,.08),(.065,.091),(.054,.091)],(x,y,z),'brass',12)
    for j in range(4):
        a=j*2*pi/4
        pts=[(x,y,z+.085),(x+.07*cos(a),y+.07*sin(a),z+.18),(x+.10*cos(a),y+.10*sin(a),z+.115)]
        tube('Trailing stem',pts,.0025,'leather',5)
        for k in range(3):
            cx=x+(.035+.028*k)*cos(a);cy=y+(.035+.028*k)*sin(a);cz=z+.16-.04*k
            vs=[(cx,cy,cz+.045),(cx-.03,cy-.012,cz+.012),(cx,cy-.02,cz-.034),(cx+.03,cy-.012,cz+.012),(cx,cy-.026,cz+.01)]
            # Double winding for opaque two-sided leaves, only eight triangles per leaf.
            fs=[(0,1,4),(1,2,4),(2,3,4),(3,0,4)];fs+= [tuple(reversed(f)) for f in fs]
            mesh('Leaf',vs,fs,'leaf',False,[(.5,1),(0,.5),(.5,0),(1,.5),(.5,.5)])
def horse(x,y,z):
    src=bpy.data.objects.get('B_Horse_Sculpt_Body')
    if not src:return
    ob=bpy.data.objects.new('Reduced existing horse',src.data.copy());scene.collection.objects.link(ob);ob.parent=root;ob['role']=role;parts.append(ob)
    active(ob);mod=ob.modifiers.new('Shelf sculpture reduction','DECIMATE');mod.decimate_type='COLLAPSE';ob.data.calc_loop_triangles();mod.ratio=min(1,650/len(ob.data.loop_triangles));bpy.ops.object.modifier_apply(modifier=mod.name)
    lo=Vector([min(v.co[i] for v in ob.data.vertices) for i in range(3)]);hi=Vector([max(v.co[i] for v in ob.data.vertices) for i in range(3)]);s=.24/(hi.z-lo.z)
    for v in ob.data.vertices:v.co=Vector((x,y,z))+(v.co-Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z)))*s
    ob.data.materials.clear();ob.data.materials.append(atlas)
    if not ob.data.uv_layers:ob.data.uv_layers.new()
    for li in ob.data.uv_layers.active.data:li.uv=uvrect(regions['brass'],.5,.5,1024)
    box('Horse pedestal',(x,y,z-.009),(.22,.14,.018),'marble',.003)

def carcass(w,d,h,shelves):
    box('Rear backing',(0,d/2-.014,h/2),(w,.028,h-.12),'wood',.002)
    for x in [-w/2+.025,w/2-.025]:box('Case side',(x,0,h/2),(.05,d,h-.10),'wood',.005)
    for z in shelves:box('Shelf',(0,0,z),(w-.06,d-.018,.033),'wood',.004)
    for z,ww,dd,hh in [(.08,w+.04,d+.04,.10),(.135,w+.025,d+.025,.024),(h-.035,w+.065,d+.055,.045),(h,w+.085,d+.075,.027)]:
        box('Stepped cornice',(0,0,z),(ww,dd,hh),'wood',.005)
        box('Cornice brass trim',(0,-dd/2-.001,z+.012),(ww,.004,.005),'brass')
    for x in [-w/2+.025,w/2-.025]:box('Front pilaster',(x,-d/2-.008,h/2),(.052,.028,h-.16),'wood',.003)
    for x in [-w/2+.09,w/2-.09]:box('Bracket foot',(x,0,.03),(.14,d+.03,.06),'wood',.006)
    col((0,0,h/2),(w+.09,d+.08,h+.03))
start('Bookcase_A');w,d,h=.88,.35,1.86
carcass(w,d,h,[.16,.65,1.02,1.39,1.77])
for x in [-.205,.205]:
    framed_panel((x,-.18,.4),.37,.43)
    tube('Door handle',[(x+(.13 if x<0 else -.13),-.223,.35),(x+(.13 if x<0 else -.13),-.223,.46)],.006,'brass',8)
role='Contents'
books(-.32,-.05,.672,4);book(.04,-.035,.672,.25,.035,1,True);book(.04,-.035,.707,.25,.035,0,True);urn(.27,0,.672)
books(-.32,-.05,1.042,5);horse(.22,-.01,1.061)
books(-.32,-.05,1.412,4);armillary(.01,0,1.412);plant(.27,0,1.412)
finish()
start('Bookcase_B');w,d,h=1.52,.36,1.02
carcass(w,d,h,[.16,.53,.94])
for x in [-.26,.26]:box('Bay divider',(x,0,.565),(.037,.35,.8),'wood',.003)
framed_panel((0,-.19,.56),.48,.76)
# Replace central wood backing with a thin glass door while retaining its frame.
back=next(o for o in reversed(parts) if o.name.startswith('Panel backing'))
parts.remove(back);bpy.data.objects.remove(back,do_unlink=True)
panel('Central glass door',(0,-.212,.56),.425,.705,material=glass)
tube('Glass door knob',[(-.194,-.23,.55),(-.194,-.249,.55)],.015,'brass',10)
role='Contents'
books(-.65,-.045,.552,5);horse(-.37,-.015,.571)
plant(-.57,0,.182);book(-.38,-.03,.182,.20,.037,1,True);book(-.38,-.03,.219,.20,.037,2,True)
books(.36,-.045,.552,6);urn(.38,-.02,.182);books(.51,-.045,.182,4)
armillary(0,0,.552);book(0,-.03,.182,.25,.035,3,True);book(0,-.03,.217,.25,.035,1,True)
finish()

start('Shelf_A');w,d=.66,.34
for x in [-.31,.31]:
    for y in [-.15,.15]:
        tube('Slender brass upright',[(x,y,.018),(x,y,1.37)],.011,'brass',10)
        lathe('Round shelf foot',[(.023,0),(.025,.01),(.017,.025)],(x,y,0),'brass',10)
for z in [.16,.53,.90,1.27]:
    box('Walnut open shelf',(0,0,z),(.66,.34,.03),'wood',.004);col((0,0,z),(.66,.34,.035))
    for x in [-.31,.31]:
        for y in [-.15,.15]:box('Corner collar',(x,y,z),(.035,.035,.045),'brass',.003)
for y in [-.15,.15]:
    pts=[(.31*cos(pi*i/16),y,1.28+.31*sin(pi*i/16)) for i in range(17)]
    tube('Arched crown',pts,.009,'brass',8)
tube('Crown crossbar',[(0,-.15,1.59),(0,.15,1.59)],.009,'brass',8)
lathe('Crown finial',[(.014,0),(.023,.015),(.017,.035),(.001,.05)],(0,0,1.6),'brass',10)
col((0,0,.8),(.68,.36,1.6));finish()
start('Shelf_B')
for z in [.25,.70,1.15]:
    box('Wall shelf board',(0,-.125,z),(.95,.28,.03),'wood',.004)
    for x in [-.36,.36]:
        box('Wall mounting plate',(x,.008,z-.06),(.035,.018,.32),'brass',.006)
        for zz in [z-.2,z+.08]:tube('Mounting screw',[(x,-.005,zz),(x,-.014,zz)],.008,'brass',8)
        pts=[(x,-.01,z-.20),(x,-.05,z-.13),(x,-.12,z-.07),(x,-.22,z-.025)]
        tube('Curved bracket',pts,.009,'brass',8)
        ring('Bracket circle',(x,-.06,z-.065),.033,.005,'brass',Matrix.Rotation(pi/2,3,'Y'),12)
    col((0,-.125,z),(.96,.29,.035))
root['mounting']='Wall mounting plane is local Y=+0.02 in Blender / local Z=-0.02 in Unity.'
finish()

manifest={'assets':[],'source_materials':'../LoungeAccessories/Materials/Lounge_BK_Atlas.mat','notes':'No text/curve objects. SignFace meshes are 4 vertices, artwork is in separate textures.'}
for r in roots:
    bpy.ops.object.select_all(action='DESELECT');r.select_set(True)
    for o in r.children_recursive:o.hide_set(False);o.select_set(True)
    bpy.context.view_layer.objects.active=r
    bpy.ops.export_scene.fbx(filepath=str(OUT/'Models'/(r.name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,mesh_smooth_type='OFF',path_mode='STRIP')
    entry={'name':r.name,'colliders':json.loads(r['colliders']),'lods':[]}
    for lod in ['LOD0','LOD1']:
        meshes=[o.data for o in r.children if o.type=='MESH' and (o.name.endswith(lod) or not any(t in o.name for t in ['LOD0','LOD1']))]
        for m in meshes:m.calc_loop_triangles()
        entry['lods'].append({'name':lod,'vertices':sum(len(m.vertices) for m in meshes),'triangles':sum(len(m.loop_triangles) for m in meshes)})
    for o in r.children:
        if 'LOD1' in o.name:o.hide_set(True);o.hide_render=True
    manifest['assets'].append(entry)
(OUT/'FurnitureUtilityManifest.json').write_text(json.dumps(manifest,indent=2))
(DOC/'Blender_Budget.json').write_text(json.dumps(manifest,indent=2))
assert not any(o.type in {'FONT','CURVE'} for o in scene.objects)
for i,r in enumerate(roots):r.location=((i//2-2)*2.45,(i%2)*2.3,0)
cam_data=bpy.data.cameras.new('Furniture overview');cam=bpy.data.objects.new('Furniture overview',cam_data);scene.collection.objects.link(cam);cam.location=(5,-13,9);target=Vector((0,1.1,.65));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam_data.type='ORTHO';cam_data.ortho_scale=13;scene.camera=cam
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D':
        a.spaces.active.shading.type='MATERIAL';a.spaces.active.overlay.show_overlays=False
        a.spaces.active.region_3d.view_rotation=cam.rotation_euler.to_quaternion();a.spaces.active.region_3d.view_location=target;a.spaces.active.region_3d.view_distance=8;a.spaces.active.region_3d.view_perspective='ORTHO'
bpy.data.libraries.write(str(DOC/'BBB_Furniture_Utility_AB_GameReady.blend'),{scene},fake_user=True)
print(json.dumps([{e['name']:e['lods']} for e in manifest['assets']],indent=2))
