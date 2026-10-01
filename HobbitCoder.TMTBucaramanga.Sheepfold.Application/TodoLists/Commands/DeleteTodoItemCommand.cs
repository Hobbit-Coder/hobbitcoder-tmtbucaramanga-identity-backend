using FluentValidation;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Exceptions;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Interfaces;
using HobbitCoder.TMTBucaramanga.Sheepfold.Application.Common.Models;
using HobbitCoder.TMTBucaramanga.Sheepfold.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HobbitCoder.TMTBucaramanga.Sheepfold.Application.TodoLists.Commands;

public class DeleteTodoItemCommand : IRequest
{
    public required Guid Id { get; set; }
}

public class DeleteTodoItemCommandHandler : IRequestHandler<DeleteTodoItemCommand, Unit>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DeleteTodoItemCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _dbContext = context;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteTodoItemCommand request, CancellationToken cancellationToken = default)
    {
        var todoItem = await _dbContext.TodoItems
            .FirstOrDefaultAsync(todoItem => todoItem.Id == request.Id && !todoItem.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(TodoItem), request.Id);

        todoItem.IsDeleted = true;
        todoItem.DeletedAt = DateTime.UtcNow;
        todoItem.DeletedBy = _currentUserService.UserId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

public class DeleteTodoItemCommandValidator : AbstractValidator<DeleteTodoItemCommand>
{
    public DeleteTodoItemCommandValidator()
    {
        RuleFor(todoItemRequest => todoItemRequest.Id).NotEmpty()
            .WithMessage("Id is required");
    }
}
