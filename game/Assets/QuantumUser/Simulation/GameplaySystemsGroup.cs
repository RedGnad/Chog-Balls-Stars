namespace Quantum
{
	using UnityEngine.Scripting;
	[Preserve]
	public class GameplaySystemsGroup : SystemMainThreadGroup
	{
		public GameplaySystemsGroup(params SystemBase[] children) : base(children) { }
		public GameplaySystemsGroup(string update, params SystemMainThread[] children) : base(children)
		{
		}
	}
}