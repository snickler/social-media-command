using Avalonia.Data.Converters;
using SocialMediaCommander.Core.Models;
using System;
using System.Globalization;

namespace SocialMediaCommander.Desktop.Converters;

public class MediaTypeToIconConverter : IValueConverter
{
    public static readonly MediaTypeToIconConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MediaType mediaType)
        {
            return mediaType switch
            {
                MediaType.Image => "🖼️",
                MediaType.Video => "🎥",
                MediaType.Audio => "🎵",
                MediaType.Document => "📄",
                _ => "📁"
            };
        }

        return "📁";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 