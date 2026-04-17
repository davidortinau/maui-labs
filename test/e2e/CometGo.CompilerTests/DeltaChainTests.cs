// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Maui.Go.Server;
using Xunit;

namespace CometGo.CompilerTests;

/// <summary>
/// Verifies that long delta chains (many consecutive edits) all succeed.
/// </summary>
public class DeltaChainTests : CompilerTestBase
{
	[Fact]
	public void CompileDelta_TenConsecutiveDeltas_AllSucceed()
	{
		var path = WriteSource("Chain.cs", """
			public class Chain
			{
			    public string Body() => "v0";
			}
			""");

		var compiler = CreateCompiler(path);
		var initial = compiler.CompileInitial();
		Assert.True(initial.Success, $"Initial failed: {string.Join("; ", initial.Errors)}");

		for (int i = 1; i <= 10; i++)
		{
			File.WriteAllText(path, $$"""
				public class Chain
				{
				    public string Body() => "v{{i}}";
				}
				""");

			var delta = compiler.CompileDelta();
			Assert.True(delta.Success, $"Delta {i} failed: {string.Join("; ", delta.Errors)}");
			Assert.NotNull(delta.MetadataDelta);
		}
	}

	[Fact]
	public void CompileDelta_AlternatingBodyChanges_AllSucceed()
	{
		var path = WriteSource("Alternate.cs", """
			public class Alternate
			{
			    public string Greet() => "Hello";
			}
			""");

		var compiler = CreateCompiler(path);
		var initial = compiler.CompileInitial();
		Assert.True(initial.Success, $"Initial failed: {string.Join("; ", initial.Errors)}");

		var messages = new[] { "Hi", "Hello", "Hey", "Howdy", "Greetings" };

		foreach (var msg in messages)
		{
			File.WriteAllText(path, $$"""
				public class Alternate
				{
				    public string Greet() => "{{msg}}";
				}
				""");

			var delta = compiler.CompileDelta();
			Assert.True(delta.Success, $"Delta '{msg}' failed: {string.Join("; ", delta.Errors)}");
			Assert.NotNull(delta.ILDelta);
		}
	}
}
