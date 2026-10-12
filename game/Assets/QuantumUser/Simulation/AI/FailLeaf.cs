namespace Quantum
{
	[System.Serializable]
	public unsafe class FailLeaf : BTLeaf
	{
		protected override BTStatus OnUpdate(BTParams btParams, ref AIContext aiContext)
		{
			return BTStatus.Failure;
		}
	}
}
