using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Tests.Storage.AsyncQueries.Model;

namespace Xtensive.Orm.Tests.Storage.AsyncQueries
{
  public class AsyncAggregateExtensionsTest : AsyncQueriesBaseTest
  {
    private static Task<Session> OpenSessionAsync(Domain domain, bool isClientProfile) =>
      domain.OpenSessionAsync(
        new SessionConfiguration(isClientProfile ? SessionOptions.ClientProfile : SessionOptions.ServerProfile));

    private static TransactionScope OpenTransactionAsync(Session session, bool isClientProfile) =>
      isClientProfile ? null : session.OpenTransaction();

    // All

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task AllAsyncExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<Teacher>();
        Assert.IsTrue(await query.Take(0).AllAsync(teacher => teacher.Gender==Gender.Male));
        Assert.IsTrue(await query.Take(0).AllAsync(teacher => teacher.Gender==Gender.Female));
        Assert.IsFalse(
          await query.Where(teacher => teacher.Gender==Gender.Female).AllAsync(teacher => teacher.Gender==Gender.Male));
        Assert.IsFalse(await query.AllAsync(teacher => teacher.Gender==Gender.Male));
        Assert.IsTrue(
          await query.Where(teacher => teacher.Gender==Gender.Male).AllAsync(teacher => teacher.Gender==Gender.Male));
      }
    }

    // Any

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task AnyAsyncNoPredicateExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<Teacher>();
        Assert.IsTrue(await query.AnyAsync());
        Assert.IsFalse(await query.Where(teacher => teacher.Name==null).AnyAsync());
        // TODO: Query translation fails
        // Assert.IsFalse(await query.Take(0).AnyAsync());
      }
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task AnyAsyncWithPredicateExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<Teacher>();
        Assert.IsFalse(
          await query.Where(teacher => teacher.Name==null).AnyAsync(teacher => teacher.Gender==Gender.Male));
        Assert.IsFalse(
          await query.Where(teacher => teacher.Name==null).AnyAsync(teacher => teacher.Gender==Gender.Female));
        Assert.IsFalse(
          await query.Where(teacher => teacher.Gender==Gender.Female).AnyAsync(teacher => teacher.Gender==Gender.Male));
        Assert.IsTrue(await query.AnyAsync(teacher => teacher.Gender==Gender.Male));
        Assert.IsTrue(
          await query.Where(teacher => teacher.Gender==Gender.Male).AnyAsync(teacher => teacher.Gender==Gender.Male));
      }
    }

    // Average<int>

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncIntExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>().Select(stat => stat.IntFactor);
        var allFactors = (await query.ExecuteAsync()).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync());
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncIntOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0).Select(stat => stat.IntFactor);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyQuery.AverageAsync());
      }
    }

    // Average<int>(selector)

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncIntWithSelectorExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>();
        var allFactors = (await query.ExecuteAsync()).Select(stat => stat.IntFactor).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync(stat => stat.IntFactor));
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncIntWithSelectorOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).Select(stat => stat.IntFactor).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyQuery.AverageAsync(stat => stat.IntFactor));
      }
    }

    // Average<int?>

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableIntExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>()
          .Select(stat => stat.IntFactor % 2 == 0 ? default(int?) : stat.IntFactor);
        var allFactors = (await query.ExecuteAsync()).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync());
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableIntOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0).Select(stat => (int?)stat.IntFactor);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.IsNull(await emptyQuery.AverageAsync());
      }
    }

    // Average<int?>(selector)

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableIntWithSelectorExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>();
        var allFactors = (await query.ExecuteAsync())
          .Select(stat => stat.IntFactor % 2 == 0 ? default(int?) : stat.IntFactor)
          .ToList();
        Assert.AreEqual(
          allFactors.Average(),
          await query.AverageAsync(stat => stat.IntFactor % 2 == 0 ? default(int?) : stat.IntFactor));
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableIntWithSelectorOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>().Where(stat => stat.IntFactor < 0);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).Select(stat => (int?)stat.IntFactor).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.IsNull(await emptyQuery.AverageAsync(stat => (int?)stat.IntFactor));
      }
    }

    // Average<long>

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncLongExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>().Select(stat => stat.LongFactor);
        var allFactors = (await query.ExecuteAsync()).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync());
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncLongOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0).Select(stat => stat.LongFactor);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyQuery.AverageAsync());
      }
    }

    // Average<long>(selector)

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncLongWithSelectorExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>();
        var allFactors = (await query.ExecuteAsync()).Select(stat => stat.LongFactor).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync(stat => stat.LongFactor));
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncLongWithSelectorOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).Select(stat => stat.LongFactor).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyQuery.AverageAsync(stat => stat.LongFactor));
      }
    }

    // Average<long?>

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableLongExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>()
          .Select(stat => stat.IntFactor % 2 == 0 ? default(long?) : stat.LongFactor);
        var allFactors = (await query.ExecuteAsync()).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync());
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableLongOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0).Select(stat => (long?)stat.LongFactor);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.IsNull(await emptyQuery.AverageAsync());
      }
    }

    // Average<long?>(selector)

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableLongWithSelectorExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>();
        var allFactors = (await query.ExecuteAsync())
          .Select(stat => stat.LongFactor % 2 == 0 ? default(long?) : stat.LongFactor)
          .ToList();
        Assert.AreEqual(
          allFactors.Average(),
          await query.AverageAsync(stat => stat.LongFactor % 2 == 0 ? default(long?) : stat.LongFactor));
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableLongWithSelectorOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>().Where(stat => stat.IntFactor < 0);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).Select(stat => (long?)stat.LongFactor).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.IsNull(await emptyQuery.AverageAsync(stat => (long?)stat.LongFactor));
      }
    }

    // Average<double>

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncDoubleExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>().Select(stat => stat.DoubleFactor);
        var allFactors = (await query.ExecuteAsync()).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync());
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncDoubleOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0).Select(stat => stat.DoubleFactor);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyQuery.AverageAsync());
      }
    }

    // Average<double>(selector)

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncDoubleWithSelectorExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>();
        var allFactors = (await query.ExecuteAsync()).Select(stat => stat.DoubleFactor).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync(stat => stat.DoubleFactor));
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncDoubleWithSelectorOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).Select(stat => stat.DoubleFactor).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyQuery.AverageAsync(stat => stat.DoubleFactor));
      }
    }

    // Average<double?>

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableDoubleExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>()
          .Select(stat => stat.IntFactor % 2 == 0 ? default(double?) : stat.DoubleFactor);
        var allFactors = (await query.ExecuteAsync()).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync());
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableDoubleOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0).Select(stat => (double?)stat.DoubleFactor);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.IsNull(await emptyQuery.AverageAsync());
      }
    }

    // Average<double?>(selector)

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableDoubleWithSelectorExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>();
        var allFactors = (await query.ExecuteAsync())
          .Select(stat => stat.LongFactor % 2 == 0 ? default(double?) : stat.DoubleFactor)
          .ToList();
        Assert.AreEqual(
          allFactors.Average(),
          await query.AverageAsync(stat => stat.LongFactor % 2 == 0 ? default(double?) : stat.DoubleFactor));
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableDoubleWithSelectorOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>().Where(stat => stat.IntFactor < 0);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).Select(stat => (double?)stat.DoubleFactor).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.IsNull(await emptyQuery.AverageAsync(stat => (double?)stat.DoubleFactor));
      }
    }

    // Average<float>

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncFloatExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>().Select(stat => stat.FloatFactor);
        var allFactors = (await query.ExecuteAsync()).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync());
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncFloatOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0).Select(stat => stat.FloatFactor);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyQuery.AverageAsync());
      }
    }

    // Average<float>(selector)

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncFloatWithSelectorExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>();
        var allFactors = (await query.ExecuteAsync()).Select(stat => stat.FloatFactor).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync(stat => stat.FloatFactor));
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncFloatWithSelectorOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).Select(stat => stat.FloatFactor).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyQuery.AverageAsync(stat => stat.FloatFactor));
      }
    }

    // Average<float?>

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableFloatExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>()
          .Select(stat => stat.IntFactor % 2 == 0 ? default(float?) : stat.FloatFactor);
        var allFactors = (await query.ExecuteAsync()).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync());
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableFloatOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0).Select(stat => (float?) stat.FloatFactor);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.IsNull(await emptyQuery.AverageAsync());
      }
    }

    // Average<float?>(selector)

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableFloatWithSelectorExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>();
        var allFactors = (await query.ExecuteAsync())
          .Select(stat => stat.LongFactor % 2 == 0 ? default(float?) : stat.FloatFactor)
          .ToList();
        Assert.AreEqual(
          allFactors.Average(),
          await query.AverageAsync(stat => stat.LongFactor % 2 == 0 ? default(float?) : stat.FloatFactor));
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableFloatWithSelectorOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>().Where(stat => stat.IntFactor < 0);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).Select(stat => (float?)stat.FloatFactor).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.IsNull(await emptyQuery.AverageAsync(stat => (float?)stat.FloatFactor));
      }
    }

    // Average<decimal>

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncDecimalExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>().Select(stat => stat.DecimalFactor);
        var allFactors = (await query.ExecuteAsync()).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync());
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncDecimalOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0).Select(stat => stat.DecimalFactor);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyQuery.AverageAsync());
      }
    }

    // Average<decimal>(selector)

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncDecimalWithSelectorExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>();
        var allFactors = (await query.ExecuteAsync()).Select(stat => stat.DecimalFactor).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync(stat => stat.DecimalFactor));
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncDecimalWithSelectorOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).Select(stat => stat.DecimalFactor).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.ThrowsAsync<InvalidOperationException>(() => emptyQuery.AverageAsync(stat => stat.DecimalFactor));
      }
    }

    // Average<decimal?>

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableDecimalExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>()
          .Select(stat => stat.IntFactor % 2 == 0 ? default(decimal?) : stat.DecimalFactor);
        var allFactors = (await query.ExecuteAsync()).ToList();
        Assert.AreEqual(allFactors.Average(), await query.AverageAsync());
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableDecimalOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>()
          .Where(stat => stat.IntFactor < 0).Select(stat => (decimal?) stat.DecimalFactor);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.IsNull(await emptyQuery.AverageAsync());
      }
    }

    // Average<decimal?>(selector)

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableDecimalWithSelectorExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>();
        var allFactors = (await query.ExecuteAsync())
          .Select(stat => stat.LongFactor % 2 == 0 ? default(decimal?) : stat.DecimalFactor)
          .ToList();
        Assert.AreEqual(
          allFactors.Average(),
          await query.AverageAsync(stat => stat.LongFactor % 2 == 0 ? default(decimal?) : stat.DecimalFactor));
      }
    }

    [Test, TestCase(true), TestCase(false)]
    public async Task AverageAsyncNullableDecimalWithSelectorOnEmptySequenceExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var emptyQuery = session.Query.All<StatRecord>().Where(stat => stat.IntFactor < 0);

        var emptyFactors = (await emptyQuery.ExecuteAsync()).Select(stat => (decimal?)stat.DecimalFactor).ToList();
        Assert.AreEqual(0, emptyFactors.Count);
        Assert.IsNull(await emptyQuery.AverageAsync(stat => (decimal?)stat.DecimalFactor));
      }
    }

    // Contains

    [Test, TestCase(true), TestCase(false)]
    public async Task ContainsAsyncExtensionTest(bool isClientProfile)
    {
      await using var session = await OpenSessionAsync(Domain, isClientProfile);
      await using (OpenTransactionAsync(session, isClientProfile)) {
        var query = session.Query.All<StatRecord>().Select(stat => stat.IntFactor);

        Assert.IsTrue(await query.ContainsAsync(50));
        Assert.IsFalse(await query.ContainsAsync(-1));
      }
    }
  }
}