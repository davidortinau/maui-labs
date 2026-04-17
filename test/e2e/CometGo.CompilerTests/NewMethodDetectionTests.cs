// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Maui.Go.Server;
using Xunit;

namespace CometGo.CompilerTests;

/// <summary>
/// Verifies that adding a new method to an existing class produces a
/// non-null metadata delta (not "no changes").
/// </summary>
public class NewMethodDetectionTests : CompilerTestBase
{
	[Fact]
	public void CompileDelta_AddNewMethod_ProducesDelta()
	{
		var path = WriteSource("App.cs", """
			public class App
			{
			    public string Body() => "Hello";
			}
			""");

		var compiler = CreateCompiler(path);
		var initial = compiler.CompileInitial();
		Assert.True(initial.Success, $"Initial failed: {string.Join("; ", initial.Errors)}");

		// Add a new helper method
		File.WriteAllText(path, """
			public class App
			{
			    public string Body() => "Hello";
			    public string CounterCard() => "Counter: 0";
			}
			""");

		var delta = compiler.CompileDelta();

		Assert.True(delta.Success, $"Delta failed: {string.Join("; ", delta.Errors)}");
		Assert.NotNull(delta.MetadataDelta);
	}

	[Fact]
	public void CompileDelta_AddNewMethod_NotNoChanges()
	{
		var path = WriteSource("App2.cs", """
			public class App2
			{
			    public int Compute() => 1 + 1;
			}
			""");

		var compiler = CreateCompiler(path);
		var initial = compiler.CompileInitial();
		Assert.True(initial.Success, $"Initial failed: {string.Join("; ", initial.Errors)}");

		// Add a completely new method
		File.WriteAllText(path, """
			public class App2
			{
			    public int Compute() => 1 + 1;
			    public int ComputeMore() => 2 + 2;
			}
			""");

		var delta = compiler.CompileDelta();

		Assert.True(delta.Success, $"Delta failed: {string.Join("; ", delta.Errors)}");
		// MetadataDelta must NOT be null — a new method is a real change
		Assert.NotNull(delta.MetadataDelta);
	}

	[Fact]
	public void CompileDelta_ModifyMethodBody_ProducesDelta()
	{
		var path = WriteSource("App3.cs", """
			public class App3
			{
			    public string Greet() => "Hello";
			}
			""");

		var compiler = CreateCompiler(path);
		var initial = compiler.CompileInitial();
		Assert.True(initial.Success, $"Initial failed: {string.Join("; ", initial.Errors)}");

		// Modify the method body
		File.WriteAllText(path, """
			public class App3
			{
			    public string Greet() => "Hello, World!";
			}
			""");

		var delta = compiler.CompileDelta();

		Assert.True(delta.Success, $"Delta failed: {string.Join("; ", delta.Errors)}");
		Assert.NotNull(delta.ILDelta);
	}
}
