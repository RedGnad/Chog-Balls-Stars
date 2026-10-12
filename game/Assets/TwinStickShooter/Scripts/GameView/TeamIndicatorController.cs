namespace TwinStickShooter
{
	using Quantum;
	using UnityEngine;

	public class TeamIndicatorController : QuantumEntityViewComponent<CustomViewContext>
	{
		private SpriteRenderer _indicator;

		private void OnEnable()
		{
			_indicator = GetComponent<SpriteRenderer>();
		}

		private void Start()
		{
			DefineColor();
		}

		public void DefineColor()
		{
			if (ViewContext.LocalView == null)
				return;

			ColorSetter colorSetter = GetComponent<ColorSetter>();

			EntityRef localEntityRef = ViewContext.LocalView.EntityRef;

			TeamInfo teamInfo = PredictedFrame.Get<TeamInfo>(EntityRef);
			TeamInfo localPlayerteamInfo = PredictedFrame.Get<TeamInfo>(localEntityRef);

			if (EntityRef == localEntityRef)
			{
				colorSetter.SetLocal();
			}
			else
			{
				colorSetter.SetRemote(teamInfo.Index == localPlayerteamInfo.Index);
			}

			CharacterInvisibilityController.OnCharacterVisibilityChange += OnCharacterVisibilityChange;
		}

		private void OnCharacterVisibilityChange(bool value, EntityRef target)
		{
			if (target != EntityRef || _indicator == null)
			{
				return;
			}

			_indicator.gameObject.SetActive(value);
		}
	}
}
