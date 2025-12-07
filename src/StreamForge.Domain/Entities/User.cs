namespace StreamForge.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Construtor para ORM
    public User() 
    { 
        Email = null!;
        PasswordHash = null!;
    }

    public User(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentNullException(nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentNullException(nameof(passwordHash));

        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetId(Guid id) => Id = id;
    public void SetEmail(string email) => Email = email;
    public void SetPasswordHash(string hash) => PasswordHash = hash;
    public void SetCreatedAt(DateTime date) => CreatedAt = date;
}
