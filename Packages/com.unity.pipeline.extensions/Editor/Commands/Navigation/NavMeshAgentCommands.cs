using System;
using UnityEditor;
using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor.Authoring;
using Unity.Pipeline.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Unity.Pipeline.Extensions.Editor.Commands.Navigation
{
    /// <summary>Runtime-style controls for a NavMeshAgent in Editor Play mode.</summary>
    public static class NavMeshAgentCommands
    {
        [CliCommand("set_navmesh_agent_destination", "Set a NavMeshAgent destination in Editor Play mode. The agent must be on a baked NavMesh.")]
        public static object SetNavMeshAgentDestination(
            [CliArg("target", "Reference to a NavMeshAgent component or its GameObject.", Required = true)] ObjectRef target,
            [CliArg("destination", "Destination world position as [x, y, z].", Required = true)] float[] destination)
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("'set_navmesh_agent_destination' requires Editor Play mode. Start play mode or use a custom RuntimeOnly command in a development Player.");
            if (destination == null || destination.Length != 3)
                throw new ArgumentException("destination must contain exactly three values: [x, y, z].");
            if (!ObjectResolver.TryResolve(target, out var obj, out var error))
                throw new ArgumentException(error);

            var agent = obj as NavMeshAgent ?? (obj as GameObject ?? (obj as Component)?.gameObject)?.GetComponent<NavMeshAgent>();
            if (agent == null)
                throw new ArgumentException($"Target '{target}' has no NavMeshAgent component.");
            if (!agent.isOnNavMesh)
                throw new InvalidOperationException($"NavMeshAgent '{agent.name}' is not on a NavMesh.");

            var value = new Vector3(destination[0], destination[1], destination[2]);
            var accepted = agent.SetDestination(value);
            return new { accepted, destination = new[] { value.x, value.y, value.z }, remainingDistance = agent.remainingDistance };
        }
    }
}
