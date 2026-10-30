using System;
using System.IO;
using System.Linq;
using System.Text;
using BuyBidBlow.Props;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BuyBidBlow.Dev.Editor
{
    public static class HallCollisionAudit
    {
        public static void VerifyBatch()
        {
            try
            {
                EditorSceneManager.OpenScene(HallLevelBuilder.ScenePath);
                Physics.SyncTransforms();
                var log = new StringBuilder();
                var colliders = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var collider in colliders)
                    if (collider is MeshCollider mesh && !mesh.sharedMesh)
                        throw new Exception("Missing collision mesh: " + collider.name);
                var doors = UnityEngine.Object.FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (doors.Length != 6) throw new Exception("Expected six doors, got " + doors.Length);
                foreach (var door in doors)
                {
                    var boxes = door.GetComponentsInChildren<BoxCollider>(true);
                    if (boxes.Length != 1) throw new Exception("Expected one moving door box: " + door.name);
                    var box = boxes[0];
                    var rotation = door.transform.localRotation;
                    var closedCenter = box.transform.TransformPoint(box.center);
                    try
                    {
                        door.transform.localRotation = rotation * Quaternion.Euler(0, door.openAngle, 0);
                        Physics.SyncTransforms();
                        var moved = Vector3.Distance(closedCenter, box.transform.TransformPoint(box.center));
                        if (moved < .1f) throw new Exception("Door collision did not follow hinge: " + door.name);
                        log.AppendLine("PASS door box follows hinge: " + door.name + " center movement=" + moved);
                    }
                    finally { door.transform.localRotation = rotation; }
                }
                Physics.SyncTransforms();
                log.AppendLine("Colliders including inactive: " + colliders.Length);
                log.AppendLine("PASS all MeshCollider references resolve; six moving doors retain independent boxes.");
                log.AppendLine("Scene was not saved. Character motor movement and staircase behavior were not tested.");
                File.WriteAllText("Docs/Arts/Environments/Collision/CleanupUnityVerification.txt", log.ToString());
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
