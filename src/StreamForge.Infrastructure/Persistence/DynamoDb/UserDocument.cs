using Amazon.DynamoDBv2.DataModel;

namespace StreamForge.Infrastructure.Persistence.DynamoDb;

[DynamoDBTable("User")]
public class UserDocument
{
    [DynamoDBHashKey] // PK: Email (para login rápido)
    public string? Email { get; set; }
    
    [DynamoDBProperty]
    public Guid Id { get; set; }
    
    [DynamoDBProperty]
    public string? PasswordHash { get; set; }
    
    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; }
}
