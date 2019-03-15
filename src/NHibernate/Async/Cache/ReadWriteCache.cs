﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Cache.Access;

namespace NHibernate.Cache
{
	using System.Threading.Tasks;
	using System.Threading;
	public partial class ReadWriteCache : IBatchableCacheConcurrencyStrategy
	{
		private readonly NHibernate.Util.AsyncLock _lockObjectAsync = new NHibernate.Util.AsyncLock();

		/// <summary>
		/// Do not return an item whose timestamp is later than the current
		/// transaction timestamp. (Otherwise we might compromise repeatable
		/// read unnecessarily.) Do not return an item which is soft-locked.
		/// Always go straight to the database instead.
		/// </summary>
		/// <remarks>
		/// Note that since reading an item from that cache does not actually
		/// go to the database, it is possible to see a kind of phantom read
		/// due to the underlying row being updated after we have read it
		/// from the cache. This would not be possible in a lock-based
		/// implementation of repeatable read isolation. It is also possible
		/// to overwrite changes made and committed by another transaction
		/// after the current transaction read the item from the cache. This
		/// problem would be caught by the update-time version-checking, if 
		/// the data is versioned or timestamped.
		/// </remarks>
		public async Task<object> GetAsync(CacheKey key, long txTimestamp, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (await _lockObjectAsync.LockAsync())
			{
				if (log.IsDebugEnabled())
				{
					log.Debug("Cache lookup: {0}", key);
				}

				// commented out in H3.1
				/*try
				{
					cache.Lock( key );*/

				ILockable lockable = (ILockable) await (Cache.GetAsync(key, cancellationToken)).ConfigureAwait(false);

				bool gettable = lockable != null && lockable.IsGettable(txTimestamp);

				if (gettable)
				{
					if (log.IsDebugEnabled())
					{
						log.Debug("Cache hit: {0}", key);
					}

					return ((CachedItem) lockable).Value;
				}
				else
				{
					if (log.IsDebugEnabled())
					{
						if (lockable == null)
						{
							log.Debug("Cache miss: {0}", key);
						}
						else
						{
							log.Debug("Cached item was locked: {0}", key);
						}
					}
					return null;
				}
				/*}
				finally
				{
					cache.Unlock( key );
				}*/
			}
		}

		public async Task<object[]> GetManyAsync(CacheKey[] keys, long timestamp, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (log.IsDebugEnabled())
			{
				log.Debug("Cache lookup: {0}", string.Join(",", keys.AsEnumerable()));
			}
			var result = new object[keys.Length];
			using (await _lockObjectAsync.LockAsync())
			{
				var lockables = await (_cache.GetManyAsync(keys.Select(o => (object) o).ToArray(), cancellationToken)).ConfigureAwait(false);
				for (var i = 0; i < lockables.Length; i++)
				{
					var lockable = (ILockable) lockables[i];
					var gettable = lockable != null && lockable.IsGettable(timestamp);

					if (gettable)
					{
						if (log.IsDebugEnabled())
						{
							log.Debug("Cache hit: {0}", keys[i]);
						}
						result[i] = ((CachedItem) lockable).Value;
					}

					if (log.IsDebugEnabled())
					{
						log.Debug(lockable == null ? "Cache miss: {0}" : "Cached item was locked: {0}", keys[i]);
					}

					result[i] = null;
				}
			}
			return result;
		}

		/// <summary>
		/// Stop any other transactions reading or writing this item to/from
		/// the cache. Send them straight to the database instead. (The lock
		/// does time out eventually.) This implementation tracks concurrent
		/// locks by transactions which simultaneously attempt to write to an
		/// item.
		/// </summary>
		public async Task<ISoftLock> LockAsync(CacheKey key, object version, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (await _lockObjectAsync.LockAsync())
			{
				if (log.IsDebugEnabled())
				{
					log.Debug("Invalidating: {0}", key);
				}

				var lockValue = await (_cache.LockAsync(key, cancellationToken)).ConfigureAwait(false);
				try
				{
					ILockable lockable = (ILockable) await (Cache.GetAsync(key, cancellationToken)).ConfigureAwait(false);
					long timeout = Cache.NextTimestamp() + Cache.Timeout;
					CacheLock @lock = lockable == null ?
					                  CacheLock.Create(timeout, NextLockId(), version) :
					                  lockable.Lock(timeout, NextLockId());
					await (Cache.PutAsync(key, @lock, cancellationToken)).ConfigureAwait(false);
					return @lock;
				}
				finally
				{
					await (_cache.UnlockAsync(key, lockValue, cancellationToken)).ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// Do not add an item to the cache unless the current transaction
		/// timestamp is later than the timestamp at which the item was
		/// invalidated. (Otherwise, a stale item might be re-added if the
		/// database is operating in repeatable read isolation mode.)
		/// </summary>
		/// <returns>Whether the items were actually put into the cache</returns>
		public async Task<bool[]> PutManyAsync(
			CacheKey[] keys, object[] values, long timestamp, object[] versions, IComparer[] versionComparers,
			bool[] minimalPuts, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var result = new bool[keys.Length];
			if (timestamp == long.MinValue)
			{
				// MinValue means cache is disabled
				return result;
			}

			using (await _lockObjectAsync.LockAsync())
			{
				if (log.IsDebugEnabled())
				{
					log.Debug("Caching: {0}", string.Join(",", keys.AsEnumerable()));
				}
				var keysArr = keys.Cast<object>().ToArray();
				var lockValue = await (_cache.LockManyAsync(keysArr, cancellationToken)).ConfigureAwait(false);
				try
				{
					var putBatch = new Dictionary<object, object>();
					var lockables = await (_cache.GetManyAsync(keysArr, cancellationToken)).ConfigureAwait(false);
					for (var i = 0; i < keys.Length; i++)
					{
						var key = keys[i];
						var version = versions[i];
						var lockable = (ILockable) lockables[i];
						bool puttable = lockable == null ||
						                lockable.IsPuttable(timestamp, version, versionComparers[i]);
						if (puttable)
						{
							putBatch.Add(key, CachedItem.Create(values[i], Cache.NextTimestamp(), version));
							if (log.IsDebugEnabled())
							{
								log.Debug("Cached: {0}", key);
							}
							result[i] = true;
						}
						else
						{
							if (log.IsDebugEnabled())
							{
								if (lockable.IsLock)
								{
									log.Debug("Item was locked: {0}", key);
								}
								else
								{
									log.Debug("Item was already cached: {0}", key);
								}
							}
							result[i] = false;
						}
					}

					if (putBatch.Count > 0)
					{
						await (_cache.PutManyAsync(putBatch.Keys.ToArray(), putBatch.Values.ToArray(), cancellationToken)).ConfigureAwait(false);
					}
				}
				finally
				{
					await (_cache.UnlockManyAsync(keysArr, lockValue, cancellationToken)).ConfigureAwait(false);
				}
			}
			return result;
		}

		/// <summary>
		/// Do not add an item to the cache unless the current transaction
		/// timestamp is later than the timestamp at which the item was
		/// invalidated. (Otherwise, a stale item might be re-added if the
		/// database is operating in repeatable read isolation mode.)
		/// </summary>
		/// <returns>Whether the item was actually put into the cache</returns>
		public async Task<bool> PutAsync(CacheKey key, object value, long txTimestamp, object version, IComparer versionComparator,
		                bool minimalPut, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (txTimestamp == long.MinValue)
			{
				// MinValue means cache is disabled
				return false;
			}

			using (await _lockObjectAsync.LockAsync())
			{
				if (log.IsDebugEnabled())
				{
					log.Debug("Caching: {0}", key);
				}

				var lockValue = await (_cache.LockAsync(key, cancellationToken)).ConfigureAwait(false);
				try
				{
					ILockable lockable = (ILockable) await (Cache.GetAsync(key, cancellationToken)).ConfigureAwait(false);

					bool puttable = lockable == null ||
					                lockable.IsPuttable(txTimestamp, version, versionComparator);

					if (puttable)
					{
						await (Cache.PutAsync(key, CachedItem.Create(value, Cache.NextTimestamp(), version), cancellationToken)).ConfigureAwait(false);
						if (log.IsDebugEnabled())
						{
							log.Debug("Cached: {0}", key);
						}
						return true;
					}
					else
					{
						if (log.IsDebugEnabled())
						{
							if (lockable.IsLock)
							{
								log.Debug("Item was locked: {0}", key);
							}
							else
							{
								log.Debug("Item was already cached: {0}", key);
							}
						}
						return false;
					}
				}
				finally
				{
					await (_cache.UnlockAsync(key, lockValue, cancellationToken)).ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// decrement a lock and put it back in the cache
		/// </summary>
		private Task DecrementLockAsync(object key, CacheLock @lock, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				//decrement the lock
				@lock.Unlock(Cache.NextTimestamp());
				return Cache.PutAsync(key, @lock, cancellationToken);
			}
			catch (System.Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		public async Task ReleaseAsync(CacheKey key, ISoftLock clientLock, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (await _lockObjectAsync.LockAsync())
			{
				if (log.IsDebugEnabled())
				{
					log.Debug("Releasing: {0}", key);
				}

				var lockValue = await (_cache.LockAsync(key, cancellationToken)).ConfigureAwait(false);
				try
				{
					ILockable lockable = (ILockable) await (Cache.GetAsync(key, cancellationToken)).ConfigureAwait(false);
					if (IsUnlockable(clientLock, lockable))
					{
						await (DecrementLockAsync(key, (CacheLock) lockable, cancellationToken)).ConfigureAwait(false);
					}
					else
					{
						await (HandleLockExpiryAsync(key, cancellationToken)).ConfigureAwait(false);
					}
				}
				finally
				{
					await (_cache.UnlockAsync(key, lockValue, cancellationToken)).ConfigureAwait(false);
				}
			}
		}

		internal Task HandleLockExpiryAsync(object key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				log.Warn("An item was expired by the cache while it was locked (increase your cache timeout): {0}", key);
				long ts = Cache.NextTimestamp() + Cache.Timeout;
				// create new lock that times out immediately
				CacheLock @lock = CacheLock.Create(ts, NextLockId(), null);
				@lock.Unlock(ts);
				return Cache.PutAsync(key, @lock, cancellationToken);
			}
			catch (System.Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		public Task ClearAsync(CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return Cache.ClearAsync(cancellationToken);
		}

		public Task RemoveAsync(CacheKey key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			return Cache.RemoveAsync(key, cancellationToken);
		}

		/// <summary>
		/// Re-cache the updated state, if and only if there there are
		/// no other concurrent soft locks. Release our lock.
		/// </summary>
		public async Task<bool> AfterUpdateAsync(CacheKey key, object value, object version, ISoftLock clientLock, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (await _lockObjectAsync.LockAsync())
			{
				if (log.IsDebugEnabled())
				{
					log.Debug("Updating: {0}", key);
				}

				var lockValue = await (_cache.LockAsync(key, cancellationToken)).ConfigureAwait(false);
				try
				{
					ILockable lockable = (ILockable) await (Cache.GetAsync(key, cancellationToken)).ConfigureAwait(false);
					if (IsUnlockable(clientLock, lockable))
					{
						CacheLock @lock = (CacheLock) lockable;
						if (@lock.WasLockedConcurrently)
						{
							// just decrement the lock, don't recache
							// (we don't know which transaction won)
							await (DecrementLockAsync(key, @lock, cancellationToken)).ConfigureAwait(false);
						}
						else
						{
							//recache the updated state
							await (Cache.PutAsync(key, CachedItem.Create(value, Cache.NextTimestamp(), version), cancellationToken)).ConfigureAwait(false);
							if (log.IsDebugEnabled())
							{
								log.Debug("Updated: {0}", key);
							}
						}
						return true;
					}
					else
					{
						await (HandleLockExpiryAsync(key, cancellationToken)).ConfigureAwait(false);
						return false;
					}
				}
				finally
				{
					await (_cache.UnlockAsync(key, lockValue, cancellationToken)).ConfigureAwait(false);
				}
			}
		}

		public async Task<bool> AfterInsertAsync(CacheKey key, object value, object version, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (await _lockObjectAsync.LockAsync())
			{
				if (log.IsDebugEnabled())
				{
					log.Debug("Inserting: {0}", key);
				}

				var lockValue = await (_cache.LockAsync(key, cancellationToken)).ConfigureAwait(false);
				try
				{
					
					ILockable lockable = (ILockable) await (Cache.GetAsync(key, cancellationToken)).ConfigureAwait(false);
					if (lockable == null)
					{
						await (Cache.PutAsync(key, CachedItem.Create(value, Cache.NextTimestamp(), version), cancellationToken)).ConfigureAwait(false);
						if (log.IsDebugEnabled())
						{
							log.Debug("Inserted: {0}", key);
						}
						return true;
					}
					else
					{
						return false;
					}
				}
				finally
				{
					await (_cache.UnlockAsync(key, lockValue, cancellationToken)).ConfigureAwait(false);
				}
			}
		}

		public Task EvictAsync(CacheKey key, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<object>(cancellationToken);
			}
			try
			{
				Evict(key);
				return Task.CompletedTask;
			}
			catch (System.Exception ex)
			{
				return Task.FromException<object>(ex);
			}
		}

		public Task<bool> UpdateAsync(CacheKey key, object value, object currentVersion, object previousVersion, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return Task.FromCanceled<bool>(cancellationToken);
			}
			try
			{
				return Task.FromResult<bool>(Update(key, value, currentVersion, previousVersion));
			}
			catch (System.Exception ex)
			{
				return Task.FromException<bool>(ex);
			}
		}
	}
}