﻿namespace Shapeshifter.UserInterface.WindowsDesktop.Data.Actions
{
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Interfaces;

    using Interfaces;

    class PinClipboardDataAction: IAction
    {
        public string Title => "Pin to clipboard";

        public string Description => "Pastes clipboard contents as plain text.";

        public byte Order => 100;

        public async Task<bool> CanPerformAsync(IClipboardDataPackage package)
        {
            return package.Contents.Any(x => x.RawData != null);
        }

        public async Task PerformAsync(IClipboardDataPackage package)
        {
            foreach (var item in package.Contents
                                        .Where(x => x.RawData != null)) { }
        }
    }
}