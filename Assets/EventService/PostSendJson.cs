using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;



/// <summary>
/// Менеджер отправки POST JSON на сервер по таймеру
/// </summary>
public class PostManager {

    // оптправка прошла с ошибкой 
    public Action<string> OnSendError;

    // отправка прошла успешно, количество отплавленных событий
    public Action<int> OnSendCountSuccess;

 
    public bool IsBusy { get; private set; } = false;


    private MonoBehaviour monoBehaviour;


    public PostManager(MonoBehaviour parent) {
        monoBehaviour = parent;
    }


    // отравка POST JSON сообщения по таймеру
    public void SendJsonTimer(float time, Func<(string, string, int)> severUrlJsonCount) {
        Debug.Assert(!IsBusy);
        Debug.Log($"Установка таймера на отправку Time: {time}");

        IsBusy = true;
        monoBehaviour.StartCoroutine(InvokeTimer(time, () => {
            var (url, json, count) = severUrlJsonCount();
            if (count != 0) {
                monoBehaviour.StartCoroutine(CallPostJson(url, json, count));
            } else {
                OnSendCountSuccess?.Invoke(0);
            }
        }));
    }


    // корутина отправки POST JSON сообщения
    internal IEnumerator CallPostJson(string url, string jsonString, int count) {
        Debug.Assert(IsBusy);

        using (var request = new UnityWebRequest(url, "POST")) {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonString);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

            IsBusy = false;

            if (request.error != null) {
                OnSendError?.Invoke(request.error);

            } else if (request.responseCode == 200) {
                Debug.Log($"Удачная отправка {count} событий на сервер: {url}, JSON: {jsonString}");
                OnSendCountSuccess?.Invoke(count);

            } else {
                OnSendError?.Invoke($"Failed Status Code: {request.responseCode}");
            }

        }
    }


    // корутина таймера
    private static IEnumerator InvokeTimer(float time, Action handler) {
        yield return new WaitForSeconds(time);
        handler?.Invoke();
    }
}

