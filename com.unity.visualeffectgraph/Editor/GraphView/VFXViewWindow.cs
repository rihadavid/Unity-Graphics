#define USE_EXIT_WORKAROUND_FOGBUGZ_1062258
using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor.VFX;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor;
using UnityObject = UnityEngine.Object;
using System.IO;
using UnityEditor.VersionControl;

namespace UnityEditor.VFX.UI
{
    [Serializable]
    class VFXViewWindow : EditorWindow
    {
        static VisualEffect s_LastAttachedComponent;

        ShortcutHandler m_ShortcutHandler;

        protected void SetupFramingShortcutHandler(VFXView view)
        {
            m_ShortcutHandler = new ShortcutHandler(
                new Dictionary<Event, ShortcutDelegate>
                {
                    {Event.KeyboardEvent("a"), view.FrameAll },
                    {Event.KeyboardEvent("f"), view.FrameSelection },
                    {Event.KeyboardEvent("o"), view.FrameOrigin },
                    {Event.KeyboardEvent("^#>"), view.FramePrev },
                    {Event.KeyboardEvent("^>"), view.FrameNext },
                    {Event.KeyboardEvent("F7"), view.OnCompile},
                    {Event.KeyboardEvent("#d"), view.OutputToDot},
                    {Event.KeyboardEvent("^&d"), view.DuplicateSelectionWithEdges},
                    {Event.KeyboardEvent("^#d"), view.OutputToDotReduced},
                    {Event.KeyboardEvent("#c"), view.OutputToDotConstantFolding},
                    {Event.KeyboardEvent("^r"), view.ReinitComponents},
                    {Event.KeyboardEvent("F5"), view.ReinitComponents},
                    {Event.KeyboardEvent("#^r"), view.ReinitAndPlayComponents},
                    {Event.KeyboardEvent("#F5"), view.ReinitAndPlayComponents},
                });
        }

        public static VFXViewWindow currentWindow => WindowLayout.FindEditorWindowOfType(typeof(VFXViewWindow)) as VFXViewWindow;

        [MenuItem("Window/Visual Effects/Visual Effect Graph", false, 3011)]
        public static void ShowWindow()
        {
            VFXLibrary.LogUnsupportedSRP();

            GetWindow<VFXViewWindow>();
        }

        public VFXView graphView
        {
            get; private set;
        }
        public void LoadAsset(VisualEffectAsset asset, VisualEffect effectToAttach)
        {
            VFXLibrary.LogUnsupportedSRP();

            string assetPath = AssetDatabase.GetAssetPath(asset);

            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);

            //Transitionning code
            if (resource == null)
            {
                resource = new VisualEffectResource();
                resource.SetAssetPath(AssetDatabase.GetAssetPath(asset));
            }

            LoadResource(resource, effectToAttach);
        }

        public void LoadResource(VisualEffectResource resource, VisualEffect effectToAttach = null)
        {
            m_ResourceHistory.Clear();
            if (graphView.controller == null || graphView.controller.model != resource)
            {
                InternalLoadResource(resource);
            }

            if (!graphView.TryAttachTo(effectToAttach))
            {
                s_LastAttachedComponent = null;
            }
        }

        List<VisualEffectResource> m_ResourceHistory = new List<VisualEffectResource>();

        public IEnumerable<VisualEffectResource> resourceHistory
        {
            get { return m_ResourceHistory; }
        }

        public void PushResource(VisualEffectResource resource)
        {
            if (graphView.controller == null || graphView.controller.model != resource)
            {
                m_ResourceHistory.Add(m_DisplayedResource);
                InternalLoadResource(resource);
            }
        }

        void InternalLoadResource(VisualEffectResource resource)
        {
            m_DisplayedResource = resource;
            graphView.controller = VFXViewController.GetController(resource, true);
            graphView.UpdateGlobalSelection();
            graphView.FrameNewController();
            graphView.UpdateIsSubgraph();
        }

        public bool CanPopResource()
        {
            return m_ResourceHistory.Any();
        }

        public void PopResource()
        {
            InternalLoadResource(m_ResourceHistory.Last());

            m_ResourceHistory.RemoveAt(m_ResourceHistory.Count - 1);
        }

        protected VisualEffectResource GetCurrentResource()
        {
            var objs = Selection.objects;

            VisualEffectResource selectedResource = null;
            if (objs != null && objs.Length == 1)
            {
                if (objs[0] is VisualEffectAsset)
                {
                    VisualEffectAsset asset = objs[0] as VisualEffectAsset;
                    selectedResource = asset.GetResource();
                }
                else if (objs[0] is VisualEffectResource)
                {
                    selectedResource = objs[0] as VisualEffectResource;
                }
            }
            if (selectedResource == null)
            {
                int instanceID = Selection.activeInstanceID;

                if (instanceID != 0)
                {
                    string path = AssetDatabase.GetAssetPath(instanceID);
                    if (path.EndsWith(VisualEffectResource.Extension))
                    {
                        selectedResource = VisualEffectResource.GetResourceAtPath(path);
                    }
                }
            }
            if (selectedResource == null && m_DisplayedResource != null)
            {
                selectedResource = m_DisplayedResource;
            }
            return selectedResource;
        }

        Action m_OnUpdateAction;

        protected void CreateGUI()
        {
            VFXManagerEditor.CheckVFXManager();

            if (m_ResourceHistory == null)
                m_ResourceHistory = new List<VisualEffectResource>();

            graphView = new VFXView();
            graphView.StretchToParentSize();
            SetupFramingShortcutHandler(graphView);

            rootVisualElement.Add(graphView);

            // make sure we don't do something that might touch the model on the view OnEnable because
            // the models OnEnable might be called after in the case of a domain reload.
            m_OnUpdateAction = () =>
            {
                var currentAsset = GetCurrentResource();
                if (currentAsset != null)
                {
                    LoadResource(currentAsset, s_LastAttachedComponent);
                }
            };

            autoCompile = true;

            graphView.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            graphView.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            if (rootVisualElement.panel != null)
            {
                rootVisualElement.AddManipulator(m_ShortcutHandler);
            }

#if USE_EXIT_WORKAROUND_FOGBUGZ_1062258
            EditorApplication.wantsToQuit += Quitting_Workaround;
#endif

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(VisualEffectAssetEditorUtility.editorResourcesPath + "/VFX/" + (EditorGUIUtility.isProSkin ? "vfx_graph_icon_gray_dark.png" : "vfx_graph_icon_gray_light.png"));
            titleContent.image = icon;
        }

#if USE_EXIT_WORKAROUND_FOGBUGZ_1062258
        private bool Quitting_Workaround()
        {
            if (graphView != null)
                graphView.controller = null;
            return true;
        }

#endif

        protected void OnDestroy()
        {
#if USE_EXIT_WORKAROUND_FOGBUGZ_1062258
            EditorApplication.wantsToQuit -= Quitting_Workaround;
#endif

            if (graphView != null)
            {
                s_LastAttachedComponent = graphView.attachedComponent;
                graphView.UnregisterCallback<AttachToPanelEvent>(OnEnterPanel);
                graphView.UnregisterCallback<DetachFromPanelEvent>(OnLeavePanel);
                graphView.controller = null;
            }
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            rootVisualElement.AddManipulator(m_ShortcutHandler);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            rootVisualElement.RemoveManipulator(m_ShortcutHandler);
        }

        void OnFocus()
        {
            if (graphView != null) // OnFocus can be somehow called before OnEnable
                graphView.OnFocus();
        }

        public void OnVisualEffectComponentChanged(IEnumerable<VisualEffect> componentChanged)
        {
            if (graphView != null)
                graphView.OnVisualEffectComponentChanged(componentChanged);
        }

        public bool autoCompile { get; set; }

        void Update()
        {
            if (graphView == null)
                return;

            if (m_OnUpdateAction != null)
            {
                m_OnUpdateAction();
                m_OnUpdateAction = null;
            }
            VFXViewController controller = graphView.controller;
            var filename = "No Asset";
            if (controller != null)
            {
                controller.NotifyUpdate();
                if (controller.model != null)
                {
                    var graph = controller.graph;
                    if (graph != null)
                    {
                        filename = controller.name;

                        if (EditorUtility.IsDirty(graph))
                        {
                            filename += "*";
                        }
                        if (autoCompile && graph.IsExpressionGraphDirty() && !graph.GetResource().isSubgraph)
                        {
                            VFXGraph.explicitCompile = true;
                            graph.errorManager.ClearAllErrors(null, VFXErrorOrigin.Compilation);
                            using (var reporter = new VFXCompileErrorReporter(controller.graph.errorManager))
                            {
                                VFXGraph.compileReporter = reporter;
                                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graphView.controller.model));
                                graph.SetExpressionGraphDirty(false); // As are implemented subgraph now, compiling dependents chain can reset dirty flag on used subgraphs, which will make an infinite loop, this is bad!
                                VFXGraph.compileReporter = null;
                            }
                            VFXGraph.explicitCompile = false;
                        }
                        else
                            graph.RecompileIfNeeded(true, true);

                        bool wasDirty = graph.IsExpressionGraphDirty();

                        controller.RecompileExpressionGraphIfNeeded();

                        // Hack to avoid infinite recompilation due to UI triggering a recompile TODO: Fix problematic cases that trigger that error
                        if (!wasDirty && graph.IsExpressionGraphDirty())
                        {
                            Debug.LogError("Expression graph was marked as dirty after compiling context for UI. Discard to avoid infinite compilation loop.");
                            graph.SetExpressionGraphDirty(false);
                        }
                    }
                }
            }

            if (VFXViewModificationProcessor.assetMoved)
            {
                graphView.AssetMoved();
                VFXViewModificationProcessor.assetMoved = false;
            }
            titleContent.text = filename;
        }

        [SerializeField]
        private VisualEffectResource m_DisplayedResource;
    }
}
