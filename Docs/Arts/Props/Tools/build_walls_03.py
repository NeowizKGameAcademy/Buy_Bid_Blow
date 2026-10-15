import bpy, math, bmesh
S=bpy.data.scenes.new('BBB_Walls_03_Geometry');bpy.context.window.scene=S
S.unit_settings.system='METRIC'
OUT='C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models/BBB_Walls_03_Geometry.blend'
for curved,off in [(False,-1.7),(True,1.7)]:
 prefix='03B_Curved' if curved else '03A_Straight'
 root=bpy.data.objects.new(prefix,None);S.collection.objects.link(root);root.location.x=off
 root['width_or_arc_length_m']=2.8;root['height_m']=3.0;root['radius_m']=5.0 if curved else 0
 root['exclusions']='No columns, flags, lanterns';root['front']='-Y'
 def strip(name,x0,x1,z0,z1,height,depth,y=0,bev=.006):
  n=max(1,math.ceil(abs(x1-x0)/.09)) if curved else 1
  vs=[]
  for i in range(n+1):
   f=i/n;x=x0+(x1-x0)*f;z=z0+(z1-z0)*f
   for d,h in [(-1,-1),(1,-1),(1,1),(-1,1)]:
    yy=y+d*depth/2;zz=z+h*height/2
    if curved:
     theta=x/5;xx=(5-yy)*math.sin(theta);yy=5-(5-yy)*math.cos(theta)
    else:xx=x
    vs.append((xx,yy,zz))
  fs=[(3,2,1,0),tuple(4*n+i for i in range(4))]+[(4*j+k,4*j+(k+1)%4,4*(j+1)+(k+1)%4,4*(j+1)+k) for j in range(n) for k in range(4)]
  me=bpy.data.meshes.new(prefix+'_'+name);me.from_pydata(vs,[],fs);me.update()
  bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
  ob=bpy.data.objects.new(me.name,me);S.collection.objects.link(ob);ob.parent=root
  if bev:
   mod=ob.modifiers.new('Soft edges','BEVEL');mod.width=bev;mod.segments=2
  uv=me.uv_layers.new(name='WallUV')
  for p in me.polygons:
   for li in p.loop_indices:
    v=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(v.x/2.8,v.z/3)
  return ob
 # Upper plaster infill and lower timber backing are separate material-ready meshes.
 strip('Upper_Panel',-1.4,1.4,1.92,1.92,1.88,.14,bev=.003)
 strip('Lower_Backing',-1.4,1.4,.52,.52,.90,.14,bev=.002)
 for tag,z,h,d in [('Plinth',.08,.16,.24),('Plinth_Upper',.19,.06,.20),('Dado_Rail',.99,.14,.22),('Dado_Lip',1.08,.04,.25),('Crown',2.90,.16,.24),('Crown_Lip',2.99,.02,.27)]:
  strip(tag,-1.4,1.4,z,z,h,d)
 # Front face wainscot boards, with fine shadow seams.
 for i in range(16):
  x=-1.4+2.8*i/16
  strip('LowerBoard_%02d'%i,x+.003,x+2.8/16-.003,.565,.565,.69,.025,-.083,.002)
 # Four repeated X-pattern bays, with no full-height posts.
 for j in range(4):
  x0=-1.4+j*.7+.055;x1=x0+.59
  strip('X_%d_A'%j,x0,x1,.27,.86,.065,.045,-.119,.003)
  strip('X_%d_B'%j,x0,x1,.86,.27,.065,.045,-.126,.003)
 for j in range(1,4):
  x=-1.4+j*.7
  strip('Dado_Divider_%d'%j,x-.025,x+.025,.565,.565,.69,.055,-.115,.003)
from mathutils import Vector
for a in bpy.context.screen.areas:
 if a.type=='VIEW_3D':
  a.spaces.active.shading.type='SOLID';a.spaces.active.overlay.show_overlays=False
  r=a.spaces.active.region_3d;r.view_rotation=(Vector((2,-9,4.6))-Vector((0,0,1.5))).to_track_quat('Z','Y');r.view_location=(0,0,1.5);r.view_distance=8.5;r.view_perspective='ORTHO'
bpy.data.libraries.write(OUT,{S},fake_user=True)
print(OUT,len(S.objects))
