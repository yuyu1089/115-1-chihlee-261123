// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
namespace Utage
{
	// 演出の強制停止コマンド。最終地点へは飛ばさず、その時点の状態で止める
	public class AdvCommandStopEffect : AdvCommand
	{
		//停止対象タイプ
		enum StopEffectType
		{
			All,
			NoWait,
		}

		private StopEffectType StopType { get; }
		private bool StopLoop { get; }

		public AdvCommandStopEffect(StringGridRow row)
			: base(row)
		{
			StopType = ParseCellOptional(AdvColumnName.Arg1, StopEffectType.All);
			StopLoop = ParseCellOptional(AdvColumnName.Arg2, false);
		}

		public override void DoCommand(AdvEngine engine)
		{
			switch (StopType)
			{
				case StopEffectType.All:
					CurrentTread.WaitManager.ForceStopAllEffect(StopLoop);
					break;
				case StopEffectType.NoWait:
					CurrentTread.WaitManager.ForceStopNoWaitEffect(StopLoop);
					break;
			}
		}
	}
}
