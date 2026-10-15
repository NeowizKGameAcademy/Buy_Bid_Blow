"""Compact the existing seating to the user's red limit; use the room rear wall."""
import bpy, math, random, runpy, json
from mathutils import Vector, Matrix

S = bpy.context.scene
OLD = bpy.data.collections['A37_02_Trial_65pct_10Seats']
H = runpy.run_path(r'C:\course\unity\BuyBidBlow\Docs\Arts\Props\Tools\AuctionSeatingSketchRevision.py')
H['mesh'].__globals__['PREFIX'] = 'A38_'
GROUPS = ['01_Floors','02_Curved_Desks','03_Stairs','04_Rails','06_Chairs','07_Lamps']
TOP = bpy.data.collections.new('A38_Compact_6Seat_Seating')
S.collection.children.link(TOP)
for name in GROUPS:
    c = bpy.data.collections.new('A38_'+name)
    TOP.children.link(c)
    H['C'][name] = c
for m in bpy.data.materials:
    if m.name.startswith('ASR_'):
        H['M'][m.name[4:]] = m

BASE = 4.008
LOW = 4.6905
HIGH = 5.373
XFACTOR = .60
YFACTOR = .73
FRONT = 10.30
OLD_FRONT = 6.608
OLD_SHIFT = 7.342499732971191
STEPS = []
WALL = S.objects['A18_Walls_Ellipse_DoorOpenings']
WALL_EVAL = WALL.evaluated_get(bpy.context.evaluated_depsgraph_get())
WALL_INV = WALL.matrix_world.inverted()


def wall_y(x):
    hit, p, n, idx = WALL_EVAL.ray_cast(WALL_INV @ Vector((x,12,6)),
        (WALL_INV.to_3x3() @ Vector((0,1,0))).normalized())
    assert hit, ('Rear wall missing',x)
    return (WALL.matrix_world @ p).y


def transform(p):
    return Vector((p.x*XFACTOR, FRONT+(p.y-OLD_FRONT)*YFACTOR, p.z))


def from_original(x,y):
    return (x*.65*XFACTOR,FRONT+(OLD_SHIFT+.65*y-OLD_FRONT)*YFACTOR)


def clone_deformed(o,group,rigid_lamp=False):
    n=o.copy(); n.data=o.data.copy(); n.parent=None
    n.matrix_parent_inverse=Matrix.Identity(4); n.matrix_world=Matrix.Identity(4)
    n.name='A38_'+o.name.replace('FIT_Trial_ASR_','')
    H['C'][group].objects.link(n)
    points=[o.matrix_world@v.co for v in o.data.vertices]
    cx=(min(p.x for p in points)+max(p.x for p in points))/2
    cy=(min(p.y for p in points)+max(p.y for p in points))/2
    center=transform(Vector((cx,cy,0)))
    zbase=(LOW if '_Front_' in o.name else HIGH)+1.18*.65
    for v,p in zip(n.data.vertices,points):
        if rigid_lamp:
            v.co=(center.x+(p.x-cx)*.85,center.y+(p.y-cy)*.85,zbase+(p.z-zbase)*.9)
        else:
            v.co=transform(p)
    for mod in n.modifiers:
        if mod.type=='BEVEL':mod.width*=.65*.7
    n.data.update()
    return n


def planks(name,poly,z):
    rng=random.Random(38)
    y=min(p[1] for p in poly); ymax=max(p[1] for p in poly)
    xmin=min(p[0] for p in poly);xmax=max(p[0] for p in poly)
    row=0
    clip=H['clipped']
    while y<ymax:
        band=clip(clip(poly,1,y+.004,True),1,y+.27-.004,False)
        x=xmin-(row%2)*.7
        while x<xmax:
            p=clip(clip(band,0,x+.004,True),0,x+1.4-.004,False) if band else []
            if len(p)>2:
                area=abs(sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(p,p[1:]+p[:1])))/2
                if area>.003:H['slab'](name,p,z-.014,z,'Plank'+str(rng.randrange(4)),'01_Floors',0)
            x+=1.4
        y+=.27;row+=1


def stair_geometry(name,a,b,z0,z1):
    a,b=Vector(a),Vector(b);d=(b-a).normalized();length=(b-a).length
    angle=math.atan2(-d.x,d.y)
    for i in range(4):
        p=a.lerp(b,(i+.5)/4);z=z0+(z1-z0)*(i+1)/4
        H['box'](name+'_Step_'+str(i+1),(p.x,p.y,(z0+z)/2),(.95,length/4+.006,z-z0),'Wood','03_Stairs',angle,.012)
        H['box'](name+'_Runner_'+str(i+1),(p.x,p.y,z+.007),(.69,length/4-.02,.014),'Carpet','03_Stairs',angle,.005)
        edge=p-d*(length/8-.026)
        H['box'](name+'_Nosing_'+str(i+1),(edge.x,edge.y,z+.015),(.91,.028,.02),'Brass','03_Stairs',angle,.004)
    STEPS.append((name,a,b,z0,z1))


def stair_edges(a,b):
    a,b=Vector(a),Vector(b);d=(b-a).normalized();out=Vector((d.y,-d.x))
    return [tuple(a-out*.475),tuple(b-out*.475),tuple(b+out*.475),tuple(a+out*.475)]


def rail_post(name,p):
    x,y,z=p;g='04_Rails'
    H['box'](name+'_Foot',(x,y,z+.055),(.30,.30,.11),'DarkWood',g,bevel=.012)
    H['box'](name+'_Post',(x,y,z+.43),(.20,.20,.86),'Wood',g,bevel=.012)
    H['box'](name+'_Collar',(x,y,z+.80),(.27,.27,.11),'Honey',g,bevel=.01)
    H['box'](name+'_Cap',(x,y,z+.89),(.31,.31,.08),'Brass',g,bevel=.012)


def rail_panel(a,b):
    a,b=Vector(a),Vector(b);d=b-a;n=Vector((-d.y,d.x,0)).normalized()
    vs=[tuple(p+n*w+Vector((0,0,h))) for w in [-.038,.038] for p,h in [(a,.17),(b,.17),(b,.73),(a,.73)]]
    fs=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    H['mesh']('Side_Panel',vs,fs,'Panel','04_Rails',.008)
    for h,w,depth,mat in [(.13,.10,.13,'Wood'),(.80,.13,.16,'Honey'),(.875,.045,.10,'Brass')]:
        H['beam']('Side_Rail',a+Vector((0,0,h)),b+Vector((0,0,h)),w,depth,mat,'04_Rails',.008)
    for side in [-1,1]:
        p=a.lerp(b,.08)+n*.055*side;q=a.lerp(b,.92)+n*.055*side
        for z0,z1 in [(.21,.71),(.71,.21)]:
            H['beam']('Side_X_Brace',p+Vector((0,0,z0)),q+Vector((0,0,z1)),.05,.05,'Wood','04_Rails',.006)


def build():
    # Preserve the actual crafted counter meshes; reshape their plan only.
    for o in OLD.objects:
        if o.type!='MESH':continue
        name=o.name.replace('FIT_Trial_ASR_','')
        if name.startswith(('Front_','Rear_')):
            if '_Lamp_' in name:clone_deformed(o,'07_Lamps',True)
            elif not any(k in name for k in ['Seat_','Rear_Wall','Rear_Enclosing']):
                clone_deformed(o,'02_Curved_Desks')

    front=[from_original(x,y) for x,y in H['rowpath'](7.514,-1)]
    rear=[from_original(x,y) for x,y in H['rowpath'](5.766,6.171)]
    la,lb=from_original(8.5,1.3),from_original(7.8,4.1)
    ua,ub=from_original(7.18,7.65),from_original(6.28,10.55)
    le=stair_edges(la,lb);ue=stair_edges(ua,ub)
    mirror=lambda p:(-p[0],p[1])
    low_right=[le[0],le[1],le[2],ue[3]]
    lowpoly=front+low_right+list(reversed(rear))+[mirror(p) for p in reversed(low_right)]
    rear_halfwidth=3.276
    back=[(rear_halfwidth*(2*i/80-1),wall_y(rear_halfwidth*(2*i/80-1))+.002) for i in range(81)]
    high_right=[ue[0],ue[1],ue[2]]
    highpoly=rear+high_right+list(reversed(back))+[mirror(p) for p in reversed(high_right)]
    H['slab']('Lower_Floor',lowpoly,BASE,LOW-.018,'DarkWood','01_Floors',.012)
    H['slab']('Upper_Floor_Wall_Fitted',highpoly,BASE,HIGH-.018,'DarkWood','01_Floors',.012)
    planks('Lower_Plank',lowpoly,LOW)
    planks('Upper_Plank',highpoly,HIGH)

    for sign in [-1,1]:
        sf=lambda p:(p[0]*sign,p[1])
        stair_geometry('Lower_'+str(sign),sf(la),sf(lb),BASE,LOW)
        stair_geometry('Upper_'+str(sign),sf(ua),sf(ub),LOW,HIGH)
        right=[(*le[3],BASE),(*le[2],LOW),(*ue[3],LOW),(*ue[2],HIGH),
               (rear_halfwidth,wall_y(rear_halfwidth)-.12,HIGH)]
        route=[(x*sign,y,z) for x,y,z in right]
        for i,p in enumerate(route):rail_post('Side_'+str(sign)+'_'+str(i),p)
        for a,b in zip(route,route[1:]):rail_panel(a,b)
        # Short terminal extends the side cap directly to the room wall.
        p=Vector(route[-1])+Vector((0,0,.80));q=p.copy();q.y=wall_y(q.x)+.004
        H['beam']('Wall_Terminal',p,q,.13,.16,'Honey','04_Rails',.005)

    # Rigid chairs retain usable scale while the table and platform get smaller.
    for row,half,y,level,spread in [('Front',7.514,-1,LOW,1.75),('Rear',5.766,6.171,HIGH,1.35)]:
        src=next(o for o in OLD.objects if o.type=='EMPTY' and o.name.endswith('ASR_'+row+'_Seat_01'))
        w=half*.65*XFACTOR
        center=from_original(0,y)[1]
        sag=1.84*.65*YFACTOR
        for seat,x in enumerate([-spread,0,spread],1):
            yy=center+sag*(x/w)**2+.94
            root=bpy.data.objects.new('A38_'+row+'_Seat_'+str(seat),None)
            H['C']['06_Chairs'].objects.link(root)
            root.location=(x,yy,level);root.rotation_euler.z=math.atan2(-x,yy-8.316);root.scale=(1.04,1.04,1.04)
            for part in src.children:
                obj=part.copy();obj.parent=root;obj.matrix_parent_inverse=Matrix.Identity(4);obj.matrix_basis=part.matrix_basis.copy()
                obj.name='A38_'+row+'_Seat_'+str(seat)+'_'+part.name.split('_Chair_')[-1]
                H['C']['06_Chairs'].objects.link(obj)

    OLD.hide_viewport=True;OLD.hide_render=True
    S.name='A38_Compact_6Seats_Wall_Fitted'
    S['seating_revision']='User red-line front limit; two rows of three; separate rear wall removed; platform follows room wall.'
    S['seating_front_limit_y']=FRONT
    S['seating_chair_count']=6
    S['seating_counter_width_scale_from_A37']=XFACTOR
    bpy.context.view_layer.update()
    print('Created compact seating',len(TOP.all_objects))


def validate():
    dg=bpy.context.evaluated_depsgraph_get()
    tests=[]
    for name,a,b,z0,z1 in STEPS:
        d=(b-a).normalized()
        for tag,p,z in [('entry',a-d*.10,z0),('exit',b+d*.10,z1)]:
            ok,loc,n,i,obj,m=S.ray_cast(dg,Vector((p.x,p.y,8)),Vector((0,0,-1)))
            tests.append({'flight':name,'point':tag,'expected_z':z,'actual_z':loc.z if ok else None,'pass':ok and abs(loc.z-z)<.04})
    ps=[o.matrix_world@Vector(v) for o in TOP.all_objects if o.type=='MESH' for v in o.bound_box]
    bounds=[[min(p[i] for p in ps),max(p[i] for p in ps)] for i in range(3)]
    report={'bounds':bounds,'dimensions':[b-a for a,b in bounds],
        'chairs':sum(o.type=='EMPTY' and '_Seat_' in o.name for o in TOP.all_objects),
        'rear_wall_objects':sum(any(t in o.name for t in ['Enclosing_Wall','Wall_Pilaster','Wall_Moulding']) for o in TOP.all_objects),
        'stair_connections':tests,'rear_contact_overlap_m':.002,
        'front_limit_y':FRONT,'source_unchanged_level_props':['A34_Auction_Rostrum','A36_Lot_Pedestal']}
    with open(r'C:\course\unity\BuyBidBlow\Docs\Arts\Environments\Models\A38_Compact_Seating_Checks.json','w',encoding='utf-8') as f:
        json.dump(report,f,ensure_ascii=False,indent=2)
    print(json.dumps(report))
    return report

