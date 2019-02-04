using BackupFolderAzureDurableFunctionDemo.Extensions;

namespace BackupFolderAzureDurableFunctionDemo.Services.Logging
{
	public enum LogHashTags
	{
		[HashTag("#lease_acquire_failed")] LeaseAquireFailed,
		[HashTag("#lease_acquired")] LeaseAquired,
		[HashTag("#lease_failed_rel_lock")] LeaseFailedRelaseLock,
		[HashTag("#lease_removed")] LeaseRemoved
	}
}