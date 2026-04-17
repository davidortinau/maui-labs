// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Maui.Go.Server;
using Xunit;

namespace CometGo.CompilerTests;

/// <summary>
/// Verifies that the DeltaCompiler's implicit usings (System, System.Linq,
/// System.Collections.Generic) allow user code to compile without explicit
/// using directives.
/// </summary>
public class ImplicitUsingsTests : CompilerTestBase
{
	[Fact]
	public void CompileInitial_WithMathAbs_NoUsingSystem_Succeeds()
	{
		// Math.Abs lives in System — the implicit "global using System;" should cover it.
		var path = WriteSource("MathTest.cs", """
			public class MathTest
			{
			    public int Run() => Math.Abs(-42);
			}
			""");

		var compiler = CreateCompiler(path);
		var result = compiler.CompileInitial();

		Assert.True(result.Success, $"Expected success but got errors: {string.Join("; ", result.Errors)}");
	}

	[Fact]
	public void CompileInitial_WithLinq_NoUsingLinq_Succeeds()
	{
		// .Where() lives in System.Linq — the implicit "global using System.Linq;" should cover it.
		var path = WriteSource("LinqTest.cs", """
			public class LinqTest
			{
			    public int Run()
			    {
			        var nums = new List<int> { 1, 2, 3, 4, 5 };
			        return nums.Where(n => n > 2).Count();
			    }
			}
			""");

		var compiler = CreateCompiler(path);
		var result = compiler.CompileInitial();

		Assert.True(result.Success, $"Expected success but got errors: {string.Join("; ", result.Errors)}");
	}

	[Fact]
	public void CompileInitial_WithGenericCollections_NoUsing_Succeeds()
	{
		// Dictionary<,> lives in System.Collections.Generic — implicit using should cover it.
		var path = WriteSource("CollectionsTest.cs", """
			public class CollectionsTest
			{
			    public Dictionary<string, List<int>> Run()
			    {
			        return new Dictionary<string, List<int>>
			        {
			            ["a"] = new List<int> { 1, 2, 3 }
			        };
			    }
			}
			""");

		var compiler = CreateCompiler(path);
		var result = compiler.CompileInitial();

		Assert.True(result.Success, $"Expected success but got errors: {string.Join("; ", result.Errors)}");
	}
}
