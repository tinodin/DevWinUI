// Copyright (c) Files Community
// Licensed under the MIT License.

using ByteSize = ByteSizeLib.ByteSize;

namespace DevWinUI
{
	internal static class StatusCenterStringExtensions
	{
		public static string ToSizeString(this double size) => ByteSize.FromBytes(size).ToSizeString();
		public static string ToSizeString(this long size) => ByteSize.FromBytes(size).ToSizeString();
		public static string ToSizeString(this ulong size) => ByteSize.FromBytes(size).ToSizeString();
		public static string ToSizeString(this decimal size) => ByteSize.FromBytes((double)size).ToSizeString();
		public static string ToSizeString(this ByteSize size) => size.ToBinaryString();

		public static string GetStatusCenterResource(this string resourceKey) => StatusCenterStrings.Get(resourceKey);

		public static string GetStatusCenterFormatResource(this string resourceKey, params object[] values) => StatusCenterStrings.Format(resourceKey, values);
	}
}
