using System;

namespace Hercules.Shell
{
    public class BrowserPage : Page
    {
        public BrowserPage(string title, string contentId, Uri source)
        {
            this.Title = title;
            this.ContentId = contentId;
            this.Source = source;
        }

        public Uri Source { get; }
    }
}
