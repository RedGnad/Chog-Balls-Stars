using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	[Preserve]
	public unsafe class InventorySystem : SystemSignalsOnly, ISignalOnTriggerEnter2D, ISignalOnCharacterDefeated
	{
		public void OnTriggerEnter2D(Frame frame, TriggerInfo2D info)
		{
			// Upon collision, we detect if it happens between a Character and a Collectible
			if(frame.Has<Character>(info.Entity) == false || frame.Get<Health>(info.Entity).IsDead == true)
			{
				return;
			}

			if (frame.Unsafe.TryGetPointer<Collectible>(info.Other, out var collectible) == false)
			{
				return;
			}

			// We try to get that collectible so, if needed, we can implement some logics on the collectibles data
			// which would block a character from collecting something
			CollectibleData data = frame.FindAsset<CollectibleData>(collectible->CollectibleData.Id);
			Inventory* inventory = frame.Unsafe.GetPointer<Inventory>(info.Entity);
			if (data.CanCollect(frame, info.Entity, inventory) == true)
			{
				// If we can collect the item, then we call it's polymorphic collect logic
				data.OnCollect(frame, info.Entity, inventory);

				// Apply the changes to the inventory
				// It currently can only hold one type of collectible
				inventory->CollectibleData = data;
				inventory->CollectiblesAmount += 1;

				frame.Destroy(info.Other);
			}
		}

		// When a character is defeated, we drop it's collectibles on the ground
		public void OnCharacterDefeated(Frame frame, EntityRef character)
		{
			Inventory* inventory = frame.Unsafe.GetPointer<Inventory>(character);
			int collectiblesAmount = inventory->CollectiblesAmount;

			if (collectiblesAmount > 0)
			{
				FPVector2 characterPosition = frame.Get<Transform2D>(character).Position;
				CollectibleData data = frame.FindAsset<CollectibleData>(inventory->CollectibleData.Id);
				data.OnDrop(frame, character, inventory, collectiblesAmount, characterPosition);

				inventory->CollectiblesAmount -= collectiblesAmount;
			}
		}
	}
}
