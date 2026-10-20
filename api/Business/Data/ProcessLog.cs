using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StargateAPI.Business.Data;

public class ProcessLog
{
    public long Id { get; set; }

    public DateTime TimestampUtc { get; set; }

    public string Level { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? Exception { get; set; }
}

public class ProcessLogConfiguration : IEntityTypeConfiguration<ProcessLog>
{
    public void Configure(EntityTypeBuilder<ProcessLog> builder)
    {
        builder.ToTable("ProcessLog");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).ValueGeneratedOnAdd();
        builder.Property(log => log.TimestampUtc).IsRequired();
        builder.Property(log => log.Level).IsRequired();
        builder.Property(log => log.Method).IsRequired();
        builder.Property(log => log.Path).IsRequired();
        builder.Property(log => log.StatusCode).IsRequired();
        builder.Property(log => log.Message).IsRequired();
        builder.HasIndex(log => log.TimestampUtc);
    }
}
