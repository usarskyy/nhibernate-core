﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using NHibernate.Event;
using NHibernate.Impl;
using NUnit.Framework;

namespace NHibernate.Test.Events.PostEvents
{
	using System.Threading.Tasks;
	using System.Threading;
	[TestFixture]
	public class PostUpdateFixtureAsync : TestCase
	{
		protected override string MappingsAssembly
		{
			get { return "NHibernate.Test"; }
		}

		protected override string[] Mappings
		{
			get { return new[] {"Events.PostEvents.SimpleEntity.hbm.xml"}; }
		}

		[Test]
		public async Task ImplicitFlushAsync()
		{
			((DebugSessionFactory) Sfi).EventListeners.PostUpdateEventListeners = new IPostUpdateEventListener[]
			                                                                          	{
			                                                                          		new AssertOldStatePostListener(
			                                                                          			eArgs =>
			                                                                          			Assert.That(eArgs.OldState, Is.Not.Null))
			                                                                          	};
			await (FillDbAsync());
			using (var ls = new LogSpy(typeof (AssertOldStatePostListener)))
			{
				using (ISession s = OpenSession())
				{
					using (ITransaction tx = s.BeginTransaction())
					{
						IList<SimpleEntity> l = await (s.CreateCriteria<SimpleEntity>().ListAsync<SimpleEntity>());
						l[0].Description = "Modified";
						await (tx.CommitAsync());
					}
				}
				Assert.That(ls.GetWholeLog(), Does.Contain(AssertOldStatePostListener.LogMessage));
			}

			await (DbCleanupAsync());
			((DebugSessionFactory) Sfi).EventListeners.PostUpdateEventListeners = Array.Empty<IPostUpdateEventListener>();
		}

		[Test]
		public async Task ExplicitUpdateAsync()
		{
			((DebugSessionFactory) Sfi).EventListeners.PostUpdateEventListeners = new IPostUpdateEventListener[]
			                                                                          	{
			                                                                          		new AssertOldStatePostListener(
			                                                                          			eArgs =>
			                                                                          			Assert.That(eArgs.OldState, Is.Not.Null))
			                                                                          	};
			await (FillDbAsync());
			using (var ls = new LogSpy(typeof (AssertOldStatePostListener)))
			{
				using (ISession s = OpenSession())
				{
					using (ITransaction tx = s.BeginTransaction())
					{
						IList<SimpleEntity> l = await (s.CreateCriteria<SimpleEntity>().ListAsync<SimpleEntity>());
						l[0].Description = "Modified";
						await (s.UpdateAsync(l[0]));
						await (tx.CommitAsync());
					}
				}
				Assert.That(ls.GetWholeLog(), Does.Contain(AssertOldStatePostListener.LogMessage));
			}

			await (DbCleanupAsync());
			((DebugSessionFactory) Sfi).EventListeners.PostUpdateEventListeners = Array.Empty<IPostUpdateEventListener>();
		}

		[Test]
		public async Task WithDetachedObjectAsync()
		{
			((DebugSessionFactory) Sfi).EventListeners.PostUpdateEventListeners = new IPostUpdateEventListener[]
			                                                                          	{
			                                                                          		new AssertOldStatePostListener(
			                                                                          			eArgs =>
			                                                                          			Assert.That(eArgs.OldState, Is.Not.Null))
			                                                                          	};
			await (FillDbAsync());
			SimpleEntity toModify;
			using (ISession s = OpenSession())
			{
				using (ITransaction tx = s.BeginTransaction())
				{
					IList<SimpleEntity> l = await (s.CreateCriteria<SimpleEntity>().ListAsync<SimpleEntity>());
					toModify = l[0];
					await (tx.CommitAsync());
				}
			}
			toModify.Description = "Modified";
			using (var ls = new LogSpy(typeof (AssertOldStatePostListener)))
			{
				using (ISession s = OpenSession())
				{
					using (ITransaction tx = s.BeginTransaction())
					{
						await (s.MergeAsync(toModify));
						await (tx.CommitAsync());
					}
				}
				Assert.That(ls.GetWholeLog(), Does.Contain(AssertOldStatePostListener.LogMessage));
			}

			await (DbCleanupAsync());
			((DebugSessionFactory) Sfi).EventListeners.PostUpdateEventListeners = Array.Empty<IPostUpdateEventListener>();
		}

		[Test]
		public async Task UpdateDetachedObjectAsync()
		{
			// When the update is used directly as method to reattach a entity the OldState is null
			// that mean that NH should not retrieve info from DB
			((DebugSessionFactory) Sfi).EventListeners.PostUpdateEventListeners = new IPostUpdateEventListener[]
			                                                                          	{
			                                                                          		new AssertOldStatePostListener(
			                                                                          			eArgs =>
			                                                                          			Assert.That(eArgs.OldState, Is.Null))
			                                                                          	};
			await (FillDbAsync());
			SimpleEntity toModify;
			using (ISession s = OpenSession())
			{
				using (ITransaction tx = s.BeginTransaction())
				{
					IList<SimpleEntity> l = await (s.CreateCriteria<SimpleEntity>().ListAsync<SimpleEntity>());
					toModify = l[0];
					await (tx.CommitAsync());
				}
			}
			toModify.Description = "Modified";
			using (var ls = new LogSpy(typeof (AssertOldStatePostListener)))
			{
				using (ISession s = OpenSession())
				{
					using (ITransaction tx = s.BeginTransaction())
					{
						await (s.UpdateAsync(toModify));
						await (tx.CommitAsync());
					}
				}
				Assert.That(ls.GetWholeLog(), Does.Contain(AssertOldStatePostListener.LogMessage));
			}

			await (DbCleanupAsync());
			((DebugSessionFactory) Sfi).EventListeners.PostUpdateEventListeners = Array.Empty<IPostUpdateEventListener>();
		}

		[Test]
		public async Task UpdateDetachedObjectWithLockAsync()
		{
			((DebugSessionFactory)Sfi).EventListeners.PostUpdateEventListeners = new IPostUpdateEventListener[]
			                                                                          	{
			                                                                          		new AssertOldStatePostListener(
			                                                                          			eArgs =>
			                                                                          			Assert.That(eArgs.OldState, Is.Not.Null))
			                                                                          	};
			await (FillDbAsync());
			SimpleEntity toModify;
			using (ISession s = OpenSession())
			{
				using (ITransaction tx = s.BeginTransaction())
				{
					IList<SimpleEntity> l = await (s.CreateCriteria<SimpleEntity>().ListAsync<SimpleEntity>());
					toModify = l[0];
					await (tx.CommitAsync());
				}
			}
			using (var ls = new LogSpy(typeof(AssertOldStatePostListener)))
			{
				using (ISession s = OpenSession())
				{
					using (ITransaction tx = s.BeginTransaction())
					{
						await (s.LockAsync(toModify, LockMode.None));
						toModify.Description = "Modified";
						await (s.UpdateAsync(toModify));
						await (tx.CommitAsync());
					}
				}
				Assert.That(ls.GetWholeLog(), Does.Contain(AssertOldStatePostListener.LogMessage));
			}

			await (DbCleanupAsync());
			((DebugSessionFactory)Sfi).EventListeners.PostUpdateEventListeners = Array.Empty<IPostUpdateEventListener>();
		}
		private async Task DbCleanupAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			using (ISession s = OpenSession())
			{
				using (ITransaction tx = s.BeginTransaction())
				{
					await (s.CreateQuery("delete from SimpleEntity").ExecuteUpdateAsync(cancellationToken));
					await (tx.CommitAsync(cancellationToken));
				}
			}
		}

		private async Task FillDbAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			using (ISession s = OpenSession())
			{
				using (ITransaction tx = s.BeginTransaction())
				{
					await (s.SaveAsync(new SimpleEntity {Description = "Something"}, cancellationToken));
					await (tx.CommitAsync(cancellationToken));
				}
			}
		}
	}
}