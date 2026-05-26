using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UIFramework.UGUI
{
    [ExecuteInEditMode, DisallowMultipleComponent]
    public class ImagePlusPlus : Image
    {
        [System.Serializable]
        public enum RoundingMode
        {
            Relative,
            Absolute
        }

        [System.Serializable]
        public enum CornerMode
        {
            None,
            RoundedLocked,
            RoundedIndependant
        }

        // Shader properties
        private static readonly string _shaderKeyword_RoundedCorners = "UNITY_UI_ROUNDED_CORNERS";
        private static readonly string _shaderKeyword_GradientFill = "UNITY_UI_GRADIENT_FILL";

        private static readonly int _shaderProp_roundedCorners = Shader.PropertyToID("_RoundedCorners");
        private static readonly int _shaderProp_radii = Shader.PropertyToID("_Radii");
        private static readonly int _shaderProp_sizeAndBorder = Shader.PropertyToID("_SizeAndBorder");
        private static readonly int _shaderProp_borderColor = Shader.PropertyToID("_BorderColor");
        private static readonly int _shaderProp_gradientFill = Shader.PropertyToID("_GradientFill");
        private static readonly int _shaderProp_gradientColor = Shader.PropertyToID("_GradientColor");
        private static readonly int _shaderProp_gradientAngle = Shader.PropertyToID("_GradientAngle");

        public static Shader Shader
        {
            get
            {
                if (_shader == null)
                {
                    _shader = Shader.Find("UI/Default++");                    
                }
                if(_shader == null)
                {
                    throw new Exception("Unable to find image plus plus shader.");
                }
                return _shader;
            }
        }
        private static Shader _shader = null;

        // Editor Fields
        [SerializeField] private bool _autoCreateMaterial = true;
        [SerializeField] private CornerMode _cornerMode = CornerMode.None;
        [SerializeField] private RoundingMode _roundingMode = RoundingMode.Relative;
        [SerializeField] private float _lockedCornerRadius = 0.5F;
        [SerializeField] private Vector4 _independantCornerRadii = new Vector4(0.5F, 0.5F, 0.5F, 0.5F);
        [SerializeField] private float _border = 0.0F;
        [SerializeField] private Color _borderColor = Color.black;
        [SerializeField] private bool _gradientFill = false;
        [SerializeField] private Color _gradientColor = Color.white;
        [SerializeField, Range(0, 90)] private float _gradientAngle = 0.0F;

        // Internally serialized properties
        [SerializeField] private Vector4 _cornerRadii = Vector4.zero;

        public override Material materialForRendering
        {
            get
            {
                var components = UnityEngine.Pool.ListPool<IMaterialModifier>.Get();
                GetComponents(components);

                Material currentMat = material;
                for (var i = 0; i < components.Count; i++)
                {
                    IMaterialModifier materialModifier = components[i];
                    Material temp = materialModifier.GetModifiedMaterial(currentMat);
                    currentMat = temp;
                }
                UnityEngine.Pool.ListPool<IMaterialModifier>.Release(components);
                return currentMat;
            }
        }

        /// <summary>
        /// See IMaterialModifier.GetModifiedMaterial
        /// </summary>
        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            Material modifiedMaterial = baseMaterial;

            if (m_ShouldRecalculateStencil)
            {
                if (maskable)
                {
                    var rootCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
                    m_StencilValue = MaskUtilities.GetStencilDepth(transform, rootCanvas);
                }
                else
                    m_StencilValue = 0;

                m_ShouldRecalculateStencil = false;
            }

            // This operates under the assumption that each ImagePlusPlus object already utilises a unique material
            //if (m_StencilValue > 0 && !isMaskingGraphic)
            //{
            //    SetStencilProperties(modifiedMaterial, (1 << m_StencilValue) - 1, StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
            //}
            //else
            //{
            //    SetStencilProperties(modifiedMaterial, 0, 0, (CompareFunction)8, (ColorWriteMask)15, 255, 255);
            //}

            // if we have a enabled Mask component then it will
            // generate the mask material. This is an optimization
            // it adds some coupling between components though :(
            if (m_StencilValue > 0 && !isMaskingGraphic)
            {
                var maskMat = StencilMaterial.Add(modifiedMaterial, (1 << m_StencilValue) - 1, StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                modifiedMaterial = m_MaskMaterial;
            }
            return modifiedMaterial;
        }

        private void SetStencilProperties(Material stencilMat, int stencilID, StencilOp operation, CompareFunction compareFunction, ColorWriteMask colorWriteMask, int readMask, int writeMask)
        {
            bool useAlphaClip = operation != StencilOp.Keep && writeMask > 0;

            stencilMat.SetFloat("_Stencil", (float)stencilID);
            stencilMat.SetFloat("_StencilOp", (float)operation);
            stencilMat.SetFloat("_StencilComp", (float)compareFunction);
            stencilMat.SetFloat("_StencilReadMask", (float)readMask);
            stencilMat.SetFloat("_StencilWriteMask", (float)writeMask);
            stencilMat.SetFloat("_ColorMask", (float)colorWriteMask);
            stencilMat.SetFloat("_UseUIAlphaClip", useAlphaClip ? 1.0f : 0.0f);

            if (useAlphaClip)
                stencilMat.EnableKeyword("UNITY_UI_ALPHACLIP");
            else
                stencilMat.DisableKeyword("UNITY_UI_ALPHACLIP");
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (Validate())
            {
                Refresh();
            }

            base.OnValidate();
        }
#endif

        protected override void OnEnable()
        {
            if (Validate())
            {
                Refresh();
            }

            base.OnEnable();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            if (enabled && Validate())
            {
                Refresh();
            }

            base.OnRectTransformDimensionsChange();
        }

        public void ForceNewMaterial()
        {
            Shader imageShader = Shader;
            material = new Material(imageShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        public bool Validate()
        {
            Shader imageShader = Shader;
            if (_autoCreateMaterial)
            {
                if (material == null || material.shader != imageShader)
                {
                    material = new Material(imageShader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
            }

            return material != null && material.shader == imageShader;
        }

        public void Refresh()
        {
            if (material != null && material.shader == Shader)
            {
                SetMaterialProperties(material);
            }

            Material currentRenderMaterial = materialForRendering;
            if (currentRenderMaterial != null && currentRenderMaterial != material && currentRenderMaterial.shader == Shader)
            {
                SetMaterialProperties(currentRenderMaterial);
            }
        }

        private void SetMaterialProperties(Material material)
        {
            Rect rect = ((RectTransform)transform).rect;

            material.SetFloat(_shaderProp_roundedCorners, _cornerMode == CornerMode.None ? 0.0F : 1.0F);
            if (_cornerMode != CornerMode.None)
            {
                switch (_cornerMode)
                {
                    case CornerMode.RoundedLocked:
                        _cornerRadii = CalculateCornerRadii(rect.size, ref _lockedCornerRadius);
                        break;
                    case CornerMode.RoundedIndependant:
                        _cornerRadii = CalculateCornerRadii(rect.size, ref _independantCornerRadii);
                        break;
                }
                material.SetVector(_shaderProp_radii, _cornerRadii);
                material.SetVector(_shaderProp_sizeAndBorder, new Vector4(rect.width, rect.height, _border, 0));
                material.SetColor(_shaderProp_borderColor, _borderColor);
                material.EnableKeyword(_shaderKeyword_RoundedCorners);
            }
            else
            {
                material.DisableKeyword(_shaderKeyword_RoundedCorners);
            }

            material.SetFloat(_shaderProp_gradientFill, _gradientFill ? 1.0F : 0.0F);
            if (_gradientFill)
            {
                material.SetFloat(_shaderProp_gradientAngle, _gradientAngle);
                material.SetColor(_shaderProp_gradientColor, _gradientColor);
                material.EnableKeyword(_shaderKeyword_GradientFill);
            }
            else
            {
                material.DisableKeyword(_shaderKeyword_GradientFill);
            }
        }

        private Vector4 CalculateCornerRadii(Vector2 size, ref float cornerRadius)
        {
            Vector2 halfSize = size * 0.5F;
            float maxCornerRadius = Mathf.Min(halfSize.x, halfSize.y);
            switch (_roundingMode)
            {
                case RoundingMode.Relative:
                    cornerRadius = Mathf.Clamp(cornerRadius, 0.0F, 1.0F);
                    float radius = cornerRadius * maxCornerRadius;
                    return new Vector4(radius, radius, radius, radius);
                case RoundingMode.Absolute:
                    float r = GetAdjustedRadius(ref cornerRadius, maxCornerRadius);
                    return new Vector4(r, r, r, r);
            }
            return Vector4.zero;
        }

        private Vector4 CalculateCornerRadii(Vector2 size, ref Vector4 cornerRadii)
        {
            Vector2 halfSize = size * 0.5F;
            float maxCornerRadius = Mathf.Min(halfSize.x, halfSize.y);
            switch (_roundingMode)
            {
                case RoundingMode.Relative:
                    cornerRadii = ClampVector4(cornerRadii, 0.0F, 1.0F);
                    return cornerRadii * maxCornerRadius;
                case RoundingMode.Absolute:
                    Vector4 r = GetAdjustedRadii(ref cornerRadii, maxCornerRadius);
                    return r;
            }
            return Vector4.zero;
        }

        private Vector4 ClampVector4(Vector4 vector, float min, float max)
        {
            vector.x = Mathf.Clamp(vector.x, min, max);
            vector.y = Mathf.Clamp(vector.y, min, max);
            vector.z = Mathf.Clamp(vector.z, min, max);
            vector.w = Mathf.Clamp(vector.w, min, max);
            return vector;
        }

        private float GetAdjustedRadius(ref float radius, float max)
        {
            return Mathf.Clamp(radius, 0.0F, max);
        }

        private Vector4 GetAdjustedRadii(ref Vector4 radii, float max)
        {
            return ClampVector4(radii, 0.0F, max);
        }
    }
}