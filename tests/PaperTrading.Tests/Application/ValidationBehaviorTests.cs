using FluentAssertions;
using FluentValidation;
using MediatR;
using PaperTrading.Application.Common.Behaviors;
using PaperTrading.Application.Common.Models;

namespace PaperTrading.Tests.Application;

public sealed class ValidationBehaviorTests
{
    private sealed record TestCommand(string Name, int Age) : IRequest<Result<string>>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.");

            RuleFor(x => x.Age)
                .GreaterThan(0).WithMessage("Age must be positive.");
        }
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShortCircuitsAndReturnsValidationError()
    {
        // Arrange
        var validators = new IValidator<TestCommand>[] { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand(string.Empty, -5);

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("Success!"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeFalse("Pipeline should short-circuit when validation fails");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ValidationError>();

        var validationError = (ValidationError)result.Error;
        validationError.Errors.Should().ContainKey("Name");
        validationError.Errors["Name"].Should().Contain("Name is required.");
        validationError.Errors.Should().ContainKey("Age");
        validationError.Errors["Age"].Should().Contain("Age must be positive.");
    }

    [Fact]
    public async Task Handle_WhenValidationSucceeds_CallsNextDelegate()
    {
        // Arrange
        var validators = new IValidator<TestCommand>[] { new TestCommandValidator() };
        var behavior = new ValidationBehavior<TestCommand, Result<string>>(validators);
        var command = new TestCommand("Alice", 25);

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (cancellationToken) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("Processed"));
        };

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("Processed");
    }
}
