using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Mappings;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Identity.Application.Common.Utils;
using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities;

namespace HobbitCoder.TMTBucaramanga.Identity.Application.Users.Common;

public static class GetUsersQueryHelpers
{
    public static IQueryable<User> Search(IQueryable<User> users, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return users;
        }

        var text = search.Trim().ToUpperInvariant();
        var phone = PhoneNumbers.Normalize(search);

        return users.Where(user => user.FirstName.ToUpper().Contains(text)
            || user.LastName.ToUpper().Contains(text)
            || user.NormalizedUserName!.Contains(text)
            || (user.NormalizedEmail != null && user.NormalizedEmail.Contains(text))
            || (phone != null && user.PhoneNumber == phone));
    }

    public static async Task<PagedResult<UserResponse>> PageAsync(TimeProvider timeProvider, IQueryable<User> users, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var today = LocalCalendar.Today(timeProvider);

        var page = await users
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Id)
            .PaginatedListAsync(pageNumber, pageSize, cancellationToken);

        return page.Select(user => UserResponse.From(user, today));
    }
}
