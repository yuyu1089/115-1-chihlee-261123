// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;
using UtageExtensions;


namespace Utage
{

	/// <summary>
	/// マクロ定義の管理クラス
	/// </summary>
	[System.Serializable]
	public class AdvEntityData
	{
		[SerializeField]
		string[] originalStrings;

		public AdvEntityData(string[] originalStrings)
		{
			this.originalStrings = originalStrings;
		}

		//今のコマンドから、エンティティ処理したコマンドを作成
		public static AdvCommand CreateEntityCommand( AdvCommand original, AdvEngine engine, AdvScenarioPageData pageData)
		{
			StringGridRow row = new StringGridRow(original.RowData.Grid, original.RowData.RowIndex);
			row.DebugIndex = original.RowData.DebugIndex;

			string[] strings = original.EntityData.CreateCommandStrings(engine.Param.GetParameter);
			row.InitFromStringArray(strings);
			AdvCommand entityCommand = AdvCommandParser.CreateCommand(original.Id, row, engine.DataManager.SettingDataManager);
			if (entityCommand is IAdvInitOnCreateEntity)
			{
				(entityCommand as IAdvInitOnCreateEntity).InitOnCreateEntity(original);
			}
			return entityCommand;
		}

		//今のパラメーターを適用して、エンティティ処理した文字列を作成
		public string[] CreateCommandStrings(System.Func<string, object> getParameter)
		{
			Profiler.BeginSample("EntityData To CommandData");
			string[] strings = new string[this.originalStrings.Length];
			for (int i = 0; i < strings.Length; ++i)
			{
				string str = strings[i] = this.originalStrings[i];
				if (str.Length <= 1) continue;
				if (str.IndexOf('&') < 0) continue;

				StringBuilder builder = new StringBuilder();
				int index = 0;
				while (index < str.Length)
				{
					if (str[index] == '&' && TryResolveEntity(str, index, getParameter, out string value, out int nextIndex))
					{
						builder.Append(value);
						index = nextIndex;
						continue;
					}

					builder.Append(str[index]);
					++index;
				}
				strings[i] = builder.ToString();
			}
			Profiler.EndSample();
			return strings;
		}


		//エンティティがあるかの簡易チェック
		public static bool ContainsEntitySimple(StringGridRow row)
		{
			for (int i = 0; i < row.Strings.Length; ++i)
			{
				int index = row.Strings[i].IndexOf('&');
				if (index < 0) continue;

				string str = row.Strings[i];
				if (index+1 < str.Length && str[index + 1] == '&')
				{
					continue;
				}
				return true;
			}
			return false;
		}

		//今のパラメーターを適用して、エンティティ処理した文字列を作成
		public static bool TryCreateEntityStrings(StringGridRow original, System.Func<string, object> getParameter, out string[] strings)
		{
			bool succeed = false;
			strings = new string[original.Strings.Length];
			for (int i = 0; i < original.Strings.Length; ++i)
			{
				string str = strings[i] = original.Strings[i];
				if (str.Length <= 1) continue;
				if (str.IndexOf('&') < 0) continue;

				//WindowTypeとPageCtrlはエンティティによる変換を無視する
				if (original.Grid.TryGetColumnIndex(AdvColumnName.WindowType.QuickToString(), out var indexWindowType))
				{
					if (i == indexWindowType)
					{
						Debug.LogError(" Can not use entity in " + AdvColumnName.WindowType.QuickToString());
						return false;
					}
				}

				if (original.Grid.TryGetColumnIndex(AdvColumnName.PageCtrl.QuickToString(), out var indexPageCtrl))
				{
					if (i == indexPageCtrl)
					{
						Debug.LogError(" Can not use entity in " + AdvColumnName.PageCtrl.QuickToString());
						return false;
					}
				}

				StringBuilder builder = new StringBuilder();
				int index = 0;
				while (index < str.Length)
				{
					if (str[index] == '&' && TryResolveEntity(str, index, getParameter, out string value, out int nextIndex))
					{
						builder.Append(value);
						index = nextIndex;
						succeed = true;
						continue;
					}

					builder.Append(str[index]);
					++index;
				}
				strings[i] = builder.ToString();
			}
			return succeed;
		}

		//文字列中の1つの'&'エンティティを解決する
		//まずCheckEntitySeparatorで区切った短いキーを試し、それが見つからずかつ直後が'['なら、
		//ParamTbl[key].member形式のテーブルアクセスとして、区切り文字を含めた完全な文字列を1つのキーとして再度解決を試みる
		//（&ParamTbl[hoge].pointのように、テーブルアクセス構文全体を1つのエンティティとして指定するケースに対応するため）
		static bool TryResolveEntity(string str, int index, System.Func<string, object> getParameter, out string value, out int nextIndex)
		{
			value = null;
			nextIndex = index;
			if (index + 1 >= str.Length) return false;

			int shortEnd = FindKeyEnd(str, index + 1, CheckEntitySeparator);
			string shortKey = str.Substring(index + 1, shortEnd - index);
			object param = getParameter(shortKey);
			if (param != null)
			{
				value = param.ToString();
				nextIndex = shortEnd + 1;
				return true;
			}

			if (shortEnd + 1 < str.Length && str[shortEnd + 1] == '[')
			{
				int closeBracket = str.IndexOf(']', shortEnd + 2);
				if (closeBracket >= 0 && closeBracket + 1 < str.Length && str[closeBracket + 1] == '.' && closeBracket + 2 < str.Length)
				{
					int memberEnd = FindKeyEnd(str, closeBracket + 2, ExpressionToken.CheckSeparator);
					string longKey = str.Substring(index + 1, memberEnd - index);
					object tblParam = getParameter(longKey);
					if (tblParam != null)
					{
						value = tblParam.ToString();
						nextIndex = memberEnd + 1;
						return true;
					}
				}
			}
			return false;
		}

		//startから始まる識別子の終端インデックス(その文字自身を含む)を返す。isSeparatorが区切り文字と判定する文字の手前まで、または文字列末尾までを識別子とみなす
		static int FindKeyEnd(string str, int start, System.Func<char, bool> isSeparator)
		{
			int idx = start;
			while (idx < str.Length - 1 && !isSeparator(str[idx + 1]))
			{
				++idx;
			}
			return idx;
		}
		
		static bool CheckEntitySeparator(char c)
		{
			switch (c)
			{
				case '[':
				case ']':
				case '.':
					return true;
				default:
					return ExpressionToken.CheckSeparator(c);
			}
		}

	}
}
