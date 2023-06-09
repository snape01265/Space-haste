using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SpawnMeteor : MonoBehaviour
{
    // Spawn Control 
    [SerializeField] private float _spawnRate = 5f;
    private Collider _sphereCollider;
    public bool _isPlayerInside = false;

    
    // Meteor Prefabs Control
    public GameObject[] objectPrefabs;
    public float destroyTime = 10.0f;
    
    
    //public GameObject meteorVFX;
    public Transform startPoint;

    public Transform[] middlePoints;

    // Control Random 
    [SerializeField] private float _minS = -15f;
    [SerializeField] private float _maxS = 15f;
    
    
    void Start()
    {
        _sphereCollider = GetComponentInChildren<Collider>();
        //Debug.Log(_sphereCollider.name);

        // Spawn objects at spawnrate 
        //InvokeRepeating("SpawnObject", 0f, _spawnRate);

        
    }
    

    public IEnumerator Spawn()
    {
        while (_isPlayerInside)
        {
            // Random Spawn from List 
            int idxPrefabList = UnityEngine.Random.Range(0, objectPrefabs.Length);

            Vector3 _randVector = new Vector3(UnityEngine.Random.Range(_minS, _maxS), 
                                              UnityEngine.Random.Range(_minS, _maxS), 
                                              UnityEngine.Random.Range(_minS, _maxS));


            // Instantiate Meteor
            Instantiate(objectPrefabs[idxPrefabList], 
                        startPoint.position + _randVector, 
                        Quaternion.identity);

            yield return new WaitForSeconds(_spawnRate);
        }
    }


    //private void SpawnObject()
    //{
        
    //    // Random Spawn from List 
    //    int idxPrefabList = UnityEngine.Random.Range (0, objectPrefabs.Length);



    //    // Instantiate Meteor
    //    GameObject newMeteor = Instantiate(objectPrefabs[idxPrefabList], startPoint.position, Quaternion.identity); 
    //    //newMeteor.GetComponent<MeteorMove>()._speed = UnityEngine.Random.Range(_minSpeed, _maxSpeed);
        
        
    //    //RotateTo(newMeteor, middlePoints[0].transform.position);
    //}


    //// Rotate Meteor Toward Destination
    //public void RotateTo(GameObject obj, Vector3 destination)
    //{
    //    var _direction = destination - obj.transform.position;
    //    var _rotation = Quaternion.LookRotation(_direction);

    //    obj.transform.localRotation = Quaternion.Lerp(obj.transform.rotation, _rotation, 1);
    //}


}

/// TODO : 내일은 꼭 이거 완성해야지 
/// rb로 움직이는거 구현하고 체크포인트 데이터 이쪽 meteor controller로 전송하는 방법 알아내야겠다 
