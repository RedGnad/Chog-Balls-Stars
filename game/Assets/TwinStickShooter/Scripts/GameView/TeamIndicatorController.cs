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

		// Whether the colour was set: a player who joins a match under way meets the other heroes before
		// its own, so the colour is tried again until the local hero exists.
		private bool _defined;

		public override void OnUpdateView()
		{
			if (_defined == false)
				DefineColor();
		}

		public void DefineColor()
		{
			if (_defined || ViewContext.LocalView == null)
				return;

			EntityRef localEntityRef = ViewContext.LocalView.EntityRef;
			if (PredictedFrame.Exists(localEntityRef) == false || PredictedFrame.Exists(EntityRef) == false)
				return;
			_defined = true;

			ColorSetter colorSetter = GetComponent<ColorSetter>();

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
