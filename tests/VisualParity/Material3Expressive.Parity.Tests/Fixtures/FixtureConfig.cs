namespace Material3Expressive.Parity.Tests;

// Protocol fixtures belong to the framework tests, independently of component suites.
public sealed record FixtureConfig(bool Enabled, string Text);
public sealed record NumericConfig(double Answer);
