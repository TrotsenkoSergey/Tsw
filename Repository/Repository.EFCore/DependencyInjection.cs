using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using Tsw.Repository.Abstractions;

namespace Tsw.Repository.EFCore;

public static class DependencyInjection
{
  public static IServiceCollection AddEfCoreRepository<TDbContext>(this IServiceCollection services)
    where TDbContext: BaseDbContext 
  {
    services.AddScoped<IUnitOfWork<DatabaseFacade>>(sp => sp.GetRequiredService<TDbContext>());

    services.AddScoped<DbContext>(sp => sp.GetRequiredService<TDbContext>());

    services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
    services.AddScoped(typeof(IReadRepository<,>), typeof(Repository<,>));
    services.AddScoped(typeof(IWriteRepository<,>), typeof(Repository<,>));

    return services;
  }
}
