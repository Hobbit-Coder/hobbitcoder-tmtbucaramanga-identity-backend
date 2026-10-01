using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Commands;

public class DeleteTodoListCommand : IRequest
{
    public required Guid Id { get; set; }
}

public class DeleteTodoListCommandHandler : IRequestHandler<DeleteTodoListCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DeleteTodoListCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _dbContext = context;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteTodoListCommand request, CancellationToken cancellationToken = default)
    {
        var todoList = await _dbContext.TodoLists
            .Include(todoList => todoList.TodoItems)
            .FirstOrDefaultAsync(todoList => todoList.Id == request.Id && !todoList.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoList), request.Id);

        var deletedAt = DateTime.UtcNow;
        var deletedBy = _currentUserService.UserId;

        // Borrado lógico en cascada: los items no deben quedar visibles colgando de una lista eliminada.
        foreach (var todoItem in todoList.TodoItems.Where(todoItem => !todoItem.IsDeleted))
        {
            todoItem.IsDeleted = true;
            todoItem.DeletedAt = deletedAt;
            todoItem.DeletedBy = deletedBy;
        }

        todoList.IsDeleted = true;
        todoList.DeletedAt = deletedAt;
        todoList.DeletedBy = deletedBy;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class DeleteTodoListCommandValidator : AbstractValidator<DeleteTodoListCommand>
{
    public DeleteTodoListCommandValidator()
    {
        RuleFor(todoListRequest => todoListRequest.Id)
            .NotEmpty().WithMessage("Id is required");
    }
}
