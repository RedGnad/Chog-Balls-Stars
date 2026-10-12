namespace Quantum
{
  using Quantum.Core;

  public partial class AIConfig : AIConfigBase
	{
	}

	public static unsafe class AIConfigExtensions
	{
		public static AIConfig GetConfig(this HFSMAgent agent, FrameBase frame)
		{
			return (AIConfig)frame.FindAsset(agent.Config);
		}
		public static AIConfig GetConfig(this HFSMAgent agent, FrameThreadSafe frame)
		{
			return (AIConfig)frame.FindAsset(agent.Config);
		}

		public static AIConfig GetConfig(this BTAgent agent, FrameBase frame)
		{
			return (AIConfig)frame.FindAsset(agent.Config);
		}

		public static AIConfig GetConfig(this BTAgent agent, FrameThreadSafe frame)
		{
			return (AIConfig)frame.FindAsset(agent.Config);
		}

		public static AIConfig GetConfig(this UTAgent agent, FrameBase frame)
		{
			return (AIConfig)frame.FindAsset(agent.Config);
		}

		public static AIConfig GetConfig(this UTAgent agent, FrameThreadSafe frame)
		{
			return (AIConfig)frame.FindAsset(agent.Config);
		}
	}
}