using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	[Preserve]
	public unsafe class InputSystem : SystemMainThreadFilter<InputSystem.Filter>, ISignalOnToggleControllers
	{
		public struct Filter
		{
			public EntityRef Entity;
			public Transform2D* Transform;
			public Character* Character;
      public InputContainer* InputContainer;
		}

		// This system deals with getting the Input structure from the appropriate place, before actually applying it to the characters
		// The input can either come from a Player with GetPlayerInput, or it might come from a Input structure stored in the Bots component
		// This is useful because we can re-use the same characters logic for both players and Bots
		public override void Update(Frame frame, ref Filter filter)
		{
			if (frame.Global->ControllersEnabled == false)
				return;

			int playerRef = frame.Get<PlayerLink>(filter.Entity).PlayerRef;
			bool controlledByBot = IsControlledByAI(frame, filter, playerRef);

			if (controlledByBot == false)
			{
				filter.InputContainer->Input = *frame.GetPlayerInput(playerRef);
			}
			else
			{
        filter.InputContainer->Input = frame.Get<Bot>(filter.Entity).Input;
			}
		}

		// Enable/disable input, used to pause characters actions when the game is starting and when it is over
		public void OnToggleControllers(Frame frame, QBoolean value)
		{
			frame.Global->ControllersEnabled = value;
		}

		private bool IsControlledByAI(Frame frame, Filter filter, int playerRef)
		{
			// If the player is not connected, we turn it into a bot
			bool playerNotPresent = frame.GetPlayerInputFlags(playerRef).HasFlag(DeterministicInputFlags.PlayerNotPresent) == true;
			if (playerNotPresent == true && frame.Get<Bot>(filter.Entity).IsActive == false)
			{
				if (frame.IsVerified)
				{
					AISetupHelper.Botify(frame, filter.Entity);
				}
			}

			if (frame.TryGet(filter.Entity, out Bot bot) == false)
				return false;

			if(bot.IsActive == false)
			{
				return false;
			}
			return true;
		}
	}
}
