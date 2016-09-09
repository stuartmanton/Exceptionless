﻿using System;
using System.Threading.Tasks;
using Exceptionless.Core.Models;
using Exceptionless.Core.Repositories.Queries;
using Foundatio.Repositories.Models;

namespace Exceptionless.Core.Repositories {
    public interface IStackRepository : IRepositoryOwnedByOrganizationAndProject<Stack> {
        Task<Stack> GetStackBySignatureHashAsync(string projectId, string signatureHash);
        Task<IFindResults<Stack>> GetMostRecentAsync(string projectId, DateTime utcStart, DateTime utcEnd, PagingOptions paging, string filter = null);
        Task<IFindResults<Stack>> GetNewAsync(string projectId, DateTime utcStart, DateTime utcEnd, PagingOptions paging, string filter = null);
        Task<IFindResults<Stack>> GetByFilterAsync(IExceptionlessSystemFilterQuery systemFilter, string userFilter, SortingOptions sorting, string field, DateTime utcStart, DateTime utcEnd, PagingOptions paging);

        Task MarkAsRegressedAsync(string stackId);
        Task IncrementEventCounterAsync(string organizationId, string projectId, string stackId, DateTime minOccurrenceDateUtc, DateTime maxOccurrenceDateUtc, int count, bool sendNotifications = true);
    }
}