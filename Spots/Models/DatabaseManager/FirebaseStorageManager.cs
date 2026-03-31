using Plugin.Firebase.Storage;
using eatMeet.ResourceManager;

namespace eatMeet.FirebaseStorage
{
    public static class FirebaseStorageManager
    {
        public static async Task<string> GetImageDownloadLink(string path)
        {
            IStorageReference storageRef = CrossFirebaseStorage.Current.GetReferenceFromPath(path);
            string imageStream = await storageRef.GetDownloadUrlAsync();

            return imageStream;
        }

        public static async Task<string> SaveFile(string path, string fileName, ImageFile imageFile)
        {
            string filePath = $"{path}/{fileName}.{imageFile.ContentType?.Replace("image/", "") ?? ""}";

            IStorageReference storageRef = CrossFirebaseStorage.Current.GetReferenceFromPath(filePath);

            try { await storageRef.DeleteAsync(); } catch (Exception) { /* Ignore if the file does not exist.*/ }

            await storageRef.PutBytes(imageFile.Bytes).AwaitAsync();
            return filePath;
        }

        public static async Task DeleteFile(string path)
        {
            IStorageReference storageRef = CrossFirebaseStorage.Current.GetReferenceFromPath(path);

            await storageRef.DeleteAsync();
        }
    }
}
