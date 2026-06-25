using FluentValidation;
using VaultGuard.Models.DTOs.Auth;

namespace VaultGuard.Services.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserProfileDto>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .MinimumLength(3)
            .WithMessage("Email address must be at least 3 characters long.")
            .EmailAddress()
            .WithMessage("Please provide a valid email address.")
            .Matches(@"^[a-zA-Z0-9@.\-_]+@[a-zA-Z0-9.\-_]+\.[a-zA-Z]{2,}$")
            .WithMessage("Email can only contain letters, numbers, @, ., -, and _");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]+$")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Confirm password is required.")
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.");

        RuleFor(x => x.FirstName)
            .MinimumLength(2)
            .WithMessage("First name must be at least 2 characters long.")
            .MaximumLength(50)
            .WithMessage("First name cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z\s'\-]+$")
            .WithMessage("First name can only contain letters, spaces, hyphens, and apostrophes.")
            .When(x => !string.IsNullOrEmpty(x.FirstName));

        RuleFor(x => x.LastName)
            .MinimumLength(2)
            .WithMessage("Last name must be at least 2 characters long.")
            .MaximumLength(50)
            .WithMessage("Last name cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z\s'\-]+$")
            .WithMessage("Last name can only contain letters, spaces, hyphens, and apostrophes.")
            .When(x => !string.IsNullOrEmpty(x.LastName));
    }
}