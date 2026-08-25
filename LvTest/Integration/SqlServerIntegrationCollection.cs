using Xunit;

namespace LvTest.Integration;

/// <summary>
/// Forces all real-SQL-Server integration tests to run sequentially against the
/// shared local instance instead of xUnit's default cross-class parallelism.
/// </summary>
[CollectionDefinition("SqlServerIntegration", DisableParallelization = true)]
public class SqlServerIntegrationCollection { }
