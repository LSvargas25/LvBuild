namespace LvTest.Integration;

/// <summary>
/// Groups every real-PostgreSQL integration test so they share one container
/// (<see cref="PostgresFixture"/>) and run sequentially, since each test resets the database.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public class PostgresIntegrationCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "PostgresIntegration";
}
