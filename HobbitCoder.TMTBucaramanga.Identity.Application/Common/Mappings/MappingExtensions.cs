using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Common.Mappings;

public static class MappingExtensions
{
    public static Task<PagedResult<TDestination>> PaginatedListAsync<TDestination>(this IQueryable<TDestination> queryable, int pageNumber, int pageSize, CancellationToken cancellationToken = default) where TDestination : class
        => PaginatedList<TDestination>.CreateAsync(queryable.AsNoTracking(), pageNumber, pageSize, cancellationToken);
}
