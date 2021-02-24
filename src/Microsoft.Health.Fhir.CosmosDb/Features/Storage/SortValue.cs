﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using EnsureThat;
using Microsoft.Health.Fhir.Core.Features.Search.SearchValues;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.CosmosDb.Features.Storage
{
    public class SortValue
    {
        public SortValue(ISearchValue low, ISearchValue high, Uri searchParameterUri)
        {
            EnsureArg.IsNotNull(searchParameterUri, nameof(searchParameterUri));

            Low = low;
            High = high;
            SearchParameterUri = searchParameterUri;
        }

        public SortValue(Uri searchParameterUri)
        {
            EnsureArg.IsNotNull(searchParameterUri, nameof(searchParameterUri));
            SearchParameterUri = searchParameterUri;
        }

        public ISearchValue Low { get; }

        public ISearchValue High { get; }

        public Uri SearchParameterUri { get; }
    }
}
