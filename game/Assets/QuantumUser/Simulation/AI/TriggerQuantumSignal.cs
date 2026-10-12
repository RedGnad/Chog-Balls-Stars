namespace Quantum
{
	[System.Serializable]
	public unsafe class TriggerQuantumSignal : AIAction
	{
		public enum QuantumSignal { None, CharacterSelectionStart, GameStart, ToggleControllers, GameOver }

		public QuantumSignal Signal;
		public bool BoolValue;

		public override void Execute(Frame frame, EntityRef entity, ref AIContext aiContext)
		{
			switch (Signal)
			{
				case QuantumSignal.CharacterSelectionStart:
					frame.Signals.OnCharacterSelectionStart();
					break;
				case QuantumSignal.GameStart:
					frame.Signals.OnGameStart();
					break;
				case QuantumSignal.ToggleControllers:
					frame.Signals.OnToggleControllers(BoolValue);
					break;
				case QuantumSignal.GameOver:
					frame.Signals.OnGameOver(BoolValue);
					break;
				default:
					break;
			}
		}
	}
}
