using System.Runtime.CompilerServices;

// Declared here because Unity's .asmdef format has no InternalsVisibleTo equivalent. These assembly names
// must match the test asmdefs; internals are exposed for pool diagnostics and history/request internals that
// tests assert against but consumers should not bind to.
[assembly: InternalsVisibleTo("UIFramework.TestUtils")]
[assembly: InternalsVisibleTo("UIFramework.Tests.EditMode")]
[assembly: InternalsVisibleTo("UIFramework.Tests.PlayMode")]
