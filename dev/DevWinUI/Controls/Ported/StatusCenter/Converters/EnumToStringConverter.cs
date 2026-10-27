// Copyright (c) Files Community
// Licensed under the MIT License.

namespace DevWinUI
{
	internal sealed partial class EnumToStringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value?.ToString() ?? string.Empty;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
