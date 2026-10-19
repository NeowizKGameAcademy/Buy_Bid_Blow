using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BuyBidBlow.Hall.Editor
{
    public static class HallBannerMountVerification
    {
        const string Key="BBB_Mounted_Banner_Check";
        static Cloth cloth;static HallBannerWind wind;static Vector3[] initial;static float start,maxMovement,maxPin,maxPenetration;
        [InitializeOnLoadMethod]static void Restore(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(!SessionState.GetBool(Key,false))
            {
                if(!File.Exists("Logs/banner-verify-request.txt")||EditorApplication.isPlayingOrWillChangePlaymode)return;
                File.Delete("Logs/banner-verify-request.txt");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;return;
            }
            if(!EditorApplication.isPlaying)return;
            try
            {
                if(!cloth)
                {
                    wind=UnityEngine.Object.FindFirstObjectByType<HallBannerWind>();cloth=wind.GetComponent<Cloth>();
                    initial=cloth.vertices;start=Time.time;return;
                }
                if(cloth.capsuleColliders.Length<3 || wind.supportColliders.Any(c=>!cloth.capsuleColliders.Contains(c)))throw new Exception("Player registration replaced beam contacts");
                var vertices=cloth.vertices;var coeff=cloth.coefficients;
                for(int i=0;i<vertices.Length;i++)
                {
                    var p=vertices[i];if(!float.IsFinite(p.sqrMagnitude))throw new Exception("Nonfinite vertex");
                    float move=Vector3.Distance(initial[i],p);
                    if(coeff[i].maxDistance==0)maxPin=Mathf.Max(maxPin,move);else maxMovement=Mathf.Max(maxMovement,move);
                    var world=cloth.transform.TransformPoint(p);
                    foreach(var c in wind.supportColliders)
                    {
                        var center=c.transform.TransformPoint(c.center);var axis=c.transform.up;var half=c.height*.5f-c.radius;
                        var q=center+axis*Mathf.Clamp(Vector3.Dot(world-center,axis),-half,half);
                        maxPenetration=Mathf.Max(maxPenetration,c.radius-Vector3.Distance(world,q));
                    }
                }
                if(Time.time-start<12)return;
                if(maxPin>.002f || maxMovement<.01f || maxPenetration>.02f)throw new Exception($"pin={maxPin} motion={maxMovement} penetration={maxPenetration}");
                Directory.CreateDirectory("Docs/Arts/Environments/UnityVerification");
                File.WriteAllText("Docs/Arts/Environments/UnityVerification/Banner_Mount_Verification.txt",$"PASS 12 seconds Play Mode\nCloth movement {maxMovement:F4}m\nPinned drift {maxPin:F6}m\nMaximum support penetration {maxPenetration:F6}m\nBoth beam colliders retained alongside player capsule.\n");
                var preview=new GameObject("Banner Verification Camera").AddComponent<Camera>();preview.CopyFrom(Camera.main);
                HallLevelVerification.Capture(preview,new Vector3(0,7.9f,5.5f),new Vector3(0,9.65f,14.8f),"Banner_Mounted");
                UnityEngine.Object.Destroy(preview.gameObject);Finish("PASS");
            }
            catch(Exception e){Finish(e.ToString());Debug.LogException(e);}
        }
        static void Finish(string result)
        {
            File.WriteAllText("Logs/banner-verify-result.txt",result);SessionState.SetBool(Key,false);EditorApplication.update-=Tick;
            EditorApplication.isPlaying=false;
        }
    }
}
