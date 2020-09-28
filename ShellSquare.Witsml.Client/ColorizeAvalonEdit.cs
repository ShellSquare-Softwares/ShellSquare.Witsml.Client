using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Windows;
using System.Windows.Media;


namespace ShellSquare.Witsml.Client
{
    public class ColorizeAvalonEdit : DocumentColorizingTransformer
    {
        private readonly string _selectedText;

        public ColorizeAvalonEdit(string selectedText)
        {
            _selectedText = selectedText;
        }
        int start = 0;
        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);

            int index;

            if (_selectedText.Trim().Length != 0)
            {

                while ((index = text.IndexOf(_selectedText, start, StringComparison.Ordinal)) >= 0)
                {
                    base.ChangeLinePart(
                        lineStartOffset + index, // startOffset
                        lineStartOffset + index + 5, // endOffset
                        (VisualLineElement element) =>
                        {
                            // This lambda gets called once for every VisualLineElement
                            // between the specified offsets.
                            Typeface tf = element.TextRunProperties.Typeface;
                            // Replace the typeface with a modified version of
                            // the same typeface
                            element.TextRunProperties.SetTypeface(new Typeface(
                                    tf.FontFamily,
                                    FontStyles.Italic,
                                    FontWeights.Bold,
                                    tf.Stretch
                                ));
                        });
                    start = index + 1; // search for next occurrence
                }
            }
        }
    }
}
