﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dicom;
using EnsureThat;
using Microsoft.Health.Dicom.Core.Extensions;
using Microsoft.Health.Dicom.Core.Features.ExtendedQueryTag;
using Microsoft.Health.Dicom.SqlServer.Features.Schema.Model;

namespace Microsoft.Health.Dicom.SqlServer.Features.ExtendedQueryTag
{
    /// <summary>
    /// Class that build ExtendedQueryTagRows.
    /// </summary>
    internal static class ExtendedQueryTagDataRowsBuilder
    {
        private static readonly Dictionary<DicomVR, Func<DicomDataset, DicomTag, DicomVR, DateTime?>> DataTimeReaders = new Dictionary<DicomVR, Func<DicomDataset, DicomTag, DicomVR, DateTime?>>()
        {
            { DicomVR.DA, Core.Extensions.DicomDatasetExtensions.GetStringDateAsDate },
        };

        public static ExtendedQueryTagDataRows Build(
            DicomDataset instance,
            IEnumerable<QueryTag> queryTags)
        {
            EnsureArg.IsNotNull(instance, nameof(instance));
            EnsureArg.IsNotNull(queryTags, nameof(queryTags));

            var stringRows = new List<InsertStringExtendedQueryTagTableTypeV1Row>();
            var longRows = new List<InsertLongExtendedQueryTagTableTypeV1Row>();
            var doubleRows = new List<InsertDoubleExtendedQueryTagTableTypeV1Row>();
            var dateTimeRows = new List<InsertDateTimeExtendedQueryTagTableTypeV1Row>();
            var personNamRows = new List<InsertPersonNameExtendedQueryTagTableTypeV1Row>();

            int maxVersion = 0;
            foreach (QueryTag queryTag in queryTags.Where(x => x.IsExtendedQueryTag))
            {
                // Update MaxVersion
                maxVersion = Math.Max(maxVersion, queryTag.ExtendedQueryTagStoreEntry.Key);

                // Create row
                ExtendedQueryTagDataType dataType = ExtendedQueryTagLimit.ExtendedQueryTagVRAndDataTypeMapping[queryTag.VR.Code];
                switch (dataType)
                {
                    case ExtendedQueryTagDataType.StringData: AddStringRow(instance, stringRows, queryTag); break;
                    case ExtendedQueryTagDataType.LongData: AddLongRow(instance, longRows, queryTag); break;
                    case ExtendedQueryTagDataType.DoubleData: AddDoubleRow(instance, doubleRows, queryTag); break;
                    case ExtendedQueryTagDataType.DateTimeData: AddDateTimeRow(instance, dateTimeRows, queryTag); break;
                    case ExtendedQueryTagDataType.PersonNameData: AddPersonNameRow(instance, personNamRows, queryTag); break;
                    default:
                        Debug.Fail($"Not able to handle {dataType}");
                        break;
                }
            }

            return new ExtendedQueryTagDataRows
            {
                StringRows = stringRows,
                LongRows = longRows,
                DoubleRows = doubleRows,
                DateTimeRows = dateTimeRows,
                PersonNameRows = personNamRows,
                MaxTagKey = maxVersion,
            };
        }

        private static void AddPersonNameRow(DicomDataset instance, List<InsertPersonNameExtendedQueryTagTableTypeV1Row> personNamRows, QueryTag queryTag)
        {
            string personNameVal = instance.GetSingleValueOrDefault<string>(queryTag.Tag, expectedVR: queryTag.VR);
            if (personNameVal != null)
            {
                personNamRows.Add(new InsertPersonNameExtendedQueryTagTableTypeV1Row(queryTag.ExtendedQueryTagStoreEntry.Key, personNameVal, (byte)queryTag.Level));
            }
        }

        private static void AddDateTimeRow(DicomDataset instance, List<InsertDateTimeExtendedQueryTagTableTypeV1Row> dateTimeRows, QueryTag queryTag)
        {
            DateTime? dateVal = DataTimeReaders.TryGetValue(
                             queryTag.VR,
                             out Func<DicomDataset, DicomTag, DicomVR, DateTime?> reader) ? reader.Invoke(instance, queryTag.Tag, queryTag.VR) : null;

            if (dateVal.HasValue)
            {
                dateTimeRows.Add(new InsertDateTimeExtendedQueryTagTableTypeV1Row(queryTag.ExtendedQueryTagStoreEntry.Key, dateVal.Value, (byte)queryTag.Level));
            }
        }

        private static void AddDoubleRow(DicomDataset instance, List<InsertDoubleExtendedQueryTagTableTypeV1Row> doubleRows, QueryTag queryTag)
        {
            double? doubleVal = instance.GetSingleValueOrDefault<double>(queryTag.Tag, expectedVR: queryTag.VR);
            if (doubleVal.HasValue)
            {
                doubleRows.Add(new InsertDoubleExtendedQueryTagTableTypeV1Row(queryTag.ExtendedQueryTagStoreEntry.Key, doubleVal.Value, (byte)queryTag.Level));
            }
        }

        private static void AddLongRow(DicomDataset instance, List<InsertLongExtendedQueryTagTableTypeV1Row> longRows, QueryTag queryTag)
        {
            long? longVal = instance.GetSingleValueOrDefault<long>(queryTag.Tag, expectedVR: queryTag.VR);

            if (longVal.HasValue)
            {
                longRows.Add(new InsertLongExtendedQueryTagTableTypeV1Row(queryTag.ExtendedQueryTagStoreEntry.Key, longVal.Value, (byte)queryTag.Level));
            }
        }

        private static void AddStringRow(DicomDataset instance, List<InsertStringExtendedQueryTagTableTypeV1Row> stringRows, QueryTag queryTag)
        {
            string stringVal = instance.GetSingleValueOrDefault<string>(queryTag.Tag, expectedVR: queryTag.VR);
            if (stringVal != null)
            {
                stringRows.Add(new InsertStringExtendedQueryTagTableTypeV1Row(queryTag.ExtendedQueryTagStoreEntry.Key, stringVal, (byte)queryTag.Level));
            }
        }
    }
}