namespace Void.Services
{
    public static class MessageImageValidator
    {
        private const int MaxImageDataLength = 4_000_000;
        private const string Base64Marker = ";base64";

        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/png",
            "image/jpeg",
            "image/jpg",
            "image/jfif",
            "image/webp",
            "image/gif"
        };

        public static void Validate(string? imageData, string? imageMimeType, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(imageData))
                return;

            var trimmedImageData = imageData.Trim();
            var trimmedMimeType = imageMimeType?.Trim();

            if (trimmedImageData.Length > MaxImageDataLength)
            {
                errors.Add("Image must be smaller than 3 MB.");
                return;
            }

            if (trimmedImageData.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                ValidateDataUrl(trimmedImageData, trimmedMimeType, errors);
                return;
            }

            if (string.IsNullOrWhiteSpace(trimmedMimeType))
            {
                errors.Add("Image MIME type is required.");
                return;
            }

            if (!IsAllowedMimeType(trimmedMimeType))
                errors.Add("Image must be PNG, JPG, JFIF, WEBP, or GIF.");

            if (!IsBase64(trimmedImageData))
                errors.Add("Image data must be base64 encoded.");
        }

        private static void ValidateDataUrl(string imageData, string? imageMimeType, List<string> errors)
        {
            var commaIndex = imageData.IndexOf(',');
            if (commaIndex < 0)
            {
                errors.Add("Image data URL is invalid.");
                return;
            }

            var metadata = imageData["data:".Length..commaIndex];
            if (!metadata.EndsWith(Base64Marker, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Image data URL must be base64 encoded.");
                return;
            }

            var dataUrlMimeType = metadata[..^Base64Marker.Length];
            if (!IsAllowedMimeType(dataUrlMimeType))
                errors.Add("Image must be PNG, JPG, JFIF, WEBP, or GIF.");

            if (!string.IsNullOrWhiteSpace(imageMimeType) && !MimeTypesMatch(imageMimeType, dataUrlMimeType))
                errors.Add("Image MIME type does not match the image data.");

            var base64Payload = imageData[(commaIndex + 1)..];
            if (!IsBase64(base64Payload))
                errors.Add("Image data must be base64 encoded.");
        }

        private static bool IsAllowedMimeType(string mimeType)
        {
            return AllowedMimeTypes.Contains(mimeType);
        }

        private static bool MimeTypesMatch(string first, string second)
        {
            return CanonicalizeMimeType(first) == CanonicalizeMimeType(second);
        }

        private static string CanonicalizeMimeType(string mimeType)
        {
            var normalized = mimeType.Trim().ToLowerInvariant();
            return normalized == "image/jpg" ? "image/jpeg" : normalized;
        }

        private static bool IsBase64(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                Convert.FromBase64String(value);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
