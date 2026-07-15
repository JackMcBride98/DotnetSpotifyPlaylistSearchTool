using Xunit;

// Force all test classes to run one after another, enabling safe Respawner runs
[assembly: CollectionBehavior(
    CollectionBehavior.CollectionPerAssembly,
    DisableTestParallelization = true
)]
[assembly: AssemblyFixture(typeof(Tests.App))]
