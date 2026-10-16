import bpy, math, json, os
from mathutils import Vector
from math import sin, cos, pi
BASE='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/RopeBarriers'
os.makedirs(BASE,exist_ok=True)
S=bpy.data.scenes.new('Rope_Barriers_AB_Untextured')
bpy.context.window.scene=S
S.unit_settings.system='METRIC'
S.unit_settings.scale_length=1
manifest=[]

def mesh(name,vs,fs,parent):
    me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update()
    o=bpy.data.objects.new(name,me);S.collection.objects.link(o);o.parent=parent
    return o

def lathe(name,profile,x,parent,n=48):
    vs=[(x+r*cos(2*pi*j/n),r*sin(2*pi*j/n),z) for z,r in profile for j in range(n)]
    fs=[(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j) for i in range(len(profile)-1) for j in range(n)]
    fs += [tuple(reversed(range(n))),tuple((len(profile)-1)*n+j for j in range(n))]
    o=mesh(name,vs,fs,parent)
    for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
    return o

def box(name,x,z,w,d,h,parent):
    vs=[(x+a*w/2,b*d/2,z+c*h/2) for a,b,c in [(-1,-1,-1),(1,-1,-1),(1,1,-1),(-1,1,-1),(-1,-1,1),(1,-1,1),(1,1,1),(-1,1,1)]]
    o=mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],parent)
    m=o.modifiers.new('Crafted_Edge','BEVEL');m.width=.008;m.segments=2
    return o

def tube(name,points,r,parent,sides=10):
    vs=[]
    for i,p in enumerate(points):
        t=(points[min(i+1,len(points)-1)]-points[max(i-1,0)]).normalized()
        u=Vector((0,1,0));v=t.cross(u).normalized()
        vs += [tuple(p+r*(u*cos(2*pi*j/sides)+v*sin(2*pi*j/sides))) for j in range(sides)]
    fs=[(i*sides+j,i*sides+(j+1)%sides,(i+1)*sides+(j+1)%sides,(i+1)*sides+j) for i in range(len(points)-1) for j in range(sides)]
    fs += [tuple(reversed(range(sides))),tuple((len(points)-1)*sides+j for j in range(sides))]
    o=mesh(name,vs,fs,parent)
    for p in o.data.polygons:p.use_smooth=len(p.vertices)==4
    return o

for kind,oy in [('A',0),('B',2.5)]:
    root=bpy.data.objects.new('Barrier_'+kind+'_ROOT',None);S.collection.objects.link(root);root.location.y=oy
    root['units']='meters';root['post_spacing']=1.6;root['physics']='Unity solver required; endpoint bones must follow anchors'
    for k,x in enumerate([-1.6,0,1.6]):
        pre=kind+'_Post_%02d_'%k
        if kind=='A':
            lathe(pre+'Base',[(0,.235),(.035,.235),(.045,.22),(.065,.22),(.075,.17),(.1,.16),(.15,.075),(.18,.06)],x,root)
            lathe(pre+'Shaft',[(.15,.049),(.2,.049),(.21,.037),(.95,.037),(.97,.055),(1,.055),(1.015,.04)],x,root)
            lathe(pre+'Finial',[(1.01,.035),(1.04,.027)]+[(1.115+.072*cos(pi-i*pi/24),.072*sin(pi-i*pi/24)) for i in range(25)],x,root)
        else:
            for j,(z,w,h) in enumerate([(.035,.36,.07),(.09,.32,.04),(.14,.27,.06),(.19,.21,.04),(.59,.155,.76),(.99,.21,.045),(1.035,.27,.045),(1.075,.22,.035)]):
                box(pre+'Tier_%02d'%j,x,z,w,w,h,root)
            for yy in [-.081,.081]:
                o=box(pre+'Inset',x,.59,.105,.008,.61,root)
                for v in o.data.vertices:v.co.y+=yy
            lathe(pre+'Finial_Base',[(1.09,.065),(1.115,.065),(1.13,.04)],x,root)
            lathe(pre+'Diamond',[(1.125,.018),(1.2,.085),(1.32,0)],x,root,n=4)
        for sign in [-1,1]:
            if (k==0 and sign<0) or (k==2 and sign>0):continue
            pts=[Vector((x+sign*(.065+.025*cos(2*pi*j/32)),0,.94+.045*sin(2*pi*j/32))) for j in range(33)]
            tube(pre+'Eye_'+str(sign),pts,.009,root,8)
    for span in range(2):
        prefix=kind+'_Rope_%02d'%span
        xa=-1.6+span*1.6+.115;xb=xa+1.37
        def center(t):return Vector((xa+(xb-xa)*t,0,.91-.34*4*t*(1-t)))
        anchors=[]
        for tag,t in [('Start',0),('End',1)]:
            a=bpy.data.objects.new(prefix+'_Anchor_'+tag,None);S.collection.objects.link(a);a.parent=root;a.location=center(t);a.empty_display_size=.06;anchors.append(a.name)
            direction=(center(.02)-center(0)).normalized() if t==0 else (center(.98)-center(1)).normalized()
            tube(prefix+'_Socket_'+tag,[center(t)-direction*.035,center(t)+direction*.07],.043,root,20)
        arm=bpy.data.armatures.new(prefix+'_Skeleton');rig=bpy.data.objects.new(prefix+'_Rig',arm);S.collection.objects.link(rig);rig.parent=root
        bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
        rb=arm.edit_bones.new('Root');rb.head=(0,0,0);rb.tail=(0,0,.1);rb.use_deform=False
        for i in range(13):
            b=arm.edit_bones.new('Rope_%02d'%i);b.head=center(i/12);b.tail=b.head+Vector((0,0,.055));b.parent=rb
        bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
        rig['fixed_bones']='Rope_00,Rope_12';rig['solver_order']=','.join('Rope_%02d'%i for i in range(13))
        strands=1 if kind=='A' else 3
        for strand in range(strands):
            pts=[]
            for j in range(97):
                t=j/96;p=center(t)
                if strands>1:
                    tangent=Vector((xb-xa,0,-1.36*(1-2*t))).normalized();v=tangent.cross(Vector((0,1,0)))
                    phase=t*2*pi*18+strand*2*pi/3;p+=.018*(Vector((0,1,0))*cos(phase)+v*sin(phase))
                pts.append(p)
            sides=12 if kind=='A' else 8
            o=tube(prefix+'_Velvet' if kind=='A' else prefix+'_Braid_%d'%strand,pts,.032 if kind=='A' else .016,root,sides)
            groups=[o.vertex_groups.new(name='Rope_%02d'%i) for i in range(13)]
            for j in range(97):
                f=j/96*12;lo=min(12,int(f));hi=min(12,lo+1);w=f-lo;idx=list(range(j*sides,(j+1)*sides))
                groups[lo].add(idx,1-w,'REPLACE')
                if hi!=lo and w>0:groups[hi].add(idx,w,'REPLACE')
            mod=o.modifiers.new('Unity_Skin','ARMATURE');mod.object=rig;mod.use_deform_preserve_volume=False
        manifest.append({'variant':kind,'rig':rig.name,'anchors':anchors,'fixed_bones':['Rope_00','Rope_12'],'ordered_particles':['Rope_%02d'%i for i in range(13)],'rest_positions_local':[list(center(i/12)) for i in range(13)]})

S['Unity_Notes']='No Blender cloth. Skinned meshes with 13 independent particle bones per span, parented to Root. Unity rope solver updates local translations, keeps rotations at bind pose, pins first/last to anchors, enforces rest lengths. No runtime physics included.'
target=Vector((0,1.1,.6));eye=Vector((4,-7,4))
for ar in bpy.context.screen.areas:
    if ar.type=='VIEW_3D':
        sp=ar.spaces.active;sp.region_3d.view_rotation=(target-eye).to_track_quat('-Z','Y');sp.region_3d.view_location=target;sp.region_3d.view_distance=8;sp.lens=50;sp.shading.type='SOLID';sp.overlay.show_overlays=False
with open(BASE+'/Rope_Barrier_Rig_Map.json','w',encoding='utf-8') as f:json.dump({'notes':S['Unity_Notes'],'spans':manifest},f,indent=2)
bpy.context.view_layer.update()
bpy.data.libraries.write(BASE+'/BBB_Rope_Barriers_AB_Rigged.blend',{S},path_remap='ABSOLUTE',fake_user=True,compress=True)
print('CREATED',len(S.objects),'objects; no materials; four rigged rope spans')
