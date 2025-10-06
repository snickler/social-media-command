using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAssertions;
using SocialMediaCommander.Tests.Helpers;
using Xunit;

namespace SocialMediaCommander.Tests.Helpers.Tests;

public class ScreenshotHelperTests
{
    [AvaloniaFact]
    public async Task Save_ShouldCreateNonEmptyPngFile()
    {
        var control = new Border
        {
            Width = 120,
            Height = 80,
            Background = Brushes.Blue
        };

        var window = new Window
        {
            Content = control,
            Width = 160,
            Height = 120
        };

        await Dispatcher.UIThread.InvokeAsync(() => window.Show());
        
        // Allow window to render with Skia backend
        await Task.Delay(100);

        // Capture uses the window dimensions when control is in a window
        var bitmap = ScreenshotHelper.Capture(control, 160, 120);

        // Verify bitmap has valid dimensions (captures window, not just control)
        bitmap.Should().NotBeNull();
        bitmap.PixelSize.Width.Should().Be(160, "captured frame matches window width");
        bitmap.PixelSize.Height.Should().Be(120, "captured frame matches window height");

        var outputDirectory = Path.Combine("Screenshots", "TestOutputs");
        Directory.CreateDirectory(outputDirectory);
        var filePath = Path.Combine(outputDirectory, $"screenshot-{Guid.NewGuid():N}.png");

        try
        {
            await ScreenshotHelper.Save(bitmap, filePath).ConfigureAwait(false);

            File.Exists(filePath).Should().BeTrue();
            // With Skia backend enabled, screenshots should contain actual pixel data
            var fileSize = new FileInfo(filePath).Length;
            fileSize.Should().BeGreaterThan(0, "screenshot should contain PNG data");
            
            // Verify it's a valid PNG file by checking the header
            using (var fs = File.OpenRead(filePath))
            {
                var header = new byte[8];
                fs.Read(header, 0, 8);
                // PNG header: 137 80 78 71 13 10 26 10
                header[0].Should().Be(137, "PNG header byte 0");
                header[1].Should().Be(80, "PNG header byte 1");
                header[2].Should().Be(78, "PNG header byte 2");
                header[3].Should().Be(71, "PNG header byte 3");
            }
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => window.Close());

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
