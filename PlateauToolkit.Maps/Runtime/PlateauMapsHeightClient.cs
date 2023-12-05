using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace PlateauToolkit.Maps {
    public class PlateauMapsHeightClient : MonoBehaviour
    {
        static PlateauMapsHeightClient s_Instance;

        public static PlateauMapsHeightClient Instance
        {
            get
            {
                // check if there is already a Height client in the scene
                PlateauMapsHeightClient instance = GameObject.FindFirstObjectByType<PlateauMapsHeightClient>();
                if (instance != null)
                {
                    s_Instance = instance;
                }

                // if there are no instances yet and instance is still null
                if (s_Instance == null)
                {
                    s_Instance = new GameObject("PlateauMapsHeightClient").AddComponent<PlateauMapsHeightClient>();
                }

                return s_Instance;
            }
        }

        public delegate void GeoidDataCallback(float geoidHeight);

        public static void RequestGeoidHeight(string uri, GeoidDataCallback callback)
        {
            Instance.StartCoroutine(Instance.RequestGeoidHeightToUri(uri, callback));
        }

        IEnumerator RequestGeoidHeightToUri(string uri, GeoidDataCallback callback)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    RootObject rootObject = JsonUtility.FromJson<RootObject>(webRequest.downloadHandler.text);
                    Debug.Log(webRequest.downloadHandler.text);
                    callback.Invoke(rootObject.OutputData.geoidHeight);
                }
                else
                {
                    Debug.Log(": Error: " + webRequest.error);
                    callback.Invoke(0f);
                }
            }
        }
    }
}