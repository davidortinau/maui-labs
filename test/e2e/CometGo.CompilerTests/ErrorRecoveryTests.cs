// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Maui.Go.Server;
using Xunit;

namespace CometGo.CompilerTests;

/// <summary>
/// Verifies that the delta chain recovers after compile errors are fixed.
/// </summary>
public class ErrorRecoveryTests : CompilerTestBase
{
	[Fact]
	public void CompileDelta_SyntaxError_ThenFix_RecoversDeltaChain()
	{
		// Step 0: valid code
		var path = WriteSource("Recovery.cs", """
			public class Recovery
			{
			    public string Body() => "OK";
			}
			""");

		var compiler = CreateCompiler(path);
		var initial = compiler.CompileInitial();
		Assert.True(initial.Success, $"Initial failed: {string.Join("; ", initial.Errors)}");

		// Step 1: introduce syntax error (missing closing brace)
		File.WriteAllText(path, """
			public class Recovery
			{
			    public string Body() => "Broken";
			
			""");

		var broken = compiler.CompileDelta();
		Assert.False(broken.Success, "Expected compilation to fail with syntax error");
		Assert.NotEmpty(broken.Errors);

		// Step 2: fix the error
		File.WriteAllText(path, """
			public class Recovery
			{
			    public string Body() => "Fixed!";
			}
			""");

		var fixed_ = compiler.CompileDelta();
		Assert.True(fixed_.Success, $"Recovery failed: {string.Join("; ", fixed_.Errors)}");
		Assert.NotNull(fixed_.MetadataDelta);
	}

	[Fact]
	public void CompileDelta_TypeError_ReportsError()
	{
		var path = WriteSource("TypeError.cs", """
			public class TypeError
			{
			    public int GetValue() => 42;
			}
			""");

		var compiler = CreateCompiler(path);
		var initial = compiler.CompileInitial();
		Assert.True(initial.Success, $"Initial failed: {string.Join("; ", initial.Errors)}");

		// Introduce a type error: returning string from int method
		File.WriteAllText(path, """
			public class TypeError
			{
			    public int GetValue() => "not an int";
			}
			""");

		var result = compiler.CompileDelta();
		Assert.False(result.Success, "Expected type error");
		Assert.NotEmpty(result.Errors);
	}

	[Fact]
	public void CompileDelta_UndefinedSymbol_ReportsStructuredDiagnostics()
	{
		var path = WriteSource("Undefined.cs", """
			public class Undefined
			{
			    public int GetValue() => 1;
			}
			""");

		var compiler = CreateCompiler(path);
		var initial = compiler.CompileInitial();
		Assert.True(initial.Success, $"Initial failed: {string.Join("; ", initial.Errors)}");

		// Reference an undefined symbol
		File.WriteAllText(path, """
			public class Undefined
			{
			    public int GetValue() => NonExistentClass.Method();
			}
			""");

		var result = compiler.CompileDelta();
		Assert.False(result.Success);
		Assert.NotEmpty(result.Diagnostics);
		// Diagnostics should have file path info
		Assert.Contains(result.Diagnostics, d => d.FilePath.Contains("Undefined.cs"));
	}
}
