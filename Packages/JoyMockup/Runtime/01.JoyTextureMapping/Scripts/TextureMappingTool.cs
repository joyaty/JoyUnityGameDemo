
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Joy.Tool
{
    /// <summary>
    /// 3D模型烘焙为2D贴图，纹理映射到Impostor的Mesh
    /// </summary>
    public class TextureMappingTool : MonoBehaviour
    {
        /// <summary>
        /// 纹理映射材质
        /// </summary>
        [SerializeField]
        private Material m_TexMappingMat;

        /// <summary>
        /// 纹理映射网格
        /// </summary>
        [SerializeField]
        private Mesh m_TexMappingMesh;

        /// <summary>
        /// 烘焙相机
        /// </summary>
        [SerializeField]
        private Camera m_Camera;

        /// <summary>
        /// 烘焙的目标GameObject
        /// </summary>
        [SerializeField]
        private GameObject m_Target;

        /// <summary>
        /// 3D模型烘焙为2D贴图的结果展示
        /// </summary>
        [SerializeField]
        private RawImage m_RetModel2DTexure;
        /// <summary>
        /// 最终纹理映射的结果展示
        /// </summary>
        [SerializeField]
        private RawImage m_RetFinal;
        /// <summary>
        /// 最终纹理映射的结果展示(线框模式)
        /// </summary>
        [SerializeField]
        private RawImage m_RetFinaWireFrame;

        // [Sirenix.OdinInspector.Button("测试功能")]
        public void OdinButtonForTestFunc()
        {
            TestTextureMapping();
        }

        private void TestTextureMapping()
        {
            ImpostorBake(m_Target, m_Camera, 512, 512, 0);
        }

        /// <summary>
        /// 烘焙ImpostorMesh的纹理贴图
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="camera"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="margin"></param>
        private void ImpostorBake(GameObject targetObject, Camera camera, int width, int height, int margin)
        {
#if UNITY_EDITOR
            // Framebuffer开启Alpha通道，避免黑色背景
            var preFramebufferAlphaSetting = UnityEditor.PlayerSettings.preserveFramebufferAlpha;
            UnityEditor.PlayerSettings.preserveFramebufferAlpha = true;
#endif
            // 烘焙目标缩放统一为1
            var preTargetObjScale = targetObject.transform.localScale;
            targetObject.transform.localScale = Vector3.one;
            // 缓存烘焙前相机参数
            var preRT = camera.targetTexture;
            var preCameraAngles = camera.transform.eulerAngles;
            var preCullMask = camera.cullingMask;
            var preClearFlags = camera.clearFlags;
            var preBackgroundColor = camera.backgroundColor;
            var urpData = camera.GetUniversalAdditionalCameraData();
            var prePostprocess = urpData && urpData.renderPostProcessing;
            // 计算烘焙目标的包围盒
            var bounds = ComputeBounds(targetObject);
            // 设置烘焙的相机参数
            camera.aspect = (float)width / (float)height;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            if (urpData != null)
            {
                urpData.renderPostProcessing = false;
            }
            camera.cullingMask = 1 << targetObject.layer;
            // 调整相机(正交)的OrthographicSize和位置，以烘焙目标为中心
            OrthFrameBounds(camera, bounds, margin);
            // 3D模型烘焙为2D贴图
            var model2DTextureRT = RenderTexture.GetTemporary(width * 4, height * 4, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = model2DTextureRT;
            camera.Render();
            camera.targetTexture = preRT;
            // 还原烘焙目标的Scale
            targetObject.transform.localScale = preTargetObjScale;
            // 预览3D模型烘焙为2D贴图的结果
            m_RetModel2DTexure.texture = model2DTextureRT;

            //3D模型的2D纹理映射到Impostor2D的Mesh上
            GL.wireframe = true;
            GL.Color(Color.red);
            var wireCommandBuffer = CommandBufferPool.Get();
            wireCommandBuffer.name = "TextureMappingWireFrame";
            var wireFrameRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var wirerti = new RenderTargetIdentifier(wireFrameRT);
            wireCommandBuffer.SetRenderTarget(wirerti);
            wireCommandBuffer.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            wireCommandBuffer.SetViewport(new Rect(0, 0, width, height));
            wireCommandBuffer.SetViewMatrix(camera.worldToCameraMatrix);
            wireCommandBuffer.SetProjectionMatrix(camera.projectionMatrix);
            wireCommandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            // 经过不同图形API测试，OpenGL不需要翻转Y，DX11/Vulkan/Metal都需要翻转Y
            int flipY = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                ? 1 : -1;
            wireCommandBuffer.SetGlobalVector("_ProjectionParams", new Vector4(flipY, camera.nearClipPlane, camera.farClipPlane, 1 / camera.farClipPlane));
            wireCommandBuffer.DrawMesh(m_TexMappingMesh, Matrix4x4.identity, m_TexMappingMat);
            Graphics.ExecuteCommandBuffer(wireCommandBuffer);
            GL.wireframe = false;
            m_RetFinaWireFrame.texture = wireFrameRT;
            m_TexMappingMat.SetTexture("_BaseMap", model2DTextureRT);
            var commandBuffer = CommandBufferPool.Get();
            commandBuffer.name = "TextureMapping";
            var finalRT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var rti = new RenderTargetIdentifier(finalRT);
            commandBuffer.SetRenderTarget(rti);
            commandBuffer.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            commandBuffer.SetViewport(new Rect(0, 0, width, height));
            commandBuffer.SetViewMatrix(camera.worldToCameraMatrix);
            commandBuffer.SetProjectionMatrix(camera.projectionMatrix);
            commandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            commandBuffer.SetGlobalVector("_ProjectionParams", new Vector4(flipY, camera.nearClipPlane, camera.farClipPlane, 1 / camera.farClipPlane));
            commandBuffer.DrawMesh(m_TexMappingMesh, Matrix4x4.identity, m_TexMappingMat);
            Graphics.ExecuteCommandBuffer(commandBuffer);
            m_RetFinal.texture = finalRT;

            // 烘焙结束，还原相机参数
            camera.cullingMask = preCullMask;
            if (urpData != null)
            {
                urpData.renderPostProcessing = prePostprocess;
            }
            camera.backgroundColor = preBackgroundColor;
            camera.clearFlags = preClearFlags;
            camera.ResetAspect();
            camera.transform.eulerAngles = preCameraAngles;
#if UNITY_EDITOR
            // 还原Framebuffer的Alpha设置
            UnityEditor.PlayerSettings.preserveFramebufferAlpha = preFramebufferAlphaSetting;
#endif

            // 拷贝RT到纹理
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
            RenderTexture.active = finalRT;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            // 保存Texture
            var path = UnityEditor.AssetDatabase.GetAssetPath(m_TexMappingMat);
            var folder = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var texPath = Path.Combine(folder, name + ".png");
            var bytes = texture.EncodeToPNG();
            File.WriteAllBytes(texPath, bytes);
            UnityEditor.AssetDatabase.Refresh();
        }

        /// <summary>
        /// 计算烘焙目标的包围盒，遍历Render，累加Render.bounds
        /// </summary>
        /// <param name="targetObject"></param>
        /// <returns></returns>
        private Bounds ComputeBounds(GameObject targetObject)
        {
            var bounds = new Bounds();
            var renderers = targetObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                { 
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return bounds;
        }

        /// <summary>
        /// 根据烘焙目标的包围盒，调整相机的orthographicSize和位置
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="bounds"></param>
        /// <param name="marginSize"></param>
        private static void OrthFrameBounds(Camera camera, Bounds bounds, float marginSize)
        {
            var max = bounds.max;
            var min = bounds.min;
            var verts = new Vector3[]
            {
                min,
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(min.x, max.y, min.z),
                max,
                new Vector3(min.x, max.y, max.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(max.x, min.y, max.z),
            };
            var boundInView = new Bounds();
            for (var i = 0; i < verts.Length; ++i)
            {
                var v = camera.transform.InverseTransformPoint(verts[i]);
                boundInView.Encapsulate(v);
            }

            var xSizeWithMargin = boundInView.extents.x + marginSize;
            var ySizeWithMarginFromX = xSizeWithMargin / camera.aspect;
            var ySizeWithMargin = boundInView.extents.y + marginSize;
            camera.orthographicSize = Mathf.Max(ySizeWithMargin, ySizeWithMarginFromX);
            var offset = boundInView.center;
            offset.z = 0.0f;
            camera.transform.Translate(offset, Space.Self);
        }

    }
}