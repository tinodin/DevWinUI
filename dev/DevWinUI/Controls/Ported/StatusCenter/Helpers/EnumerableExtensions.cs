// Copyright (c) Files Community
// Licensed under the MIT License.

namespace DevWinUI
{
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Creates <see cref="List{T}"/> and returns <see cref="IEnumerable{T}"/> with provided <paramref name="item"/>
		/// </summary>
		/// <typeparam name="T">The item type</typeparam>
		/// <param name="item">The item</param>
		/// <returns><see cref="IEnumerable{T}"/> with <paramref name="item"/></returns>
		public static IEnumerable<T> CreateEnumerable<T>(this T item)
		{
			return [item];
		}
	}
}
