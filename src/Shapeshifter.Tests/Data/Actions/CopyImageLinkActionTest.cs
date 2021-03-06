﻿namespace Shapeshifter.WindowsDesktop.Data.Actions
{
	using System;
	using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;

    using Autofac;

    using Data.Interfaces;

	using FluffySpoon.Http;

	using Interfaces;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NSubstitute;

    using Services.Clipboard.Interfaces;
    using Services.Images.Interfaces;
    using Services.Web;
    using Services.Web.Interfaces;

    [TestClass]
    public class CopyImageLinkActionTest: ActionTestBase<ICopyImageLinkAction>
    {
        [TestMethod]
        public async Task CanPerformIsFalseForNonTextTypes()
        {
            var someNonTextData = CreateClipboardDataPackageContaining<IClipboardData>();

            Assert.IsFalse(await SystemUnderTest.CanPerformAsync(someNonTextData));
        }

        [TestMethod]
        public void OrderIsCorrect()
        {
            Assert.AreEqual(100, SystemUnderTest.Order);
        }

        [TestMethod]
        public async Task CanPerformIsFalseForTextTypesWithNoImageLink()
        {
            Container.Resolve<ILinkParser>()
                .HasLinkOfTypeAsync(
                    Arg.Any<string>(),
                    LinkType.ImageFile)
                .Returns(Task.FromResult(false));
            
            var canPerform = await SystemUnderTest.CanPerformAsync(Substitute.For<IClipboardDataPackage>());
            Assert.IsFalse(canPerform);
        }

		[TestMethod]
		public async Task CanPerformIsTrueForTextTypesWithImageLink()
		{
			Container.Resolve<ILinkParser>()
				.HasLinkOfTypeAsync(
					Arg.Any<string>(),
					LinkType.ImageFile)
				.Returns(Task.FromResult(true));

			Assert.IsTrue(await SystemUnderTest.CanPerformAsync(CreateClipboardDataPackageContaining<IClipboardTextData>()));
		}

		[TestMethod]
		public async Task PerformTriggersImageDownload()
		{
			var firstFakeDownloadedImageBytes = new byte[]
			{
				1
			};
			var secondFakeDownloadedImageBytes = new byte[]
			{
				2
			};

			Container.Resolve<IDownloader>()
			 .DownloadBytesAsync(Arg.Any<Uri>())
			 .Returns(
				 Task.FromResult(
					 firstFakeDownloadedImageBytes),
				 Task.FromResult(
					 secondFakeDownloadedImageBytes));

			FakeHasImageLinks(
				new[]
				{
					"http://foobar.com",
					"http://example.com"
				});

			await SystemUnderTest.PerformAsync(CreateClipboardDataPackageContaining<IClipboardTextData>());

			var fakeClipboardInjectionService = Container.Resolve<IClipboardInjectionService>();
			fakeClipboardInjectionService.Received(2)
										 .InjectImageAsync(Arg.Any<BitmapSource>())
										 .IgnoreAwait();

			var fakeImageFileInterpreter = Container.Resolve<IImageFileInterpreter>();
			fakeImageFileInterpreter.Received(1)
									.Interpret(firstFakeDownloadedImageBytes);
			fakeImageFileInterpreter.Received(1)
									.Interpret(secondFakeDownloadedImageBytes);

			var fakeDownloader = Container.Resolve<IDownloader>();
			fakeDownloader.Received(1)
						  .DownloadBytesAsync(new Uri("http://foobar.com"))
						  .IgnoreAwait();
			fakeDownloader.Received(1)
						  .DownloadBytesAsync(new Uri("http://example.com"))
						  .IgnoreAwait();
		}

		void FakeHasImageLinks(string[] linkUrls)
        {
            Container.Resolve<ILinkParser>()
                     .HasLinkOfTypeAsync(
                         Arg.Any<string>(),
                         LinkType.ImageFile)
                     .Returns(Task.FromResult(true));

            Container.Resolve<ILinkParser>()
                     .ExtractLinksFromTextAsync(Arg.Any<string>())
                     .Returns(
                         Task
                             .FromResult
                             <IReadOnlyCollection<string>>(
                                 linkUrls));
        }
    }
}