// Copyright (c) Files Community
// Licensed under the MIT License.

using Windows.Storage;

namespace DevWinUI
{
	public interface IStorageItemWithPath
	{
		public string Name { get; }

		public string Path { get; }

		public IStorageItem? Item { get; }

		public FilesystemItemType ItemType { get; }
	}
}
