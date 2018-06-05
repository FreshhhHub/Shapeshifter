﻿namespace Shapeshifter.WindowsDesktop.Services.Clipboard
{
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using System.Windows.Input;

	using Controls.Window.Interfaces;

	using Interfaces;

	using Keyboard;
	using Keyboard.Interfaces;

	using KeyboardHookInterception;

	using Mediators.Interfaces;

	using Messages.Interceptors.Hotkeys.Interfaces;
	using Serilog;

	class ClipboardPasteService: IClipboardPasteService
    {
        readonly IPasteHotkeyInterceptor pasteHotkeyInterceptor;
        readonly ILogger logger;
        readonly IMainWindowHandleContainer handleContainer;
		readonly IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator;
		readonly IKeyboardManager keyboardManager;

        public ClipboardPasteService(
            IPasteHotkeyInterceptor pasteHotkeyInterceptor,
            ILogger logger,
            IMainWindowHandleContainer handleContainer,
			IClipboardUserInterfaceInteractionMediator clipboardUserInterfaceInteractionMediator,
            IKeyboardManager keyboardManager)
        {
            this.pasteHotkeyInterceptor = pasteHotkeyInterceptor;
            this.logger = logger;
            this.handleContainer = handleContainer;
			this.clipboardUserInterfaceInteractionMediator = clipboardUserInterfaceInteractionMediator;
			this.keyboardManager = keyboardManager;
        }

        public async Task PasteClipboardContentsAsync()
        {
            var isVDown = keyboardManager.IsKeyDown(Key.V);
            var isCtrlDown = keyboardManager.IsKeyDown(Key.LeftCtrl);

			await SimulatePaste(isCtrlDown, isVDown);

            logger.Information("Paste simulated.", 1);
        }

        async Task SimulatePaste(bool isCtrlDown, bool isVDown)
        {
            logger.Information(
                $"Simulating paste with CTRL {(isCtrlDown ? "down" : "released")} and V {(isVDown ? "down" : "released")}.");

            DisablePasteHotkeyInterceptor();
            UninstallPasteHotkeyInterceptor();

			clipboardUserInterfaceInteractionMediator.Disconnect();

			await RunFirstKeyboardPhase(isCtrlDown, isVDown);

            InstallPasteHotkeyInterceptor();

            await RunSecondKeyboardPhaseAsync(isCtrlDown, isVDown);

            EnablePasteHotkeyInterceptor();

			clipboardUserInterfaceInteractionMediator.Connect();

			logger.Verbose("Paste hotkey interceptor has been re-enabled.");
		}

		void EnablePasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.IsEnabled = true;
            logger.Information("Enabled paste hotkey interceptor.");
        }

        void DisablePasteHotkeyInterceptor()
        {
			pasteHotkeyInterceptor.IsEnabled = false;
            logger.Information("Disabled paste hotkey interceptor.");
        }

        async Task RunFirstKeyboardPhase(bool isCtrlDown, bool isVDown)
        {
            var firstPhaseOperations = GetFirstPhaseKeyOperations(isCtrlDown, isVDown);
            await keyboardManager.SendKeysAsync(firstPhaseOperations.ToArray());
        }

        async Task RunSecondKeyboardPhaseAsync(bool isCtrlDown, bool isVDown)
        {
            if (!isCtrlDown && !isVDown)
                return;

            var secondPhaseOperations = GetSecondPhaseKeyOperations(isCtrlDown, isVDown);
            await keyboardManager.SendKeysAsync(secondPhaseOperations.ToArray());
        }

        static List<KeyOperation> GetSecondPhaseKeyOperations(bool isCtrlDown, bool isVDown)
        {
            var secondPhaseOperations = new List<KeyOperation>();
            if (isCtrlDown)
                secondPhaseOperations.Add(new KeyOperation(Key.LeftCtrl, KeyDirection.Down));

            if (isVDown)
                secondPhaseOperations.Add(new KeyOperation(Key.V, KeyDirection.Down));

            return secondPhaseOperations;
        }

        static List<KeyOperation> GetFirstPhaseKeyOperations(bool isCtrlDown, bool isVDown)
        {
            var operations = new List<KeyOperation>();

            if (!isCtrlDown)
                operations.Add(new KeyOperation(Key.LeftCtrl, KeyDirection.Down));

            if (!isVDown)
                operations.Add(new KeyOperation(Key.V, KeyDirection.Down));

            operations.Add(new KeyOperation(Key.V, KeyDirection.Up));
            operations.Add(new KeyOperation(Key.LeftCtrl, KeyDirection.Up));

            return operations;
        }

        void InstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.Install(handleContainer.Handle);
            HookHostCommunicator.SetShouldIgnoreHook(false);

            logger.Information("Installed paste hotkey interceptor.");
        }

        void UninstallPasteHotkeyInterceptor()
        {
            pasteHotkeyInterceptor.Uninstall();
            HookHostCommunicator.SetShouldIgnoreHook(true);

            logger.Information("Uninstalled paste hotkey interceptor.");
        }
    }
}