using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BuyBidBlow.Rendering
{
    // A single horizontal floor and one game camera. Requests run outside SRP callbacks:
    // SubmitRenderRequest cannot recursively render from beginCameraRendering in Unity 6.
    [ExecuteAlways, DefaultExecutionOrder(10000), DisallowMultipleComponent]
    public sealed class PlanarFloorReflection : MonoBehaviour
    {
        public Camera sourceCamera;
        public Renderer[] coatings = Array.Empty<Renderer>();
        public float floorHeight;
        [Range(.1f, 1)] public float resolutionScale = .5f;
        [Range(128, 2048)] public int maximumDimension = 1024;
        [Range(1, 60)] public int updatesPerSecond = 30;
        [Min(1)] public float reflectionDistance = 45;
        [Min(.001f)] public float clipOffset = .025f;
        public LayerMask reflectionLayers = ~0;
        public bool reflectShadows = true;
        public bool updateInEditMode;

        public int RenderCount { get; private set; }
        public double LastSubmissionMilliseconds { get; private set; }
        public RenderTexture ReflectionTexture => texture;

        Camera reflectionCamera;
        RenderTexture texture;
        MaterialPropertyBlock properties;
        double nextUpdate;
        static bool rendering;
        static readonly int TextureId = Shader.PropertyToID("_PlanarTexture");
        static readonly int MatrixId = Shader.PropertyToID("_PlanarVP");
        static readonly int ValidId = Shader.PropertyToID("_PlanarValid");

        void LateUpdate()
        {
            if (!Application.isPlaying && !updateInEditMode) return;
            if (Time.realtimeSinceStartupAsDouble < nextUpdate) return;
            Camera source = sourceCamera ? sourceCamera : Camera.main;
            if (!source || !source.isActiveAndEnabled) { SetValid(false); return; }
            RenderFor(source);
            nextUpdate = Time.realtimeSinceStartupAsDouble + 1.0 / Mathf.Max(1, updatesPerSecond);
        }

        public void RenderFor(Camera source)
        {
            if (rendering || !isActiveAndEnabled || !source || source == reflectionCamera) return;
            SetValid(false);
            // XR requires separate per-eye projections; don't silently use a mono reflection.
            if (source.stereoEnabled || source.transform.position.y <= floorHeight + clipOffset
                || source.cameraType == CameraType.Reflection || coatings.Length == 0) return;
            bool visible = false;
            var planes = GeometryUtility.CalculateFrustumPlanes(source);
            foreach (var receiver in coatings)
                if (receiver && receiver.enabled && receiver.gameObject.activeInHierarchy
                    && (source.cullingMask & (1 << receiver.gameObject.layer)) != 0
                    && GeometryUtility.TestPlanesAABB(planes, receiver.bounds)) { visible = true; break; }
            if (!visible) return;

            EnsureResources(source);
            reflectionCamera.CopyFrom(source);
            reflectionCamera.enabled = false;
            // URP reads UniversalAdditionalCameraData only for Game/VR cameras.
            // This disabled, private camera uses a standalone request, never automatic rendering.
            reflectionCamera.cameraType = CameraType.Game;
            reflectionCamera.targetTexture = texture;
            reflectionCamera.rect = new Rect(0, 0, 1, 1);
            reflectionCamera.aspect = source.aspect;
            reflectionCamera.allowHDR = true;
            reflectionCamera.allowMSAA = false;
            reflectionCamera.useOcclusionCulling = false;
            reflectionCamera.cullingMask = source.cullingMask & reflectionLayers.value & ~(1 << 5);
            reflectionCamera.farClipPlane = Mathf.Min(source.farClipPlane, reflectionDistance);
            var data = reflectionCamera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false; // Main camera applies grading/bloom once.
            data.renderShadows = reflectShadows;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.antialiasing = AntialiasingMode.None;
            data.renderType = CameraRenderType.Base;

            Matrix4x4 mirror = Matrix4x4.identity;
            mirror.m11 = -1;
            mirror.m13 = 2 * floorHeight;
            reflectionCamera.transform.SetPositionAndRotation(mirror.MultiplyPoint(source.transform.position),
                Quaternion.LookRotation(mirror.MultiplyVector(source.transform.forward), mirror.MultiplyVector(source.transform.up)));
            reflectionCamera.worldToCameraMatrix = source.worldToCameraMatrix * mirror;
            // Reset projection after CopyFrom so reflectionDistance actually limits the frustum.
            reflectionCamera.ResetProjectionMatrix();
            var worldPoint = new Vector3(0, floorHeight + clipOffset, 0);
            Vector3 point = reflectionCamera.worldToCameraMatrix.MultiplyPoint(worldPoint);
            Vector3 normal = reflectionCamera.worldToCameraMatrix.MultiplyVector(Vector3.up).normalized;
            var clipPlane = new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(point, normal));
            reflectionCamera.projectionMatrix = reflectionCamera.CalculateObliqueMatrix(clipPlane);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = texture };
            if (!RenderPipeline.SupportsRenderRequest(reflectionCamera, request)) return;

            bool previousCulling = GL.invertCulling;
            var hiddenStates = new bool[coatings.Length];
            var timer = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                rendering = true;
                for (int i = 0; i < coatings.Length; i++)
                    if (coatings[i]) { hiddenStates[i] = coatings[i].forceRenderingOff; coatings[i].forceRenderingOff = true; }
                GL.invertCulling = !previousCulling;
                RenderPipeline.SubmitRenderRequest(reflectionCamera, request);
                texture.GenerateMips();
                Matrix4x4 vp = GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix, true) * reflectionCamera.worldToCameraMatrix;
                properties ??= new MaterialPropertyBlock();
                foreach (var receiver in coatings)
                {
                    if (!receiver) continue;
                    receiver.GetPropertyBlock(properties);
                    properties.SetTexture(TextureId, texture);
                    properties.SetMatrix(MatrixId, vp);
                    properties.SetFloat(ValidId, 1);
                    receiver.SetPropertyBlock(properties);
                }
                RenderCount++;
            }
            finally
            {
                GL.invertCulling = previousCulling;
                for (int i = 0; i < coatings.Length; i++) if (coatings[i]) coatings[i].forceRenderingOff = hiddenStates[i];
                rendering = false;
                timer.Stop();
                LastSubmissionMilliseconds = timer.Elapsed.TotalMilliseconds;
            }
        }

        void EnsureResources(Camera source)
        {
            if (!reflectionCamera)
            {
                var go = new GameObject("Planar Floor Camera") { hideFlags = HideFlags.HideAndDontSave };
                reflectionCamera = go.AddComponent<Camera>();
                reflectionCamera.enabled = false;
                go.AddComponent<UniversalAdditionalCameraData>();
            }
            int sourceWidth = Mathf.Max(1, source.pixelWidth), sourceHeight = Mathf.Max(1, source.pixelHeight);
            float scale = Mathf.Min(Mathf.Clamp(resolutionScale, .1f, 1), Mathf.Max(128, maximumDimension) / (float)Mathf.Max(sourceWidth, sourceHeight));
            int width = Mathf.Max(32, Mathf.RoundToInt(sourceWidth * scale));
            int height = Mathf.Max(32, Mathf.RoundToInt(sourceHeight * scale));
            if (texture && texture.width == width && texture.height == height) return;
            ReleaseTexture();
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
                ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            texture = new RenderTexture(width, height, 24, format, RenderTextureReadWrite.Linear)
            {
                name = "Planar Floor HDR", hideFlags = HideFlags.HideAndDontSave,
                useMipMap = true, autoGenerateMips = false, filterMode = FilterMode.Trilinear,
                wrapMode = TextureWrapMode.Clamp, antiAliasing = 1
            };
            texture.Create();
        }

        void SetValid(bool valid)
        {
            properties ??= new MaterialPropertyBlock();
            foreach (var receiver in coatings)
            {
                if (!receiver) continue;
                receiver.GetPropertyBlock(properties);
                properties.SetFloat(ValidId, valid ? 1 : 0);
                receiver.SetPropertyBlock(properties);
            }
        }
        void ReleaseTexture()
        {
            if (!texture) return;
            if (reflectionCamera) reflectionCamera.targetTexture = null;
            texture.Release();
            Dispose(texture); texture = null;
        }
        void OnDisable()
        {
            SetValid(false); ReleaseTexture();
            if (reflectionCamera) Dispose(reflectionCamera.gameObject);
            reflectionCamera = null;
        }
        static void Dispose(UnityEngine.Object value)
        {
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
    }
}
