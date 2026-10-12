namespace Quantum
{
	public unsafe partial struct Immunity
	{
		public bool IsImmune => Timer > 0;
	}
}
