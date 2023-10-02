namespace Tsw.Repository.Abstractions;

public interface IIdentifiable<TId>
  where TId : IEquatable<TId>
{
  TId Id { get; }
}
