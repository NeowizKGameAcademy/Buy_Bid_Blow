import bpy, math
from mathutils import Vector

OUT = 'C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Railings_Geometry.blend'
scene = bpy.data.scenes.new('BBB_Railings_Geometry')
bpy.context.window.scene = scene
scene.unit_settings.system = 'METRIC'

def mesh(name, vs, fs, parent, bevel=0.008):
    me=bpy.data.meshes.new(name); me.from_pydata(vs,[],fs); me.update()
    ob=bpy.data.objects.new(name,me); scene.collection.objects.link(ob); ob.parent=parent
    if bevel:
        mod=ob.modifiers.new('Soft crafted edges','BEVEL'); mod.width=bevel; mod.segments=2
    return ob

def box(name, center, size, parent, angle=0, bevel=.008):
    vs=[]
    for x,y,z in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]:
        x*=size[0]/2;y*=size[1]/2;z*=size[2]/2
        vs.append((center[0]+x*math.cos(angle)-y*math.sin(angle),center[1]+x*math.sin(angle)+y*math.cos(angle),center[2]+z))
    return mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],parent,bevel)

def plaque(name, outline, y, depth, parent, bevel=.003):
    n=len(outline);vs=[(x,y+dy,z) for dy in [-depth/2,depth/2] for x,z in outline]
    fs=[tuple(range(n-1,-1,-1)),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,vs,fs,parent,bevel)

for curved,offset in [(False,-1.4),(True,1.4)]:
    prefix='01B_Curved' if curved else '01A_Straight'
    root=bpy.data.objects.new(prefix,None);scene.collection.objects.link(root);root.location.x=offset
    root['post_center_span_m']=2.0;root['height_m']=1.25;root['curve_radius_m']=3.5 if curved else 0
    root['purpose']='Independent railing module; local floor-center pivot; no banner'
    def pos(s):
        return (3.5*math.sin(s/3.5),3.5*(1-math.cos(s/3.5)),s/3.5) if curved else (s,0,0)
    for end,s in [('L',-1),('R',1)]:
        x,y,a=pos(s)
        for tag,z,w,d,h in [('Foot',.09,.27,.29,.18),('LowerCollar',.205,.23,.25,.05),('Shaft',.63,.18,.20,.80),('UpperCollar',1.055,.245,.26,.10),('Cap',1.12,.27,.28,.05),('FinialSeat',1.16,.115,.115,.035)]:
            box(prefix+'_'+end+'_'+tag,(x,y,z),(w,d,h),root,a)
        # Low-poly rounded finial with real facets.
        vs=[];segments=12;rings=7
        for j in range(rings):
            t=math.pi*j/(rings-1)
            for i in range(segments):
                q=2*math.pi*i/segments
                vs.append((x+.083*math.sin(t)*math.cos(q),y+.083*math.sin(t)*math.sin(q),1.205+.045*math.cos(t)))
        fs=[(j*segments+i,j*segments+(i+1)%segments,(j+1)*segments+(i+1)%segments,(j+1)*segments+i) for j in range(rings-1) for i in range(segments)]
        mesh(prefix+'_'+end+'_Finial',vs,fs,root,0)
    for tag,z,depth,height in [('BottomRail',.25,.16,.14),('Handrail',1.035,.20,.13)]:
        vs=[];n=32 if curved else 1
        for i in range(n+1):
            s=-.94+1.88*i/n;x,y,a=pos(s)
            for d,h in [(-1,-1),(1,-1),(1,1),(-1,1)]:
                vs.append((x-d*depth/2*math.sin(a),y+d*depth/2*math.cos(a),z+h*height/2))
        fs=[(3,2,1,0),tuple(4*n+i for i in range(4))]+[(4*j+k,4*j+(k+1)%4,4*(j+1)+(k+1)%4,4*(j+1)+k) for j in range(n) for k in range(4)]
        mesh(prefix+'_'+tag,vs,fs,root)
    for i in range(7):
        x,y,a=pos(-.75+i*.25)
        box(prefix+'_Baluster_%02d'%i,(x,y,.635),(.075,.10,.67),root,a,.005)
for a in bpy.context.screen.areas:
    if a.type=='VIEW_3D':
        a.spaces.active.shading.type='SOLID'
        a.spaces.active.region_3d.view_rotation=(Vector((3,-7,3.3))-Vector((0,0,.65))).to_track_quat('Z','Y')
        a.spaces.active.region_3d.view_distance=6.7
        a.spaces.active.region_3d.view_location=(0,0,.65)
        a.spaces.active.region_3d.view_perspective='ORTHO'
bpy.data.libraries.write(OUT,{scene},fake_user=True)
print('CREATED',OUT,'objects',len(scene.objects))

