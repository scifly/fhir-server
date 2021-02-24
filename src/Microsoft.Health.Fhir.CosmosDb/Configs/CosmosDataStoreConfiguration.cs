﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Health.Fhir.CosmosDb.Configs
{
    public class CosmosDataStoreConfiguration
    {
        public string Host { get; set; }

        public string Key { get; set; }

        public string DatabaseId { get; set; }

        public int? InitialDatabaseThroughput { get; set; }

        public ConnectionMode ConnectionMode { get; set; } = ConnectionMode.Direct;

        public ConsistencyLevel? DefaultConsistencyLevel { get; set; }

        public bool AllowDatabaseCreation { get; set; } = true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "This is a configuration class")]
        public IList<string> PreferredLocations { get; set; }

        public int DataMigrationBatchSize { get; set; } = 100;

        /// <summary>
        /// Retry options that are fed into the Cosmos DB sdk for individual database calls.
        /// </summary>
        public CosmosDataStoreRetryOptions RetryOptions { get; } = new CosmosDataStoreRetryOptions { MaxNumberOfRetries = 3, MaxWaitTimeInSeconds = 5 };

        /// <summary>
        /// Allows more generous retry options when processing batches to avoid actions with 429 response codes.
        /// </summary>
        public CosmosDataStoreRetryOptions IndividualBatchActionRetryOptions { get; } = new CosmosDataStoreRetryOptions { MaxNumberOfRetries = 18, MaxWaitTimeInSeconds = 90 };

        public int? ContinuationTokenSizeLimitInKb { get; set; }

        /// <summary>
        /// The maximum number of seconds to spend fetching search result pages when the first page comes up with fewer results than requested.
        /// This time includes the time to fetch the first page.
        /// </summary>
        public int SearchEnumerationTimeoutInSeconds { get; set; } = 30;

        public HashSet<string> SortSearchParameters { get; } = new HashSet<string>();
    }
}
