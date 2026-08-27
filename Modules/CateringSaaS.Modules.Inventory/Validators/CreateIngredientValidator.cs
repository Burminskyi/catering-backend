using CateringSaaS.Modules.Inventory.DTOs;
using FluentValidation;

namespace CateringSaaS.Modules.Inventory.Validators;

public sealed class CreateIngredientValidator : AbstractValidator<CreateIngredientRequest>
{
    public CreateIngredientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.BaseUnit).NotEmpty();
    }
}

public sealed class UpdateIngredientValidator : AbstractValidator<UpdateIngredientRequest>
{
    public UpdateIngredientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.BaseUnit).NotEmpty();
    }
}

public sealed class AddStockPurchaseValidator : AbstractValidator<AddStockPurchaseRequest>
{
    public AddStockPurchaseValidator()
    {
        RuleFor(x => x.IngredientId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Unit).NotEmpty();
        RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0);
    }
}

public sealed class ConsumeStockValidator : AbstractValidator<ConsumeStockRequest>
{
    public ConsumeStockValidator()
    {
        RuleFor(x => x.IngredientId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Unit).NotEmpty();
    }
}

public sealed class CreateSupplierValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(64);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class UpdateSupplierValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(64);
        RuleFor(x => x.Email).MaximumLength(256);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
