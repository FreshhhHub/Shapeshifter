﻿namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Data.Factories.Interfaces;
    using Data.Interfaces;

    using Files.Interfaces;

    using Interfaces;

    using Structures;

    class ClipboardPersistanceService: IClipboardPersistanceService
    {
        readonly IFileManager fileManager;
        readonly IClipboardDataPackageFactory clipboardDataPackageFactory;
		readonly IClipboardFormatFactory clipboardFormatFactory;

		public ClipboardPersistanceService(
            IFileManager fileManager,
            IClipboardDataPackageFactory clipboardDataPackageFactory,
			IClipboardFormatFactory clipboardFormatFactory)
        {
            this.fileManager = fileManager;
            this.clipboardDataPackageFactory = clipboardDataPackageFactory;
			this.clipboardFormatFactory = clipboardFormatFactory;
		}

        public async Task PersistClipboardPackageAsync(IClipboardDataPackage package)
        {
            var packageFolder = PrepareUniquePackageFolder();
            for (var i = 0; i < package.Contents.Count; i++)
            {
                var content = package.Contents[i];
                var filePath = Path.Combine(
                    packageFolder,
                    i + 1 + "." + content.RawFormat);
                await fileManager.WriteBytesToFileAsync(
                    filePath,
                    content.RawData);
            }
        }

        string PrepareUniquePackageFolder()
        {
            var packageFolder = fileManager.PrepareNewIsolatedFolder(
                Path.Combine("Pinned"));
            return packageFolder;
        }

        public async Task<IEnumerable<IClipboardDataPackage>> GetPersistedPackagesAsync()
        {
            var packageList = new List<IClipboardDataPackage>();
            var packageFolder = fileManager.PrepareIsolatedFolder("Pinned");
            var packageDirectories = Directory.GetDirectories(packageFolder);
            foreach (var packageDirectory in packageDirectories)
            {
                packageList.Add(await GetPersistedPackageAsync(packageDirectory));
            }

            return packageList;
        }

        async Task<IClipboardDataPackage> GetPersistedPackageAsync(string directory)
        {
            var packageFiles = Directory
                .GetFiles(directory)
                .OrderBy(x => x);

            var dataPairs = new List<FormatDataPair>();
            foreach (var file in packageFiles)
            {
                var fileExtension = Path.GetExtension(file);
                Debug.Assert(fileExtension != null, "fileExtension != null");

                var fileExtensionWithoutDot = fileExtension.Substring(1);

                var format = uint.Parse(fileExtensionWithoutDot);
                var data = File.ReadAllBytes(file);

                dataPairs.Add(new FormatDataPair(
					clipboardFormatFactory.Create(format), 
					data));
            }

            return clipboardDataPackageFactory.CreateFromFormatsAndData(dataPairs.ToArray());
        }

        public Task DeletePackageAsync(IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }

        public Task IsPinnedAsync(IClipboardDataPackage package)
        {
            throw new NotImplementedException();
        }
    }
}