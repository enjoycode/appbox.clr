using System;
using System.Collections.Generic;
using System.Globalization;

namespace appbox.Drawing
{
    /// <summary>
    /// Translates colors to and from GDI+ <see cref='Color'/> objects.
    /// </summary>
    public static class ColorTranslator
    {
        // COLORREF is 0x00BBGGRR
        //internal const int COLORREF_RedShift = 0;
        //internal const int COLORREF_GreenShift = 8;
        //internal const int COLORREF_BlueShift = 16;

        //private const int OleSystemColorFlag = unchecked((int)0x80000000);

        private static Dictionary<string, Color> s_htmlSysColorTable;

        //internal static uint COLORREFToARGB(uint value)
        //    => ((value >> COLORREF_RedShift) & 0xFF) << Color.ARGBRedShift
        //        | ((value >> COLORREF_GreenShift) & 0xFF) << Color.ARGBGreenShift
        //        | ((value >> COLORREF_BlueShift) & 0xFF) << Color.ARGBBlueShift
        //        | Color.ARGBAlphaMask; // COLORREF's are always fully opaque

        /// <summary>
        /// Translates an Html color representation to a GDI+ <see cref='Color'/>.
        /// </summary>
        public static Color FromHtml(string htmlColor)
        {
            Color c = Color.Empty;

            // empty color
            if ((htmlColor == null) || (htmlColor.Length == 0))
                return c;

            // #RRGGBB or #RGB
            if ((htmlColor[0] == '#') &&
                ((htmlColor.Length == 7) || (htmlColor.Length == 4)))
            {
                if (htmlColor.Length == 7)
                {
                    c = Color.FromArgb(Convert.ToInt32(htmlColor.Substring(1, 2), 16),
                                       Convert.ToInt32(htmlColor.Substring(3, 2), 16),
                                       Convert.ToInt32(htmlColor.Substring(5, 2), 16));
                }
                else
                {
                    string r = char.ToString(htmlColor[1]);
                    string g = char.ToString(htmlColor[2]);
                    string b = char.ToString(htmlColor[3]);

                    c = Color.FromArgb(Convert.ToInt32(r + r, 16),
                                       Convert.ToInt32(g + g, 16),
                                       Convert.ToInt32(b + b, 16));
                }
            }

            // special case. Html requires LightGrey, but .NET uses LightGray
            if (c.IsEmpty && string.Equals(htmlColor, "LightGrey", StringComparison.OrdinalIgnoreCase))
            {
                c = Color.LightGray;
            }

            // System color
            if (c.IsEmpty)
            {
                if (s_htmlSysColorTable == null)
                {
                    InitializeHtmlSysColorTable();
                }

                s_htmlSysColorTable!.TryGetValue(htmlColor.ToLowerInvariant(), out c);
            }

            // resort to type converter which will handle named colors
            if (c.IsEmpty)
            {
                try
                {
                    c = ColorConverterCommon.ConvertFromString(htmlColor, CultureInfo.CurrentCulture);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(ex.Message, nameof(htmlColor), ex);
                }
            }

            return c;
        }

        /// <summary>
        /// Translates the specified <see cref='Color'/> to an Html string color representation.
        /// </summary>
        public static string ToHtml(Color c)
        {
            string colorString = string.Empty;

            if (c.IsEmpty)
                return colorString;

            if (c.IsSystemColor)
            {
                switch (c.ToKnownColor())
                {
                    case KnownColor.ActiveBorder:
                        colorString = "activeborder";
                        break;
                    case KnownColor.GradientActiveCaption:
                    case KnownColor.ActiveCaption:
                        colorString = "activecaption";
                        break;
                    case KnownColor.AppWorkspace:
                        colorString = "appworkspace";
                        break;
                    case KnownColor.Desktop:
                        colorString = "background";
                        break;
                    case KnownColor.Control:
                    case KnownColor.ControlLight:
                        colorString = "buttonface";
                        break;
                    case KnownColor.ControlDark:
                        colorString = "buttonshadow";
                        break;
                    case KnownColor.ControlText:
                        colorString = "buttontext";
                        break;
                    case KnownColor.ActiveCaptionText:
                        colorString = "captiontext";
                        break;
                    case KnownColor.GrayText:
                        colorString = "graytext";
                        break;
                    case KnownColor.HotTrack:
                    case KnownColor.Highlight:
                        colorString = "highlight";
                        break;
                    case KnownColor.MenuHighlight:
                    case KnownColor.HighlightText:
                        colorString = "highlighttext";
                        break;
                    case KnownColor.InactiveBorder:
                        colorString = "inactiveborder";
                        break;
                    case KnownColor.GradientInactiveCaption:
                    case KnownColor.InactiveCaption:
                        colorString = "inactivecaption";
                        break;
                    case KnownColor.InactiveCaptionText:
                        colorString = "inactivecaptiontext";
                        break;
                    case KnownColor.Info:
                        colorString = "infobackground";
                        break;
                    case KnownColor.InfoText:
                        colorString = "infotext";
                        break;
                    case KnownColor.MenuBar:
                    case KnownColor.Menu:
                        colorString = "menu";
                        break;
                    case KnownColor.MenuText:
                        colorString = "menutext";
                        break;
                    case KnownColor.ScrollBar:
                        colorString = "scrollbar";
                        break;
                    case KnownColor.ControlDarkDark:
                        colorString = "threeddarkshadow";
                        break;
                    case KnownColor.ControlLightLight:
                        colorString = "buttonhighlight";
                        break;
                    case KnownColor.Window:
                        colorString = "window";
                        break;
                    case KnownColor.WindowFrame:
                        colorString = "windowframe";
                        break;
                    case KnownColor.WindowText:
                        colorString = "windowtext";
                        break;
                }
            }
            else if (c.IsNamedColor)
            {
                if (c == Color.LightGray)
                {
                    // special case due to mismatch between Html and enum spelling
                    colorString = "LightGrey";
                }
                else
                {
                    colorString = c.Name;
                }
            }
            else
            {
                colorString = "#" + c.R.ToString("X2", null) +
                                    c.G.ToString("X2", null) +
                                    c.B.ToString("X2", null);
            }

            return colorString;
        }

        private static void InitializeHtmlSysColorTable()
        {
            s_htmlSysColorTable = new Dictionary<string, Color>(27)
            {
                ["activeborder"] = Color.FromKnownColor(KnownColor.ActiveBorder),
                ["activecaption"] = Color.FromKnownColor(KnownColor.ActiveCaption),
                ["appworkspace"] = Color.FromKnownColor(KnownColor.AppWorkspace),
                ["background"] = Color.FromKnownColor(KnownColor.Desktop),
                ["buttonface"] = Color.FromKnownColor(KnownColor.Control),
                ["buttonhighlight"] = Color.FromKnownColor(KnownColor.ControlLightLight),
                ["buttonshadow"] = Color.FromKnownColor(KnownColor.ControlDark),
                ["buttontext"] = Color.FromKnownColor(KnownColor.ControlText),
                ["captiontext"] = Color.FromKnownColor(KnownColor.ActiveCaptionText),
                ["graytext"] = Color.FromKnownColor(KnownColor.GrayText),
                ["highlight"] = Color.FromKnownColor(KnownColor.Highlight),
                ["highlighttext"] = Color.FromKnownColor(KnownColor.HighlightText),
                ["inactiveborder"] = Color.FromKnownColor(KnownColor.InactiveBorder),
                ["inactivecaption"] = Color.FromKnownColor(KnownColor.InactiveCaption),
                ["inactivecaptiontext"] = Color.FromKnownColor(KnownColor.InactiveCaptionText),
                ["infobackground"] = Color.FromKnownColor(KnownColor.Info),
                ["infotext"] = Color.FromKnownColor(KnownColor.InfoText),
                ["menu"] = Color.FromKnownColor(KnownColor.Menu),
                ["menutext"] = Color.FromKnownColor(KnownColor.MenuText),
                ["scrollbar"] = Color.FromKnownColor(KnownColor.ScrollBar),
                ["threeddarkshadow"] = Color.FromKnownColor(KnownColor.ControlDarkDark),
                ["threedface"] = Color.FromKnownColor(KnownColor.Control),
                ["threedhighlight"] = Color.FromKnownColor(KnownColor.ControlLight),
                ["threedlightshadow"] = Color.FromKnownColor(KnownColor.ControlLightLight),
                ["window"] = Color.FromKnownColor(KnownColor.Window),
                ["windowframe"] = Color.FromKnownColor(KnownColor.WindowFrame),
                ["windowtext"] = Color.FromKnownColor(KnownColor.WindowText)
            };
        }
    }
}
