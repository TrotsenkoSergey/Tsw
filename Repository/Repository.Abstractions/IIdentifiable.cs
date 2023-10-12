namespace Tsw.Repository.Abstractions;

public interface IIdentifiable<TId>
{
  TId Id { get; }
}
