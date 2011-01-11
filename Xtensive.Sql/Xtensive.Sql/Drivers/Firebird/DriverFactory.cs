// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Csaba Beer
// Created:    2011.01.08

using System;
using FirebirdSql.Data.FirebirdClient;
using Xtensive.Core;
using Xtensive.Sql.Drivers.Firebird.Resources;
using Xtensive.Sql.Info;

namespace Xtensive.Sql.Firebird
{
    /// <summary>
    /// A <see cref="SqlDriver"/> factory for Firebird.
    /// </summary>
    public class DriverFactory : SqlDriverFactory
    {
        private const int DefaultPort = 3050;

        private const string DataSourceFormat =
          "server={0};port={1};database={2};";
        private readonly string DatabaseAndSchemaQuery =
          "select mon$database_name, '" + Xtensive.Sql.Drivers.Firebird.Constants.DefaultSchemaName + "' from mon$database";

        /// <inheritdoc/>
        public override SqlDriver CreateDriver(string connectionString)
        {
            using (var connection = new FbConnection(connectionString))
            {
                connection.Open();
                var dataSource = new FbConnectionStringBuilder(connectionString).DataSource;
                var coreServerInfo = new CoreServerInfo
                {
                    ServerLocation = new Location("firebird", dataSource),
                    ServerVersion = connection.ServerVersionNumber,
                    ConnectionString = connectionString,
                    MultipleActiveResultSets = true,
                };
                SqlHelper.ReadDatabaseAndSchema(connection, DatabaseAndSchemaQuery, coreServerInfo);
                if (Int32.Parse(coreServerInfo.ServerVersion.Major.ToString() + coreServerInfo.ServerVersion.Minor.ToString()) < 25)
                    throw new NotSupportedException(Strings.ExFirebirdBelow25IsNotSupported);
                //if (coreServerInfo.ServerVersion.Major == 2 && coreServerInfo.ServerVersion.Minor == 1)
                //    return new v2_1.Driver(coreServerInfo);
                if (coreServerInfo.ServerVersion.Major == 2 && coreServerInfo.ServerVersion.Minor == 5)
                    return new Xtensive.Sql.Drivers.Firebird.v2_5.Driver(coreServerInfo);
                return null;
            }
        }

        /// <inheritdoc/>
        public override string BuildConnectionString(UrlInfo connectionUrl)
        {
            SqlHelper.ValidateConnectionUrl(connectionUrl);
            ArgumentValidator.EnsureArgumentNotNullOrEmpty(connectionUrl.Resource, "connectionUrl.Resource");

            var builder = new FbConnectionStringBuilder();

            // host, port, database
            if (!string.IsNullOrEmpty(connectionUrl.Host))
            {
                int port = connectionUrl.Port != 0 ? connectionUrl.Port : DefaultPort;
                builder.DataSource = string.Format(DataSourceFormat, connectionUrl.Host, port, connectionUrl.Resource);
            }
            else
                builder.DataSource = connectionUrl.Resource;

            // user, password
            if (!string.IsNullOrEmpty(connectionUrl.User))
            {
                builder.UserID = connectionUrl.User;
                builder.Password = connectionUrl.Password;
            }

            // custom options
            foreach (var parameter in connectionUrl.Params)
                builder.Add(parameter.Key, parameter.Value);

            return builder.ToString();
        }
    }
}