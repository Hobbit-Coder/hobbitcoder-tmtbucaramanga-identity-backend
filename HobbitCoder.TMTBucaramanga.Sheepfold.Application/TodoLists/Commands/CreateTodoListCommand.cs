using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Enums;
using HobbitCoder.TMTBucaramanga.Sheepfold.Mediator.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Commands;

public class CreateTodoListCommand : IRequest<Guid>
{
    public required string Title { get; set; }
    public required TodoStatus Status { get; set; }
    public List<CreateTodoItemCommand>? Items { get; set; } = [];
}

public class CreateTodoListCommandHandler : IRequestHandler<CreateTodoListCommand, Guid>
{
    private readonly IApplicationDbContext _dbContext;

    public CreateTodoListCommandHandler(IApplicationDbContext context)
    {
        _dbContext = context;
    }

    public async Task<Guid> Handle(CreateTodoListCommand request, CancellationToken cancellationToken = default)
    {
        var todoList = new TodoList
        {
            Title = request.Title,
            Status = request.Status
        };

        _dbContext.TodoLists.Add(todoList);

        if (request.Items is not null && request.Items.Count > 0)
        {
            foreach (var item in request.Items)
            {
                var todoItem = new TodoItem
                {
                    TodoListId = todoList.Id,
                    Title = item.Title,
                    Description = item.Description,
                    Status = item.Status
                };

                _dbContext.TodoItems.Add(todoItem);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return todoList.Id;
    }
}

public class CreateTodoListCommandValidator : AbstractValidator<CreateTodoListCommand>
{
    public CreateTodoListCommandValidator()
    {
        RuleFor(todoListRequest => todoListRequest.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(todoListRequest => todoListRequest.Status)
            .IsInEnum().WithMessage("Invalid status");
    }
}
