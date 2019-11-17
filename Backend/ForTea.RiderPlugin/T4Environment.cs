using System;
using System.Collections.Generic;
using System.Linq;
using GammaJul.ForTea.Core;
using JetBrains.Application;
using JetBrains.Application.platforms;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.Util;
using JetBrains.Util.Dotnet.TargetFrameworkIds;
using Microsoft.VisualStudio.TextTemplating;

namespace JetBrains.ForTea.RiderPlugin
{
	// TODO: use more accurate values
	[ShellComponent]
	public sealed class T4Environment : IT4Environment
	{
		public TargetFrameworkId TargetFrameworkId { get; } =
			TargetFrameworkId.Create(FrameworkIdentifier.NetFramework, new Version(4, 7, 2));

		public CSharpLanguageLevel CSharpLanguageLevel => CSharpLanguageLevel.Latest;

		public IEnumerable<string> TextTemplatingAssemblyNames
		{
			get
			{
				yield return typeof(ITextTemplatingEngineHost).Assembly.Location;
			}
		}

		public bool IsSupported => true;
		public IEnumerable<FileSystemPath> IncludePaths => Enumerable.Empty<FileSystemPath>();
	}
}
