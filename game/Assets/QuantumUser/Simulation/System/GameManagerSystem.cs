using Photon.Deterministic;
using UnityEngine.Scripting;

namespace Quantum
{
	[Preserve]
	public unsafe class GameManagerSystem : SystemMainThread, ISignalOnCharacterSelectionStart, ISignalOnGameStart, ISignalOnGameOver
	{
		public override void OnInit(Frame frame)
		{
			HFSMRoot hfsmRoot = frame.FindAsset<HFSMRoot>(frame.RuntimeConfig.GameManagerHFSM.Id);

			HFSMData* hfsmData = &frame.Global->GameManagerHFSM;
			hfsmData->Root = hfsmRoot;
			HFSMManager.Init(frame, hfsmData, default, hfsmRoot);
		}

		public override void Update(Frame frame)
		{
			HFSMManager.Update(frame, frame.DeltaTime, &frame.Global->GameManagerHFSM, default);
		}

		public void OnCharacterSelectionStart(Frame frame)
		{
			frame.Global->State = GameState.CharacterSelection;
		}

		public void OnGameStart(Frame frame)
		{
			frame.Global->State = GameState.Playing;
		}

		public void OnGameOver(Frame frame, QBoolean value)
		{
			frame.Global->State = GameState.Over;
		}
	}
}
