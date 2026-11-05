using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrading.Infrastructure.Identity;

namespace PaperTrading.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.DisplayName)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(u => u.CreatedAtUtc)
            .IsRequired();
    }
}
