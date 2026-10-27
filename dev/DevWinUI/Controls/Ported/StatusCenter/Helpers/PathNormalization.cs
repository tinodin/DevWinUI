// Copyright (c) Files Community
// Licensed under the MIT License.

namespace DevWinUI
{
	public static class PathNormalization
	{
		public static string GetParentDir(string? path)
		{
			if (string.IsNullOrEmpty(path))
				return string.Empty;

			var index = path.Contains('/', StringComparison.Ordinal) ? path.LastIndexOf('/') : path.LastIndexOf('\\');
			return path.Substring(0, index != -1 ? index : path.Length);
		}
	}
}
