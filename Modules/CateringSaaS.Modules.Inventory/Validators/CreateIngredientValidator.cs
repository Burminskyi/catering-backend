using CateringSaaS.Modules.Inventory.Domain.Models;
using CateringSaaS.Modules.Inventory.DTOs;
using CateringSaaS.Shared.Data;
using CateringSaaS.Shared.MultiTenancy;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CateringSaaS.Modules.Inventory.Validators;

public sealed class CreateIngredientValidator : AbstractValidator<CreateIngredientRequest>
{
    public CreateIngredientValidator(AppDbContext dbContext, ITenantContext tenantContext)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Category)
            .NotEmpty();

        RuleFor(x => x.BaseUnit)
            .NotEmpty();

        RuleFor(x => x.Name)
            .MustAsync(async (name, cancellationToken) =>
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return true;
                }

                var normalized = name.Trim().ToLowerInvariant();
                var currentWorkspaceId = tenantContext.WorkspaceId;

                // Reject if the same name already exists as a global ingredient
                // OR as an ingredient in the current workspace.
                return !await dbContext.Set<Ingredient>()
                    .AnyAsync(
                        i => i.Name.ToLower() == normalized
                             && (i.WorkspaceId == null || i.WorkspaceId == currentWorkspaceId),
                        cancellationToken);
            })
            .WithMessage(
                "An ingredient with this name already exists in the global catalog or in the current workspace.");
    }
}
