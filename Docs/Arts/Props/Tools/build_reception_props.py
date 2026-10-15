import bpy, math, bmesh, json
from mathutils import Vector,Matrix
from pathlib import Path
OUT=Path('C:/course/unity/BuyBidBlow/Docs/Arts/Props/Models')
# Preserve the current user's hall edits in a separate recoverable file.
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'BBB_Hall_Working_Backup_BeforeReceptionProps.blend'),copy=True)
scene=bpy.data.scenes.new('BBB_Reception_Props_Geometry')
bpy.context.window.scene=scene
roots=[]
def asset(name,pos):
 global root,col
 col=bpy.data.collections.new(name);scene.collection.children.link(col)
 root=bpy.data.objects.new(name+'_ROOT',None);col.objects.link(root);root.location=pos
 root['asset_name']=name;root['units']='meters';roots.append(root)
def mesh(name,vs,fs):
 m=bpy.data.meshes.new(root.name[:-5]+'_'+name);m.from_pydata(vs,[],fs);m.update()
 o=bpy.data.objects.new(m.name,m);col.objects.link(o);o.parent=root
 return o
def box(name,c,sz,bev=.003):
 x,y,z=[v/2 for v in sz]
 vs=[(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
 o=mesh(name,vs,[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]);o.location=c
 if bev:
  b=o.modifiers.new('Soft edges','BEVEL');b.width=min(bev,min(sz)*.24);b.segments=2
 return o
def lathe(name,c,prof,n=24,closed=False):
 vs=[(r*math.cos(i*2*math.pi/n),r*math.sin(i*2*math.pi/n),z) for r,z in prof for i in range(n)]
 fs=[]
 for j in range(len(prof) if closed else len(prof)-1):
  k=(j+1)%len(prof)
  for i in range(n):fs.append((j*n+i,j*n+(i+1)%n,k*n+(i+1)%n,k*n+i))
 if not closed:fs += [tuple(reversed(range(n))),tuple((len(prof)-1)*n+i for i in range(n))]
 o=mesh(name,vs,fs);o.location=c;return o
def rod(name,a,b,r,n=12):
 a,b=Vector(a),Vector(b);o=lathe(name,a,[(r,0),(r,(b-a).length)],n)
 o.rotation_mode='QUATERNION';o.rotation_quaternion=(b-a).to_track_quat('Z','Y');return o
def cable(name,points,r=.003):
 for i in range(len(points)-1):rod(name+'_%02d'%i,points[i],points[i+1],r,8)
def textmesh(name,string,c,size,rotation=(math.pi/2,0,0)):
 cu=bpy.data.curves.new(name,'FONT');cu.body=string;cu.size=size;cu.extrude=.0006
 temp=bpy.data.objects.new(name,cu);col.objects.link(temp);temp.parent=root;temp.location=c;temp.rotation_euler=rotation
 dg=bpy.context.evaluated_depsgraph_get();me=bpy.data.meshes.new_from_object(temp.evaluated_get(dg))
 ob=bpy.data.objects.new(root.name[:-5]+'_'+name,me);col.objects.link(ob);ob.parent=root;ob.matrix_basis=temp.matrix_basis.copy()
 bpy.data.objects.remove(temp,do_unlink=True);bpy.data.curves.remove(cu)
 return ob
asset('01_Employee_Monitor',(-2.4,1.8,0))
box('Base',(0,0,.013),(.25,.18,.026),.01)
box('Stand',(0,.034,.13),(.045,.045,.23),.008)
box('RearHousing',(0,.035,.35),(.55,.05,.325),.012)
box('Screen',(0,.005,.35),(.512,.006,.288),.002)
for x in [-.266,.266]:box('SideBezel',(x,-.001,.35),(.018,.012,.322))
for z in [.195,.505]:box('HorizontalBezel',(0,-.001,z),(.55,.012,.018))
box('RearElectronics',(0,.072,.32),(.25,.034,.17),.009)
for i in range(18):box('Vent',(-.17+i*.02,.061,.474),(.012,.003,.008),.001)
box('RearPorts',(0,.091,.27),(.085,.008,.025))
cable('PowerCable',[(.03,.094,.265),(.05,.11,.14),(.08,.14,.03),(.16,.23,.008),(.30,.22,.008)])
asset('02_Keyboard',(-1.45,1.8,0))
box('Shell',(0,0,.014),(.445,.145,.028),.01)
for row in range(5):
 for j in range(14):
  if row==4 and 3<=j<=7:continue
  box('Key_%d_%d'%(row,j),(-.205+j*.027,.055-row*.025,.034),(.023,.021,.013),.003)
box('Spacebar',(-.056,-.045,.034),(.132,.021,.013))
cable('Cable',[(.08,.072,.012),(.09,.12,.009),(.15,.18,.007),(.26,.19,.007)])
asset('03_Mouse',(-.65,1.8,0))
# Low-poly rounded shell formed by elliptical rings.
vs=[];fs=[];N=24
for z,s in [(0,.75),(.01,1),(.029,.95),(.042,.55),(.045,.05)]:
 for i in range(N):
  a=i*2*math.pi/N;vs.append((.032*s*math.cos(a),.055*s*math.sin(a),z))
for j in range(4):
 for i in range(N):fs.append((j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i))
fs += [tuple(reversed(range(N))),tuple(4*N+i for i in range(N))]
mesh('Shell',vs,fs)
box('ButtonSeam',(0,.018,.041),(.0015,.055,.002),0)
rod('ScrollWheel',(-.004,.022,.042),(.004,.022,.042),.009,12)
cable('Cable',[(0,.05,.017),(0,.1,.01),(.04,.16,.006),(.13,.18,.006)],.002)
asset('04_Receipt_Printer',(.15,1.8,0))
box('Body',(0,0,.09),(.17,.20,.18),.016)
box('Lid',(0,.005,.184),(.164,.18,.025),.008)
box('PaperExit',(0,-.058,.201),(.105,.012,.008))
box('Button',(.054,-.100,.13),(.018,.004,.018))
for x in [-.06,.06]:
 for y in [-.07,.07]:box('Foot',(x,y,.004),(.025,.025,.008))
# Curved receipt strip with thickness.
pts=[(-.058,.203),(-.035,.245),(-.006,.275),(.024,.286),(.054,.289)]
vs=[(x,y,z) for dz in [0,.0007] for y,z0 in pts for x in [-.045,.045] for z in [z0+dz]]
fs=[]
for j in range(4):fs.extend([(2*j,2*j+1,2*j+3,2*j+2),(10+2*j,12+2*j,13+2*j,11+2*j)])
fs.extend([(0,10,11,1),(8,9,19,18)])
for j in range(4):fs.extend([(2*j,2*j+2,12+2*j,10+2*j),(2*j+1,11+2*j,13+2*j,2*j+3)])
mesh('Receipt',vs,fs)
box('PowerSocket',(-.045,.102,.04),(.024,.004,.018))
cable('PowerCable',[(-.045,.105,.04),(-.06,.14,.025),(-.03,.22,.008),(.06,.23,.007)])
asset('05_Staff_Chair',(1.55,1.75,0))
lathe('GasLift',(0,0,0),[(.06,.09),(.06,.13),(.034,.15),(.034,.42)])
for i in range(5):
 a=2*math.pi*i/5;end=(.29*math.cos(a),.29*math.sin(a),.07)
 rod('Spoke',(0,0,.13),end,.023)
 rod('CasterStem',end,(end[0],end[1],.045),.012)
 # Paired wheels across horizontal axle.
 for off in [-.018,.018]:
  rod('CasterWheel',(end[0]+off-.007,end[1],.039),(end[0]+off+.007,end[1],.039),.038,16)
box('SeatFrame',(0,0,.445),(.47,.46,.045),.025)
box('SeatCushion',(0,-.01,.488),(.46,.45,.065),.04)
box('BackSupport',(0,.205,.68),(.12,.035,.46),.015)
back=box('BackFrame',(0,.223,.795),(.455,.06,.47),.04);back.rotation_euler[0]=math.radians(-6)
back=box('BackCushion',(0,.187,.795),(.42,.045,.43),.035);back.rotation_euler[0]=math.radians(-6)
for x in [-.285,.285]:
 rod('ArmPost',(x,.07,.455),(x,.07,.67),.018)
 box('Armrest',(x,-.015,.682),(.065,.30,.045),.015)
rod('AdjustmentLever',(.09,0,.42),(.27,-.06,.40),.009)
box('LeverGrip',(.28,-.065,.40),(.06,.035,.018))
asset('06_Document_Trays',(-2.4,.45,0))
for k in range(3):
 z=.012+k*.08
 box('TrayFloor',(0,0,z),(.255,.335,.012))
 for x in [-.126,.126]:box('Side',(x,0,z+.022),(.012,.335,.045))
 box('Back',(0,.16,z+.022),(.255,.012,.045))
 for x in [-.086,.086]:box('FrontLip',(x,-.16,z+.014),(.073,.012,.027))
 for j in range(4):box('Paper',(0,-.004,z+.009+j*.0013),(.215,.28,.001),.0002)
for x in [-.134,.134]:
 for y in [-.16,.16]:lathe('Post',(x,y,0),[(.011,0),(.011,.23),(.016,.234),(.01,.245)],12)
asset('07_Bound_Ledger',(-1.35,.45,0))
box('Pages',(0,0,.023),(.195,.275,.033),.002)
for z in [.003,.043]:box('Cover',(0,0,z),(.22,.29,.008),.003)
box('Spine',(-.107,0,.023),(.012,.29,.045),.005)
for x in [-.097,.097]:
 for y in [-.132,.132]:
  box('CornerHorizontal',(x,y,.048),(.024,.008,.003),.001)
  box('CornerVertical',(x,y,.048),(.008,.026,.003),.001)
for z in [.01,.015,.02,.025,.03,.035]:box('PageEdge',(0,-.138,z),(.19,.001,.0004),0)
asset('08_Order_Mat_Clipboard',(-.45,.45,0))
box('Mat',(0,0,.005),(.48,.34,.01),.014)
box('MatInset',(0,0,.011),(.457,.318,.002),.012)
box('Clipboard',(0,0,.017),(.235,.30,.012),.007)
box('OrderSheet',(0,-.003,.024),(.21,.276,.001),.001)
box('Clip',(0,.129,.032),(.075,.025,.014))
rod('ClipHinge',(-.039,.14,.033),(.039,.14,.033),.007)
for j in range(8):box('FormRule',(0,-.105+j*.025,.0247),(.18,.0005,.0003),0)
asset('09_Pen_Cup',(.5,.45,0))
lathe('Cup',(0,0,0),[(.033,0),(.04,.005),(.04,.012),(.034,.018),(.034,.105),(.031,.111),(.028,.111),(.028,.01)],32,True)
# Pens are independently parented under their cup; also a separate pen asset below.
for i in range(3):
 x=(i-1)*.014
 rod('PenBarrel',(x,0,.02),(x+.018,.006,.16+i*.009),.0045)
 rod('PenCap',(x+.018,.006,.152+i*.009),(x+.019,.006,.166+i*.009),.005)
asset('10_Pen',(.95,.45,0))
lathe('Pen',(0,0,0),[(.0008,0),(.004,.022),(.0045,.115),(.005,.116),(.005,.145),(.004,.15)],16)
rod('Clip',(.005,0,.141),(.005,0,.115),.0012,8)
asset('11_Stamp_InkPad',(1.45,.45,0))
lathe('Stamp',(-.055,0,0),[(.031,0),(.033,.008),(.035,.012),(.035,.025),(.023,.03),(.012,.043),(.014,.063),(.024,.088),(.026,.10),(.018,.115),(.006,.12)],32)
box('InkCase',(.045,0,.009),(.075,.065,.018),.005)
box('InkSurface',(.045,0,.019),(.062,.052,.003),.003)
lid=box('Lid',(.045,.061,.035),(.075,.007,.065),.004);lid.rotation_euler[0]=math.radians(-20)
rod('Hinge',(.01,.034,.015),(.08,.034,.015),.004)
asset('12_Service_Bell',(2.15,.45,0))
lathe('Base',(0,0,0),[(.055,0),(.064,.007),(.064,.017),(.057,.023)],40)
lathe('Dome',(0,0,0),[(.058,.021),(.060,.027),(.054,.044),(.043,.060),(.025,.072),(.007,.076)],40)
lathe('Plunger',(0,0,0),[(.006,.072),(.006,.092),(.017,.095),(.017,.10),(.015,.103)],24)
for idx,(label,x) in enumerate([('STRAW',-2.4),('WOOD',-1.3),('BRICK',-.2)]):
 asset('13_Sign_'+label,(x,-.85,0))
 face=box('FrontPlate',(0,0,.064),(.32,.012,.115),.006);face.rotation_euler[0]=math.radians(-12)
 box('Base',(0,.024,.008),(.33,.10,.016),.004)
 rear=box('RearSupport',(0,.055,.052),(.30,.009,.10),.003);rear.rotation_euler[0]=math.radians(30)
 for z in [.013,.115]:box('FrameHorizontal',(0,-.010,z),(.325,.014,.010))
 for xx in [-.158,.158]:box('FrameVertical',(xx,-.01,.064),(.010,.014,.112))
 # Raised readable lettering facing customer -Y.
 textmesh('Label',label,(-.116,-.019,.045),.041)
 rot=Matrix.Rotation(math.radians(-12),3,'X');pivot=Vector((0,0,.064))
 for o in root.children:
  if any(s in o.name for s in ['FrameHorizontal','FrameVertical','Label']):
   o.location=pivot+rot@(o.location-pivot);o.rotation_euler=(rot@o.rotation_euler.to_matrix()).to_euler()
asset('14_Emblem_Straw',(1,-.85,0))
for i in range(13):
 a=(i-6)*.15;top=(.16*math.sin(a),0,.37+.04*math.cos(a));bottom=(.065*math.sin(a),0,.02)
 rod('Stem',bottom,(0,0,.16),.007,8);rod('Stem',(0,0,.16),top,.008,8)
box('Binding',(0,-.009,.157),(.105,.029,.042),.006)
for z in [.146,.168]:box('BindingRidge',(0,-.026,z),(.11,.009,.006))
asset('15_Emblem_Wood',(1.7,-.85,0))
for x,z in [(-.068,.075),(.068,.075),(0,.195)]:
 rod('Log',(x,.025,z),(x,-.025,z),.067,24)
 for rad in [.024,.044,.059]:
  # End-grain rings on front, cylinder axes along Y.
  o=lathe('GrowthRing',(x,-.027,z),[(rad-.0015,-.001),(rad+.0015,-.001),(rad+.0015,.001),(rad-.0015,.001)],32,True);o.rotation_euler[0]=math.pi/2
asset('16_Emblem_Brick',(2.4,-.85,0))
for row in range(3):
 count=3-row
 for j in range(count):box('Brick',((j-(count-1)/2)*.108,0,.035+row*.07),(.10,.025,.062),.006)
# Consistent normals and mesh-only reusable assets.
for o in scene.objects:
 if o.type=='MESH':
  bm=bmesh.new();bm.from_mesh(o.data);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);bm.free()
scene['description']='Reception props geometry only; no materials. One common geometry per color variant. Independent root empties, meter units.'
bpy.context.view_layer.update()
manifest=[]
for r in roots:
 obs=[o for o in r.children_recursive if o.type=='MESH']
 manifest.append({'asset':r['asset_name'],'root':r.name,'mesh_parts':len(obs)})
(OUT/'BBB_Reception_Props_Geometry_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
path=str(OUT/'BBB_Reception_Props_Geometry.blend')
bpy.data.libraries.write(path,{scene},fake_user=True)
print('Created',len(roots),'assets;',sum(o.type=='MESH' for o in scene.objects),'meshes;',path)

