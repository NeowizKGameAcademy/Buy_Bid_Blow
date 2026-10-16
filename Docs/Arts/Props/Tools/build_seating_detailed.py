import bpy, math, os
from mathutils import Vector, Matrix
from math import sin, cos, pi, exp, sqrt

SCENE='Seating_AB_Detailed_Geometry'
s=bpy.data.scenes.new(SCENE)
bpy.context.window.scene=s
COLORS={'wood':(.43,.40,.35,1),'cloth':(.68,.65,.59,1),'metal':(.53,.50,.43,1),'seam':(.52,.49,.44,1)}
def root(name,location):
    o=bpy.data.objects.new(name,None);s.collection.objects.link(o);o.location=location;return o
def mesh(name,verts,faces,r,kind='wood',smooth=False):
    d=bpy.data.meshes.new(name);d.from_pydata(verts,[],faces);d.update()
    o=bpy.data.objects.new(name,d);s.collection.objects.link(o);o.parent=r;o.color=COLORS[kind]
    for p in d.polygons:p.use_smooth=smooth
    return o
def bevel(o,w=.006,segments=3):
    b=o.modifiers.new('Rounded joinery edges','BEVEL');b.width=w;b.segments=segments
    n=o.modifiers.new('Corner normals','WEIGHTED_NORMAL');return o
def box(n,loc,dim,r,kind='wood',bev=.006):
    vs=[(loc[0]+x*dim[0]/2,loc[1]+y*dim[1]/2,loc[2]+z*dim[2]/2) for x,y,z in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    o=mesh(n,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],r,kind)
    if bev:bevel(o,bev)
    return o
def tube(n,pts,r,rad=.004,kind='seam',cyclic=False):
    # Explicit tubular mesh, portable without curve evaluation.
    pts=[Vector(v) for v in pts];N=len(pts);vs=[];sides=8
    for i,p in enumerate(pts):
        t=(pts[(i+1)%N]-pts[(i-1)%N]) if cyclic else (pts[min(i+1,N-1)]-pts[max(i-1,0)])
        t.normalize();a=t.cross(Vector((0,0,1)))
        if a.length<.01:a=t.cross(Vector((0,1,0)))
        a.normalize();b=t.cross(a).normalized()
        for j in range(sides):vs.append(tuple(p+rad*(a*cos(2*pi*j/sides)+b*sin(2*pi*j/sides))))
    fs=[(i*sides+j,i*sides+(j+1)%sides,((i+1)%N)*sides+(j+1)%sides,((i+1)%N)*sides+j) for i in range(N if cyclic else N-1) for j in range(sides)]
    return mesh(n,vs,fs,r,kind,True)
def bead(n,p,r,rad=.007,kind='metal',scale=(1,1,1)):
    N=10;M=6;vs=[]
    for j in range(M+1):
        t=pi*j/M
        for i in range(N):
            a=2*pi*i/N;vs.append((p[0]+rad*scale[0]*sin(t)*cos(a),p[1]+rad*scale[1]*sin(t)*sin(a),p[2]+rad*scale[2]*cos(t)))
    return mesh(n,vs,[(j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i) for j in range(M) for i in range(N)],r,kind,True)
def lathe(n,profile,r,center=(0,0,0),axis='Z',kind='metal'):
    N=32;vs=[]
    for rad,z in profile:
        for j in range(N):
            a=2*pi*j/N;v=(rad*cos(a),rad*sin(a),z)
            if axis=='X':v=(z,v[0],v[1])
            if axis=='Y':v=(v[0],z,v[1])
            vs.append(tuple(center[i]+v[i] for i in range(3)))
    fs=[(k*N+i,k*N+(i+1)%N,(k+1)*N+(i+1)%N,(k+1)*N+i) for k in range(len(profile)-1) for i in range(N)]
    return mesh(n,vs,fs,r,kind,True)
def sweep(n,pts,width,depth,r,kind='wood'):
    # rectangular joinery sweep with variable width
    vs=[];pts=[Vector(p) for p in pts]
    for i,p in enumerate(pts):
        t=(pts[min(i+1,len(pts)-1)]-pts[max(i-1,0)]).normalized();a=Vector((1,0,0))
        if abs(t.dot(a))>.9:a=Vector((0,1,0))
        a=(a-t*t.dot(a)).normalized();b=t.cross(a).normalized();w=width[i] if isinstance(width,list) else width
        for u,v in [(-1,-1),(1,-1),(1,1),(-1,1)]:vs.append(tuple(p+a*u*w/2+b*v*depth/2))
    fs=[(0,3,2,1)]+[(4*i+j,4*i+(j+1)%4,4*(i+1)+(j+1)%4,4*(i+1)+j) for i in range(len(pts)-1) for j in range(4)]+[tuple(4*(len(pts)-1)+j for j in range(4))]
    return bevel(mesh(n,vs,fs,r,kind),.004)
def sqmap(u,v,w,h,n=10):
    rad=max(abs(u),abs(v))
    if rad<1e-9:return 0,0
    a=math.atan2(v,u);k=(abs(cos(a))**n+abs(sin(a))**n)**(-1/n)
    return w/2*rad*k*cos(a),h/2*rad*k*sin(a)
def cushion(n,w,h,d,r,center,vertical=False,tuft=False,cx=.25,cy=.20,rotation=None):
    # Unified closed upholstered shell: diamond valleys, button pulls, side folds, welt.
    NX=max(48,int(w*100));NY=max(40,int(h*100));verts=[];faces=[]
    def topz(x,y,u,v):
        edge=max(abs(u),abs(v));base=d*.18+d*.34*(max(0,1-edge**10))**.28
        if tuft:
            q=x/cx+y/cy;qq=x/cx-y/cy
            f=q-math.floor(q);g=qq-math.floor(qq)
            puff=(sin(pi*f)*sin(pi*g))**.55
            val=min(abs(q-round(q)),abs(qq-round(qq)))
            fade=min(1,max(0,(1-edge)/.12))
            base+=fade*(.032*puff-.009*exp(-(val/.045)**2))
        else:base+=.025*(1-edge**2)
        return base
    def tx(x,y,z):
        p=Vector((x,-z,y) if vertical else (x,y,z))
        if rotation:p=rotation@p
        return tuple(p+Vector(center))
    for layer in [0,1]:
        for j in range(NY+1):
            v=2*j/NY-1
            for i in range(NX+1):
                u=2*i/NX-1;x,y=sqmap(u,v,w,h)
                z=topz(x,y,u,v) if layer==0 else -d*.46+.01*(1-max(abs(u),abs(v)))
                verts.append(tx(x,y,z))
    stride=NX+1;off=stride*(NY+1)
    for j in range(NY):
        for i in range(NX):
            a=j*stride+i;faces.append((a,a+1,a+stride+1,a+stride));faces.append((off+a+stride,off+a+stride+1,off+a+1,off+a))
    boundary=list(range(NX+1))+[j*stride+NX for j in range(1,NY+1)]+[NY*stride+i for i in range(NX-1,-1,-1)]+[j*stride for j in range(NY-1,0,-1)]
    for i,a in enumerate(boundary):b=boundary[(i+1)%len(boundary)];faces.append((a,off+a,off+b,b))
    ob=mesh(n,verts,faces,r,'cloth',True)
    seam=[]
    for a in boundary:
        j=a//stride;i=a%stride;x,y=sqmap(2*i/NX-1,2*j/NY-1,w,h);seam.append(tx(x,y,d*.03))
    tube(n+'_Piped_Seam',seam,r,.0032,'seam',True)
    if tuft:
        for j in range(-int(h/cy)-2,int(h/cy)+3):
            y=j*cy/2
            for i in range(-int(w/cx)-2,int(w/cx)+3):
                x=(i+(j%2)*.5)*cx
                if abs(x)<w*.45 and abs(y)<h*.43:
                    z=topz(x,y,x/(w/2),y/(h/2))+.003
                    bead(n+'_Inset_Button',tx(x,y,z),r,.009,'metal' if 'Chair' in n else 'cloth',(.85,.85,.65))
    return ob
def plaque(n,p,r,size=.07):
    x,y,z=p;box(n+'_Plate',p,(size,.014,size),r,'metal',.003)
    box(n+'_Inset',(x,y-.009,z),(size*.78,.006,size*.78),r,'wood',.001)
    for k in range(8):
        a=k*pi/4;pts=[]
        for j in range(13):
            t=j/12;rr=size*.34*sin(pi*t);aa=a+.20*sin(2*pi*t)
            pts.append((x+rr*sin(aa),y-.016-.007*sin(pi*t),z+rr*cos(aa)))
        tube(n+'_Rosette_Petal',pts,r,.003,'metal')
    bead(n+'_Flower_Center',(x,y-.02,z),r,.008)
def tapered_leg(n,x,y,height,r,back=False):
    pts=[]
    for i in range(17):
        t=i/16;dx=.025*(1-t)**2*(1 if x>0 else -1);dy=(.045 if back else -.018)*(1-t)**2
        pts.append((x+dx,y+dy,.035+(height-.035)*t))
    widths=[.039+.035*(i/16)**.8 for i in range(17)]
    sweep(n,pts,widths,.065,r)
    box(n+'_Foot_Sabot',pts[0],(.052,.073,.055),r,'metal',.003)
    box(n+'_Collar',(x,y,height-.005),(.087,.081,.028),r,'metal',.003)
    # inset tapered panel and its raised narrow mouldings
    for sg in [-1,1]:tube(n+'_Flute',[(x+sg*.016,y-.035,.09),(x+sg*.025,y-.035,height-.055)],r,.002,'wood')
def apron(n,w,d,z,r):
    for yy in [-d/2,d/2]:
        box(n+'_Long_Rail',(0,yy,z),(w,.048,.09),r)
        for zz in [z-.034,z+.034]:tube(n+'_Rail_Mould', [(-w/2,yy-.026,zz),(w/2,yy-.026,zz)],r,.004,'wood')
    for xx in [-w/2,w/2]:box(n+'_End_Rail',(xx,0,z),(.048,d,.09),r)

def build_benchA():
    r=root('Bench_A_Detailed_ROOT',(-2.6,-2,0));w=1.55;d=.65
    for x in [-.69,.69]:
        for y in [-.245,.245]:
            tapered_leg('BA_Fluted_Leg',x,y,.34,r);box('BA_Corner_Block',(x,y,.385),(.10,.10,.11),r);plaque('BA_Corner_Diamond',(x,y-.055,.385),r,.073)
    apron('BA',1.4,.51,.38,r);cushion('Bench_A_Deep_Diamond_Tuft',w,d,.18,r,(0,0,.50),tuft=True,cx=.25,cy=.24)
    return r
def build_benchB():
    r=root('Bench_B_Detailed_ROOT',(-.4,-2,0));apron('BB',1.4,.51,.40,r)
    cushion('Bench_B_Single_Pillow',1.55,.65,.17,r,(0,0,.51))
    tube('BB_Underseat_Bead',[(-.74,-.29,.45),(.74,-.29,.45),(.74,.29,.45),(-.74,.29,.45),(-.74,-.29,.45)],r,.005,'metal')
    for x in [-.65,.65]:
        for sg in [-1,1]:
            pts=[(x,sg*(-.29*cos(pi*t/32)),.035+.36*t/32) for t in range(33)]
            sweep('BB_Curved_Cross_Trestle',pts,.065,.075,r)
            tube('BB_Trestle_Edge',[(x-.035,y,z) for _,y,z in pts],r,.003,'wood')
            box('BB_Trestle_Shoe',(x,-sg*.29,.025),(.081,.095,.05),r,'metal')
            lathe('BB_Round_Boss',[(0,0),(.029,0),(.031,.009),(.020,.015),(0,.015)],r,(x,-sg*.30,.396),'Y')
    lathe('BB_Turned_Crossbar',[(.021,-.66),(.025,-.62),(.019,-.59),(.012,-.20),(.019,-.12),(.025,-.10),(.017,-.07),(.017,.07),(.025,.10),(.019,.12),(.012,.20),(.019,.59),(.025,.62),(.021,.66)],r,(0,0,.20),'X')
    return r

def build_chair(kind,x):
    r=root('Chair_'+kind+'_Detailed_ROOT',(x,-2,0));w=.60;d=.59
    for xx in [-.255,.255]:
        for y in [-.25,.25]:tapered_leg('C'+kind+'_Leg',xx,y,.39,r,y>0)
    apron('C'+kind,.55,.52,.42,r);cushion('Chair_'+kind+'_Seat',.61,.61,.16,r,(0,-.025,.515))
    for xx in [-.285,.285]:
        if kind=='A':box('CA_Back_Post',(xx,.275,.945),(.058,.07,1.05),r)
        else:sweep('CB_Back_Post',[(xx,.24,.46),(xx,.26,.80),(xx*.98,.31,1.18),(xx,.32,1.36)],.055,.065,r)
    if kind=='A':
        box('CA_Head_Rail',(0,.275,1.445),(.64,.075,.065),r)
        for z in [1.415,1.466]:tube('CA_Rail_Moulding',[(-.29,.232,z),(.29,.232,z)],r,.004,'wood')
        for xx in [-.285,.285]:plaque('CA_Top_Rosette',(xx,.225,1.44),r,.076)
        cushion('Chair_A_Diamond_Back',.51,.82,.10,r,(0,.272,.972),True,True,.19,.23)
    else:
        cushion('Chair_B_Padded_Back',.51,.78,.12,r,(0,.29,.963),True,False)
        crown=[(.30*(2*i/48-1),.31,1.35+.105*cos((2*i/48-1)*pi/2)**6) for i in range(49)]
        tube('CB_Swept_Crown',crown,r,.032,'wood');tube('CB_Crown_Raised_Mould',[(x,y-.028,z-.012) for x,y,z in crown],r,.006,'wood')
        for xx in [-.25,.25]:
            for i in range(31):bead('CB_Back_Brass_Nail',(xx,.222,.59+i*.024),r,.0055)
        for i in range(24):bead('CB_Top_Nail',(-.245+i*.0213,.222,1.345+.02*cos(i/23*pi*2)),r,.0055)
        # Five carved shell flutes in the crown.
        for k in range(-2,3):
            tube('CB_Shell_Crest',[(.008*k,.267,1.375),(.023*k,.262,1.415),(.033*k,.267,1.44-abs(k)*.011)],r,.010-abs(k)*.001,'metal')
        for sg in [-1,1]:
            pts=[]
            for i in range(33):
                t=i/32;pts.append((sg*(.31+.024*sin(pi*t)),-.29+.59*t,.79+.045*t+.16*t**6))
            sweep('CB_Curved_Arm_Rail',pts,.073,.045,r)
            tube('CB_Arm_Moulding',[(x+sg*.034,y,z) for x,y,z in pts],r,.004,'wood')
            support=[(sg*(.29+.025*sin(pi*t/24)),-.28+.035*(t/24),.43+.34*t/24) for t in range(25)]
            sweep('CB_Cabriole_Arm_Support',support,.055,.053,r)
            box('CB_Handrest_Cap',(sg*.31,-.27,.79),(.082,.095,.048),r,'metal',.013)
            plaque('CB_Arm_Rosette',(sg*.29,-.323,.44),r,.073)
    for yy in [-.29,.29]:
        for i in range(25):bead('C'+kind+'_Seat_Nail',(-.28+i*.56/24,yy,.455),r,.0047)
    for xx in [-.29,.29]:
        for i in range(23):bead('C'+kind+'_Seat_Nail',(xx,-.27+i*.54/22,.455),r,.0047)
    return r

def sofa_legs(r,w,d):
    for x in [-w/2,w/2]:
        for y in [-d/2,d/2]:lathe('Sofa_Turned_Foot',[(0,.01),(.022,.01),(.025,.035),(.022,.055),(.034,.13),(.043,.15),(.046,.17),(.038,.19),(.04,.21)],r,(x,y,0))
def U_path(t):return Vector((-1.05*cos(pi*t),-.30+.75*sin(pi*t),0))
def sofaA():
    r=root('Sofa_A_Detailed_ROOT',(-1.55,.65,0));sofa_legs(r,1.94,.68)
    # Flowing tub shell; the arms and back are one continuous padded sweep.
    vs=[];fs=[];N=120;K=28
    for i in range(N+1):
        t=i/N;p=U_path(t);tangent=(U_path(min(1,t+.001))-U_path(max(0,t-.001))).normalized();out=Vector((-tangent.y,tangent.x,0));height=.89+.17*sin(pi*t)**.5;mid=(height+.21)/2;hz=(height-.21)/2
        for j in range(K):
            a=2*pi*j/K;z=mid+hz*(1 if sin(a)>=0 else -1)*abs(sin(a))**.48
            thick=.10*(1 if cos(a)>=0 else -1)*abs(cos(a))**.7
            # Fine panel channels, pressed into the inside of the curved back.
            channel=sum(.010*exp(-((t-f)/.009)**2) for f in [.14,.28,.43,.57,.72,.86])
            if cos(a)<0:thick+=channel
            vs.append(tuple(p+out*thick+Vector((0,0,z))))
    for i in range(N):
        for j in range(K):fs.append((i*K+j,i*K+(j+1)%K,(i+1)*K+(j+1)%K,(i+1)*K+j))
    fs.extend([tuple(range(K-1,-1,-1)),tuple(N*K+j for j in range(K))]);mesh('SA_Continuous_Curved_Upholstery',vs,fs,r,'cloth',True)
    for z in [.205,.26]:tube('SA_Curved_Base_Moulding',[tuple(U_path(i/120)+Vector((0,0,z))) for i in range(121)],r,.027,'wood')
    tube('SA_Front_Walnut_Base',[(x,-.44+.05*x*x,.23) for x in [-1.03+2.06*i/80 for i in range(81)]],r,.041,'wood')
    for k in [-1,0,1]:
        x=k*.62;y=-.085+.06*abs(k);rot=Matrix.Rotation(-k*.105,3,'Z');cushion('Sofa_A_Seat_'+str(k),.65,.76,.19,r,(x,y,.45),rotation=rot)
        cushion('Sofa_A_Lower_Bolster_'+str(k),.65,.75,.08,r,(x,y,.325),rotation=rot)
    for t in [.02,.14,.28,.43,.57,.72,.86,.98]:
        p=U_path(t);tube('SA_Vertical_Panel_Seam',[(p.x*.94,p.y-.035,z) for z in [.4,.55,.7,.85,.89+.17*sin(pi*t)**.5-.015]],r,.0025,'seam')
    for end in [0,1]:
        pts=[tuple(U_path(end+(1 if end==0 else -1)*i/120)+Vector((0,0,.89+.17*sin(pi*(end+(1 if end==0 else -1)*i/120))**.5))) for i in range(61)]
        tube('SA_Wood_Arm_Outline',pts,r,.018,'wood')
    # Loose decorative pillows are separate future props, per user request.
    return r

def sofaB():
    r=root('Sofa_B_Detailed_ROOT',(1.45,.65,0));sofa_legs(r,1.94,.72)
    cushion('SB_Front_Deep_Apron',1.96,.19,.14,r,(0,-.38,.29),True,True,.25,.25)
    box('SB_Underframe',(0,0,.235),(2.03,.85,.09),r,'wood',.025)
    for x in [-.433,.433]:cushion('SB_Separate_Seat',.85,.74,.23,r,(x,-.09,.47))
    cushion('Sofa_B_Chesterfield_Diamond_Back',1.82,.61,.21,r,(0,.345,.83),True,True,.25,.20)
    # Cylindrical rolled arms with diamond compression around the roll.
    for sg in [-1,1]:
        cushion('SB_Outer_Arm_Panel',.86,.52,.17,r,(sg*1.01,0,.57),True,rotation=Matrix.Rotation(sg*pi/2,3,'Z'))
        vs=[];fs=[];N=68;K=72
        for i in range(N+1):
            y=-.43+.89*i/N
            for j in range(K):
                a=2*pi*j/K;q=y/.22+a/pi*1.8;qq=y/.22-a/pi*1.8;f=q-math.floor(q);g=qq-math.floor(qq)
                puff=.012*(sin(pi*f)*sin(pi*g))**.6;rr=.145+puff
                vs.append((sg*(1.0+rr*cos(a)),y,.92+rr*sin(a)))
        for i in range(N):
            for j in range(K):fs.append((i*K+j,i*K+(j+1)%K,(i+1)*K+(j+1)%K,(i+1)*K+j))
        mesh('SB_Quilted_Rolled_Arm',vs,fs,r,'cloth',True)
        # Front arm ends, broad scroll outline and individual nail heads.
        outline=[(sg*.905,-.455,.23),(sg*1.105,-.455,.23),(sg*1.105,-.455,.79)]
        for i in range(41):
            a=-.75+4.4*i/40;outline.append((sg*(1.0+.155*cos(a)),-.455,.92+.155*sin(a)))
        outline.extend([(sg*.905,-.455,.78),(sg*.905,-.455,.23)])
        vs2=outline+[(x,y+.055,z) for x,y,z in outline];L=len(outline)
        faces=[tuple(range(L-1,-1,-1)),tuple(range(L,2*L))]+[(i,(i+1)%L,(i+1)%L+L,i+L) for i in range(L)]
        bevel(mesh('SB_Scroll_Arm_Face',vs2,faces,r,'cloth'),.012,4)
        tube('SB_Arm_Edge_Piping',outline,r,.004,'seam')
        for a,b in zip(outline,outline[1:]):
            A=Vector(a);B=Vector(b);steps=max(1,int((B-A).length/.018))
            for i in range(steps):bead('SB_Arm_Nailhead',tuple(A.lerp(B,i/steps)+Vector((0,-.007,0))),r,.0048)
        # Inner spiral seam ending in the scroll center.
        pts=[]
        for i in range(70):
            t=i/69;a=1.0+4.8*t;rad=.127*(1-t)+.025*t;pts.append((sg*(1+rad*cos(a)),-.464,.92+rad*sin(a)))
        tube('SB_Roll_Spiral_Seam',pts,r,.003,'seam')
    return r

roots=[build_benchA(),build_benchB(),build_chair('A',1.2),build_chair('B',2.45),sofaA(),sofaB()]
for r in roots:
    r['reference']='FurnitureConcepts_AB / geometry fidelity revision';r['materials_pending']=True
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D':
        sh=a.spaces.active.shading;sh.type='SOLID';sh.light='STUDIO';sh.color_type='OBJECT';sh.show_shadows=True;sh.show_cavity=True
        v=a.spaces.active.region_3d;v.view_location=(0,-.15,.65);v.view_distance=6.6;v.view_rotation=Vector((.25,-1,.65)).to_track_quat('Z','Y')
out='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/Seating/BBB_Seating_AB_Detailed_Geometry.blend'
bpy.data.libraries.write(out,{s},fake_user=True)
print('Built',len(s.objects),'objects;',sum(len(o.data.polygons) for o in s.objects if o.type=='MESH'),'polygons',out)
