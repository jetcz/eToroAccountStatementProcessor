using System;
using System.Collections.Generic;

namespace eToroAccountStatementProcessor.BO
{
	public static class Extensions
	{
		public static bool IsIn(this string s, List<string> list)
		{
			for (int i = 0; i < list.Count; i++)
			{
				bool result = s.IndexOf(list[i], StringComparison.OrdinalIgnoreCase) >= 0;
				if (result)
				{
					return true;
				}
			}

			return false;
		}
	}
}
