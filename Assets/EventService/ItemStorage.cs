using UnityEngine;

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


/// <summary>
/// Хранение событий, добавление, удаление первых, сохранение и загрузка файлов, преобразование в JSON
/// </summary>
/// <typeparam name="T"></typeparam>
internal class EventsStorage<T> where T : class {

    private const int maxCapacity = 100000;
    private string saveFilePath = Application.persistentDataPath + "/EventLogData.dat";

    [Serializable]
    private class Wrapper {
        public T[] events = null;
    }

    // существует ли файл с сохраненными событиями
    public bool IsExistSavedItems => File.Exists(saveFilePath);

    // сообщает
    // 1. добавляении первого элемента
    // 2. не пусто после удаления первых
    // 3. не пусто после загрузки из файла
    public Action OnIsNotEmpty; 

    public Action<string> OnSaveError;

    public Action<string> OnLoadError;


    public int Count { get { return itemList.Count; } }


    private List<T> itemList = new List<T>(maxCapacity);


    public EventsStorage() { }


    public void AddItem(T item) {
        if (itemList.Count >= maxCapacity) {
            itemList.RemoveAt(0);
        }

        itemList.Add(item);

        if (itemList.Count == 1) {
            OnIsNotEmpty?.Invoke();
        }
    }

    // берет число первых элементов и преобразует в JSON
    public string GetJsonFromFirstItems(int count) {
        return GetJsonFromItems(itemList.Take(count).ToArray());
    }

    // массив преобразует в JSON
    public static string GetJsonFromItems(T[] items) {
        Wrapper wrapper = new Wrapper { events = items };
        return JsonUtility.ToJson(wrapper);
    }

    // массив из JSON
    public static T[] GetItemsFromJson(string json) {
        Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);
        return wrapper.events;
    }


    public void RemoveFirstItems(int count) {
        if (count < itemList.Count) {
            itemList.RemoveRange(0, count);
            Debug.Log($"Удаление первых {count} событий из хранилища, count: {itemList.Count}");

            OnIsNotEmpty?.Invoke();  
        } else {
            Debug.Log($"Удаление всех {itemList.Count} событий");
            itemList.Clear();
        }
    }


    public void Save() {
        if (itemList.Count != 0) {
            string jsonStr = GetJsonFromFirstItems(itemList.Count);
            try {
                File.WriteAllText(saveFilePath, jsonStr);

                Debug.Log($"Успешное сохранение в файл {itemList.Count} событий");

                itemList.Clear();

            } catch (Exception e) {
                OnSaveError?.Invoke(e.Message);
            }
        }
    }


    public void Load() {
        if (IsExistSavedItems) {
            try {
                var jsonStr = File.ReadAllText(saveFilePath);
                File.Delete(saveFilePath);

                T[] items = GetItemsFromJson(jsonStr);
                itemList.InsertRange(0, items);

                Debug.Log($"Загрузка из файла прошла успешно, count {itemList.Count}");

            } catch (Exception e) {
                OnLoadError?.Invoke(e.Message);
            }
        }

        if (itemList.Count != 0) {
            OnIsNotEmpty?.Invoke();
        }
    }
}
