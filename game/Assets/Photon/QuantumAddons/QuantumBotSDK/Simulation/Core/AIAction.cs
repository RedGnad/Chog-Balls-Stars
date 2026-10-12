namespace Quantum
{
	public abstract partial class AIAction : AIActionBase
	{
		public override void Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
		{
			Execute((Frame)frame, entity, ref aiContext);
		}

		public virtual void Execute(Frame frame, EntityRef entity, ref AIContext aiContext) { }
	}
}