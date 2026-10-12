using Photon.Deterministic;
using System;
using System.Collections.Generic;

namespace Quantum
{
	public unsafe abstract partial class CollectibleData : AssetObject
	{
		public AssetRef<EntityPrototype> Prototype;

		/// <summary>
		/// Can the entity get this collectible at this moment?
		/// </summary>
		public virtual bool CanCollect(Frame frame, EntityRef entity, Inventory* inventory)
		{
			return true;
		}

		/// <summary>
		/// Defines what happens when the entity gets this collectible. Used to perform polymorphic collect logic
		/// </summary>
		public abstract void OnCollect(Frame frame, EntityRef entity, Inventory* inventory);

		/// <summary>
		/// When dropping an item, we scatter it around the entity in random position on a circle
		/// </summary>
		public virtual void OnDrop(Frame frame, EntityRef entity, Inventory* inventory, int amount, FPVector2 position)
		{
			for (int i = 0; i < amount; i++)
			{
				EntityRef collectible = frame.Create(Prototype);
				Transform2D* collectibleTransform = frame.Unsafe.GetPointer<Transform2D>(collectible);
				collectibleTransform->Position = FPVector2Helpers.RandomInsideCircle(frame, position, 1);
			}
		}
	}
}
