using System;
using UnityEngine;


/// <summary>
/// Сервис приемки событий и отправки их на сервер
/// записывает не отплавленные данные в файл при выходе из приложения
/// </summary>
public class EventService : MonoBehaviour {

    [Header("Ожидание отправки, сек")]
    public float cooldownBeforeSend = 5;

    [Header("Сервер логирования")]
    public string serverUrl = "http://httpbin.org/post";

    [Header("Максимум событий для отправки")]
    public int maxSendEvents = 100;


    public static EventService Instance { get; private set; }


    private EventsStorage<EventData> storage;
    private PostManager postManager;


    [Serializable]
    private class EventData {
        public string type;
        public string data;
    }


    private void Awake() {
        Debug.Assert(Instance == null);
        Instance = this;

        StartService();
    }


    void OnApplicationQuit() {
        Debug.Log("Завершение приложения");

        StopService();
    }



    internal void StartService() {
        StorageInitialise();
        PostManagerInitialise();

        storage.Load();
    }



    internal void StopService() {
        if (storage.Count != 0) {
            if (storage.IsExistSavedItems) {
                storage.Load();
            }
            storage.Save();
        }
    }


    #region EventsStorage


    // получение события
    public void TrackEvent(string type, string data) {
        Debug.Log($"Новое событие [type: {type} data: {data}], count: {storage.Count}");

        var item = new EventData() { type = type, data = data };
        storage.AddItem(item);
    }


    // инициализация хранилища списка событий
    private void StorageInitialise() {
        Debug.Log("Инициализация хранилища списка событий");

        storage = new EventsStorage<EventData>();

        // в хранилище событий появились элементы
        storage.OnIsNotEmpty += delegate () {
            Debug.Log($"В хранилище появились элементы, count: {storage.Count}");

            if (postManager.IsBusy) return;

            SendPost();
        };

        // ошибка загрузки событий из файла
        storage.OnLoadError += delegate (string error) {
            TrackEvent("Error Load Events", error);
        };

        // ошибка сохранения событий в файл
        storage.OnSaveError += delegate (string error) {
            TrackEvent("Error Save Events", error);
        };
    }


    #endregion



    #region PostManager

    // отправка сообщений (если заполнено на полную отправку, то шлем без задержки)
    private void SendPost() {
        if (storage.Count < maxSendEvents) {
            SendPostTimer(cooldownBeforeSend);
        } else {
            SendPostTimer(0);
        }
    }

    // отправка сообщений через время ожидания
    private void SendPostTimer(float time) {
        Debug.Assert(storage.Count != 0 && !postManager.IsBusy);

        Debug.Log($"Старт таймера {time} сек на отправкау, count: {storage.Count}");
   
        postManager.SendJsonTimer(time, () => {
            var sendItemsCount = Math.Min(maxSendEvents, storage.Count);
            var jsonStr = storage.GetJsonFromFirstItems(sendItemsCount);
            return (serverUrl, jsonStr, sendItemsCount);
        });
    }


    // инициализация менеджера отправки сообщений
    private void PostManagerInitialise() {
        Debug.Log("Инициализация менеджера отправки сообщений");
        postManager = new PostManager(this);

        // удачно отравилось на сервер
        postManager.OnSendCountSuccess += delegate (int count) {
            // улвляем число отправленных
            storage.RemoveFirstItems(count);

            if (storage.Count == 0) return;
            if (postManager.IsBusy) return;

            SendPost();
        };

        // отправка POST сообщения не удалась
        postManager.OnSendError += delegate (string error) {
            Debug.Log("отправка POST сообщения не удалась");

            TrackEvent("Error sending events", error);

            if (postManager.IsBusy) return;

            // повторяем оптравку через 2 сек после не удачи
            SendPostTimer(2f);
        };
            
    }

    #endregion
}




