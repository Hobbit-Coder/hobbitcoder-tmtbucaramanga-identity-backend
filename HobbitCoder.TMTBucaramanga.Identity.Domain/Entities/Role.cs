using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities.Common;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Entities
{
    public class Role : IdentityRole<Guid>, ISoftDeleteableEntity
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public string CreatedBy { get; set; } = Guid.Empty.ToString();
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
