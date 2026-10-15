using System;
using System.Collections.Generic;
using Unity.Pipeline.Config;
using UnityEngine;

namespace Unity.Pipeline
{
    /// <summary>
    /// Serialized compatibility shell for scenes authored with Pipeline 0.5. Runtime settings now
    /// live in Project Settings > Pipeline > Runtime; RuntimePipelineBootstrap owns the sole driver.
    /// This component never creates a server, changes discovery, or tears down the shared driver.
    /// </summary>
    [Obsolete("Configure Project Settings > Pipeline > Runtime and use RuntimePipelineBootstrap.Instance.")]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class RuntimePipelineManager : MonoBehaviour
    {
        // Preserve old serialized values for migration, but never override the new project settings.
        [HideInInspector] public bool enableInBuilds;
        [HideInInspector] public int port;
        [HideInInspector] public int requestTimeoutMs = 30000;
        [HideInInspector] public bool enableAuditLogging = true;
        [HideInInspector] public bool autoStart = true;
        [HideInInspector] public int maxWorkItemsPerFrame = 10;
        [SerializeField, HideInInspector] private List<string> m_AllowedReloadRoots = new List<string>();

        private RuntimePipelineDriver Driver => RuntimePipelineBootstrap.Instance;
        public RuntimePipelineServer Server => Driver?.Server;
        public bool IsServerRunning => Driver != null && Driver.IsServerRunning;
        public int ActualPort => Driver?.ActualPort ?? 0;
        public RuntimePipelineConfig Config => Driver?.Config;
        public IReadOnlyList<string> AllowedReloadRoots => Driver?.AllowedReloadRoots ?? m_AllowedReloadRoots;

        public void SetAllowedReloadRoots(IEnumerable<string> roots)
        {
            m_AllowedReloadRoots = roots != null ? new List<string>(roots) : new List<string>();
        }

        public void StartServer() => RuntimePipelineBootstrap.Bootstrap()?.StartServer();
        public void StopServer() => Driver?.StopServer();
        public ValidationResult ValidateConfiguration() => Driver != null
            ? Driver.ValidateConfiguration()
            : ValidationResult.Error("Configure Project Settings > Pipeline > Runtime; legacy scene fields no longer configure the server.");
    }
}
