using Xunit;

// Force all test classes to run one after another, enabling safe Respawner runs
[assembly: CollectionBehavior(DisableTestParallelization = true)]