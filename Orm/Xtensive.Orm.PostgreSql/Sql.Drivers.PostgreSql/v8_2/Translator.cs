// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Xtensive.Sql.Dml;
using Xtensive.Sql.Model;

namespace Xtensive.Sql.Drivers.PostgreSql.v8_2
{
  internal class Translator : v8_1.Translator
  {
    [DebuggerStepThrough]
    public override string QuoteString(string str)
    {
      return "E'" + str.Replace("'", "''").Replace(@"\", @"\\").Replace("\0", string.Empty) + "'";
    }

    protected override void AppendIndexStorageParameters(StringBuilder builder, Index index)
    {
      if (index.FillFactor != null) {
        _ = builder.AppendFormat("WITH(FILLFACTOR={0})", index.FillFactor);
      }
    }

    public override string Translate(SqlFunctionType type)
    {
      switch (type) {
        //date
        case SqlFunctionType.CurrentDate:
          return "date_trunc('day', clock_timestamp())";
        case SqlFunctionType.CurrentTimeStamp:
          return "clock_timestamp()";
        default:
          return base.Translate(type);
      }
    }


    // Constructors

    public Translator(SqlDriver driver)
      : base(driver)
    {
    }
  }
}