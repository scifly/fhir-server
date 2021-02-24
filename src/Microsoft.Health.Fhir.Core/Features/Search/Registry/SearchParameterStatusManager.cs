﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnsureThat;
using MediatR;
using Microsoft.Health.Core;
using Microsoft.Health.Extensions.DependencyInjection;
using Microsoft.Health.Fhir.Core.Features.Definition;
using Microsoft.Health.Fhir.Core.Features.Search.Parameters;
using Microsoft.Health.Fhir.Core.Messages.Search;
using Microsoft.Health.Fhir.Core.Models;

namespace Microsoft.Health.Fhir.Core.Features.Search.Registry
{
    public class SearchParameterStatusManager : IRequireInitializationOnFirstRequest
    {
        private readonly ISearchParameterStatusDataStore _searchParameterStatusDataStore;
        private readonly ISearchParameterDefinitionManager _searchParameterDefinitionManager;
        private readonly ISearchParameterSupportResolver _searchParameterSupportResolver;
        private readonly IMediator _mediator;

        public SearchParameterStatusManager(
            ISearchParameterStatusDataStore searchParameterStatusDataStore,
            ISearchParameterDefinitionManager searchParameterDefinitionManager,
            ISearchParameterSupportResolver searchParameterSupportResolver,
            IMediator mediator)
        {
            EnsureArg.IsNotNull(searchParameterStatusDataStore, nameof(searchParameterStatusDataStore));
            EnsureArg.IsNotNull(searchParameterDefinitionManager, nameof(searchParameterDefinitionManager));
            EnsureArg.IsNotNull(searchParameterSupportResolver, nameof(searchParameterSupportResolver));
            EnsureArg.IsNotNull(mediator, nameof(mediator));

            _searchParameterStatusDataStore = searchParameterStatusDataStore;
            _searchParameterDefinitionManager = searchParameterDefinitionManager;
            _searchParameterSupportResolver = searchParameterSupportResolver;
            _mediator = mediator;
        }

        public async Task EnsureInitialized()
        {
            var updated = new List<SearchParameterInfo>();

            var parameters = (await _searchParameterStatusDataStore.GetSearchParameterStatuses())
                .ToDictionary(x => x.Uri);

            // Set states of known parameters
            foreach (SearchParameterInfo p in _searchParameterDefinitionManager.AllSearchParameters)
            {
                if (parameters.TryGetValue(p.Url, out ResourceSearchParameterStatus result))
                {
                    bool isSearchable = result.Status == SearchParameterStatus.Enabled;
                    bool isSupported = result.Status == SearchParameterStatus.Supported || result.Status == SearchParameterStatus.Enabled;
                    bool isPartiallySupported = result.IsPartiallySupported;

                    if (result.Status == SearchParameterStatus.Disabled)
                    {
                        // Re-check if this parameter is now supported.
                        (bool Supported, bool IsPartiallySupported) supportedResult = _searchParameterSupportResolver.IsSearchParameterSupported(p);
                        isSupported = supportedResult.Supported;
                        isPartiallySupported = supportedResult.IsPartiallySupported;
                    }

                    if (p.IsSearchable != isSearchable ||
                        p.IsSupported != isSupported ||
                        p.IsPartiallySupported != isPartiallySupported ||
                        p.SortStatus != result.SortStatus)
                    {
                        p.IsSearchable = isSearchable;
                        p.IsSupported = isSupported;
                        p.IsPartiallySupported = isPartiallySupported;
                        p.SortStatus = result.SortStatus;

                        updated.Add(p);
                    }
                }
                else
                {
                    p.IsSearchable = false;

                    // Check if this parameter is now supported.
                    (bool Supported, bool IsPartiallySupported) supportedResult = _searchParameterSupportResolver.IsSearchParameterSupported(p);
                    p.IsSupported = supportedResult.Supported;
                    p.IsPartiallySupported = supportedResult.IsPartiallySupported;

                    updated.Add(p);
                }
            }

            await _mediator.Publish(new SearchParametersUpdated(updated));
        }

        public async Task UpdateSearchParameterStatusAsync(IReadOnlyCollection<string> searchParameterUris, SearchParameterStatus status)
        {
            var searchParameterStatusList = new List<ResourceSearchParameterStatus>();
            var updated = new List<SearchParameterInfo>();
            var parameters = (await _searchParameterStatusDataStore.GetSearchParameterStatuses())
                .ToDictionary(x => x.Uri);

            foreach (string uri in searchParameterUris)
            {
                var searchParamUri = new Uri(uri);

                var paramInfo = _searchParameterDefinitionManager.GetSearchParameter(searchParamUri);
                updated.Add(paramInfo);
                paramInfo.IsSearchable = status == SearchParameterStatus.Enabled;
                paramInfo.IsSupported = status == SearchParameterStatus.Supported || status == SearchParameterStatus.Enabled;

                if (parameters.TryGetValue(searchParamUri, out var existingStatus))
                {
                    existingStatus.LastUpdated = Clock.UtcNow;
                    existingStatus.Status = status;

                    if (paramInfo.IsSearchable && existingStatus.SortStatus == SortParameterStatus.Supported)
                    {
                        existingStatus.SortStatus = SortParameterStatus.Enabled;
                        paramInfo.SortStatus = SortParameterStatus.Enabled;
                    }

                    searchParameterStatusList.Add(existingStatus);
                }
                else
                {
                    searchParameterStatusList.Add(new ResourceSearchParameterStatus
                    {
                        LastUpdated = Clock.UtcNow,
                        Status = status,
                        Uri = searchParamUri,
                    });
                }
            }

            await _searchParameterStatusDataStore.UpsertStatuses(searchParameterStatusList);

            await _mediator.Publish(new SearchParametersUpdated(updated));
        }

        internal async Task AddSearchParameterStatusAsync(IReadOnlyCollection<string> searchParamUris)
        {
            // new search parameters are added as supported, until reindexing occurs, when
            // they will be fully enabled
            await UpdateSearchParameterStatusAsync(searchParamUris, SearchParameterStatus.Supported);
        }

        internal async Task DeleteSearchParameterStatusAsync(string url)
        {
            var searchParamUris = new List<string>() { url };
            await UpdateSearchParameterStatusAsync(searchParamUris, SearchParameterStatus.Deleted);
        }
    }
}
