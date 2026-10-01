using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Interceptors
{
    public class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUserService;

        public AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUserService)
        {
            _currentUserService = currentUserService;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            UpdateEntities(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void UpdateEntities(DbContext? context)
        {
            if (context == null) return;

            var currentUser = _currentUserService.UserId ?? "System";
            var now = DateTime.UtcNow;

            foreach (var entry in context.ChangeTracker.Entries().Where(e => e.Entity is IAuditableEntity))
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Property("CreatedBy").CurrentValue = currentUser;
                        entry.Property("CreatedDate").CurrentValue = now;
                        break;

                    case EntityState.Modified:
                        entry.Property("UpdatedBy").CurrentValue = currentUser;
                        entry.Property("UpdatedDate").CurrentValue = now;
                        break;
                }
            }
        }
    }
}
