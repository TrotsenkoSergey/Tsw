using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Unicode;
using Marten.Services;

namespace Tsw.EventBus.Outbox.Marten;

public static class DependencyInjection
{
  public static IServiceCollection AddOutboxIntegrationEventsWithMarten(
    this IServiceCollection services, string connectionString, string assemblyFullNameWhereIntegrationEventsStore)
  {
    services.AddOutboxServices(assemblyFullNameWhereIntegrationEventsStore);

    var encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic);
    var jsonOptions = new JsonSerializerOptions
    {
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
      Encoder = encoder,
      PropertyNameCaseInsensitive = true
    };

    services.AddOptions<JsonSerializerOptions>()
      .Configure(opt => 
      {
        opt.DefaultIgnoreCondition = jsonOptions.DefaultIgnoreCondition;
        opt.Encoder = jsonOptions.Encoder;
        opt.PropertyNameCaseInsensitive = jsonOptions.PropertyNameCaseInsensitive;
      });

    if (!services.Any(d => d.ServiceType == typeof(IDocumentStore)))
    {
      var serializer = new SystemTextJsonSerializer()
      {
        EnumStorage = EnumStorage.AsString,
        Casing = Casing.SnakeCase
      };

      serializer.Customize(opt =>
      {
        opt.DefaultIgnoreCondition = jsonOptions.DefaultIgnoreCondition;
        opt.Encoder = jsonOptions.Encoder;
        opt.PropertyNameCaseInsensitive = jsonOptions.PropertyNameCaseInsensitive;
      });

      services.AddMarten(options =>
      {
        options.Connection(connectionString);

        //options.UseDefaultSerialization(EnumStorage.AsString, Casing.SnakeCase);
        options.Serializer(serializer);

        options.AutoCreateSchemaObjects = AutoCreate.All;
        options.DatabaseSchemaName = "public";
        options.Events.DatabaseSchemaName = "public";

        options.RegisterDocumentType<IntegrationEventLog>();
      });
    }

    return services;
  }

  private static IServiceCollection AddOutboxServices(this IServiceCollection services, string assemblyFullNameWhereIntegrationEventsStore)
  {
    services.AddCommonOutboxServices();

    services.AddSingleton(new EventLogSettings(assemblyFullNameWhereIntegrationEventsStore));
    services.AddTransient<IIntegrationEventLogPersistence, IntegrationEventLogService>();

    return services;
  }
}
