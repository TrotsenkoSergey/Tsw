using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Tsw.EventBus.Outbox;

/// <summary>
/// Contains extensions methods for the <see cref="ModelBuilder"/> class.
/// </summary>
public static class ModelBuilderExtensions
{
  private static readonly ValueConverter<DateTime, DateTime> UtcValueConverter =
      new(outside => outside, inside => DateTime.SpecifyKind(inside, DateTimeKind.Utc));

  /// <summary>
  /// Applies the UTC date-time converter to all of the properties that are <see cref="DateTime"/> and end with Utc.
  /// </summary>
  /// <param name="modelBuilder">The model builder.</param>
  public static void ApplyUtcDateTimeConverter(this ModelBuilder modelBuilder)
  {
    foreach (IMutableEntityType mutableEntityType in modelBuilder.Model.GetEntityTypes())
    {
      IEnumerable<IMutableProperty> dateTimeUtcProperties = mutableEntityType.GetProperties()
          .Where(p => p.ClrType == typeof(DateTime) && p.Name.EndsWith("Utc", StringComparison.Ordinal));

      foreach (IMutableProperty mutableProperty in dateTimeUtcProperties)
      {
        mutableProperty.SetValueConverter(UtcValueConverter);
      }
    }
  }

  public static void ApplyEnumTableBuilding<TEnum>(this ModelBuilder modelBuilder)
      where TEnum : struct
  {
    var enumType = typeof(TEnum);
    var enumName = enumType.Name;
    string tableName = enumName;
    if (enumName.EndsWith('s'))
    {
      tableName += "es";
    }
    else if (enumName.EndsWith('y'))
    {
      tableName = tableName.Remove(tableName.Length - 1);
      tableName += "ies";
    }
    else 
    {
      tableName += "s";
    }

    const string Id = "Id";
    const string Name = "Name";

    modelBuilder.Entity(enumName).ToTable(tableName);

    modelBuilder.Entity(enumName).Property<short>(Id);
    modelBuilder.Entity(enumName).HasKey(Id);

    modelBuilder.Entity(enumName)
        .Property<string>(Name)
        .HasMaxLength(30)
        .IsRequired();

    modelBuilder
        .Entity(enumName)
        .HasData(Enum.GetValues(enumType)
            .Cast<TEnum>()
            .Select(@enum => new
            {
              Id = Convert.ToInt16(@enum),
              Name = @enum.ToString()
            })
    );
  }
}
