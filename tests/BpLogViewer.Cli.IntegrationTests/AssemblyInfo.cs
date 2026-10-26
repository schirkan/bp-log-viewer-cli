using Xunit;

// Console.Out/Console.Error and environment variables are process-global.
// xUnit runs test classes in parallel by default, so a concurrently running test can
// write into another test's captured buffer and break the JSON parsing there.
// The whole suite finishes in about a second, so serializing it is the cheap and
// reliable fix.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
