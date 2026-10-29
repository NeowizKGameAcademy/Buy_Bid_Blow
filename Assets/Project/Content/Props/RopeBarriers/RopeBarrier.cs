using UnityEngine;
using UnityEngine.Rendering;

namespace BuyBidBlow.Props
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RopeBarrier : MonoBehaviour
    {
        public Transform startAnchor, endAnchor;
        public Vector3[] restPoints;
        [Min(.1f)] public float ropeLength = 2f;
        [Range(.005f,.1f)] public float radius = .03f;
        public CapsuleCollider[] contacts = new CapsuleCollider[0];
        Vector3[] points, vertices, normals;
        RopeBarrierSimulation simulation;
        Transform[] bones;
        Mesh mesh;
        const int Sides = 8;
        public float PinError { get; private set; }
        public float SegmentError { get; private set; }
        public Vector3 Midpoint => points == null ? transform.position : points[points.Length / 2];
        // 로프 점·본·시뮬레이션과 런타임 튜브 메시를 한 번만 생성
        public void Initialize()
        {
            if (points != null) return;
            points = new Vector3[restPoints.Length];
            for (int i = 0; i < points.Length; i++) points[i] = transform.TransformPoint(restPoints[i]);
            simulation = GetComponent<RopeBarrierSimulation>();
            if (!simulation) simulation = gameObject.AddComponent<RopeBarrierSimulation>();
            simulation.drivenExternally=true; simulation.sway=true; simulation.ropeLength=ropeLength;
            bones=new Transform[points.Length];
            for(int i=0;i<bones.Length;i++){var bone=transform.Find("Rope_"+i.ToString("00"));if(!bone)bone=new GameObject("Rope_"+i.ToString("00")).transform;bone.SetParent(transform);bone.position=points[i];bones[i]=bone;}
            simulation.placements=new[]{new RopeBarrierSimulation.Placement{bones=bones,start=startAnchor,end=endAnchor}};
            simulation.Initialize();
            mesh = new Mesh { name = "RuntimeRope", indexFormat = IndexFormat.UInt32 }; mesh.MarkDynamic();
            vertices = new Vector3[points.Length * Sides]; normals = new Vector3[vertices.Length];
            var uv = new Vector2[vertices.Length]; var triangles = new int[(points.Length - 1) * Sides * 6];
            for (int i = 0; i < points.Length; i++) for (int j = 0; j < Sides; j++)
            {
                uv[i * Sides + j] = new Vector2(j / (float)Sides, i / (float)(points.Length - 1) * ropeLength);
                if (i == points.Length - 1) continue;
                int a = i * Sides + j, b = i * Sides + (j + 1) % Sides, c = a + Sides, d = b + Sides, k = (i * Sides + j) * 6;
                triangles[k] = a; triangles[k+1] = c; triangles[k+2] = b; triangles[k+3] = b; triangles[k+4] = c; triangles[k+5] = d;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles; GetComponent<MeshFilter>().sharedMesh = mesh; UpdateMesh();
        }
        // 기준 점이 충분하면 초기화
        void Awake() { if (restPoints != null && restPoints.Length > 1) Initialize(); }
        void FixedUpdate() => Simulate(Time.fixedDeltaTime);
        // 시뮬레이션을 한 스텝 진행하고 점 위치·오차를 갱신한 뒤 메시 업데이트
        public void Simulate(float dt)
        {
            if (!startAnchor || !endAnchor || restPoints == null || restPoints.Length < 2) return;
            Initialize();
            // The rope reacts to every player's body unless contacts were assigned explicitly.
            simulation.ropeLength=ropeLength; simulation.contacts=contacts.Length>0?contacts:PlayerContacts.Get(); simulation.contactRadius=radius;
            simulation.Step(Mathf.Min(dt,.04f));
            for(int i=0;i<points.Length;i++)points[i]=bones[i].position;
            PinError=simulation.MaxPinError;SegmentError=simulation.MaxSegmentError;
            UpdateMesh();
        }
        // 로프 점을 따라 튜브 메시 정점과 법선 재계산
        void UpdateMesh()
        {
            for(int i=0;i<points.Length;i++)
            {
                var tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;
                var a=Vector3.Cross(tangent,Vector3.up).normalized;if(a.sqrMagnitude<.1f)a=Vector3.right;var b=Vector3.Cross(tangent,a).normalized;
                for(int j=0;j<Sides;j++){float angle=j*Mathf.PI*2/Sides;var n=a*Mathf.Cos(angle)+b*Mathf.Sin(angle);vertices[i*Sides+j]=transform.InverseTransformPoint(points[i]+n*radius);normals[i*Sides+j]=transform.InverseTransformDirection(n);}
            }
            mesh.vertices=vertices;mesh.normals=normals;mesh.RecalculateBounds();
        }
        // 런타임 생성 메시 파기
        void OnDestroy(){if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}
