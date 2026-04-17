// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Maui.Go.Server;

namespace CometGo.CompilerTests;

/// <summary>
/// Base class for DeltaCompiler tests. Creates a temp directory per test
/// and cleans up after disposal.
/// </summary>
public abstract class CompilerTestBase : IDisposable
{
	protected readonly string TempDir;

	protected CompilerTestBase()
	{
		TempDir = Path.Combine(Path.GetTempPath(), "CometGoTests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(TempDir);
	}

	/// <summary>
	/// Writes a .cs file to the temp directory and returns its full path.
	/// </summary>
	protected string WriteSource(string fileName, string content)
	{
		var path = Path.Combine(TempDir, fileName);
		File.WriteAllText(path, content);
		return path;
	}

	/// <summary>
	/// Creates a DeltaCompiler in single-file mode for the given source file path.
	/// </summary>
	protected static DeltaCompiler CreateCompiler(string csFilePath)
		=> DeltaCompiler.ForSingleFile(csFilePath);

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(TempDir))
				Directory.Delete(TempDir, recursive: true);
		}
		catch
		{
			// Best-effort cleanup
		}
		GC.SuppressFinalize(this);
	}
}
