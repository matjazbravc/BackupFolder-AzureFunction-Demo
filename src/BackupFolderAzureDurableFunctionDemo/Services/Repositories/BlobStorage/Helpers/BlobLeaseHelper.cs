using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using BackupFolderAzureDurableFunctionDemo.Exceptions;
using BackupFolderAzureDurableFunctionDemo.Extensions;
using BackupFolderAzureDurableFunctionDemo.Services.Logging;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Config;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Providers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Helpers
{
	public sealed class BlobLeaseHelper : BlobStorageProviderBase
    {
		private readonly Dictionary<string, Lease> _acquiredLeases;
		private readonly ILog _log;
        private CloudBlockBlob _leaseBlob;

		public BlobLeaseHelper(ILog log, IRepositoryConfig repositoryConfig, ValidateStorage validateStorage, CloudBlockBlobMd5Helper cloudBlockBlobMd5Helper, BlobRequestOptionsHelper blobRequestOptionsHelper)
			: base(log, validateStorage, cloudBlockBlobMd5Helper, blobRequestOptionsHelper)
		{
		    _log = log;
		    _log.Debug();
			_acquiredLeases = new Dictionary<string, Lease>();
		    ConnectionString = repositoryConfig.StorageAccountConnectionString;
		}

		public void Dispose()
		{
			_log.Debug();
			_acquiredLeases.ToList().ForEach(pair => pair.Value.KeepAlive.Dispose());
		}

		public bool HasLease()
		{
			_log.Debug();
			ThrowWhenLeaseBlobDoesNotExist();
			return _acquiredLeases.ContainsKey(_leaseBlob.Name);
		}

		public void Initialize(string containerName, string blobName)
		{
			_log.Debug();

            Initialize(containerName);
			_leaseBlob = GetBlockBlob(blobName) ?? SetBlobFromStringAsync(blobName, "LeaseBlobDummyContent").Result;
		}

		public void ReleaseLease()
		{
			_log.Debug();
			if (!HasLease())
			{
				return;
			}
			var leaseId = GetLeaseId();
			ClearLease();
			try
			{
				_leaseBlob.ReleaseLeaseAsync(new AccessCondition
				{
					LeaseId = leaseId
				}).Wait();
			}
			catch (StorageException)
			{
				_log.Info($"{LogHashTags.LeaseFailedRelaseLock.GetHashTag()} failed attempt at releasing blob lock on {_leaseBlob.Name} using lease id {leaseId}");
			}
		}

		public bool TryAcquireLease(int leaseTimeInSeconds = 30)
		{
			_log.Debug();
			ThrowWhenLeaseBlobDoesNotExist();
			Contract.Requires<ArgumentNullException>(leaseTimeInSeconds >= 15 && leaseTimeInSeconds <= 60, "value must be greater than 15 and smaller than 60");
			try
			{
				var proposedLeaseId = Guid.NewGuid().ToString();
				var leaseTime = TimeSpan.FromSeconds(leaseTimeInSeconds);
				var leaseId = _leaseBlob.AcquireLeaseAsync(leaseTime, proposedLeaseId).Result;
				UpdateAcquiredLease(leaseId, leaseTimeInSeconds);
				_log.Info($"{LogHashTags.LeaseAquired.GetHashTag()} successfully acquire lease for a blob {_leaseBlob.Name}");
				return true;
			}
			catch (StorageException ex)
			{
				// Probably exception (409) Conflict: There is already a lease present
				var requestInformation = ex.RequestInformation;
				var information = requestInformation.ExtendedErrorInformation;
				if (information != null)
				{
					var message = $"({information.ErrorCode}) {information.ErrorMessage}";
					switch (information.ErrorCode)
					{
						case "LeaseAlreadyPresent":
							//_log.Warn(message);
							break;
						case "ContainerNotFound":
							_log.Error(message);
							break;
						default:
							_log.Warn($"{LogHashTags.LeaseAquireFailed.GetHashTag()} failed to acquire lease for a blob {_leaseBlob.Name}");
							break;
					}
					return false;
				}
				_log.Warn($"{LogHashTags.LeaseAquireFailed.GetHashTag()} failed to acquire lease for a blob {_leaseBlob.Name}");
				return false;
			}
		}

		private void ClearLease()
		{
			_log.Debug();
			ThrowWhenLeaseBlobDoesNotExist();
			var name = _leaseBlob.Name;
			var lease = _acquiredLeases[name];
			lease.KeepAlive.Dispose();
			_acquiredLeases.Remove(name);
		}

		private string GetLeaseId()
		{
			_log.Debug();
			ThrowWhenLeaseBlobDoesNotExist();
			return HasLease() ? _acquiredLeases[_leaseBlob.Name].LeaseId : string.Empty;
		}

		private Lease MakeLease(string leaseId, int lockTimeInSeconds)
		{
			_log.Debug();
			var interval = TimeSpan.FromSeconds(lockTimeInSeconds - 1);
			return new Lease
			{
				LeaseId = leaseId,
				KeepAlive = Observable.Interval(interval).Subscribe(l => RenewLease())
			};
		}

		private void RenewLease()
		{
			_log.Debug();
			if (!HasLease())
			{
				return;
			}
			try
			{
				_leaseBlob.RenewLeaseAsync(new AccessCondition
					{
						LeaseId = _acquiredLeases[_leaseBlob.Name].LeaseId
					}).Wait();
			}
			catch (StorageException)
			{
				_acquiredLeases.Remove(_leaseBlob.Name);
				_log.Info($"{LogHashTags.LeaseRemoved.GetHashTag()} removed lease from acquired leases for a blob {_leaseBlob.Name}");
			}
		}

		private void ThrowWhenLeaseBlobDoesNotExist([CallerMemberName] string memberName = "")
		{
			// inspired by: http://www.kunal-chowdhury.com/2012/07/whats-new-in-c-50-learn-about.html
			if (_leaseBlob != null)
			{
				return;
			}
			var exceptionMessage = $"{memberName}: before usage, Initialize must be called to create lease blob";
			throw new LeaseBlobDoesNotExistException(exceptionMessage);
		}

		private void UpdateAcquiredLease(string leaseId, int lockTimeInSeconds)
		{
			if (HasLease())
			{
				ClearLease();
			}
			_acquiredLeases.Add(_leaseBlob.Name, MakeLease(leaseId, lockTimeInSeconds));
		}

		private struct Lease
		{
			internal IDisposable KeepAlive { get; set; }

			internal string LeaseId { get; set; }
		}
	}
}