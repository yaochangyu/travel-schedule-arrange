using Microsoft.EntityFrameworkCore;
using TravelScheduleArrange.Api.Models;

namespace TravelScheduleArrange.Api.Data;

/// <summary>
/// 應用程式的 EF Core DbContext。
/// 資料表 Schema 於步驟 5（SQLite 資料持久化）建立。
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Attraction> Attractions => Set<Attraction>();

    public DbSet<Itinerary> Itineraries => Set<Itinerary>();

    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Attraction>(entity =>
        {
            entity.HasIndex(a => a.SourceId);
        });

        modelBuilder.Entity<Itinerary>(entity =>
        {
            entity.HasMany(i => i.Items)
                .WithOne(item => item.Itinerary)
                .HasForeignKey(item => item.ItineraryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItineraryItem>(entity =>
        {
            entity.HasOne(item => item.Attraction)
                .WithMany(a => a.ItineraryItems)
                .HasForeignKey(item => item.AttractionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(item => new { item.ItineraryId, item.Order });
        });
    }
}
