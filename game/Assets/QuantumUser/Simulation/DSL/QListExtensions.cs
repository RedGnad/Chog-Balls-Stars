using Quantum.Collections;
using System;

namespace Quantum
{
	public static partial class QListExtensions
	{
		public static bool Contains<T>(this QList<T> qList, T item) where T : unmanaged
		{
			for (int i = 0; i < qList.Count; i++)
			{
				if (qList[i].Equals(item) == true)
				{
					return true;
				}
			}

			return false;
		}
	}
}
