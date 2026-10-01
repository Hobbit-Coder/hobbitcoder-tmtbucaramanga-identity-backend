using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Infrastructure.Persistence.Contexts
{
    public class SheepfoldContext : DbContext, IApplicationDbContext
    {
        public DbSet<TodoItem> TodoItems { get; set; }
        public DbSet<TodoList> TodoLists { get; set; }

        public SheepfoldContext(DbContextOptions<SheepfoldContext> options) : base(options)
        {
            TodoItems = Set<TodoItem>();
            TodoLists = Set<TodoList>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Sheepfold");
            base.OnModelCreating(modelBuilder);
        }
    }
}
