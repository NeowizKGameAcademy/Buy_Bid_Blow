import bpy, math, bmesh, json
from mathutils import Vector, Matrix, Quaternion
from math import sin,cos,pi,sqrt
src=bpy.data.scenes['BBB_Hall_8Arches_Columns']
s=bpy.data.scenes.new('BBB_Hall_A17_Smooth_6Columns')
s.world=src.world
bpy.context.window.scene=s
copies={}
for old in src.objects:
    o=old.copy()
    if old.data: o.data=old.data.copy()
    s.collection.objects.link(o); copies[old]=o
for old,o in copies.items():
    o.parent=copies.get(old.parent)
    o.matrix_parent_inverse=old.matrix_parent_inverse.copy()
    o.matrix_basis=old.matrix_basis.copy()
def remove(o):
    bpy.data.objects.remove(o,do_unlink=True)
def mesh(name,vs,fs,mat,smooth=False):
    me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
    bm=bmesh.new();bm.from_mesh(me);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=0.00001);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
    o=bpy.data.objects.new(name,me);s.collection.objects.link(o)
    if mat:me.materials.append(mat)
    uv=me.uv_layers.new(name='UVMap')
    for p in me.polygons:
        p.use_smooth=smooth and abs(p.normal.z)<.99
        for li in p.loop_indices:
            v=me.vertices[me.loops[li].vertex_index].co
            uv.data[li].uv=(v.x/4,v.y/4) if abs(p.normal.z)>.5 else ((v.x if abs(p.normal.y)>.5 else v.y)/4,v.z/4)
    return o
def inside(x,y,new=True):
    yy=5.5 if y>=0 or not new else 8.5
    ry=12 if y>=0 or not new else 9
    return (max(abs(x)-2,0)/12)**2+(max(abs(y)-yy,0)/ry)**2<=1
def radius(a,new=True):
    lo,hi=0.,24.
    for _ in range(42):
        r=(lo+hi)/2
        if inside(r*cos(a),r*sin(a),new):lo=r
        else:hi=r
    return (lo+hi)/2
# Apply the same outline to the ceiling and its concentric timber rings.
for old,o in copies.items():
    if old.name.startswith(('Hall_Roof_','A10_Dome_Ring')):
        mw=o.matrix_world.copy();inv=mw.inverted()
        for v in o.data.vertices:
            p=mw@v.co
            if p.y<0:
                a=math.atan2(p.y,p.x);k=radius(a)/radius(a,False)
                p.x*=k;p.y*=k;v.co=inv@p
        o.data.update()
outline=[]
for cx,cy,a0,ry in [(2,5.5,0,12),(-2,5.5,90,12),(-2,-8.5,180,9),(2,-8.5,270,9)]:
    for j in range(97):
        a=math.radians(a0+j*90/96);outline.append((cx+12*cos(a),cy+ry*sin(a)))
n=len(outline)
floor=mesh('A17_Floor_Ellipse',[(x,y,z) for z in [-.3,0] for x,y in outline],[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)],bpy.data.materials['Hall_Floor'])
for old,o in list(copies.items()):
    if old.name.startswith(('Hall_Floor_','Floor_DoorAprons','Hall_Walls_','Hall_Columns_')):remove(o)
# Continuous curved wall strips, with existing door openings retained.
vs=[];fs=[]
def wallstrip(p,q,zp=0,zq=0):
    dx=q[0]-p[0];dy=q[1]-p[1];l=math.hypot(dx,dy)
    if l<1e-8:return
    def norm(p):
        x,y=p
        if abs(x)>=13.9999:return (math.copysign(1,x),0)
        if abs(y+17.5)<1e-5:return (0,-1)
        if abs(y-17.5)<1e-5:return (0,1)
        cx=math.copysign(2,x);cy=5.5 if y>=0 else -8.5;ry=12 if y>=0 else 9
        nx=(x-cx)/144;ny=(y-cy)/(ry*ry);l=math.hypot(nx,ny);return(nx/l,ny/l)
    np,nq=norm(p),norm(q);k=len(vs)
    for side in [1,-1]:
        for pos,z,n in [(p,zp,np),(q,zq,nq),(q,9,nq),(p,9,np)]:vs.append((pos[0]+side*.12*n[0],pos[1]+side*.12*n[1],z))
    fs.extend([tuple(k+j for j in f) for f in [(0,1,2,3),(7,6,5,4),(3,2,6,7),(4,5,1,0)]])
for i,p in enumerate(outline):
    q=outline[(i+1)%n]
    if abs(p[0]-q[0])<1e-7 and abs(abs(p[0])-14)<1e-7:continue
    wallstrip(p,q)
for sign in [-1,1]:
    breaks=[-8.5,5.5]
    if sign<0:breaks += [-8.26,-6.46,-4.76,-2.96]
    else:breaks += [-5.44+2.045*cos(pi*j/64) for j in range(65)]
    breaks=sorted(set(breaks))
    for a,b in zip(breaks,breaks[1:]):
        mid=(a+b)/2
        def bottom(y):
            if sign<0:return 2.7 if (-8.26<mid<-6.46 or -4.76<mid<-2.96) else 0
            return 3.85+1.64*sqrt(max(0,1-((y+5.44)/2.045)**2)) if abs(mid+5.44)<2.045 else 0
        wallstrip((sign*14,a),(sign*14,b),bottom(a),bottom(b))
walls=mesh('A17_Walls_Ellipse_DoorOpenings',vs,fs,bpy.data.materials['Hall_Teal'],True)
# Keep four end columns and repurpose two of the removed corner columns at the side midpoints.
keep={3,5,11,13}; moves={2:(13.52,-.4),6:(-13.52,-.4)}
for old,o in list(copies.items()):
    if old.name.startswith('Column04A_'):
        idx=int(old.name.split('_')[1])
        if idx not in keep and idx not in moves:
            for child in list(o.children):remove(child)
            remove(o)
        elif idx in moves:
            x,y=moves[idx];o.location=(x,y,0);o.rotation_euler.z=0
    if old.name.startswith(('RadialArch_','ArchSupport_')):remove(o)
# Six roof ribs: shaped to the new roof with a short capital-to-roof transition.
column_positions=[(6.694645,16.162304),(-6.694645,16.162304),(-13.52,-.4),(-6.694645,-16.162304),(6.694645,-16.162304),(13.52,-.4)]
for idx,(x,y) in enumerate(column_positions,1):
    a=math.atan2(y,x);r0=math.hypot(x,y);R=radius(a);v=[];f=[]
    for j in range(97):
        t=j/96;r=r0+(1.4-r0)*t
        roof=9+7*sqrt(max(0,1-(r/R)**2))
        startroof=9+7*sqrt(max(0,1-(r0/R)**2))
        z=roof-.08-(startroof-.08-9)*max(0,1-t/.13)**2
        for w,h in [(-.32,0),(.32,0),(.32,-.60),(-.32,-.60)]:
            v.append((r*cos(a)-w*sin(a),r*sin(a)+w*cos(a),z+h))
    for j in range(96):
        for k in range(4):f.append((j*4+k,j*4+(k+1)%4,(j+1)*4+(k+1)%4,(j+1)*4+k))
    f += [(3,2,1,0),(384,385,386,387)]
    mesh('A17_RadialArch_P'+str(idx),v,f,bpy.data.materials['BBB_Column_Walnut'])
    # Small cap saddle between the column top and arch spring.
    mesh('A17_Capital_Saddle_P'+str(idx),[(x+dx,y+dy,z) for z in [8.84,9] for dx,dy in [(-.40,-.40),(.40,-.40),(.40,.40),(-.40,.40)]],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],bpy.data.materials['BBB_Column_Walnut'])
s['drawing_reference']='27_Hall_Ellipse_Guided_Wall.svg / A17'
s['lower_curve']='quarter ellipse rx12 ry9 center (+/-2,-8.5); footprint28x35'
s['column_count']=6;s['arch_count']=6
bpy.context.view_layer.update()
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_location=(0,0,3)
        area.spaces.active.region_3d.view_rotation=Quaternion((1,0,0),0)
        area.spaces.active.region_3d.view_distance=43
        area.spaces.active.region_3d.view_perspective='ORTHO'
        area.spaces.active.shading.type='MATERIAL'
print(json.dumps({'scene':s.name,'objects':len(s.objects),'columns':len([o for o in s.objects if o.name.startswith('Column04A_')]),'arches':len([o for o in s.objects if o.name.startswith('A17_RadialArch')])}))
