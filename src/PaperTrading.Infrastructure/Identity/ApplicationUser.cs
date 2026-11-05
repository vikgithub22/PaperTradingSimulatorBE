using Microsoft.AspNetCore.Identity;

namespace PaperTrading.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
