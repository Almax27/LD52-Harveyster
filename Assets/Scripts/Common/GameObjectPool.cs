using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectPool : SingletonBehaviour<GameObjectPool>
{
    class PrefabPool
    {
        PrefabPool() { }

        public PrefabPool(string name, Transform parent) 
        {
            root = new GameObject("Pool_" + name).transform;
            root.transform.parent = parent;
            set = new HashSet<GameObject>();
        }

        Transform root;
        HashSet<GameObject> set;

        public bool HasObjects() { return set.Count > 0; }

        public GameObject Get()
        {
            if(HasObjects())
            {
                var enumerator = set.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    var instance = enumerator.Current;
                    set.Remove(instance);
                    if (instance)
                    {
                        instance.transform.parent = null;
                        return instance;
                    }
                }
            }
            return null;
        }
        public bool Add(GameObject gobj)
        {
            if(set.Add(gobj))
            {
                gobj.SetActive(false);
                gobj.transform.parent = root;
                return true;
            }
            return false;
        }
    }

    public struct PooledGameObject
    {
        public PooledGameObject(GameObject prefab, GameObject instance) { Prefab = prefab; Instance = instance; }
        public GameObject Prefab { get; private set; }
        public GameObject Instance { get; private set; }

        public void AutoDestruct()
        {
            AutoDestruct autoDestruct = Instance.GetComponent<AutoDestruct>();
            if(!autoDestruct) autoDestruct = Instance.AddComponent<AutoDestruct>();
            autoDestruct.PoolWhenFinished(Prefab);
        }
    }

    Dictionary<GameObject, PrefabPool> PrefabPools = new Dictionary<GameObject, PrefabPool>();


    public PooledGameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation = default, Transform parent = null)
    {
        GameObject instance = null;
        PrefabPool pool;
        if(PrefabPools.TryGetValue(prefab, out pool) && pool.HasObjects())
        {
            instance = pool.Get();
            instance.transform.parent = parent;
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.SetActive(true);
        }
        else
        {
            instance = GameObject.Instantiate(prefab, position, rotation, parent);
        }
        return new PooledGameObject(prefab, instance);
    }

    public void Pool(PooledGameObject pooledGameObject)
    {
        if (pooledGameObject.Prefab && pooledGameObject.Instance)
        {
            pooledGameObject.Instance.SetActive(false);

            PrefabPool pool;
            if (PrefabPools.TryGetValue(pooledGameObject.Prefab, out pool))
            {
                pool.Add(pooledGameObject.Instance);
            }
            else
            {
                pool = new PrefabPool(pooledGameObject.Prefab.name, this.transform);
                pool.Add(pooledGameObject.Instance);
                PrefabPools.Add(pooledGameObject.Prefab, pool);
            }
        }
    }
}
