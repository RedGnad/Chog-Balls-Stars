using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	// This is the basic skills "State Machine", dealing with the activation/update/deactivation of a skill entity
	[Preserve]
	public unsafe class SkillSystem : SystemMainThreadFilter<SkillSystem.Filter>, ISignalOnCreateSkill
	{
		public struct Filter
		{
			public EntityRef Entity;
			public Skill* Skill;
		}

		// Polymorphic "OnCreate" is called with skill specific logic
		// Also, trigger an event for the character to execute the Attack animation
		public void OnCreateSkill(Frame frame, EntityRef character, FPVector2 characterPos, SkillData data, FPVector2 actionVector)
		{
			data.OnCreate(frame, character, data, characterPos, actionVector);

			frame.Events.CharacterSkill(character);
		}

		// Polymorphic "OnUpdate" is called with skill specific logic
		// When the TTL is over, we call the polymorphic "OnDisable"
		public override void Update(Frame frame, ref Filter filter)
		{
			SkillData data = frame.FindAsset<SkillData>(filter.Skill->SkillData.Id);
			data.OnUpdate(frame, filter.Skill->Source, filter.Entity, filter.Skill);
			if (data.HasTTL == true && filter.Skill->TTL >= (data.ActionAmount * data.ActionInterval))
			{
				data.OnDeactivate(frame, filter.Entity, filter.Skill);
			}
		}
	}
}
