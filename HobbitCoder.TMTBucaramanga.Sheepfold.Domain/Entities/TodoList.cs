using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities.Common;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;

public class TodoList : SoftDeleteableEntity
{
    public required string Title { get; set; }
    public required TodoStatus Status { get; set; } = TodoStatus.Pending;

    public virtual ICollection<TodoItem> TodoItems { get; set; } = [];
}
