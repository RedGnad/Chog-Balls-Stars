namespace Quantum
{
	[System.Serializable]
	public unsafe class SetTactic : AIAction
	{
		public string TacticEvent;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			HFSMManager.TriggerEvent(frame, entity, TacticEvent);
		}
	}
}
