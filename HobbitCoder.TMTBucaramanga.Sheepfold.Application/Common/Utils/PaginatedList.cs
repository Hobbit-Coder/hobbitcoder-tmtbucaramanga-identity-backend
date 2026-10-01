using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Utils;

public static class PaginatedList<TObject>
{
    public static Task<PagedResult<TObject>> CreateAsync(IQueryable<TObject> source, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        return GetPagedResultAsync(source, pageNumber, pageSize, cancellationToken);
    }

    public static async Task<PagedResult<TObject>> GetPagedResultAsync(IQueryable<TObject> source, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<TObject>(items, count, pageNumber, pageSize);
    }
}
