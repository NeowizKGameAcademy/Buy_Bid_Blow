"""Export curated solid furniture bodies, before render meshes lose their hierarchy.

Run in the approved hall Blender file. This reads source geometry without modifying it.
Decorative trim, foliage, paper, wall art and rugs intentionally do not block players.
Curved counters retain their concave outline and the staff knee/access spaces.
"""
import bpy, bmesh, json, os, math
from mathutils import Vector

BASE = 'C:/course/unity/BuyBidBlow'
OUT = BASE + '/Docs/Arts/Environments/Collision'
os.makedirs(OUT, exist_ok=True)
convert = lambda v: [float(v[0]), float(v[2]), float(v[1])]
visible = [o for o in bpy.context.scene.objects if o.visible_get() and not o.hide_render and o.type == 'MESH']

def category(o):
    p = o.parent.name if o.parent else ''
    n = o.name
    if p.startswith('Desk_') and any(k in n for k in ('FrontCore','Plinth','CustomerLedge','StaffWorktop','Cabinet','Stile')):
        return p
    if p.startswith('End_') and any(k in n for k in ('End_IvoryPanel','End_Top','End_Plinth','GatePost')):
        return p
    if p.startswith('Staff_Gate_') and n.startswith(('GateRail','GateStile')):
        return p
    if p == '01_Auction_Rostrum_ROOT' and any(k in n for k in ('Front_Board','Side_Panel','_Post','_Plinth','Lower_Shelf','Rear_Shelf')):
        return p
    if p == '02_Lot_Pedestal_ROOT' and not any(k in n for k in ('Banner','Emblem','Corner_Cap')):
        return p
    if p == 'A86_Direction_Sign_ROOT' and any(k in n for k in ('Low_Base','Slim_Post','Border_')):
        return p
    if p in ('Round_Table_A_ROOT','Round_Table_B_ROOT') and any(k in n for k in ('Top_Edge','Green_Marble_Top','Fluted_Pedestal','RA_Base','Tapered_Leg','Curved_Stretcher')) and 'Bead' not in n:
        return p
    if p.startswith('BBB_Areca_Card_Planter_ROOT') and 'LargePalm_Brass_Pot' in n:
        return p
    return None

groups = {}
for o in visible:
    name = category(o)
    if not name: continue
    g = groups.setdefault(name, dict(name=name, sources=[], vertices=[], triangles=[], samples=[]))
    g['sources'].append(o.name)
    # Use the modeling cage: render bevels and decorative modifiers are unnecessary
    # in a static collision mesh. Dissolve only nearly coplanar edges (1 degree).
    bm = bmesh.new(); bm.from_mesh(o.data); bm.transform(o.matrix_world)
    if name.startswith(('Round_Table','BBB_Areca')):
        # Each selected part is a convex solid: ignore fluting and the inside of
        # the pot, keeping the tabletop and legs as separate hulls.
        hull = bmesh.ops.convex_hull(bm, input=list(bm.verts), use_existing_faces=False)
        remove = list(set(hull['geom_interior'] + hull['geom_unused']))
        if remove: bmesh.ops.delete(bm, geom=remove, context='VERTS')
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(1), verts=list(bm.verts), edges=list(bm.edges))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    # Zero-area triangles do not define a collision surface.
    degenerate = [f for f in bm.faces if f.calc_area() < 1e-10]
    if degenerate:
        bmesh.ops.delete(bm, geom=degenerate, context='FACES_ONLY')
    bm.verts.ensure_lookup_table(); bm.verts.index_update()
    offset = len(g['vertices']) // 3
    g['vertices'] += [c for v in bm.verts for c in convert(v.co)]
    for f in bm.faces:
        a,b,c = [v.index + offset for v in f.verts]
        g['triangles'] += [a,c,b]
    # Independent surface samples for pre/post repair physics coverage checks.
    faces = sorted(bm.faces, key=lambda f: f.calc_area(), reverse=True)
    for f in faces[:6]:
        if f.calc_area() < .002: continue
        g['samples'].append(dict(source=o.name, point=convert(f.calc_center_median()), normal=convert(f.normal)))
    bm.free()

# Audit existing authored seats, tables, pots and architecture without guessing
# from the spatially merged Unity renderer names.
audit=[]
for o in visible:
    n=o.name; p=o.parent.name if o.parent else ''
    if not (any(k in n for k in ('Hall_Steps','Hall_Landing','Hall_Mezzanine','A18_Floor','A18_Walls','Column04','LargePalm_Brass_Pot')) or p.startswith(('A90_','A91_')) or 'Round_Table' in p): continue
    if any(k in n for k in ('Leaf','Foliage','Rachis','Cane','Stem','Soil','Bead')): continue
    points=[o.matrix_world@Vector(c) for c in o.bound_box]
    lo=[min(p[i] for p in points) for i in range(3)]; hi=[max(p[i] for p in points) for i in range(3)]
    audit.append(dict(name=n, parent=p, center=convert([(a+b)/2 for a,b in zip(lo,hi)]),size=convert([b-a for a,b in zip(lo,hi)])))

result=dict(source=bpy.data.filepath,groups=list(groups.values()),audit=audit)
with open(OUT+'/FurnitureCollision.json','w') as f: json.dump(result,f,indent=2)
print('Exported',len(groups),'collision groups;',sum(len(g['triangles'])//3 for g in groups.values()),'triangles')
for g in groups.values():print(g['name'],len(g['sources']),'bodies',len(g['triangles'])//3,'triangles')
