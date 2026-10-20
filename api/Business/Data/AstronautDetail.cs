using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace StargateAPI.Business.Data
{
    [Table("AstronautDetail")]
    public class AstronautDetail
    {
        public int Id { get; set; }

        public int PersonId { get; set; }

        public string CurrentRank { get; set; } = string.Empty;

        public string CurrentDutyTitle { get; set; } = string.Empty;

        // Looks like should be non-nullable even though initial create has nullable Business\Migrations\20240122154939_InitialCreate.cs
        public DateTime CareerStartDate { get; set; }

        public DateTime? CareerEndDate { get; set; }

        // Treat as non-null for now, change to required virtual later if prove to need compiler to enforce
        // PersonId is required though so a person should exist in theory
        public virtual Person Person { get; set; } = null!;
    }

    public class AstronautDetailConfiguration : IEntityTypeConfiguration<AstronautDetail>
    {
        public void Configure(EntityTypeBuilder<AstronautDetail> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
        }
    }
}
