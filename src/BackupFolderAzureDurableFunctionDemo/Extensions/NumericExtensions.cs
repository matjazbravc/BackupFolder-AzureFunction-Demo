using System;

namespace BackupFolderAzureDurableFunctionDemo.Extensions
{
	public static class NumericExtensions
	{
		private const long ONE_KB = 1024;
		private const long ONE_MB = ONE_KB * 1024;
		private const long ONE_GB = ONE_MB * 1024;
		private const long ONE_TB = ONE_GB * 1024;

		public static string ToPrettySize(this int value, int decimalPlaces = 0)
		{
			return ((long)value).ToPrettySize(decimalPlaces);
		}

		public static string ToPrettySize(this long value, int decimalPlaces = 0)
		{
			var asTb = Math.Round((double)value / ONE_TB, decimalPlaces);
			var asGb = Math.Round((double)value / ONE_GB, decimalPlaces);
			var asMb = Math.Round((double)value / ONE_MB, decimalPlaces);
			var asKb = Math.Round((double)value / ONE_KB, decimalPlaces);
			var chosenValue = asTb > 1 ? $"{asTb}Tb"
			    : asGb > 1 ? $"{asGb}Gb"
			        : asMb > 1 ? $"{asMb}Mb"
			            : asKb > 1 ? $"{asKb}Kb"
			                : $"{Math.Round((double) value, decimalPlaces)}B";
			return chosenValue;
		}
	}
}
