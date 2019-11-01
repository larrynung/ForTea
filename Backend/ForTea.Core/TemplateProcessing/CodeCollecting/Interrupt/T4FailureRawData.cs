using GammaJul.ForTea.Core.Parsing.Ranges;
using GammaJul.ForTea.Core.Tree;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Psi.Tree;

namespace GammaJul.ForTea.Core.TemplateProcessing.CodeCollecting.Interrupt
{
	public readonly struct T4FailureRawData
	{
		public int Line { get; }
		public int Column { get; }

		[NotNull]
		public IT4File File { get; }

		[NotNull]
		public string Message { get; }

		private T4FailureRawData(int line, int column, [NotNull] IT4File file, [NotNull] string message)
		{
			Line = line;
			Column = column;
			File = file;
			Message = message;
		}

		public static T4FailureRawData FromElement([NotNull] ITreeNode node, [NotNull] string message)
		{
			var file = node.GetContainingFile() as IT4File;
			Assertion.AssertNotNull(file, "file != null");
			file.GetSolution().Locks.AssertReadAccessAllowed();
			var offset = T4UnsafeManualRangeTranslationUtil.GetDocumentStartOffset(node);
			var coords = offset.Document.GetCoordsByOffset(offset.Offset);
			return new T4FailureRawData((int) coords.Line, (int) coords.Column, file, message);
		}
	}
}
