namespace Quantum
{
	public class AIFunction : AIFunctionBase
	{
	}

	public abstract partial class AIFunction<T> : AIFunctionBase<T>
	{
		public virtual T Execute(Frame frame, EntityRef entity, ref AIContext aiContext) { return default; }

		public override T Execute(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
		{
			return Execute((Frame)frame, entity, ref aiContext);
		}
	}
}