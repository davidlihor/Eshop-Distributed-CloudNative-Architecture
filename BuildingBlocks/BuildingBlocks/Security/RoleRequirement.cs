using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Security;

public enum UserRole
{
    Admin,
    Employee,
    Customer
}

public class RoleRequirement(UserRole role) : IAuthorizationRequirement
{
    public UserRole Role { get; } = role;
}

public class RoleRequirementHandler : AuthorizationHandler<RoleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true) return Task.CompletedTask;

        if (context.User.IsInRole(requirement.Role.ToString()))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}