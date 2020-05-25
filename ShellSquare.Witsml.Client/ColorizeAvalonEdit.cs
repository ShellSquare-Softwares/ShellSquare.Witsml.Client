using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ShellSquare.Witsml.Client
{
    public class ColorizeAvalonEdit : DocumentColorizingTransformer
    {
        public string TextToHighlight { get; set; }

        public ColorizeAvalonEdit(string textToHighlight)
        {
            TextToHighlight = textToHighlight;
        }

        protected override void ColorizeLine(ICSharpCode.AvalonEdit.Document.DocumentLine line)
        {
            if (string.IsNullOrWhiteSpace(TextToHighlight))
            {
                return;
            }

            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            int start = 0;
            int index;
            while ((index = text.IndexOf(TextToHighlight, start, StringComparison.CurrentCultureIgnoreCase)) >= 0)
            {
                base.ChangeLinePart(
                    lineStartOffset + index, // startOffset
                    lineStartOffset + index + TextToHighlight.Length, // endOffset
                    ApplyChanges);
                start = index + 1; // search for next occurrence
            }
        }

        void ApplyChanges(VisualLineElement element)
        {
            // This is where you do anything with the line

            // element.TextRunProperties.SetForegroundBrush(Brushes.Red);
            element.TextRunProperties.SetBackgroundBrush(Brushes.LightGreen);

        }
    }

}
