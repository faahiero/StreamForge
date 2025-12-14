using Xunit;

namespace StreamForge.Tests.Integration;

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory<Program>>
{
    // Esta classe não tem código, serve apenas para definir a coleção.
}
