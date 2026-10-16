namespace Quantum
{
	public unsafe partial struct QuantumDemoInputTopDown
	{
		// The sample's Dash button, which this game never uses, says whether the attacks of this
		// tick are aimed with a pointer (a mouse). Those leave when their button is pressed, since
		// a mouse already aims then; a stick's leave when it is let go, once the finger has aimed.
		// Records made before it existed carry it up, and replay as they did.
		public Button AimsWithPointer
		{
			get => Dash;
			set => Dash = value;
		}

		// Whether this button starts its attack on this tick.
		public bool Casts(Button button) => AimsWithPointer ? button.WasPressed : button.WasReleased;
	}
}
