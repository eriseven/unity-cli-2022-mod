using Unity.Pipeline.Commands;
using Unity.Pipeline.Editor;

namespace Unity.Pipeline.Tests.Editor
{
    // Keep the inherited server tests isolated while exercising 0.9's executor-based gates.
    class IsolatedEditorServer : EditorPipelineServer
    {
        protected override bool AdvertisesEndpoint => false;
        protected override bool IsAdvertised => true;
        protected virtual bool IsSettled => EditorExecutor.IsSettled;
        protected static bool IsSessionSettled => EditorCommandExecutor.IsSessionSettled;
        protected virtual void OnCommandDoneMainThread(in CommandExecutionInfo info) { }
        protected virtual bool? PinnedSettled => null;

        internal override DefaultCommandExecutor CreateExecutor(ICommandRegistry registry, ICommandQueue queue)
            => new TestExecutor(this, registry, queue);

        sealed class TestExecutor : EditorCommandExecutor
        {
            readonly IsolatedEditorServer m_Owner;
            public TestExecutor(IsolatedEditorServer owner, ICommandRegistry registry, ICommandQueue queue)
                : base(registry, queue, owner.Dialogs, owner.Dispatcher) { m_Owner = owner; }
            internal override bool IsSettled => m_Owner.PinnedSettled ?? base.IsSettled;
            protected override void ReportOnMainThread(in CommandExecutionInfo info)
                => m_Owner.OnCommandDoneMainThread(info);
        }
    }
}
