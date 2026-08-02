using System;
using Plugin.Firebase.Storage;
using eatMeet.ResourceManager;

namespace eatMeet.FirebaseStorage
{
    public static class FirebaseStorageManager
    {
        private static string BuildFilePath(string path, string fileName, string? contentType)
        {
            string normalizedPath = (path ?? string.Empty).Trim().Trim('/');
            string normalizedFileName = (fileName ?? string.Empty).Trim().Trim('/');

            while (normalizedPath.Contains("//", StringComparison.Ordinal))
            {
                normalizedPath = normalizedPath.Replace("//", "/", StringComparison.Ordinal);
            }

            while (normalizedFileName.Contains("//", StringComparison.Ordinal))
            {
                normalizedFileName = normalizedFileName.Replace("//", "/", StringComparison.Ordinal);
            }

            string extension = string.Empty;
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                int slashIndex = contentType.LastIndexOf('/');
                extension = slashIndex >= 0 ? contentType[(slashIndex + 1)..] : contentType;
                extension = extension.Trim().Trim('/', '.', '[', ']', '(', ')', '\'', '"').ToLowerInvariant();
            }

            if (!string.IsNullOrWhiteSpace(extension) && !normalizedFileName.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFileName = $"{normalizedFileName}.{extension}";
            }

            return string.IsNullOrWhiteSpace(normalizedPath)
                ? normalizedFileName
                : $"{normalizedPath}/{normalizedFileName}";
        }

        public static async Task<string> GetImageDownloadLink(string path)
        {
            IStorageReference storageRef = CrossFirebaseStorage.Current.GetReferenceFromPath(path);
            string imageStream = await storageRef.GetDownloadUrlAsync();

            return imageStream;
        }

        public static async Task<string> SaveFile(string path, string fileName, ImageFile imageFile)
        {
            if (imageFile?.Bytes == null || imageFile.Bytes.Length == 0)
            {
                throw new ArgumentException("Cannot upload an empty image file.", nameof(imageFile));
            }

            string filePath = BuildFilePath(path, fileName, imageFile.ContentType);

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("The generated Firebase Storage path is empty.", nameof(path));
            }

            IStorageReference storageRef = CrossFirebaseStorage.Current.GetReferenceFromPath(filePath);

            try { await storageRef.DeleteAsync(); } catch (Exception) { /* Ignore if the file does not exist.*/ }

            try
            {
                await storageRef.PutBytes(imageFile.Bytes).AwaitAsync();
            }
            catch (Exception ex)
            {
                string details = $"path='{filePath}', contentType='{imageFile.ContentType ?? "null"}', bytes={imageFile.Bytes.Length}";
                throw new InvalidOperationException($"Firebase Storage upload failed ({details}). {ex.Message}", ex);
            }

            return filePath;
        }

        public static async Task DeleteFile(string path)
        {
            IStorageReference storageRef = CrossFirebaseStorage.Current.GetReferenceFromPath(path);

            await storageRef.DeleteAsync();
        }
    }
}
