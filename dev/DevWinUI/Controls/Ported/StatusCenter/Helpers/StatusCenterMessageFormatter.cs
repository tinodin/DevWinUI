// Copyright (c) Files Community
// Licensed under the MIT License.

using Jeffijoe.MessageFormat;
using System.Globalization;

namespace DevWinUI
{
	internal static class StatusCenterMessageFormatter
	{
		private static readonly CustomValueFormatters _customFormatter = new()
		{
			Number = (CultureInfo _, object? value, string? style, out string? formatted) =>
			{
				if (style is not null && style == string.Empty)
				{
					formatted = $"{value:#,##0}";
					return true;
				}

				formatted = null;
				return false;
			}
		};

		private static readonly MessageFormatter _formatter = new(
			useCache: false,
			culture: CultureInfo.GetCultureInfo("en"),
			customValueFormatter: _customFormatter);

		public static string Format(string resourceKey, object[] values)
		{
			var message = StatusCenterStrings.Get(resourceKey);

			if (string.IsNullOrEmpty(message))
				return string.Empty;

			var pairs = values
				.Select((value, index) => new KeyValuePair<string, object?>(index.ToString(), value))
				.ToDictionary(pair => pair.Key, pair => pair.Value);

			try
			{
				return _formatter.FormatMessage(message, pairs);
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}
