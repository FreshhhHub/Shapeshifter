﻿namespace Shapeshifter.WindowsDesktop.Services.Screen
{
	using System.Windows;

	public class ScreenBounds
	{
		public ScreenBounds(
			Vector position, 
			Vector size)
		{
			X = position.X;
			Y = position.Y;

			Width = size.X;
			Height = size.Y;
		}

		public double X { get; set; }
		public double Y { get; set; }

		public double Width { get; set; }
		public double Height { get; set; }
	}

	public class ScreenInformation
	{
		public ScreenBounds Bounds { get; private set; }
		public ScreenBounds WorkingArea { get; private set; }

		public ScreenInformation(
			ScreenBounds bounds,
			ScreenBounds workingArea)
		{
			Bounds = bounds;
			WorkingArea = workingArea;
		}
	}
}