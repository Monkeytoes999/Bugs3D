using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FlyFloat : MonoBehaviour
{
    public int maxHeight = 2;
    public int minHeight = 1;
    float currentOffset;
    int dir = 1;
    void Start()
    {
        currentOffset = (maxHeight + minHeight)/2f;
        gameObject.GetComponent<NavMeshAgent>().baseOffset = (maxHeight + minHeight)/2f;
    }

    void Update()
    {
        //currentOffset = currentOffset + Random.value*.1f - .05f;
        currentOffset = currentOffset + (dir*.01f);
        if (currentOffset > maxHeight) {
            dir = -1;
            currentOffset = maxHeight;
        }
        if (currentOffset < minHeight) {
            dir = 1;
            currentOffset = minHeight;
        }
        gameObject.GetComponent<NavMeshAgent>().baseOffset = currentOffset;
    }
}
