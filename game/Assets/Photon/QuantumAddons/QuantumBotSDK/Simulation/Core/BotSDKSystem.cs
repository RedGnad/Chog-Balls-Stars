namespace Quantum.BotSDK
{
  using UnityEngine.Scripting;

  /// <summary>
  /// Using this system is optional. It is only used to aim the Debugger on the Unity side.
  /// It is also safe to copy logic from this system into your own systems, if it better suits your architecture.
  /// </summary>
  [Preserve]
	public unsafe partial class BotSDKSystem : SystemSignalsOnly, ISignalOnComponentAdded<HFSMAgent>,
																												ISignalOnComponentAdded<BTAgent>, ISignalOnComponentRemoved<BTAgent>,
																												ISignalOnComponentAdded<UTAgent>, ISignalOnComponentRemoved<UTAgent>,
																												ISignalOnComponentAdded<AIBlackboardComponent>, ISignalOnComponentRemoved<AIBlackboardComponent>
	{
		// ========== HFSM ============================================================================================

		public void OnAdded(Frame frame, EntityRef entity, HFSMAgent* component)
		{
			HFSMData* hfsmData = &component->Data;
			if (hfsmData->Root == default)
				return;

			HFSMRoot rootAsset = frame.FindAsset<HFSMRoot>(hfsmData->Root.Id);
			HFSMManager.Init(frame, entity, rootAsset);
		}

		// ========== BT ============================================================================================

		public void OnAdded(Frame frame, EntityRef entity, BTAgent* component)
		{
      // Mainly used to automatically initialize entity prototypes
      // If the prototype's Tree reference is not default and the BTAgent
      // is not initialized yet, then it is initialized here;
			if (component->Tree != default)
			{
        var btRoot = frame.FindAsset<BTRoot>(component->Tree.Id);
        BTManager.Init(frame, entity, btRoot);
			}
		}

		public void OnRemoved(Frame frame, EntityRef entity, BTAgent* component)
		{
      BTManager.Free(frame, entity);
		}

		// ========== UT ============================================================================================

		public void OnAdded(Frame frame, EntityRef entity, UTAgent* component)
		{
			UTManager.Init(frame, &component->UtilityReasoner, component->UtilityReasoner.UTRoot, entity);
		}

		public void OnRemoved(Frame frame, EntityRef entity, UTAgent* component)
		{
			component->UtilityReasoner.Free(frame);
		}

    // ========== BLACKBOARD ============================================================================================

    public void OnAdded(Frame frame, EntityRef entity, AIBlackboardComponent* component)
    {
      if(component->Board != null)
      {
        var blackboardAsset = frame.FindAsset<AIBlackboard>(component->Board.Id);
        blackboardAsset.Initialize(frame, component);
      }
    }

    public void OnRemoved(Frame frame, EntityRef entity, AIBlackboardComponent* component)
		{
			component->Free(frame);
		}
  }
}
