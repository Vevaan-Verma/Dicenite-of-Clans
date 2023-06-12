using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class PlaceableObjectDatabase : ScriptableObject {

    [Header("Object Settings")]
    public List<ObjectData> objectData;

}

[Serializable]
public class ObjectData {

    [field: SerializeField]
    public string name {

        get; private set;

    }

    [field: SerializeField]
    public int ID {

        get; private set;

    }

    [field: SerializeField]
    public Vector2Int size {

        get; private set;

    } = Vector2Int.one;

    [field: SerializeField]
    public GameObject prefab {

        get; private set;

    }

    [field: SerializeField]
    public bool stackable {

        get; private set;

    }
}
