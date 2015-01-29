﻿using System;
using Npgsql.Messages;
using NpgsqlTypes;
using System.Data;

namespace Npgsql.TypeHandlers.DateTimeHandlers
{
    /// <remarks>
    /// http://www.postgresql.org/docs/9.3/static/datatype-datetime.html
    /// </remarks>
    [TypeMapping("timestamp", NpgsqlDbType.Timestamp, new[] { DbType.DateTime, DbType.DateTime2 }, typeof(NpgsqlTimeStamp))]
    internal class TimeStampHandler : TypeHandlerWithPsv<DateTime, NpgsqlTimeStamp>,
        ISimpleTypeReader<DateTime>, ISimpleTypeReader<NpgsqlTimeStamp>
    {
        /// <summary>
        /// A deprecated compile-time option of PostgreSQL switches to a floating-point representation of some date/time
        /// fields. Npgsql (currently) does not support this mode.
        /// </summary>
        readonly bool _integerFormat;

        public TimeStampHandler(NpgsqlConnector connector)
        {
            _integerFormat = connector.BackendParams["integer_datetimes"] == "on";
        }

        public override bool SupportsBinaryWrite
        {
            get
            {
                return false; // TODO: Implement
            }
        }

        public DateTime Read(NpgsqlBuffer buf, FieldDescription fieldDescription, int len)
        {
            // TODO: Convert directly to DateTime without passing through NpgsqlTimeStamp?
            return (DateTime)((ISimpleTypeReader<NpgsqlTimeStamp>)this).Read(buf, fieldDescription, len);
        }

        NpgsqlTimeStamp ISimpleTypeReader<NpgsqlTimeStamp>.Read(NpgsqlBuffer buf, FieldDescription fieldDescription, int len)
        {
            if (!_integerFormat) {
                throw new NotSupportedException("Old floating point representation for timestamps not supported");
            }
            return NpgsqlTimeStamp.FromInt64(buf.ReadInt64());
        }
    }
}
