using SkiaSharp;
using SocialMediaCommander.Core.Services;
using Serilog;

namespace SocialMediaCommander.Services.Helpers;

/// <summary>
/// Helper class for compressing images to meet platform requirements
/// </summary>
public static class ImageCompressionHelper
{
    private static readonly ILogger _logger = LoggingService.ForContext("ImageCompressionHelper");

    /// <summary>
    /// Compresses an image to fit within the specified maximum file size
    /// </summary>
    /// <param name="imageBytes">Original image bytes</param>
    /// <param name="maxFileSizeBytes">Maximum file size in bytes (default: 1MB for BlueSky)</param>
    /// <param name="quality">Initial JPEG quality (0-100, default: 85)</param>
    /// <returns>Compressed image bytes, or null if compression fails</returns>
    public static byte[]? CompressImage(byte[] imageBytes, int maxFileSizeBytes = 1_000_000, int quality = 85)
    {
        try
        {
            _logger.Information("Compressing image: Original size {OriginalSize:N0} bytes, Target max {MaxSize:N0} bytes",
                imageBytes.Length, maxFileSizeBytes);

            // If already under the limit, return as-is
            if (imageBytes.Length <= maxFileSizeBytes)
            {
                _logger.Information("Image already under size limit, no compression needed");
                return imageBytes;
            }

            // Decode the image
            using var inputStream = new MemoryStream(imageBytes);
            using var originalBitmap = SKBitmap.Decode(inputStream);

            if (originalBitmap == null)
            {
                _logger.Error("Failed to decode image for compression");
                return null;
            }

            _logger.Information("Decoded image: {Width}x{Height}, ColorType: {ColorType}",
                originalBitmap.Width, originalBitmap.Height, originalBitmap.ColorType);

            // Try compression with decreasing quality
            byte[]? compressedBytes = null;
            int currentQuality = quality;
            int attempts = 0;
            const int maxAttempts = 5;

            while (currentQuality > 20 && attempts < maxAttempts)
            {
                attempts++;
                using var outputStream = new MemoryStream();

                // Encode as JPEG with current quality
                using var image = SKImage.FromBitmap(originalBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, currentQuality);
                data.SaveTo(outputStream);

                compressedBytes = outputStream.ToArray();

                _logger.Information("Attempt {Attempt}: Quality {Quality}%, Size {Size:N0} bytes",
                    attempts, currentQuality, compressedBytes.Length);

                if (compressedBytes.Length <= maxFileSizeBytes)
                {
                    _logger.Information("Successfully compressed image to {Size:N0} bytes (Quality: {Quality}%)",
                        compressedBytes.Length, currentQuality);
                    return compressedBytes;
                }

                // Reduce quality for next attempt
                currentQuality -= 15;
            }

            // If quality reduction didn't work, try resizing
            _logger.Warning("Quality reduction insufficient, attempting resize");

            int targetWidth = originalBitmap.Width;
            int targetHeight = originalBitmap.Height;
            double scaleFactor = Math.Sqrt((double)maxFileSizeBytes / imageBytes.Length);

            // Apply a more aggressive scale factor to ensure we get under the limit
            scaleFactor *= 0.8;

            targetWidth = (int)(targetWidth * scaleFactor);
            targetHeight = (int)(targetHeight * scaleFactor);

            _logger.Information("Resizing image from {OrigW}x{OrigH} to {NewW}x{NewH}",
                originalBitmap.Width, originalBitmap.Height, targetWidth, targetHeight);

            using var resizedBitmap = originalBitmap.Resize(
                new SKImageInfo(targetWidth, targetHeight),
                new SKSamplingOptions(SKFilterMode.Linear)
            );
            if (resizedBitmap == null)
            {
                _logger.Error("Failed to resize image");
                return null;
            }

            // Encode resized image with good quality
            using var resizedOutputStream = new MemoryStream();
            using var resizedImage = SKImage.FromBitmap(resizedBitmap);
            using var resizedData = resizedImage.Encode(SKEncodedImageFormat.Jpeg, 75);
            resizedData.SaveTo(resizedOutputStream);

            compressedBytes = resizedOutputStream.ToArray();

            _logger.Information("Resized and compressed image: {Size:N0} bytes", compressedBytes.Length);

            if (compressedBytes.Length <= maxFileSizeBytes)
            {
                return compressedBytes;
            }

            _logger.Error("Failed to compress image below {MaxSize:N0} bytes after resize. Final size: {FinalSize:N0} bytes",
                maxFileSizeBytes, compressedBytes.Length);
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Exception during image compression");
            return null;
        }
    }
}
