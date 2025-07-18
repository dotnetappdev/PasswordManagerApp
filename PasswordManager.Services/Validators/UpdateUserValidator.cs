using FluentValidation;
using PasswordManager.Models.DTOs.Auth;

namespace PasswordManager.Services.Validators;

public class UpdateUserValidator : AbstractValidator<UpdateUserProfileDto>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("User ID is required.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage("Please provide a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.FirstName)
            .MaximumLength(50)
            .WithMessage("First name cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(50)
            .WithMessage("Last name cannot exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.LastName));

        RuleFor(x => x.MasterPasswordHint)
            .MaximumLength(500)
            .WithMessage("Master password hint cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.MasterPasswordHint));
    }
}