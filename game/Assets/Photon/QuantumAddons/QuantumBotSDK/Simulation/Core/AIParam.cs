namespace Quantum
{
	public abstract partial class AIParam<T> : AIParamBase<T>
	{
		protected override T GetFunctionValue(FrameThreadSafe frame, EntityRef entity, ref AIContext aiContext)
		{
			return GetFunctionValue((Frame)frame, entity, ref aiContext);
		}

		protected virtual T GetFunctionValue(Frame frame, EntityRef entity, ref AIContext aiContext) { return default; }
	}
}