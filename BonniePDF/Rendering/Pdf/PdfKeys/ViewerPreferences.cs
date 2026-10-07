namespace BonniePDF.Rendering.Pdf;

internal static partial class PdfKeys
{
    /// <summary>Entries requesting viewer presentation preferences.</summary>
    internal static class ViewerPreferences
    {
        /// <summary>Whether the viewer should hide its toolbars while displaying the document.</summary>
        internal const string HideToolbar = "/HideToolbar";

        /// <summary>Whether the viewer should hide its menu bar while displaying the document.</summary>
        internal const string HideMenubar = "/HideMenubar";

        /// <summary>Whether the viewer should hide window controls such as scroll bars.</summary>
        internal const string HideWindowUi = "/HideWindowUI";

        /// <summary>Whether the viewer should resize the document window to fit the first displayed page.</summary>
        internal const string FitWindow = "/FitWindow";

        /// <summary>Whether the viewer should center the document window on the screen.</summary>
        internal const string CenterWindow = "/CenterWindow";

        /// <summary>Whether the window title should show the document title instead of the filename.</summary>
        internal const string DisplayDocumentTitle = "/DisplayDocTitle";
    }
}
