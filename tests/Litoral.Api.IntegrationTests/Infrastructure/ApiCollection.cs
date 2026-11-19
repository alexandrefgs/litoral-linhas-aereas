namespace Litoral.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<LitoralApiFactory>
{
    public const string Name = "Litoral API";
}