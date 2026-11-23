// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	
	// コマンド：サウンドの終了待ち
	public class AdvCommandWaitSound : AdvCommandWaitBase
		, IAdvCommandEffect
		, IAdvCommandUpdateWait
	{
		SoundType SoundType { get; set; }
		string SoundName { get; set; }
		AdvEngine Engine { get; set; }

		internal AdvCommandWaitSound(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.SoundType = ParseCell<SoundType>(AdvColumnName.Arg1);
			this.SoundName = ParseCellOptional<string>(AdvColumnName.Arg2,"");
			WaitType = ParseCellOptional(AdvColumnName.WaitType, AdvCommandWaitType.Default);
		}

		//開始時のコールバック
		protected override void OnStart(AdvEngine engine, AdvScenarioThread thread)
		{
			Engine = engine;
		}

		//コマンド終了待ち
		public bool UpdateCheckWait()
		{
			switch (SoundType)
			{
				case SoundType.Bgm:
					return Engine.SoundManager.IsPlayingBgm();
				case SoundType.Ambience:
					return Engine.SoundManager.IsPlayingAmbience();
				case SoundType.Se:
					if (SoundName.IsNullOrEmpty())
					{
						return Engine.SoundManager.IsPlayingSe();
					}
					else
					{
						return Engine.SoundManager.IsPlayingSe(SoundName);
					}
				case SoundType.Voice:
					if (SoundName.IsNullOrEmpty())
					{
						return Engine.SoundManager.IsPlayingVoice();
					}
					else
					{
						return Engine.ScenarioSound.IsPlayingScenarioVoice(SoundName);
					}
				default:
					return false;
			}
		}
		
		public void OnEffectFinalize()
		{
			Engine = null;
		}

		//サウンドは再生を止めず、待機のみ解除する（スキップと同じ）
		public void OnEffectStop()
		{
		}

		public void OnEffectSkip()
		{
		}
	}
}
