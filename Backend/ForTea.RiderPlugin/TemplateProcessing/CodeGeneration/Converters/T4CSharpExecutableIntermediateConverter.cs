using GammaJul.ForTea.Core.Parsing.Ranges;
using GammaJul.ForTea.Core.Psi.Resolve.Macros;
using GammaJul.ForTea.Core.TemplateProcessing;
using GammaJul.ForTea.Core.TemplateProcessing.CodeCollecting;
using GammaJul.ForTea.Core.TemplateProcessing.CodeGeneration;
using GammaJul.ForTea.Core.TemplateProcessing.CodeGeneration.Converters;
using GammaJul.ForTea.Core.Tree;
using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ForTea.RiderPlugin.TemplateProcessing.CodeGeneration.Reference;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.Util.dataStructures.TypedIntrinsics;

namespace JetBrains.ForTea.RiderPlugin.TemplateProcessing.CodeGeneration.Converters
{
	public sealed class T4CSharpExecutableIntermediateConverter : T4CSharpIntermediateConverter
	{
		[NotNull] private const string SuffixResource =
			"GammaJul.ForTea.Core.Resources.TemplateBaseFullExecutableSuffix.cs";

		[NotNull] private const string HostspecificSuffixResource =
			"GammaJul.ForTea.Core.Resources.HostspecificTemplateBaseFullExecutableSuffix.cs";

		[NotNull] private const string HostResource = "GammaJul.ForTea.Core.Resources.Host.cs";

		[NotNull] private const string AssemblyRegisteringResource =
			"GammaJul.ForTea.Core.Resources.AssemblyRegistering.cs";

		protected override string GeneratedClassName => GeneratedClassNameString;
		protected override string GeneratedBaseClassName => GeneratedBaseClassNameString;

		[NotNull]
		private IT4ReferenceExtractionManager ReferenceExtractionManager { get; }

		public T4CSharpExecutableIntermediateConverter(
			[NotNull] T4CSharpCodeGenerationIntermediateResult intermediateResult,
			[NotNull] IT4File file,
			[NotNull] IT4ReferenceExtractionManager referenceExtractionManager
		) : base(intermediateResult, file)
		{
			file.AssertContainsNoIncludeContext();
			ReferenceExtractionManager = referenceExtractionManager;
		}

		protected override void AppendNamespacePrefix()
		{
			if (!IntermediateResult.HasHost) return;
			AppendHostDefinition();
		}

		// When creating executable, it is better to put base class first,
		// to make error messages more informative
		protected override void AppendClasses()
		{
			AppendBaseClass();
			AppendMainContainer();
			AppendClass();
		}

		protected override void AppendHost()
		{
			AppendIndent();
			Result.AppendLine(
				"public virtual Microsoft.VisualStudio.TextTemplating.ITextTemplatingEngineHost Host { get; set; } =");
			AppendIndent();
			Result.AppendLine("    new Microsoft.VisualStudio.TextTemplating.TextTemplatingEngineHost();");
		}

		protected override bool ShouldAppendPragmaDirectives => true;

		private void AppendHostDefinition()
		{
			var provider = new T4TemplateResourceProvider(HostResource);
			string filePath = File.GetSourceFile().GetLocation().FullPath;
			string macros = GenerateExpandableMacros();
			string host = provider.ProcessResource(filePath, GetGeneratedBaseClassFqn(), macros);
			Result.Append(host);
		}

		[NotNull]
		private string GetGeneratedBaseClassFqn()
		{
			string ns = GetNamespace();
			if (ns.IsNullOrWhitespace()) return GeneratedBaseClassName;
			return $"{ns}.{GeneratedBaseClassName}";
		}

		[NotNull]
		private string GenerateExpandableMacros()
		{
			var projectFile = File.GetSourceFile().ToProjectFile();
			if (projectFile == null) return "";
			var resolver = File.GetSolution().GetComponent<IT4MacroResolver>();
			var macros = resolver.ResolveAllLightMacros(projectFile);
			return macros.AggregateString(",\n", (builder, pair) => builder
				.Append("{\"")
				.Append(StringLiteralConverter.EscapeToRegular(pair.Key))
				.Append("\", \"")
				.Append(StringLiteralConverter.EscapeToRegular(pair.Value))
				.Append("\"}")
			);
		}

		private void AppendMainContainer()
		{
			string resource = IntermediateResult.HasHost ? HostspecificSuffixResource : SuffixResource;
			var provider = new T4TemplateResourceProvider(resource);
			string encoding = IntermediateResult.Encoding ?? T4EncodingsManager.GetEncoding(File);
			string suffix = provider.ProcessResource(GeneratedClassName, encoding);
			Result.Append(suffix);
			AppendAssemblyRegistering();
			// assembly registration code is part of main class,
			// so resources do not include closing brace
			Result.Append("}");
		}

		private void AppendAssemblyRegistering()
		{
			var provider = new T4TemplateResourceProvider(AssemblyRegisteringResource);
			string references = GetReferences();
			string registering = provider.ProcessResource(references);
			Result.Append(registering);
		}

		[NotNull]
		private string GetReferences() => ReferenceExtractionManager
			.ExtractReferenceLocationsTransitive(File)
			.AggregateString(",\n", (builder, it) => builder
				.Append("{\"")
				.Append(StringLiteralConverter.EscapeToRegular(it.FullName))
				.Append("\", \"")
				.Append(StringLiteralConverter.EscapeToRegular(it.Location.FullPath))
				.Append("\"}"));

		#region IT4ElementAppendFormatProvider
		public override string ToStringConversionPrefix
		{
			get
			{
				if (IntermediateResult.HasBaseClass) return "ToStringInstanceHelper.ToStringWithCulture(";
				return base.ToStringConversionPrefix;
			}
		}

		public override bool ShouldBreakExpressionWithLineDirective => true;

		public override void AppendMappedIfNeeded(T4CSharpCodeGenerationResult destination, IT4Code code) =>
			destination.Append(code.GetText());

		public override void AppendCompilationOffset(T4CSharpCodeGenerationResult destination, IT4TreeNode node)
		{
			int documentOffset = T4UnsafeManualRangeTranslationUtil.GetDocumentStartOffset(node).Offset;
			var lineOffset = node
				.FindLogicalPsiSourceFile()
				.Document
				.GetCoordsByOffset(documentOffset)
				.Column;
			for (var i = Int32<DocColumn>.O; i < lineOffset; i++)
			{
				destination.Append(" ");
			}
		}
		#endregion IT4ElementAppendFormatProvider
	}
}
