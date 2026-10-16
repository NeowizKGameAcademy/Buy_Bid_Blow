using System;
using System.Linq;
using UnityEngine;

namespace BuyBidBlow.Dev.RopeBarrier
{
    public class RopeBarrierDemo : MonoBehaviour
    {
        [Range(1, 2.6f)] public float postDistance = 1.6f;
        [Range(1, 3.5f)] public float ropeLength = 1.65f;
        public bool sway = true;
        public float EffectiveLength { get; private set; }
        public float MaxPinError { get; private set; }
        public float MaxSegmentError { get; private set; }
        public float Sag { get; private set; }
        class Span { public Transform[] bones; public Transform a,b; public Vector3[] p,prev; }
        class Move { public Transform t; public Vector3 original; public int post; }
        Span[] spans; Move[] moves; float lastDistance=-1, lastLength=-1, clock;
        public void Initialize()
        {
            var all=GetComponentsInChildren<Transform>();
            spans=all.Where(t=>t.name.EndsWith("_Rig")).Select(r=> {
                string prefix=r.name.Substring(0,r.name.Length-4);
                return new Span { bones=r.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Rope_")).OrderBy(t=>t.name).ToArray(),
                    a=all.Single(t=>t.name==prefix+"_Anchor_Start"),b=all.Single(t=>t.name==prefix+"_Anchor_End") };
            }).ToArray();
            if(spans.Length!=2 || spans.Any(s=>s.bones.Length!=13)) throw new Exception("Expected two spans with 13 bones");
            moves=all.Where(t=>t.name.Contains("_Post_") || t.name.Contains("_Socket_") || t.name.Contains("_Anchor_")).Select(t=> {
                float x=t.position.x-transform.position.x;
                var mf=t.GetComponent<MeshFilter>();
                if(mf) x=t.TransformPoint(mf.sharedMesh.bounds.center).x-transform.position.x;
                return new Move { t=t,original=t.localPosition,post=Mathf.RoundToInt(x/1.6f) };
            }).ToArray();
            foreach(var s in spans) {s.p=new Vector3[13];s.prev=new Vector3[13];}
            ResetRopes();
        }
        void Start() { if(spans==null) Initialize(); }
        public void ResetRopes()
        {
            foreach(var m in moves) m.t.localPosition=m.original+m.t.parent.InverseTransformVector(Vector3.right*(postDistance-1.6f)*m.post);
            foreach(var s in spans) for(int i=0;i<13;i++) {
                float t=i/12f; s.p[i]=Vector3.Lerp(s.a.position,s.b.position,t)+Vector3.down*(.25f*4*t*(1-t));s.prev[i]=s.p[i];
            }
            lastDistance=postDistance;lastLength=ropeLength;
        }
        void FixedUpdate() { Step(Time.fixedDeltaTime); }
        public void Step(float dt)
        {
            if(spans==null) Initialize();
            if(lastDistance!=postDistance || lastLength!=ropeLength) ResetRopes();
            clock+=dt;MaxPinError=0;MaxSegmentError=0;Sag=0;
            foreach(var s in spans) {
                float distance=Vector3.Distance(s.a.position,s.b.position);
                float length=Mathf.Max(ropeLength,distance*1.005f);EffectiveLength=length;
                float rest=length/12;
                for(int i=1;i<12;i++) {var p=s.p[i];s.p[i]+=(p-s.prev[i])*.985f+(Vector3.down*9.81f+(sway?Vector3.forward*Mathf.Sin(clock*2)*.8f:Vector3.zero))*dt*dt;s.prev[i]=p;}
                for(int it=0;it<100;it++) {
                    s.p[0]=s.a.position;s.p[12]=s.b.position;
                    for(int j=0;j<12;j++) {int i=(it%2==0)?j:11-j;var d=s.p[i+1]-s.p[i];float mag=d.magnitude;if(mag<.000001f)continue;var correction=d*((mag-rest)/mag);
                        if(i==0)s.p[1]-=correction;else if(i==11)s.p[11]+=correction;else{s.p[i]+=correction*.5f;s.p[i+1]-=correction*.5f;}}
                }
                s.p[0]=s.a.position;s.p[12]=s.b.position;
                for(int i=0;i<13;i++) {if(!float.IsFinite(s.p[i].sqrMagnitude))throw new Exception("Nonfinite rope");s.bones[i].position=s.p[i];}
                for(int i=0;i<12;i++)MaxSegmentError=Mathf.Max(MaxSegmentError,Mathf.Abs(Vector3.Distance(s.p[i],s.p[i+1])-rest));
                MaxPinError=Mathf.Max(MaxPinError,Vector3.Distance(s.bones[0].position,s.a.position),Vector3.Distance(s.bones[12].position,s.b.position));
                Sag=Mathf.Max(Sag,(s.a.position.y+s.b.position.y)*.5f-s.p.Min(p=>p.y));
            }
        }
        void OnGUI()
        {
            if(name!="Barrier_A_Test")return;
            GUILayout.BeginArea(new Rect(15,15,340,240),GUI.skin.box);
            GUILayout.Label("Rope Barrier A / B — live test");
            GUILayout.Label($"Post spacing: {postDistance:F2} m");postDistance=GUILayout.HorizontalSlider(postDistance,1,2.6f);
            GUILayout.Label($"Rope length: {ropeLength:F2} m");ropeLength=GUILayout.HorizontalSlider(ropeLength,1,3.5f);
            sway=GUILayout.Toggle(sway,"Sway");
            GUILayout.Label($"Effective length: {EffectiveLength:F2} m (clamped to span)");
            if(GUILayout.Button("Reset"))ResetRopes();
            foreach(var other in FindObjectsByType<RopeBarrierDemo>(FindObjectsSortMode.None))if(other!=this){other.postDistance=postDistance;other.ropeLength=ropeLength;other.sway=sway;}
            GUILayout.EndArea();
        }
    }
}
