
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Joy.Tool
{
    /// <summary>
    /// 纹理映射烘焙过程
    /// </summary>
    public class JoyTextureMappingProcedure
    {
        // 烘焙输入参数
        private Camera m_Camera;
        private GameObject m_TargetObject;
        private int m_Width;
        private int m_Height;
        private int m_Margin;

        // 烘焙过程使用的材质Shader
        private readonly Material m_ImpostorPBRBaker;
        private readonly Material m_ImpostorMaskBaker;
        private readonly Material m_TexMappingBaker;
        private readonly Material m_BlitBaker;

        // Framebuffer开启Alpha通道，避免HDR下黑色背景
        private bool m_PreFramebufferAlphaSetting;
        // 烘焙目标缩放统一为1，缓存烘焙前的缩放数据
        private Vector3 m_PreTargetObjScale;
        // 缓存烘焙前相机参数
        private bool m_PreIsOrth;
        private float m_PreOrthSize;
        private RenderTexture m_PreRT;
        private Vector3 m_PreCameraAngles;
        private Vector3 m_PreCameraPosition;
        private int m_PreCullMask;
        private CameraClearFlags m_PreClearFlags;
        private Color m_PreBackgroundColor;
        private UniversalAdditionalCameraData m_URPData;
        private bool m_PrePostprocess;

        public JoyTextureMappingProcedure()
        {
            // PBR参数烘焙材质
            m_ImpostorPBRBaker = new Material(Shader.Find("Shader Graphs/ImposterPBRBaker"));
            // Mask参数烘焙材质
            m_ImpostorMaskBaker = new Material(Shader.Find("Shader Graphs/ImposterMaskBaker"));
            // 纹理映射材质
            m_TexMappingBaker = new Material(Shader.Find("Joy/Tool/TextureMappingShader"));
            // PBR和Mask参数通道混合处理材质
            m_BlitBaker = new Material(Shader.Find("Joy/Tool/JoyPBRMaskBlitShader"));
        }

        /// <summary>
        /// 3D模型烘焙2D纹理
        /// </summary>
        /// <param name="targetObject"></param>
        /// <param name="camera"></param>
        /// <param name="impostor2DMesh"></param>
        /// <param name="savePath"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="margin"></param>
        public void BakeTexture2D(GameObject targetObject, Camera camera, Mesh impostor2DMesh, string savePath, int width, int height, int margin)
        {
            m_Camera = camera;
            m_TargetObject = targetObject;
            m_Width = width;
            m_Height = height;
            m_Margin = margin;
            // 开始烘焙，初始化烘焙相机参数
            BeginBaker();
            InitCameraBakeState();
            // 烘焙模型2D纹理和PBR+Mask的参数贴图
            var model2DRT = RenderTexture.GetTemporary(m_Width * 4, m_Height * 4, 24, RenderTextureFormat.ARGB32);
            var pbrMaskRT = RenderTexture.GetTemporary(m_Width * 4, m_Height * 4, 24, RenderTextureFormat.ARGB32);
            BakeTextureByCamera(model2DRT, pbrMaskRT);
            // 纹理映射到Mesh的纹理坐标上
            var outputImpost2DRT = RenderTexture.GetTemporary(m_Width, m_Height, 24, RenderTextureFormat.ARGB32);
            TextureMappingMesh(model2DRT, impostor2DMesh, outputImpost2DRT, new Color(0.0f, 0.0f, 0.0f, 0.0f));
            var outputPBRMaskRT = RenderTexture.GetTemporary(m_Width, m_Height, 24, RenderTextureFormat.ARGB32);
            TextureMappingMesh(pbrMaskRT, impostor2DMesh, outputPBRMaskRT, new Color(1.0f, 1.0f, 1.0f, 0.0f));
            // 保存纹理
            SaveTextureToFile(outputImpost2DRT, outputPBRMaskRT, savePath, targetObject.name);
            // 释放RT
            RenderTexture.ReleaseTemporary(model2DRT);
            RenderTexture.ReleaseTemporary(pbrMaskRT);
            RenderTexture.ReleaseTemporary(outputImpost2DRT);
            RenderTexture.ReleaseTemporary(outputPBRMaskRT);
            // 结束烘焙
            EndBaker();
        }

        private void BeginBaker()
        {

#if UNITY_EDITOR
            // Framebuffer开启Alpha通道，避免黑色背景
            m_PreFramebufferAlphaSetting = UnityEditor.PlayerSettings.preserveFramebufferAlpha;
            UnityEditor.PlayerSettings.preserveFramebufferAlpha = true;
#endif
            // 烘焙目标缩放统一为1
            m_PreTargetObjScale = m_TargetObject.transform.localScale;
            m_TargetObject.transform.localScale = Vector3.one;
            // 缓存烘焙前相机参数
            m_PreIsOrth = m_Camera.orthographic;
            m_PreOrthSize = m_Camera.orthographicSize;
            m_PreRT = m_Camera.targetTexture;
            m_PreCameraPosition = m_Camera.transform.position;
            m_PreCameraAngles = m_Camera.transform.eulerAngles;
            m_PreCullMask = m_Camera.cullingMask;
            m_PreClearFlags = m_Camera.clearFlags;
            m_PreBackgroundColor = m_Camera.backgroundColor;
            m_URPData = m_Camera.GetUniversalAdditionalCameraData();
            m_PrePostprocess = m_URPData && m_URPData.renderPostProcessing;
        }

        private void EndBaker()
        {
            m_Camera.ResetAspect();
            if (m_URPData != null)
            {
                m_URPData.renderPostProcessing = m_PrePostprocess;
            }
            m_Camera.backgroundColor = m_PreBackgroundColor;
            m_Camera.clearFlags = m_PreClearFlags;
            m_Camera.cullingMask = m_PreCullMask;
            m_Camera.transform.eulerAngles = m_PreCameraAngles;
            m_Camera.transform.position = m_PreCameraPosition;
            m_Camera.targetTexture = m_PreRT;
            m_Camera.orthographicSize = m_PreOrthSize;
            m_Camera.orthographic = m_PreIsOrth;
            m_TargetObject.transform.localScale = m_PreTargetObjScale;
#if UNITY_EDITOR
            // 还原Framebuffer的Alpha设置
            UnityEditor.PlayerSettings.preserveFramebufferAlpha = m_PreFramebufferAlphaSetting;
#endif
            m_TargetObject = null;
            m_Camera = null;
        }

        private void InitCameraBakeState()
        {
            // 计算烘焙目标的包围盒
            var bounds = ComputeBounds(m_TargetObject);
            // 设置烘焙的相机参数
            m_Camera.orthographic = true;
            m_Camera.aspect = (float)m_Width / (float)m_Height;
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            if (m_URPData != null)
            {
                m_URPData.renderPostProcessing = false;
            }
            m_Camera.cullingMask = 1 << m_TargetObject.layer;
            // 调整相机(正交)的OrthographicSize和位置，以烘焙目标为中心
            OrthFrameBounds(m_Camera, bounds, m_Margin);
        }

        private void BakeTextureByCamera(RenderTexture model2DRT, RenderTexture pbrMaskRT)
        {
            // 烘焙2D纹理贴图
            m_Camera.targetTexture = model2DRT;
            m_Camera.Render();
            var renderers = m_TargetObject.GetComponentsInChildren<Renderer>();
            var preMaterials = new Material[renderers.Length];
            // 替换材质，输出PBR参数
            for (var i = 0; i < renderers.Length; ++i)
            {
                var preMaterial = renderers[i].sharedMaterial;
                // 拷贝原始纹理的贴图，这里输入金属度贴图和法线贴图
                if (renderers[i].sharedMaterial.shader.name.Equals("Custom/Lit"))
                {
                    var newMaterial = new Material(m_ImpostorPBRBaker);
                    newMaterial.SetTexture("MAES", preMaterial.GetTexture("_MetallicGlossMap"));
                    newMaterial.SetTexture("NormalMap", preMaterial.GetTexture("_BumpMap"));
                    renderers[i].sharedMaterial = newMaterial;
                }
                preMaterials[i] = preMaterial;
            }
            // 烘焙PBR相关参数贴图
            var pbrRT = RenderTexture.GetTemporary(pbrMaskRT.width, pbrMaskRT.height, 24, RenderTextureFormat.ARGB32);
            m_Camera.backgroundColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
            m_Camera.targetTexture = pbrRT;
            m_Camera.Render();
            // 替换材质，输出Mask参数
            for (var i = 0; i < renderers.Length; ++i)
            {
                // 拷贝原始纹理的贴图，这里输入Mask，用于渲染特定的阵营色
                if (preMaterials[i].shader.name.Equals("Custom/Lit"))
                {
                    var newMaterial = new Material(m_ImpostorMaskBaker);
                    newMaterial.SetTexture("Mask", preMaterials[i].GetTexture("_EnemyMaskMap"));
                    renderers[i].sharedMaterial = newMaterial;
                }
            }
            // 烘焙Mask相关参数贴图
            var maskRT = RenderTexture.GetTemporary(pbrMaskRT.width, pbrMaskRT.height, 24, RenderTextureFormat.ARGB32);
            m_Camera.targetTexture = maskRT;
            m_Camera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
            m_Camera.Render();

            // 混合PBR输出的RGB通道和Mask输出的Alpha通道
            var commandBuffer = CommandBufferPool.Get();
            m_BlitBaker.SetTexture("_BaseMap", pbrRT);
            m_BlitBaker.SetTexture("_MaskMap", maskRT);
            commandBuffer.Blit(pbrRT, new RenderTargetIdentifier(pbrMaskRT), m_BlitBaker); //(new RenderTargetIdentifier(pbrMaskRT), blitMaterial);
            Graphics.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
            // 还原原始材质
            for (var i = 0; i < renderers.Length; ++i)
            {
                renderers[i].sharedMaterial = preMaterials[i];
            }
            RenderTexture.ReleaseTemporary(pbrRT);
            RenderTexture.ReleaseTemporary(maskRT);
        }

        /// <summary>
        /// 3D模型的2D纹理映射到Impostor2D的Mesh上
        /// </summary>
        /// <param name="originRT"></param>
        /// <param name="impostor2DMesh"></param>
        /// <param name="outputRT"></param>
        private void TextureMappingMesh(RenderTexture originRT, Mesh impostor2DMesh, RenderTexture outputRT, Color backColor)
        {
            m_TexMappingBaker.SetTexture("_BaseMap", originRT);
            var commandBuffer = CommandBufferPool.Get();
            commandBuffer.name = "TextureMapping";
            var rti = new RenderTargetIdentifier(outputRT);
            commandBuffer.SetRenderTarget(rti);
            commandBuffer.ClearRenderTarget(true, true, backColor);
            commandBuffer.SetViewport(new Rect(0, 0, m_Width, m_Height));
            commandBuffer.SetViewMatrix(m_Camera.worldToCameraMatrix);
            commandBuffer.SetProjectionMatrix(m_Camera.projectionMatrix);
            commandBuffer.SetViewProjectionMatrices(m_Camera.worldToCameraMatrix, m_Camera.projectionMatrix);
            // 翻转Y参数，多渲染API验证，OpenGL不需要翻转，DX/Vulkan/Metal需要翻转Y
            var flipYParam = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2
                || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                ? 1 : -1;
            commandBuffer.SetGlobalVector("_ProjectionParams", new Vector4(flipYParam, m_Camera.nearClipPlane, m_Camera.farClipPlane, 1 / m_Camera.farClipPlane));
            commandBuffer.DrawMesh(impostor2DMesh, Matrix4x4.identity, m_TexMappingBaker);
            Graphics.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        /// <summary>
        /// 报错纹理到文件
        /// </summary>
        /// <param name="model2DOutputTex"></param>
        /// <param name="pbrMaskOutputTex"></param>
        /// <param name="savePath"></param>
        /// <param name="fileName"></param>
        private void SaveTextureToFile(RenderTexture model2DOutputTex, RenderTexture pbrMaskOutputTex, string savePath, string fileName)
        {
            var preRT = RenderTexture.active;
            // 拷贝RT到纹理
            var model2DTex = new Texture2D(model2DOutputTex.width, model2DOutputTex.height, TextureFormat.ARGB32, false, true);
            RenderTexture.active = model2DOutputTex;
            model2DTex.ReadPixels(new Rect(0, 0, model2DOutputTex.width, model2DOutputTex.height), 0, 0);
            model2DTex.Apply();
            RenderTexture.active = pbrMaskOutputTex;
            var pbrMaskTex = new Texture2D(pbrMaskOutputTex.width, pbrMaskOutputTex.height, TextureFormat.ARGB32, -1, true);
            pbrMaskTex.ReadPixels(new Rect(0, 0, pbrMaskOutputTex.width, pbrMaskOutputTex.height), 0, 0);
            pbrMaskTex.Apply();
            RenderTexture.active = preRT;
            // 保存Texture
            var model2DTexPath = Path.Combine(savePath, fileName + "_BaseMap.png");
            var pbrMaskTexPath = Path.Combine(savePath, fileName + "_Mask.png");
            // 写入Model2D纹理
            var bytes = model2DTex.EncodeToPNG();
            File.WriteAllBytes(model2DTexPath, bytes);
            // 写入PBR+Mask纹理
            bytes = pbrMaskTex.EncodeToPNG();
            File.WriteAllBytes(pbrMaskTexPath, bytes);
            UnityEditor.AssetDatabase.Refresh();
        }

        /// <summary>
        /// 计算烘焙目标的包围盒，遍历Render，累加Render.bounds
        /// </summary>
        /// <param name="targetObject"></param>
        /// <returns></returns>
        private static Bounds ComputeBounds(GameObject targetObject)
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

    /// <summary>
    /// 3D模型烘焙为2D贴图，纹理映射到Impostor的Mesh
    /// </summary>
    public class JoyTextureMappingTool : MonoBehaviour
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

        public void OdinButtonForTestFunc()
        {
            TestTextureMapping();
        }

        private void TestTextureMapping()
        {
            NewImpostorBake(512, 512, 0);
        }

        private void NewImpostorBake(int width, int height, int margin)
        {
            var mappingProcedure = new JoyTextureMappingProcedure();
            var path = UnityEditor.AssetDatabase.GetAssetPath(m_TexMappingMat);
            var folder = Path.GetDirectoryName(path);
            mappingProcedure.BakeTexture2D(m_Target, m_Camera, m_TexMappingMesh, folder, width, height, margin);
        }
    }
}