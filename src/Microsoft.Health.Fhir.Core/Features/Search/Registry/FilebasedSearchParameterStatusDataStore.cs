﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnsureThat;
using Microsoft.Health.Core;
using Microsoft.Health.Fhir.Core.Data;
using Microsoft.Health.Fhir.Core.Features.Definition;
using Microsoft.Health.Fhir.Core.Models;
using Newtonsoft.Json;

namespace Microsoft.Health.Fhir.Core.Features.Search.Registry
{
    public class FilebasedSearchParameterStatusDataStore : ISearchParameterStatusDataStore
    {
        private readonly ISearchParameterDefinitionManager _searchParameterDefinitionManager;
        private readonly IModelInfoProvider _modelInfoProvider;
        private ResourceSearchParameterStatus[] _statusResults;

        public FilebasedSearchParameterStatusDataStore(
            ISearchParameterDefinitionManager searchParameterDefinitionManager,
            IModelInfoProvider modelInfoProvider)
        {
            EnsureArg.IsNotNull(searchParameterDefinitionManager, nameof(searchParameterDefinitionManager));

            _searchParameterDefinitionManager = searchParameterDefinitionManager;
            _modelInfoProvider = modelInfoProvider;
        }

        public delegate ISearchParameterStatusDataStore Resolver();

        public Task<IReadOnlyCollection<ResourceSearchParameterStatus>> GetSearchParameterStatuses()
        {
            if (_statusResults == null)
            {
                using Stream stream = _modelInfoProvider.OpenVersionedFileStream("unsupported-search-parameters.json");
                using TextReader reader = new StreamReader(stream);
                UnsupportedSearchParameters unsupportedParams = JsonConvert.DeserializeObject<UnsupportedSearchParameters>(reader.ReadToEnd());

                // Loads unsupported parameters
                var support = unsupportedParams.Unsupported
                    .Select(x => new ResourceSearchParameterStatus
                    {
                        Uri = x,
                        Status = SearchParameterStatus.Disabled,
                        LastUpdated = Clock.UtcNow,
                    })
                    .Concat(unsupportedParams.PartialSupport
                        .Select(x => new ResourceSearchParameterStatus
                        {
                            Uri = x,
                            Status = SearchParameterStatus.Enabled,
                            IsPartiallySupported = true,
                            LastUpdated = Clock.UtcNow,
                        }))
                    .ToDictionary(x => x.Uri);

                // Merge with supported list
                _statusResults = _searchParameterDefinitionManager.AllSearchParameters
                    .Where(x => !support.ContainsKey(x.Url))
                    .Select(x => new ResourceSearchParameterStatus
                    {
                        Uri = x.Url,
                        Status = SearchParameterStatus.Enabled,
                        LastUpdated = Clock.UtcNow,
                    })
                    .Concat(support.Values)
                    .ToArray();
            }

            return Task.FromResult<IReadOnlyCollection<ResourceSearchParameterStatus>>(_statusResults);
        }

        public async Task<ResourceSearchParameterStatus> GetSearchParameterStatus(Uri searchParameterUri, CancellationToken cancellationToken)
        {
            return (await GetSearchParameterStatuses()).FirstOrDefault(x => x.Uri == searchParameterUri);
        }

        public Task UpsertStatuses(List<ResourceSearchParameterStatus> statuses)
        {
            // File based registry does not persist runtime updates
            return Task.CompletedTask;
        }
    }
}
