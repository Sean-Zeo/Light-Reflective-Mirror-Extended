using UnityEngine;

namespace MultiPlay
{
    public class Utils
    {
        private static string GetAppFolder()
        {
            int dirDepth = Application.dataPath.Split('/').Length;
            return Application.dataPath.Split('/')[dirDepth - 2];
        }
        public static int GetCurrentClientIndex()
        {
            int clientIndex = 0;
            string appFolderName = GetAppFolder();
            if (IsClient)
            {
                clientIndex = 1;

                if (appFolderName.IndexOf('[') > 0)
                {
                    int.TryParse(appFolderName.Substring(
                    appFolderName.IndexOf('[') + 1, 1), out clientIndex);
                }
            }
            return clientIndex;
        }

        public static bool IsClient => GetAppFolder().EndsWith("___Client");
    }
}