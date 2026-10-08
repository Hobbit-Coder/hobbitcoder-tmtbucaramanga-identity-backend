using HobbitCoder.TMTBucaramanga.Identity.Domain.Entities.Common;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Identity.Domain.Entities
{
    public class User : IdentityUser<Guid>, ISoftDeleteableEntity
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required bool IsActive { get; set; }

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        public string CreatedBy { get; set; } = Guid.Empty.ToString();
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = [];
        public virtual ICollection<AuthorizationCode> AuthorizationCodes { get; set; } = [];
    }
}
