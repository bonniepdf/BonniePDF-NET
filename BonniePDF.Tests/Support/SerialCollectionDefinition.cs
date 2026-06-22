using Xunit;

namespace BonniePDF.Tests;

/// <summary>
/// Marks a collection whose test classes run sequentially (not in parallel).
/// Used for classes that mutate process-wide static state:
/// <see cref="BonniePDF.Configuration.InternalConfiguration.EnableCompression"/> and
/// <see cref="BonniePDF.Typography.FontManager"/>.
/// </summary>
[CollectionDefinition("Serial", DisableParallelization = true)]
public sealed class SerialCollectionDefinition { }
