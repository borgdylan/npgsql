﻿using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Security.Protocol.Ntlm;
using Npgsql.Messages;
using NpgsqlTypes;

namespace Npgsql.TypeHandlers
{
    /// <summary>
    /// Base class for all type handlers which handle PostgreSQL arrays.
    /// </summary>
    /// <remarks>
    /// http://www.postgresql.org/docs/9.3/static/arrays.html
    /// </remarks>
    internal abstract class ArrayHandler : TypeHandler<Array>
    {
        Array _readValue;
        IList _writeValue;
        ReadState _readState;
        WriteState _writeState;
        IEnumerator _enumerator;
        NpgsqlBuffer _buf;
        FieldDescription _fieldDescription;
        int _dimensions;
        int[] _dimLengths, _indices;
        int _index;
        int _elementLen;

        /// <summary>
        /// The array currently being written
        /// </summary>
        bool _hasNulls;
        bool _wroteElementLen;

        public override bool SupportsBinaryRead { get { return ElementHandler.SupportsBinaryRead; } }
        public override bool SupportsBinaryWrite { get { return ElementHandler.SupportsBinaryWrite; } }

        internal override Type GetFieldType(FieldDescription fieldDescription)
        {
            return typeof (Array);
        }

        internal override Type GetProviderSpecificFieldType(FieldDescription fieldDescription)
        {
            return typeof (Array);
        }

        internal abstract Type GetElementFieldType(FieldDescription fieldDescription);
        internal abstract Type GetElementPsvType(FieldDescription fieldDescription);

        /// <summary>
        /// The type handler for the element that this array type holds
        /// </summary>
        internal TypeHandler ElementHandler { get; private set; }

        /// <summary>
        /// The delimiter character for this array.
        /// </summary>
        internal char TextDelimiter { get; private set; }

        protected ArrayHandler(TypeHandler elementHandler, char textDelimiter)
        {
            ElementHandler = elementHandler;
            TextDelimiter = textDelimiter;
        }

        #region Read

        protected void PrepareRead(NpgsqlBuffer buf, FieldDescription fieldDescription, int len)
        {
            Contract.Assert(_readState == ReadState.NeedPrepare);
            if (_readState != ReadState.NeedPrepare)  // Checks against recursion and bugs
                throw new InvalidOperationException("Started reading a value before completing a previous value");

            _buf = buf;
            _fieldDescription = fieldDescription;
            _elementLen = -1;
            _readState = ReadState.ReadNothing;
        }

        protected bool Read<TElement>(out Array result)
        {
            switch (_readState)
            {
                case ReadState.ReadNothing:
                    if (_buf.ReadBytesLeft < 12)
                    {
                        result = null;
                        return false;
                    }
                    _dimensions = _buf.ReadInt32();
                    var hasNulls = _buf.ReadInt32();            // Not populated by PG?
                    var elementOID = _buf.ReadUInt32();
                    Contract.Assume(elementOID == ElementHandler.OID);
                    _dimLengths = new int[_dimensions];
                    if (_dimensions > 1) {
                        _indices = new int[_dimensions];
                    }
                    _index = 0;
                    goto case ReadState.ReadHeader;

                case ReadState.ReadHeader:
                    if (_buf.ReadBytesLeft < _dimensions * 8) {
                        result = null;
                        return false;
                    }
                    for (var i = 0; i < _dimensions; i++)
                    {
                        _dimLengths[i] = _buf.ReadInt32();
                        _buf.ReadInt32(); // We don't care about the lower bounds
                    }
                    if (_dimensions == 0)
                    {
                        result = new TElement[0];
                        _readState = ReadState.NeedPrepare;
                        return true;
                    }
                    _readValue = Array.CreateInstance(typeof(TElement), _dimLengths);
                    _readState = ReadState.ReadingElements;
                    goto case ReadState.ReadingElements;

                case ReadState.ReadingElements:
                    var completed = _readValue is TElement[]
                        ? ReadElementsOneDimensional<TElement>()
                        : ReadElementsMultidimensional<TElement>();

                    if (!completed)
                    {
                        result = null;
                        return false;
                    }

                    result = _readValue;
                    _readValue = null;
                    _buf = null;
                    _fieldDescription = null;
                    _readState = ReadState.NeedPrepare;
                    return true;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Optimized population for one-dimensional arrays without boxing/unboxing
        /// </summary>
        bool ReadElementsOneDimensional<TElement>()
        {
            var array = (TElement[])_readValue;

            for (; _index < array.Length; _index++)
            {
                TElement element;
                if (!ReadSingleElement(out element)) { return false; }
                array[_index] = element;
            }
            return true;
        }

        /// <summary>
        /// Recursively populates an array from PB binary data representation.
        /// </summary>
        bool ReadElementsMultidimensional<TElement>()
        {
            while (true)
            {
                TElement element;
                if (!ReadSingleElement(out element)) { return false; }
                _readValue.SetValue(element, _indices);
                if (!MoveNextInMultidimensional()) { return true; }
            }
        }

        bool MoveNextInMultidimensional()
        {
            _indices[_dimensions - 1]++;
            for (var dim = _dimensions - 1; dim >= 0; dim--) {
                if (_indices[dim] <= _readValue.GetUpperBound(dim)) {
                    continue;
                }

                if (dim == 0) {
                    return false;
                }

                for (var j = dim; j < _dimensions; j++)
                    _indices[j] = _readValue.GetLowerBound(j);
                _indices[dim - 1]++;
            }
            return true;
        }

        bool ReadSingleElement<TElement>(out TElement element)
        {
            try
            {
                if (_elementLen == -1)
                {
                    if (_buf.ReadBytesLeft < 4)
                    {
                        element = default(TElement);
                        return false;
                    }
                    _elementLen = _buf.ReadInt32();
                    if (_elementLen == -1)
                    {
                        // TODO: Nullables
                        element = default(TElement);
                        return true;
                    }
                }

                var asSimpleReader = ElementHandler as ISimpleTypeReader<TElement>;
                if (asSimpleReader != null)
                {
                    if (_buf.ReadBytesLeft < _elementLen)
                    {
                        element = default(TElement);
                        return false;
                    }
                    element = asSimpleReader.Read(_buf, _fieldDescription, _elementLen);
                    _elementLen = -1;
                    return true;
                }

                var asChunkingReader = ElementHandler as IChunkingTypeReader<TElement>;
                if (asChunkingReader != null)
                {
                    asChunkingReader.PrepareRead(_buf, _fieldDescription, _elementLen);
                    if (!asChunkingReader.Read(out element))
                    {
                        return false;
                    }
                    _elementLen = -1;
                    return true;
                }

                throw PGUtil.ThrowIfReached();
            }
            catch (SafeReadException e)
            {
                // TODO: Implement safe reading for array: read all values to the end, only then raise the
                // SafeReadException. For now, translate the safe exception to an unsafe one to break the connector.
                throw e.InnerException;
            }
        }

        enum ReadState
        {
            NeedPrepare,
            ReadNothing,
            ReadHeader,
            ReadingElements,
        }

        #endregion

        #region Write

        public virtual void PrepareWrite(NpgsqlBuffer buf, object value)
        {
            Contract.Assert(_readState == ReadState.NeedPrepare);
            if (_writeState != WriteState.NeedPrepare)  // Checks against recursion and bugs
                throw new InvalidOperationException("Started reading a value before completing a previous value");

            _buf = buf;
            var asArray = value as Array;
            _writeValue = (IList)value;
            _dimensions = asArray != null ? asArray.Rank : 1;
            _index = 0;
            _wroteElementLen = false;
            _writeState = WriteState.WroteNothing;
        }

        public bool Write<TElement>(ref byte[] directBuf)
        {
            Array asArray;

            switch (_writeState)
            {
                case WriteState.WroteNothing:
                    var len =
                        4 +               // ndim
                        4 +               // has_nulls
                        4 +               // element_oid
                        _dimensions * 8;  // dim (4) + lBound (4)

                    if (_buf.WriteSpaceLeft < len) {
                        Contract.Assume(_buf.Size >= len, "Buffer too small for header");
                        return false;
                    }
                    _buf.WriteInt32(_dimensions);
                    _buf.WriteInt32(_hasNulls ? 1 : 0); // Actually not used by backend
                    _buf.WriteInt32((int)ElementHandler.OID);
                    asArray = _writeValue as Array;
                    if (asArray != null)
                    {
                        for (var i = 0; i < _dimensions; i++)
                        {
                            _buf.WriteInt32(asArray.GetLength(i));
                            _buf.WriteInt32(1);
                            // We set lBound to 1 and silently ignore if the user had set it to something else
                        }
                    }
                    else
                    {
                        _buf.WriteInt32(_writeValue.Count);
                        _buf.WriteInt32(1);
                        _enumerator = _writeValue.GetEnumerator();
                    }

                    var asGeneric = _writeValue as IList<TElement>;
                    _enumerator = asGeneric != null ? asGeneric.GetEnumerator() : _writeValue.GetEnumerator();
                    if (!_enumerator.MoveNext()) {
                        goto case WriteState.Cleanup;
                    }

                    _writeState = WriteState.WritingElements;
                    goto case WriteState.WritingElements;

                case WriteState.WritingElements:
                    var genericEnumerator = _enumerator as IEnumerator<TElement>;
                    if (genericEnumerator != null)
                    {
                        // TODO: Actually call the element writer generically...!
                        do
                        {
                            if (!WriteSingleElement(genericEnumerator.Current, out directBuf)) { return false; }
                        } while (genericEnumerator.MoveNext());
                    }
                    else
                    {
                        do {
                            if (!WriteSingleElement(_enumerator.Current, out directBuf)) { return false; }
                        } while (_enumerator.MoveNext());                       
                    }
                    goto case WriteState.Cleanup;

                case WriteState.Cleanup:
                    _writeValue = null;
                    _buf = null;
                    _writeState = WriteState.NeedPrepare;
                    return true;

                default:
                    throw PGUtil.ThrowIfReached();
            }
        }

        bool WriteSingleElement(object element, out byte[] directBuf)
        {
            // TODO: Need generic version of this...
            directBuf = null;

            if (element == null || element is DBNull) {
                if (_buf.WriteSpaceLeft < 4) {
                    return false;
                }
                _buf.WriteInt32(-1);
                return true;
            }

            var asSimpleWriter = ElementHandler as ISimpleTypeWriter;
            if (asSimpleWriter != null)
            {
                var elementLen = asSimpleWriter.ValidateAndGetLength(element);
                if (_buf.WriteSpaceLeft < 4 + elementLen) { return false; }
                _buf.WriteInt32(elementLen);
                asSimpleWriter.Write(element, _buf);
                return true;
            }

            var asChunkedWriter = ElementHandler as IChunkingTypeWriter;
            if (asChunkedWriter != null)
            {
                if (!_wroteElementLen) {
                    if (_buf.WriteSpaceLeft < 4) {
                        return false;
                    }
                    _buf.WriteInt32(asChunkedWriter.ValidateAndGetLength(element));
                    asChunkedWriter.PrepareWrite(_buf, element);
                    _wroteElementLen = true;
                }
                if (!asChunkedWriter.Write(ref directBuf)) {
                    return false;
                }
                _wroteElementLen = false;
                return true;
            }

            throw PGUtil.ThrowIfReached();
        }

        public int ValidateAndGetLength<TElement>(object value)
        {
            // Take care of single-dimensional arrays and generic IList<T>
            var asGenericList = value as IList<TElement>;
            if (asGenericList != null) {
                return 12 + (1 * 8) + asGenericList.Sum(e => 4 + GetSingleElementLength(e));
            }

            // Take care of multi-dimensional arrays and non-generic IList, we have no choice but to do
            // boxing/unboxing
            var asNonGenericList = value as IList;
            if (asNonGenericList != null)
            {
                var asMultidimensional = value as Array;
                var dimensions = asMultidimensional != null ? asMultidimensional.Rank : 1;
                return 12 + (dimensions * 8) + asNonGenericList.Cast<object>().Sum(element => 4 + GetSingleElementLength(element));
            }

            throw new InvalidCastException(String.Format("Can't write type {0} as an array", value.GetType()));
        }

        int GetSingleElementLength(object element)
        {
            if (element == null || element is DBNull) {
                return 0;
            }
            var asSimpleWriter = ElementHandler as ISimpleTypeWriter;
            return asSimpleWriter != null ? asSimpleWriter.ValidateAndGetLength(element) : ((IChunkingTypeWriter)ElementHandler).ValidateAndGetLength(element);
        }

        enum WriteState
        {
            NeedPrepare,
            WroteNothing,
            WritingElements,
            Cleanup,
        }

        #endregion
    }

    /// <remarks>
    /// http://www.postgresql.org/docs/9.3/static/arrays.html
    /// </remarks>
    /// <typeparam name="TElement">The .NET type contained as an element within this array</typeparam>
    internal class ArrayHandler<TElement> : ArrayHandler,
        IChunkingTypeReader<Array>, IChunkingTypeWriter
    {
        /// <summary>
        /// The type of the elements contained within this array
        /// </summary>
        /// <param name="fieldDescription"></param>
        internal override Type GetElementFieldType(FieldDescription fieldDescription)
        {
            return typeof(TElement);
        }

        /// <summary>
        /// The provider-specific type of the elements contained within this array,
        /// </summary>
        /// <param name="fieldDescription"></param>
        internal override Type GetElementPsvType(FieldDescription fieldDescription)
        {
            return typeof(TElement);
        }

        public ArrayHandler(TypeHandler elementHandler, char textDelimiter)
            : base(elementHandler, textDelimiter) { }

        public new void PrepareRead(NpgsqlBuffer buf, FieldDescription fieldDescription, int len)
        {
            base.PrepareRead(buf, fieldDescription, len);
        }

        public bool Read(out Array result)
        {
            return base.Read<TElement>(out result);
        }

        public int ValidateAndGetLength(object value)
        {
            return base.ValidateAndGetLength<TElement>(value);
        }

        public bool Write(ref byte[] directBuf)
        {
            return base.Write<TElement>(ref directBuf);
        }
    }

    /// <remarks>
    /// http://www.postgresql.org/docs/9.4/static/arrays.html
    /// </remarks>
    /// <typeparam name="TNormal">The .NET type contained as an element within this array</typeparam>
    /// <typeparam name="TPsv">The .NET provider-specific type contained as an element within this array</typeparam>
    internal class ArrayHandlerWithPsv<TNormal, TPsv> : ArrayHandler<TNormal>, ITypeHandlerWithPsv
    {
        /// <summary>
        /// The provider-specific type of the elements contained within this array,
        /// </summary>
        /// <param name="fieldDescription"></param>
        internal override Type GetElementPsvType(FieldDescription fieldDescription)
        {
            return typeof(TPsv);
        }

        internal override object ReadPsvAsObject(DataRowMessage row, FieldDescription fieldDescription)
        {
            PrepareRead(row.Buffer, fieldDescription, row.ColumnLen);
            Array result;
            while (!Read<TPsv>(out result)) {
                row.Buffer.ReadMore();
            }
            return result;
        }

        public ArrayHandlerWithPsv(TypeHandler elementHandler, char textDelimiter)
            : base(elementHandler, textDelimiter) {}
    }
}
