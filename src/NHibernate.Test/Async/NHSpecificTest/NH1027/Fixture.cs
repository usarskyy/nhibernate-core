﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using NHibernate.Criterion;
using NHibernate.Dialect;
using NHibernate.SqlCommand;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.NH1027
{
	using System.Threading.Tasks;
	[TestFixture]
	public class FixtureAsync : BugTestCase
	{
		private void AssertDialect()
		{
			if (!(Dialect is MsSql2005Dialect))
				Assert.Ignore("This test is specific for MsSql2005Dialect");
		}

		[Test]
		public async Task CanMakeCriteriaQueryAcrossBothAssociationsAsync()
		{
			AssertDialect();
			using (ISession s = OpenSession())
			{
				ICriteria criteria = s.CreateCriteria(typeof(Item));
				criteria.CreateCriteria("Ships", "s", JoinType.InnerJoin)
							.Add(Expression.Eq("s.Id", 15));
				criteria.CreateCriteria("Containers", "c", JoinType.LeftOuterJoin)
				 .Add(Expression.Eq("c.Id", 15));
				criteria.SetMaxResults(2);
				await (criteria.ListAsync());
			}
		}
	}
}