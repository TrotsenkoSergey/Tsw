namespace Tsw.Repository.Abstractions;

public interface IIdentifiable<TId>
  where TId : notnull
{
  TId Id { get; }
}
