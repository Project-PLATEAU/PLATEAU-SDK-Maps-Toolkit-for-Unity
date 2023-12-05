using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using UnityEditor;

public static class FileDownloader
{
    public static void DownloadFile(string url, string savePath, System.Action<string> onComplete)
    {
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        webRequest.SendWebRequest();

        EditorApplication.update += EditorUpdate;

        void EditorUpdate()
        {
            if (webRequest.isDone)
            {
                EditorApplication.update -= EditorUpdate;

                if (webRequest.isNetworkError || webRequest.isHttpError)
                {
                    Debug.LogError("Error: " + webRequest.error);
                    onComplete?.Invoke(null);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                    File.WriteAllBytes(savePath, webRequest.downloadHandler.data);
                    Debug.Log("File successfully downloaded and saved to " + savePath);
                    onComplete?.Invoke(savePath);
                }
            }
        }
    }
}
