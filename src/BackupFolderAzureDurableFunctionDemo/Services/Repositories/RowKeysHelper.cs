using System;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories
{
	// inspired by: http://fabriccontroller.net/blog/posts/nuget-package-for-storing-table-storage-entities-in-reverse-chronological-order/
	public static class RowKeysHelper
	{
		public const string SEPARATOR = "-";

		public static long GetTicksChronological(DateTime dateTime)
		{
			return new DateTimeOffset(dateTime).UtcDateTime.Ticks;
		}

		public static long GetTicksDescending(DateTime dateTime)
		{
			return (DateTimeOffset.MaxValue.UtcDateTime - new DateTimeOffset(dateTime).UtcDateTime).Ticks;
		}

		private static string FormatKey(long ticks, string suffix)
		{
			return $"{ticks:d21}{SEPARATOR}{suffix}";
		}

		private static string FormatKeyTicks(long ticks)
		{
			return $"{ticks:d21}";
		}

		public static string CreateChronological(DateTime dateTime, bool useOnlyTicks = false)
		{
			return CreateChronological(dateTime, Guid.NewGuid().ToString("N").ToUpper(), useOnlyTicks);
		}

		public static string CreateChronologicalKeyStart(DateTime dateTime, bool useOnlyTicks = false)
		{
			return CreateChronological(dateTime, string.Empty, useOnlyTicks);
		}

		public static string CreateChronological(DateTime dateTime, string suffix, bool useOnlyTicks = false)
		{
			var result = useOnlyTicks ? FormatKeyTicks(GetTicksChronological(dateTime)) : FormatKey(GetTicksChronological(dateTime), suffix);
			return result;
		}

		public static string CreateReverseChronological(DateTime dateTime, bool useOnlyTicks = false)
		{
			return CreateReverseChronological(dateTime, Guid.NewGuid().ToString("N").ToUpper(), useOnlyTicks);
		}

		public static string CreateReverseChronological(DateTime dateTime, string suffix, bool useOnlyTicks = false)
		{
			var result = useOnlyTicks ? FormatKeyTicks(GetTicksChronological(dateTime)) : FormatKey(GetTicksChronological(dateTime), suffix);
			return result;
		}

		public static string CreateReverseChronologicalKeyStart(DateTime dateTime, bool useOnlyTicks = false)
		{
			return CreateReverseChronological(dateTime, string.Empty, useOnlyTicks);
		}
	}
}