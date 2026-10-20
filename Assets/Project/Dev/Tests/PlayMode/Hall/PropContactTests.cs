using System.Collections;
using BuyBidBlow.Props;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PropContactTests
{
    [UnityTest]
    public IEnumerator BannerTracksLatePlayersAndRetainsSupports()
    {
        var banner = new GameObject("Contact test banner");
        var support = new GameObject("Support");
        var first = new GameObject("Late player");
        var second = new GameObject("Second player");
        var ropeObject = new GameObject("Contact test rope");
        var end = new GameObject("Rope end");
        var mesh = new Mesh
        {
            vertices = new[] { Vector3.zero, Vector3.right, Vector3.up },
            triangles = new[] { 0, 1, 2 }
        };
        try
        {
            banner.AddComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
            var cloth = banner.AddComponent<Cloth>();
            var wind = banner.AddComponent<BannerWind>();
            var fixedBody = support.AddComponent<CapsuleCollider>();
            wind.supportColliders = new[] { fixedBody };
            wind.SetContacts(new CapsuleCollider[0]);
            end.transform.position = Vector3.right;
            var rope = ropeObject.AddComponent<RopeBarrier>();
            rope.startAnchor = ropeObject.transform;
            rope.endAnchor = end.transform;
            rope.restPoints = new Vector3[13];
            for (int i = 0; i < 13; i++) rope.restPoints[i] = Vector3.right * (i / 12f);
            rope.contacts = new[] { fixedBody };
            yield return null;

            first.tag = second.tag = "Player";
            var firstBody = first.AddComponent<CapsuleCollider>();
            var secondBody = second.AddComponent<CapsuleCollider>();
            yield return new WaitForSecondsRealtime(.35f);
            CollectionAssert.IsSubsetOf(new[] { fixedBody, firstBody, secondBody }, cloth.capsuleColliders);
            rope.Simulate(.02f);
            var simulation = ropeObject.GetComponent<RopeBarrierSimulation>();
            CollectionAssert.IsSubsetOf(new[] { fixedBody, firstBody, secondBody }, simulation.contacts);

            first.SetActive(false);
            yield return new WaitForSecondsRealtime(.35f);
            CollectionAssert.DoesNotContain(cloth.capsuleColliders, firstBody);
            CollectionAssert.IsSubsetOf(new[] { fixedBody, secondBody }, cloth.capsuleColliders);
            rope.Simulate(.02f);
            CollectionAssert.DoesNotContain(simulation.contacts, firstBody);
            CollectionAssert.IsSubsetOf(new[] { fixedBody, secondBody }, simulation.contacts);

            first.SetActive(true);
            yield return new WaitForSecondsRealtime(.35f);
            CollectionAssert.IsSubsetOf(new[] { fixedBody, firstBody, secondBody }, cloth.capsuleColliders);
            rope.Simulate(.02f);
            CollectionAssert.IsSubsetOf(new[] { fixedBody, firstBody, secondBody }, simulation.contacts);
        }
        finally
        {
            Object.DestroyImmediate(banner);
            Object.DestroyImmediate(support);
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(ropeObject);
            Object.DestroyImmediate(end);
        }
    }
}
