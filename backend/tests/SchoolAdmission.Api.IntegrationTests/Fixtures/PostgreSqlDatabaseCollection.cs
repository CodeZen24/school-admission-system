namespace SchoolAdmission.Api.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class PostgreSqlDatabaseTestGroup : ICollectionFixture<PostgreSqlDatabaseFixture>
{
    public const string Name = "PostgreSqlDatabase";
}
