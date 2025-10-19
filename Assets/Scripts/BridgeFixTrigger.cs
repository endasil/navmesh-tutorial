using System;
using Unity.AI.Navigation;
using UnityEngine;

public class BridgeFixTrigger : MonoBehaviour
{

    public GameObject brokenBridge;

    public GameObject fixedBridge;
    public NavMeshSurface navMeshSurface;

    public void OnTriggerEnter(Collider other)
    {
        brokenBridge.SetActive(false);
        fixedBridge.SetActive(true);
        navMeshSurface.BuildNavMesh();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
