namespace VaultMind.API.Interfaces;

public interface IUser : IEntity
{
    string Email { get; set; }
    string Name { get; set; }
}
