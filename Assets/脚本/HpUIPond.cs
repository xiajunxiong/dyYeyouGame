using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HpUIPond : MonoBehaviour
{
    public int pondNum = 10;
    public int maxPondNum = 100;
    public int numPerExpandOperation = 10;
    public GameObject addHpPrefab;
    public GameObject reduceHpPrefab;
    private Queue<GameObject> addHpQueue = new Queue<GameObject>();
    private Queue<GameObject> reduceHpQueue = new Queue<GameObject>();
    public static HpUIPond Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        for (int i = 0; i < pondNum; i++)
        {
            GameObject addHpObj = Instantiate(addHpPrefab, transform);
            addHpObj.SetActive(false);
            addHpQueue.Enqueue(addHpObj);
            GameObject reduceHpObj = Instantiate(reduceHpPrefab, transform);
            reduceHpObj.SetActive(false);
            reduceHpQueue.Enqueue(reduceHpObj);
        }
    }

    // 回收对象
    public void RecycleAddHpObj(GameObject obj)
    {
        obj.SetActive(false);
        addHpQueue.Enqueue(obj);
    }
    public void RecycleReduceHpObj(GameObject obj)
    {
        obj.SetActive(false);
        reduceHpQueue.Enqueue(obj);
    }

    /// <summary>
    /// 传入 0 获取一个加血对象，传入 1 获取一个减血对象
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public GameObject GetHpObj(int type)
    {
        if (type == 0)
        {
            return GetAddHpObj();
        }
        else if(type == 1)
        {
            return GetReduceHpObj();
        }
        return null;
    }

    // 扩容池
    public void ExpandPond()
    {
        for (int i = 0; i < numPerExpandOperation; i++)
        {
            if (addHpQueue.Count < maxPondNum)
            {
                GameObject addHpObj = Instantiate(addHpPrefab, transform);
                addHpObj.SetActive(false);
                addHpQueue.Enqueue(addHpObj);
            }
            if (reduceHpQueue.Count < maxPondNum)
            {
                GameObject reduceHpObj = Instantiate(reduceHpPrefab, transform);
                reduceHpObj.SetActive(false);
                reduceHpQueue.Enqueue(reduceHpObj);
            }
        }
        pondNum += numPerExpandOperation;
    }
    // 从池中获取一个对象
    public GameObject GetAddHpObj()
    {
        if (addHpQueue.Count == 0)
        {
            ExpandPond();
        }
        return addHpQueue.Dequeue();
    }
    public GameObject GetReduceHpObj()
    {
        if (reduceHpQueue.Count == 0)
        {
            ExpandPond();
        }
        return reduceHpQueue.Dequeue();
    }

    /// <summary>
    ///  定时回收对象，传入 0 回收加血对象，传入 1 回收减血对象
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="delay"></param>
    /// <param name="type"></param>
    public void RecycleObjWithDelay(GameObject obj, float delay, int type)
    {
        StartCoroutine(RecycleObjCoroutine(obj, delay, type));
    }
    IEnumerator RecycleObjCoroutine(GameObject obj, float delay, int type)
    {
        yield return new WaitForSeconds(delay);
        if (type == 0)
        {
            RecycleAddHpObj(obj);
        }
        else if (type == 1)
        {
            RecycleReduceHpObj(obj);
        }
    }
}
