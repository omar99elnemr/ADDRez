using Microsoft.AspNetCore.Authorization;

namespace ADDRez.Api.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class PermissionAttribute : AuthorizeAttribute
{
    public PermissionAttribute(string permission) : base(policy: $"Permission:{permission}")
    {
        Permission = permission;
    }

    public string Permission { get; }
}
