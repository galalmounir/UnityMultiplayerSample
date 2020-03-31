using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HTTPClient : MonoBehaviour
{
    // Start is called before the first frame update
    void Start() {
        StartCoroutine(GetDateTime());
        StartCoroutine(GetUnityTest());
        StartCoroutine(UnityPOST());
    }

    IEnumerator GetDateTime() {
        UnityWebRequest www = UnityWebRequest.Get("https://crcotbe9o6.execute-api.us-east-2.amazonaws.com/default/lab1");
        yield return www.SendWebRequest();
 
        if(www.isNetworkError || www.isHttpError) {
            Debug.Log(www.error);
        }
        else {
            // Show results as text
            Debug.Log(www.downloadHandler.text);
 
            // Or retrieve results as binary data
            byte[] results = www.downloadHandler.data;
        }
    }

    IEnumerator GetUnityTest() {
        UnityWebRequest www = UnityWebRequest.Get("https://9rtvrin7r5.execute-api.us-east-2.amazonaws.com/default/UnityTest");
        yield return www.SendWebRequest();
 
        if(www.isNetworkError || www.isHttpError) {
            Debug.Log(www.error);
        }
        else {
            // Show results as text
            Debug.Log(www.downloadHandler.text);
        }
    }

    IEnumerator UnityPOST()
    {
        string jsonString = "{\"username\":\"Galal\"}";
        string username = "galal";
        byte[] myData = System.Text.Encoding.UTF8.GetBytes(jsonString);

        // UnityWebRequest www = UnityWebRequest.Post("https://9rtvrin7r5.execute-api.us-east-2.amazonaws.com/default/UnityTest",username);
        UnityWebRequest www = UnityWebRequest.Put("https://9rtvrin7r5.execute-api.us-east-2.amazonaws.com/default/UnityTest", myData);
        
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log(www.downloadHandler.text);
        }
    }
}
