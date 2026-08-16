#if UNITY_6000_0_OR_NEWER
using Unity.Properties;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitBlurredBackground
{
    /// <summary>
    /// The blurred background works by adding an additional mesh on top of the default mesh via OnGenerateVisualContent().
    /// </summary>
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class BlurredBackground : VisualElement
    {

        /// <summary>
        /// Number of segments on the x axis if it is in a world space canvas.
        /// </summary>
        public static int WorldSpaceGridDivssionsX = 15;
        public static int WorldSpaceGridDivssionsY = 15;
        public static int WorldSpaceGridDivisionsYSide = 2;
        
        
        public static List<BlurredBackground> ActiveBackgrounds = new List<BlurredBackground>();
        
        // Reset static variables on play mode enter to support disabling domain reload.
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlayModeEnter()
        {
            ActiveBackgrounds.Clear();
        }
#endif
        
        public static Color BackgroundColorDefault = new Color(0, 0, 0, 0);

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<BlurredBackground, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlFloatAttributeDescription m_BlurStrength =
                new UxmlFloatAttributeDescription { name = "Blur-Strength", defaultValue = 15f };

            UxmlEnumAttributeDescription<ShaderQuality> m_BlurQuality =
                new UxmlEnumAttributeDescription<ShaderQuality> { name = "Blur-Quality", defaultValue = ShaderQuality.Medium };

            UxmlIntAttributeDescription m_BlurIterations =
                new UxmlIntAttributeDescription { name = "Blur-Iterations", defaultValue = 1 };

            UxmlEnumAttributeDescription<SquareResolution> m_BlurResolution =
                new UxmlEnumAttributeDescription<SquareResolution> { name = "Blur-Resolution", defaultValue = SquareResolution._512 };

            UxmlColorAttributeDescription m_BlurTint =
                new UxmlColorAttributeDescription { name = "Blur-Tint", defaultValue = new Color(1f, 1f, 1f, 1f) };

            UxmlFloatAttributeDescription m_BlurMeshCornerOverlap =
                new UxmlFloatAttributeDescription { name = "Blur-Mesh-Corner-Overlap", defaultValue = 0.3f };

            UxmlIntAttributeDescription m_BlurMeshCornerSegments =
                new UxmlIntAttributeDescription { name = "Blur-Mesh-Corner-Segments", defaultValue = 12 };

            UxmlColorAttributeDescription m_BackgroundColor =
                new UxmlColorAttributeDescription { name = "Background-Color", defaultValue = BackgroundColorDefault };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var bg = ve as BlurredBackground;

                // Delay in editor to avoid "SendMessage cannot be called during Awake, CheckConsistency, or OnValidate" warnings.
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
#endif
                    bg.BlurStrength = m_BlurStrength.GetValueFromBag(bag, cc);
                    bg.BlurQuality = m_BlurQuality.GetValueFromBag(bag, cc);
                    bg.BlurIterations = m_BlurIterations.GetValueFromBag(bag, cc);
                    bg.BlurResolution = m_BlurResolution.GetValueFromBag(bag, cc);
                    bg.BlurTint = m_BlurTint.GetValueFromBag(bag, cc);
                    bg.BlurMeshCornerOverlap = m_BlurMeshCornerOverlap.GetValueFromBag(bag, cc);
                    bg.BlurMeshCornerSegments = m_BlurMeshCornerSegments.GetValueFromBag(bag, cc);
                    bg.BackgroundColor = m_BackgroundColor.GetValueFromBag(bag, cc);
#if UNITY_EDITOR
                };
#endif
            }
        }
#endif

        // Cache to have a value to return in GET if no style is defined.
        [System.NonSerialized]
        private int _blurIterations = 1;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Iterations")]
        [CreateProperty]
#endif
        public int BlurIterations
        {
            get
            {
                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Iterations, this, _blurIterations);
            }

            set
            {
                var oldValue = _blurIterations;
                _blurIterations = value;
                
                int newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Iterations, this, value);
                if (newValue != oldValue)
                {
                    if (newValue < 0)
                        newValue = 0;

                    BlurManager.Instance.GetOrCreateRenderer(this);
                    MarkDirtyRepaint();
                }
            }
        }

        [System.NonSerialized]
        private float _blurStrength = 15;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Strength")]
        [CreateProperty]
#endif
        public float BlurStrength
        {
            get
            {
                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Strength, this, _blurStrength);
            }

            set
            {
                var oldValue = _blurStrength;
                _blurStrength = value;

                float newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Strength, this, value);
                if (newValue != oldValue)
                {
                    if (newValue < 0f)
                        newValue = 0f;
                    
                    BlurManager.Instance.GetOrCreateRenderer(this);
                    MarkDirtyRepaint();
                }
            }
        }

        protected Vector2Int _blurResolutionSize = new Vector2Int(512, 512);
        public Vector2Int BlurResolutionSize
        {
            get
            {
                return _blurResolutionSize;
            }

            set
            {
                if (value.x < 2 || value.y < 2)
                    value = new Vector2Int(2, 2);

                var oldValue = _blurResolutionSize;
                _blurResolutionSize = value;

                if (oldValue != value)
                {
                    BlurManager.Instance.GetOrCreateRenderer(this);
                    MarkDirtyRepaint();
                }
            }
        }

        [System.NonSerialized]
        private SquareResolution _blurResolution = SquareResolution._512;

        /// <summary>
        /// Enable this to control the blur resolution via the BlurResolutionSize instead of the Resolution enum.
        /// </summary>
        public bool IgnoreSquareResolution = false;
        
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Resolution")]
        [CreateProperty]
#endif
        /// <summary>
        /// If you want to set a non-square resolution use BlurResolutionSize instead with IgnoreSquareResolution set to TRUE.
        /// </summary>
        public SquareResolution BlurResolution
        {
            get
            {
                if (customStyle.TryGetValue(BlurredBackgroundStyles.Resolution, out var width))
                {
                    return SquareResolutionsUtils.FromResolution(new Vector2Int(width, width));
                }
                else
                {
                    return _blurResolution;
                }
            }

            set
            {
                _blurResolution = value;
                
                var newResolutionSize = SquareResolutionsUtils.ToResolution(value);
                if (customStyle.TryGetValue(BlurredBackgroundStyles.Resolution, out var newWidth))
                {
                    newResolutionSize = new Vector2Int(newWidth, newWidth);
                    _blurResolution = SquareResolutionsUtils.FromResolution(newResolutionSize);
                }

                if (!IgnoreSquareResolution)
                {
                    BlurResolutionSize = newResolutionSize;   
                }
            }
        }

        [System.NonSerialized]
        private ShaderQuality _blurQuality = ShaderQuality.Medium;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Quality")]
        [CreateProperty]
#endif
        public ShaderQuality BlurQuality
        {
            get
            {
                if (customStyle.TryGetValue(BlurredBackgroundStyles.Quality, out var qualityString))
                {
                    return ShaderQualityTools.FromString(qualityString);
                }
                else
                {
                    return _blurQuality;
                }
            }

            set
            {
                var oldValue = _blurQuality;
                _blurQuality = value;

                ShaderQuality newValue = value;
                if (customStyle.TryGetValue(BlurredBackgroundStyles.Quality, out var qualityString))
                {
                    newValue = ShaderQualityTools.FromString(qualityString);
                }
                
                if (newValue != oldValue)
                {
                    BlurManager.Instance.GetOrCreateRenderer(this);
                    MarkDirtyRepaint();
                }
            }
        }

        protected Color _blurTint = new Color(1f, 1f, 1f, 1f);
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Tint")]
        [CreateProperty]
#endif
        public Color BlurTint
        {
            get
            {
                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Tint, this, _blurTint);
            }

            set
            {
                var newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.Tint, this, value);
                if (newValue != _blurTint)
                {
                    _blurTint = newValue;
                    MarkDirtyRepaint();
                }
            }
        }

        protected int _blurMeshCornerSegments = 12;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Mesh-Corner-Segments")]
        [CreateProperty]
#endif
        public int BlurMeshCornerSegments
        {
            get
            {
                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.MeshCornerSegments, this, _blurMeshCornerSegments);
            }

            set
            {
                var newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.MeshCornerSegments, this, value);
                if (newValue != _blurMeshCornerSegments)
                {
                    if (newValue < 1)
                        newValue = 1;

                    _blurMeshCornerSegments = newValue;
                    MarkDirtyRepaint();
                }
            }
        }

        protected float _blurMeshCornerOverlap = 0.3f;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Blur-Mesh-Corner-Overlap")]
        [CreateProperty]
#endif
        public float BlurMeshCornerOverlap
        {
            get
            {
                return BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.MeshCornerOverlap, this, _blurMeshCornerOverlap);
            }

            set
            {
                var newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.MeshCornerOverlap, this, value);
                if (newValue != _blurMeshCornerOverlap)
                {
                    if (newValue < 0f)
                        newValue = 0f;

                    _blurMeshCornerOverlap = newValue;
                    MarkDirtyRepaint();
                }
            }
        }

        public static bool IgnoreBlurredBackgroundColor = false;

        protected Color _defaultBackgroundColor = BackgroundColorDefault;
#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute("Background-Color")]
        [CreateProperty]
#endif
        public Color BackgroundColor
        {
            get
            {
                if (IgnoreBlurredBackgroundColor)
                {
                    return style.backgroundColor.value;
                }
                
                var result = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.BackgroundColor, this, _defaultBackgroundColor);
                return result;
            }

            set
            {
                var newValue = BlurredBackgroundStyles.ResolveStyle(BlurredBackgroundStyles.BackgroundColor, this, value);
                _defaultBackgroundColor = newValue;
                if (!IgnoreBlurredBackgroundColor)
                {
                    style.backgroundColor = _defaultBackgroundColor;
                }
            }
        }

        protected bool _isInWorldSpacePanel;
        protected UIDocument _firstUIDocument;

        // Mesh Data
        List<Vertex> _vertices = new List<Vertex>();
        List<ushort> _indices = new List<ushort>();

        Vertex[] _verticesArray;
        ushort[] _indicesArray;
        
        protected VisualElement rootParent;
        protected System.Action _markDirtyRepaintAction;
        
        protected Vector3 _lastKnownCameraPosition;
        protected Quaternion _lastKnownCameraRotation;
        protected float _lastKnownCameraFOV;
        protected Vector3 _lastKnownDocumentPosition;
        protected Quaternion _lastKnownDocumentRotation;
        protected Vector3 _lastKnownDocumentScale;
        
        public BlurredBackground()
        {
            generateVisualContent = OnGenerateVisualContent;

            RegisterCallback<AttachToPanelEvent>(attach);
            RegisterCallback<DetachFromPanelEvent>(detach);
            RegisterCallback<GeometryChangedEvent>(onGeometryChanged);
        }
        
        
        /// <summary>
        /// Use this to trigger a repaint in the next frame in context where you are not sure if it is safe to call MarkDirtyRepaint immediately.
        /// </summary>
        public void ScheduleMarkDirtyRepaint()
        {
            if (_markDirtyRepaintAction == null)
                _markDirtyRepaintAction = () => MarkDirtyRepaint();
            
            schedule.Execute(_markDirtyRepaintAction);
        }

        public void UpdateIfNecessary()
        {
            if (!_isInWorldSpacePanel)
                return;

            var cam = CameraFinder.FindPlayModeCamera();
            if (cam == null)
                return;

            var doc = UIDocumentHelper.GetFirstDocument(this);
            if (doc == null)
                return;
            
            // Determine if we should regenerate the mesh.
            var camTransform = cam.transform;
            var docTransform = doc.transform;
            if (_lastKnownCameraPosition != camTransform.position ||
                _lastKnownCameraRotation != camTransform.rotation ||
                !Mathf.Approximately(_lastKnownCameraFOV, cam.fieldOfView) ||
                _lastKnownDocumentPosition != docTransform.position ||
                _lastKnownDocumentRotation != docTransform.rotation ||
                _lastKnownDocumentScale != docTransform.localScale
               )
            {
                _lastKnownCameraPosition = camTransform.position;
                _lastKnownCameraRotation = camTransform.rotation;
                _lastKnownCameraFOV = cam.fieldOfView;
                _lastKnownDocumentPosition = docTransform.position;
                _lastKnownDocumentRotation = docTransform.rotation;
                _lastKnownDocumentScale = docTransform.localScale;

                ScheduleMarkDirtyRepaint();
            }
        }

        void attach(AttachToPanelEvent evt)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
#endif
                BlurManager.Instance.AttachElement(this);
#if UNITY_EDITOR
            };
#endif

            _firstUIDocument = UIDocumentHelper.GetFirstDocument(this);
            if (_firstUIDocument)
                _isInWorldSpacePanel = PanelSettingsReflectionUtils.IsRenderModeWorldSpace(_firstUIDocument.panelSettings);
        }

        void detach(DetachFromPanelEvent evt)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
#endif
                BlurManager.Instance.DetachElement(this);
#if UNITY_EDITOR
            };
#endif
        }
        
        bool? m_lastIsDrawnValue; // Nullable to ensure we catch the initial change.

        protected virtual void onGeometryChanged(GeometryChangedEvent evt)
        {
            bool isDrawn = IsDrawn();

            if (!m_lastIsDrawnValue.HasValue || m_lastIsDrawnValue.Value != isDrawn)
            {
                m_lastIsDrawnValue = isDrawn;

                // Update blur renderer properties to ensure they are applied last.
                if (isDrawn)
                {
                    // Only do it if it is drawn.
                    BlurManager.Instance.GetOrCreateRenderer(this);
                    
                    if (!ActiveBackgrounds.Contains(this))
                        ActiveBackgrounds.Add(this);
                }
                else
                {
                    if (ActiveBackgrounds.Contains(this))
                        ActiveBackgrounds.Remove(this);

                    // If disabled then change blur settings to those of the last active one.
                    //if (ActiveBackgrounds.Count > 0)
                    //{
                    //    var last = ActiveBackgrounds[ActiveBackgrounds.Count - 1];
                    //    BlurManager.Instance.GetOrCreateRenderer(last);
                    //}
                }
            }
        }

        /// <summary>
        /// Returns whether or not the image is drawn based on the contentRect size.

        /// It acts like the activeInHierarchy we are used to from game objects (takes parents into account).
        /// </summary>
        /// <returns></returns>
        public bool IsDrawn()
        {
            var rect = contentRect;
            return !float.IsNaN(rect.width) && !Mathf.Approximately(rect.width, 0f) && !Mathf.Approximately(rect.height, 0f);
        }


        public virtual void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            // Remember: "generateVisualContent is an addition to the default rendering, it's not a replacement"
            // See: https://forum.unity.com/threads/hp-bars-at-runtime-image-masking-or-fill.1076486/#post-6948578 

            if (BlurManager.Instance == null)
                return;
           
            // If no blur is required then do not even draw the mesh.
            if (BlurIterations <= 0 || BlurStrength <= 0.001f || contentRect.width == 0 || contentRect.height == 0)
                return;
            
            Rect contentRectAbs = contentRect;

            if (contentRectAbs.width + resolvedStyle.paddingLeft + resolvedStyle.paddingRight < 0.01f || contentRectAbs.height + resolvedStyle.paddingTop + resolvedStyle.paddingBottom < 0.01f)
                return;

            // Clamp content
            if (resolvedStyle.borderLeftWidth < 0) contentRectAbs.xMin -= resolvedStyle.borderLeftWidth;
            if (resolvedStyle.borderRightWidth < 0) contentRectAbs.xMax += resolvedStyle.borderRightWidth;
            if (resolvedStyle.borderTopWidth < 0) contentRectAbs.yMin -= resolvedStyle.borderTopWidth;
            if (resolvedStyle.borderBottomWidth < 0) contentRectAbs.yMax += resolvedStyle.borderBottomWidth;


            // Mesh generation

            // clamp to positive
            float borderLeft = Mathf.Clamp(resolvedStyle.borderLeftWidth, 0, resolvedStyle.width * 0.5f);
            float borderRight = Mathf.Clamp(resolvedStyle.borderRightWidth, 0, resolvedStyle.width * 0.5f);
            float borderTop = Mathf.Clamp(resolvedStyle.borderTopWidth, 0, resolvedStyle.height * 0.5f);
            float borderBottom = Mathf.Clamp(resolvedStyle.borderBottomWidth, 0, resolvedStyle.height * 0.5f);

            float radiusTopLeft = Mathf.Max(0, resolvedStyle.borderTopLeftRadius);
            float radiusTopRight = Mathf.Max(0, resolvedStyle.borderTopRightRadius);
            float radiusBottomLeft = Mathf.Max(0, resolvedStyle.borderBottomLeftRadius);
            float radiusBottomRight = Mathf.Max(0, resolvedStyle.borderBottomRightRadius);

            float paddingLeft = Mathf.Max(0, resolvedStyle.paddingLeft);
            float paddingRight = Mathf.Max(0, resolvedStyle.paddingRight);
            float paddingTop = Mathf.Max(0, resolvedStyle.paddingTop);
            float paddingBottom = Mathf.Max(0, resolvedStyle.paddingBottom);

            contentRectAbs.xMin -= paddingLeft;
            contentRectAbs.xMax += paddingRight;
            contentRectAbs.yMin -= paddingTop;
            contentRectAbs.yMax += paddingBottom;

            // Calc inner rect
            // It only starts to curve on the inside once the radius is > the bigger border width
            Vector2 topLeftCornerSize = new Vector2(
                Mathf.Clamp(radiusTopLeft - borderLeft, 0, resolvedStyle.width * 0.5f - borderLeft),
                Mathf.Clamp(radiusTopLeft - borderTop, 0, resolvedStyle.height * 0.5f - borderTop)
            );

            Vector2 topRightCornerSize = new Vector2(
                Mathf.Clamp(radiusTopRight - borderRight, 0, resolvedStyle.width * 0.5f - borderRight),
                Mathf.Clamp(radiusTopRight - borderTop, 0, resolvedStyle.height * 0.5f - borderTop)
            );

            Vector2 bottomLeftCornerSize = new Vector2(
                Mathf.Clamp(radiusBottomLeft - borderLeft, 0, resolvedStyle.width * 0.5f - borderLeft),
                Mathf.Clamp(radiusBottomLeft - borderBottom, 0, resolvedStyle.height * 0.5f - borderBottom)
            );

            Vector2 bottomRightCornerSize = new Vector2(
                Mathf.Clamp(radiusBottomRight - borderRight, 0, resolvedStyle.width * 0.5f - borderRight),
                Mathf.Clamp(radiusBottomRight - borderBottom, 0, resolvedStyle.height * 0.5f - borderBottom)
            );


            // Calc inner quad with corner radius taken into account
            Vector2 innerTopLeft = new Vector2(contentRectAbs.xMin + topLeftCornerSize.x, contentRectAbs.yMin + topLeftCornerSize.y);
            Vector2 innerTopRight = new Vector2(contentRectAbs.xMax - topRightCornerSize.x, contentRectAbs.yMin + topRightCornerSize.y);
            Vector2 innerBottomLeft = new Vector2(contentRectAbs.xMin + bottomLeftCornerSize.x, contentRectAbs.yMax - bottomLeftCornerSize.y);
            Vector2 innerBottomRight = new Vector2(contentRectAbs.xMax - bottomRightCornerSize.x, contentRectAbs.yMax - bottomRightCornerSize.y);

            int verticesPerCorner = BlurMeshCornerSegments;

            // Clear tmp lists
            _vertices.Clear();
            _indices.Clear();

            ushort innerBottomLeftVertex = 0, innerTopLeftVertex = 0, innerTopRightVertex = 0, innerBottomRightVertex = 0;
            createCenterRect(innerBottomLeft, innerTopLeft, innerTopRight, innerBottomRight,
                ref innerBottomLeftVertex, ref innerTopLeftVertex, ref innerTopRightVertex, ref innerBottomRightVertex);
            
            ushort bottomLeftLeftVertex, bottomLeftBottomVertex, bottomRightRightVertex, bottomRightBottomVertex,
                   topLeftLeftVertex, topLeftTopVertex, topRightTopVertex, topRightRightVertex;

            // We add an overlap to make the new mesh overlap the borders a little to reduce gaps.
            float overlapWidth = BlurMeshCornerOverlap;

            // Sides (indices)
            // Top
            createSide( 
                new Vector3(innerTopLeft.x, innerTopLeft.y, Vertex.nearZ), 
                new Vector3(innerTopRight.x, innerTopRight.y, Vertex.nearZ),
                new Vector3(innerTopLeft.x, innerTopLeft.y - topLeftCornerSize.y - overlapWidth, Vertex.nearZ),
                new Vector3(innerTopRight.x, innerTopRight.y - topRightCornerSize.y - overlapWidth, Vertex.nearZ),
                out topLeftTopVertex, out topRightTopVertex
                );
            // Right
            createSide(
                new Vector3(innerTopRight.x, innerTopRight.y, Vertex.nearZ),
                new Vector3(innerBottomRight.x, innerBottomRight.y, Vertex.nearZ),
                new Vector3(innerTopRight.x + topRightCornerSize.x + overlapWidth, innerTopRight.y, Vertex.nearZ),
                new Vector3(innerBottomRight.x + bottomRightCornerSize.x + overlapWidth, innerBottomRight.y, Vertex.nearZ),
                out topRightRightVertex, out bottomRightRightVertex
                );
            // Bottom
            createSide(
                new Vector3(innerBottomRight.x, innerBottomRight.y, Vertex.nearZ),
                new Vector3(innerBottomLeft.x, innerBottomLeft.y, Vertex.nearZ),
                new Vector3(innerBottomRight.x, innerBottomRight.y + bottomRightCornerSize.y + overlapWidth, Vertex.nearZ),
                new Vector3(innerBottomLeft.x, innerBottomLeft.y + bottomLeftCornerSize.y + overlapWidth, Vertex.nearZ),
                out bottomRightBottomVertex, out bottomLeftBottomVertex
                );
            // Left
            createSide(
                new Vector3(innerBottomLeft.x, innerBottomLeft.y, Vertex.nearZ),
                new Vector3(innerTopLeft.x, innerTopLeft.y, Vertex.nearZ),
                new Vector3(innerBottomLeft.x - bottomLeftCornerSize.x - overlapWidth, innerBottomLeft.y, Vertex.nearZ),
                new Vector3(innerTopLeft.x - topLeftCornerSize.x - overlapWidth, innerTopLeft.y, Vertex.nearZ),
                out bottomLeftLeftVertex, out topLeftLeftVertex
                );

            if (verticesPerCorner > 0)
            {
                createCorner(topLeftCornerSize, innerTopLeft, verticesPerCorner, innerTopLeftVertex, topLeftLeftVertex, topLeftTopVertex, 2);
                createCorner(topRightCornerSize, innerTopRight, verticesPerCorner, innerTopRightVertex, topRightTopVertex, topRightRightVertex, 3);
                createCorner(bottomRightCornerSize, innerBottomRight, verticesPerCorner, innerBottomRightVertex, bottomRightRightVertex, bottomRightBottomVertex, 0);
                createCorner(bottomLeftCornerSize, innerBottomLeft, verticesPerCorner, innerBottomLeftVertex, bottomLeftBottomVertex, bottomLeftLeftVertex, 1);
            }

            // Stop if in world space but document has not been found.
            if (_isInWorldSpacePanel && _firstUIDocument == null)
                return;
            
            // Trim empty tris
            for (int i = _indices.Count-3; i >= 0; i-=3)
            {
                var v0 = _vertices[_indices[i]].position;
                var v1 = _vertices[_indices[i+1]].position;
                var v2 = _vertices[_indices[i+2]].position;

                if (v0 == v1 || v0 == v2 || v1 == v2)
                {
                    _indices.RemoveAt(i);
                    _indices.RemoveAt(i);
                    _indices.RemoveAt(i);
                }
            }
            
            // UVs
            if (rootParent == null)
            {
                rootParent = GetDocumentRoot(this);
            }

            Camera camera = null;
            if (_isInWorldSpacePanel)
            {
                camera = CameraFinder.FindPlayModeCamera();
            }

            int vertexCount = _vertices.Count;
            for (int n = 0; n < vertexCount; n++)
            {
                var vertex = _vertices[n]; 
                vertex.tint = BlurTint;

                Vector2 uv;
                if (_isInWorldSpacePanel && camera != null)
                {
                    var worldPos = PanelToWorld(this, _vertices[n].position, _firstUIDocument);
                    var viewportPos = camera.WorldToViewportPoint(worldPos);
                    uv = viewportPos;
                }
                else
                {
                    uv = this.LocalToWorld(_vertices[n].position);
                    uv.x /= rootParent.worldBound.width;
                    uv.y /= rootParent.worldBound.height;
                    uv.y = 1f - uv.y;
                }

                vertex.uv = uv;
                _vertices[n] = vertex;
            }
            
            // Data type conversion
            if (_verticesArray == null || 
                _verticesArray.Length != _vertices.Count ||
                _indicesArray.Length != _indices.Count)
            {
                _verticesArray = _vertices.ToArray();
                _indicesArray = _indices.ToArray();
            }
            else
            {
                var vCount = _vertices.Count;
                for (int i = 0; i < vCount; i++)
                {
                    _verticesArray[i].position = _vertices[i].position;
                    _verticesArray[i].uv = _vertices[i].uv;
                    _verticesArray[i].tint = _vertices[i].tint;
                }
                var iCount = _indices.Count;
                for (int i = 0; i < iCount; i++)
                {
                    _indicesArray[i] = _indices[i];
                }
            }
            
            var texture = _isInWorldSpacePanel ? BlurManager.Instance.GetBlurredTextureWorld(this) : BlurManager.Instance.GetBlurredTexture(this);
            MeshWriteData mwd = mgc.Allocate(_verticesArray.Length, _indicesArray.Length, texture);
            
            mwd.SetAllVertices(_verticesArray);
            mwd.SetAllIndices(_indicesArray);
        }

        public static Vector3 PanelToWorld(VisualElement element, Vector3 p, UIDocument doc = null)
        {
            if (doc == null)
                doc = UIDocumentHelper.GetFirstDocument(element);
            
            if (doc == null)
                return Vector3.zero;
            
            var rootPos = element.LocalToWorld(p);
            var world3DPos = doc.gameObject.transform.TransformPoint(rootPos);
            return world3DPos;
        }
        
        public static Vector2 PanelToScreen(IPanel panel, Vector2 pos)
        {
            var min = RuntimePanelUtils.ScreenToPanel(panel, Vector2.zero);
            var max = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(Screen.width, Screen.height));

            float screenX = (pos.x - min.x) / (max.x - min.x) * Screen.width;
            float screenY = (pos.y - min.y) / (max.y - min.y) * Screen.height;
            screenY = Screen.height - screenY;

            return new Vector2(screenX, screenY);
        }
        
        void createCenterRect(
            Vector2 innerBottomLeft,
            Vector2 innerTopLeft,
            Vector2 innerTopRight,
            Vector2 innerBottomRight,
            ref ushort innerBottomLeftVertex,
            ref ushort innerTopLeftVertex,
            ref ushort innerTopRightVertex,
            ref ushort innerBottomRightVertex
            )
        {
            // Grid settings
            int gridStepsX = 1;
            int gridStepsY = 1;

            if (_isInWorldSpacePanel)
            {
                gridStepsX = WorldSpaceGridDivssionsX;
                gridStepsY = WorldSpaceGridDivssionsY;
            }

            int startVertex = _vertices.Count;
            
            // Generate vertices
            for (int y = 0; y <= gridStepsY; y++)
            {
                for (int x = 0; x <= gridStepsX; x++)
                {
                    // Interpolate position within the quad
                    float tx = (float)x / gridStepsX;
                    float ty = (float)y / gridStepsY;
        
                    Vector3 bottomInterp = Vector3.Lerp(innerBottomLeft, innerBottomRight, tx);
                    Vector3 topInterp = Vector3.Lerp(innerTopLeft, innerTopRight, tx);
                    Vector3 position = Vector3.Lerp(bottomInterp, topInterp, ty);

                    var vertex = new Vertex();
                    vertex.position = new Vector3(position.x, position.y, Vertex.nearZ);
                    _vertices.Add(vertex);
                    
                    // Store corner vertex indices
                    if (x == 0 && y == 0) innerBottomLeftVertex = (ushort)(_vertices.Count - 1);
                    else if (x == 0 && y == gridStepsY) innerTopLeftVertex = (ushort)(_vertices.Count - 1);
                    else if (x == gridStepsX && y == gridStepsY) innerTopRightVertex = (ushort)(_vertices.Count - 1);
                    else if (x == gridStepsX && y == 0) innerBottomRightVertex = (ushort)(_vertices.Count - 1);
                }
            }

            // Generate triangle indices for the grid
            for (int y = 0; y < gridStepsY; y++)
            {
                for (int x = 0; x < gridStepsX; x++)
                {
                    // Calculate vertex indices for current quad
                    ushort bottomLeft = (ushort) (startVertex + (y * (gridStepsX + 1) + x));
                    ushort bottomRight = (ushort) ((bottomLeft + 1));
                    ushort topLeft = (ushort) (startVertex + ((y + 1) * (gridStepsX + 1) + x));
                    ushort topRight = (ushort) ((topLeft + 1));
        
                    // First triangle (bottom-left, top-left, top-right)
                    _indices.Add(bottomLeft);
                    _indices.Add(topLeft);
                    _indices.Add(topRight);
        
                    // Second triangle (top-right, bottom-right, bottom-left)
                    _indices.Add(topRight);
                    _indices.Add(bottomRight);
                    _indices.Add(bottomLeft);
                }
            }
        }

        private void createCorner(Vector2 cornerSize, Vector2 innerPos, int verticesPerCorner, ushort innerVertex, ushort startVertex, ushort endVertex, int quadrantOffset)
        {
            if (cornerSize.x > 0 && cornerSize.y > 0)
            {
                ushort center = innerVertex;
                ushort last = startVertex;

                float offset = Mathf.PI * 0.5f * quadrantOffset;
                float stepSizeInQuadrant = 1f / (verticesPerCorner + 1) * Mathf.PI * 0.5f;

                for (int c = 1; c < verticesPerCorner + 1; c++)
                {
                    float x = Mathf.Cos(offset + stepSizeInQuadrant * c);
                    float y = Mathf.Sin(offset + stepSizeInQuadrant * c);
                    // We also add an overlap to make the new mesh overlap the borders a little to reduce gaps.
                    float overlapWidth = BlurMeshCornerOverlap;
                    var vertex = new Vertex();
                    vertex.position = new Vector3(innerPos.x + x * (cornerSize.x + overlapWidth), innerPos.y + y * (cornerSize.y + overlapWidth), Vertex.nearZ);
                    _vertices.Add(vertex);

                    _indices.Add(center);
                    _indices.Add(last);
                    _indices.Add((ushort)(_vertices.Count - 1));
                    last = _indices[_indices.Count - 1];
                }

                // End at the existing vertex
                _indices.Add(center);
                _indices.Add(last);
                _indices.Add(endVertex);
            }
        }

        void createSide(
            Vector3 firstInnerPos, Vector3 secondInnerPos,
            Vector3 newFirstOuterPos, Vector3 newSecondOuterPos,
            out ushort newFirstOuterVertex, out ushort newSecondOuterVertex)
        {
            newFirstOuterVertex = 0;
            newSecondOuterVertex = 0;

            int gridStepsX = 1;
            int gridStepsY = 1;
            
            if (_isInWorldSpacePanel)
            {
                gridStepsX = WorldSpaceGridDivssionsX;
                gridStepsY = WorldSpaceGridDivisionsYSide;
            }
            
            int startVertex = _vertices.Count;
            
            // Generate vertices
            for (int y = 0; y <= gridStepsY; y++)
            {
                for (int x = 0; x <= gridStepsX; x++)
                {
                    // Interpolate position within the quad
                    float tx = (float)x / gridStepsX;
                    float ty = (float)y / gridStepsY;
        
                    // Lerp two lines on inner and outer side, then interpolate between them for y.
                    Vector3 interInner = Vector3.Lerp(firstInnerPos, secondInnerPos, tx);
                    Vector3 interOuter = Vector3.Lerp(newFirstOuterPos, newSecondOuterPos, tx);
                    Vector3 position = Vector3.Lerp(interInner, interOuter, ty);

                    var vertex = new Vertex();
                    vertex.position = new Vector3(position.x, position.y, Vertex.nearZ);
                    _vertices.Add(vertex);
                    
                    // Store new corner vertex indices
                    if (x == 0 && y == gridStepsY) newFirstOuterVertex = (ushort)(_vertices.Count - 1);
                    else if (x == gridStepsX && y == gridStepsY) newSecondOuterVertex = (ushort)(_vertices.Count - 1);
                }
            }

            // Generate triangle indices for the grid
            for (int y = 0; y < gridStepsY; y++)
            {
                for (int x = 0; x < gridStepsX; x++)
                {
                    // Calculate vertex indices for current quad
                    ushort innerFirst = (ushort) (startVertex + (y * (gridStepsX + 1) + x));
                    ushort innerSecond = (ushort) ((innerFirst + 1));
                    ushort outerFirst = (ushort) (startVertex + ((y + 1) * (gridStepsX + 1) + x));
                    ushort outerSecond = (ushort) ((outerFirst + 1));
        
                    // First triangle
                    _indices.Add(innerFirst);
                    _indices.Add(outerFirst);
                    _indices.Add(outerSecond);
        
                    // Second triangle
                    _indices.Add(outerSecond);
                    _indices.Add(innerSecond);
                    _indices.Add(innerFirst);
                }
            }
        }

        bool cornerSizeNotZeroX(Vector2 cornerSize)
        {
            return cornerSize.x > 0;
        }

        bool cornerSizeNotZeroY(Vector2 cornerSize)
        {
            return cornerSize.y > 0;
        }

        public VisualElement GetDocumentRoot(VisualElement ele)
        {
            while (ele.parent != null)
            {
                ele = ele.parent;
            }

            return ele;
        }
    }
}