using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	// Deals with characters being invisible or not. A character is invisible when they are inside a bush
	[Preserve]
	public unsafe class VisibilitySystem : SystemMainThreadFilter<VisibilitySystem.Filter>, ISignalOnTriggerEnter2D, ISignalOnTriggerExit2D,
		ISignalOnCreateSkill, ISignalOnCharacterDamage,
		ISignalOnComponentAdded<Invisibility>
	{
		public struct Filter
		{
			public EntityRef Entity;
			public Invisibility* Invisibility;
		}

		private readonly FP EXPOSURE_TIME = FP._2;

		public void OnAdded(Frame f, EntityRef entity, Invisibility* component)
		{
			// On the invisibility component, we will need store the static collider id linked to that specific bush
			// -1 means "no bush"
			component->StaticColliderId = -1;
		}

		public override void OnInit(Frame frame)
		{
			base.OnInit(frame);

			var invisibilitySpots = frame.AllocateDictionary<int, EntityRef>();

			// We keep track of all invisibility spots and their correlated Entity
			// This is useful so we can quickly ask for a spot and get it's position from the entity
			var allInvisibilitySpots = frame.Filter<InvisibilitySpot>();
			while (allInvisibilitySpots.NextUnsafe(out EntityRef entity, out InvisibilitySpot* inviSpot) == true)
			{
				invisibilitySpots.Add(inviSpot->StaticColliderId, entity);
			}

			frame.Global->InvisibilitySpots = invisibilitySpots;
		}

		public void OnCreateSkill(Frame frame, EntityRef character, FPVector2 characterPos, SkillData data, FPVector2 actionDirection)
		{
			// If the character uses a skill, it will become exposed for a few seconds
			SetCharacterExposed(frame, character);
		}

		public void OnCharacterDamage(Frame frame, EntityRef character)
		{
			// If the character is damaged, it will become exposed for a few seconds
			SetCharacterExposed(frame, character);
		}

		private void SetCharacterExposed(Frame frame, EntityRef character)
		{
			// What is hurt may be a tower or a core, which hides nowhere.
			if (frame.Unsafe.TryGetPointer<Invisibility>(character, out Invisibility* invisibility))
			{
				invisibility->ExposureTimer = EXPOSURE_TIME;
			}
		}

		public void OnTriggerEnter2D(Frame frame, TriggerInfo2D info)
		{
			if (info.IsStatic == true
				&& frame.Has<PlayerLink>(info.Entity) == true
				&& frame.Unsafe.TryGetPointer<Invisibility>(info.Entity, out var invisibility) == true)
			{
				// We store the amount of bushes that the character is in contact with
				// The character is not invisible when it is contact with no bush
				invisibility->InvisibilitySpotsCount++;
				invisibility->StaticColliderId = info.StaticData.ColliderIndex;
			}
		}

		public void OnTriggerExit2D(Frame frame, ExitInfo2D info)
		{
			if (info.IsStatic && frame.Has<PlayerLink>(info.Entity) && frame.Unsafe.TryGetPointer<Invisibility>(info.Entity, out var invisibility))
			{
				invisibility->InvisibilitySpotsCount--;
			}
		}

		public override void Update(Frame frame, ref Filter filter)
		{
			if (filter.Invisibility->ExposureTimer > 0)
			{
				filter.Invisibility->IsInvisible = false;
				filter.Invisibility->ExposureTimer -= frame.DeltaTime;
			}
			else
			{
				// We store the invisibility status on the component
				// This could be done with a boolean Property on a partial declaraion of the Invisibility structure too
				filter.Invisibility->IsInvisible = filter.Invisibility->InvisibilitySpotsCount > 0 ? true : false;
			}
		}
	}
}
