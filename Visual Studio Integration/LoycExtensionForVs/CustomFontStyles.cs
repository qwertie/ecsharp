using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Loyc.VisualStudio
{

    namespace ItalicComments
    {
        [Export(typeof(IWpfTextViewCreationListener))]
        [ContentType("code")]
        [TextViewRole(PredefinedTextViewRoles.Document)]
        internal sealed class ViewCreationListener : IWpfTextViewCreationListener
        {
            [Import]
            IClassificationFormatMapService formatMapService = null;

            [Import]
            IClassificationTypeRegistryService typeRegistry = null;

            /// <summary>
            /// When a text view is created, make all comments italicized.
            /// </summary>
            public void TextViewCreated(IWpfTextView textView)
            {
                textView.Properties.GetOrCreateSingletonProperty(() =>
                         new FormatMapWatcher(textView, formatMapService.GetClassificationFormatMap(textView), typeRegistry));
            }
        }

        internal sealed class FormatMapWatcher
        {
            bool _inUpdate = false;
            IClassificationFormatMap _formatMap;
            IClassificationTypeRegistryService _typeRegistry;
            IClassificationType _text;

            static List<string> CommentTypes = new List<string>() { "comment", "xml doc comment", "vb xml doc comment" };
            static List<string> DocTagTypes = new List<string>() { "xml doc tag", "vb xml doc tag" };

            public FormatMapWatcher(ITextView view, IClassificationFormatMap formatMap, IClassificationTypeRegistryService typeRegistry)
            {
                this._formatMap = formatMap;
                this._text = typeRegistry.GetClassificationType("text");
                this._typeRegistry = typeRegistry;
                this.FixComments();

                this._formatMap.ClassificationFormatMappingChanged += FormatMapChanged;

                view.GotAggregateFocus += FirstGotFocus;
            }

            void FirstGotFocus(object sender, EventArgs e)
            {
                ((ITextView)sender).GotAggregateFocus -= FirstGotFocus;

                Debug.Assert(!_inUpdate, "How can we be updating *while* the view is getting focus?");

                this.FixComments();
            }

            void FormatMapChanged(object sender, System.EventArgs e)
            {
                if (!_inUpdate)
                    this.FixComments();
            }

            internal void FixComments()
            {
                try {
                    _inUpdate = true;

                    // First, go through the ones we know about:

                    // 1) Known comment types are italicized
                    foreach (var type in CommentTypes.Select(t => _typeRegistry.GetClassificationType(t))
                                                     .Where(t => t != null)) {
                        Italicize(type);
                    }

                    // 2) Known doc tags
                    foreach (var type in DocTagTypes.Select(t => _typeRegistry.GetClassificationType(t))
                                                    .Where(t => t != null)) {
                        Fade(type);
                    }

                    // 3) Grab everything else that looks like a comment or doc tag
                    foreach (var classification in _formatMap.CurrentPriorityOrder.Where(c => c != null)) {
                        string name = classification.Classification.ToLowerInvariant();
                        if (CommentTypes.Contains(name) || DocTagTypes.Contains(name))
                            continue;

                        if (name.Contains("comment")) {
                            Italicize(classification);
                        }
                        else if (name.Contains("doc tag")) {
                            Fade(classification);
                        }
                    }
                }
                finally {
                    _inUpdate = false;
                }
            }

            void Italicize(IClassificationType classification)
            {
                var textFormat = _formatMap.GetTextProperties(_text);
                var properties = _formatMap.GetTextProperties(classification);
                var typeface = properties.Typeface;

                // If this is already italic, skip it
                if (typeface.Style == FontStyles.Italic)
                    return;

                // Add italic and (possibly) remove bold, and change to a font that has good looking
                // italics (i.e. *not* Consolas)
                var newTypeface = new Typeface(new FontFamily("Lucida Sans"), FontStyles.Italic, FontWeights.Normal, typeface.Stretch);
                properties = properties.SetTypeface(newTypeface);

                // Also, make the font size a little bit smaller than the normal text size
                properties = properties.SetFontRenderingEmSize(textFormat.FontRenderingEmSize - 1);

                // And put it back in the format map
                _formatMap.SetTextProperties(classification, properties);
            }

            void Fade(IClassificationType classification)
            {
                var textFormat = _formatMap.GetTextProperties(_text);
                var properties = _formatMap.GetTextProperties(classification);

                var brush = properties.ForegroundBrush as SolidColorBrush;

                // If the opacity is already not 1.0, skip this
                if (brush == null || brush.Opacity != 1.0)
                    return;

                // Make the font size a little bit smaller than the normal text size
                properties = properties.SetFontRenderingEmSize(textFormat.FontRenderingEmSize - 1);

                // Set the opacity to be a bit lighter
                properties = properties.SetForegroundBrush(new SolidColorBrush(brush.Color) { Opacity = 0.7 });

                _formatMap.SetTextProperties(classification, properties);
            }
        }
    }
}
