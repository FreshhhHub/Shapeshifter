﻿namespace Shapeshifter.WindowsDesktop.Infrastructure.Events
{
    using System;
    using System.Windows.Input;

    public class KeyDetectedArgument: EventArgs
    {
        public KeyDetectedArgument(
            Key key, 
            KeyStates keyState,
            bool isControlDown)
        {
            Key = key;
            KeyState = keyState;
            IsControlDown = isControlDown;
        }

        public bool Cancel { get; set; }

        public KeyStates KeyState { get; }

        public bool IsControlDown { get; }

        public Key Key { get; }
    }
}