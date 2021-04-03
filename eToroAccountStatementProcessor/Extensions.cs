using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eToroAccountStatementProcessor
{
	public static class Extensions
	{
		public static bool Contains(this string s, List<string> list)
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
