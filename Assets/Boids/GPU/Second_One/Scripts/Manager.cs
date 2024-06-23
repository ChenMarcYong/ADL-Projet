using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;

public class Manager : MonoBehaviour
{

    List<Boid>[] listOfBoids;
    int[] destroyedBoids;
    public Camera playerCamera;

    public BoidParameters[] boidParam;
    public ComputeShader computeShader;

    public Transform player;

    public Boid[] prefabs;

    public int spawnRay;

    public int nbOfBoids;

    public float[] percentSpawn;

    private float distanceThreshold = 10.0f;

    private float checkInterval = 10.0f;

    private float spawnInterval = 3f;

    private float frustumMargin = 0.1f;

    private int destroyedCount = 0;
    private int activeCount = 0;

    //public int maxActiveBoid;

    void Start()
    {
        Vector3 playerPos = new Vector3(player.position.x, -spawnRay, player.position.z);

        listOfBoids = new List<Boid>[System.Enum.GetNames(typeof(Boid.Type)).Length];
        destroyedBoids = new int[System.Enum.GetNames(typeof(Boid.Type)).Length];

        playerCamera = Camera.main;

        for (int it = 0; it < listOfBoids.Length; it++)
        {
            listOfBoids[it] = new List<Boid>();

            int boidCount = (int)((percentSpawn[it] / 100.0f) * nbOfBoids);
            for (int i = 0; i < boidCount; i++)
            {
                Vector3 pos = playerPos + UnityEngine.Random.insideUnitSphere * spawnRay;
                initiateBoid(pos, it);
            }
        }

        StartCoroutine(CheckBoidDistances());
        StartCoroutine(SpawnNewBoids());
    }

    IEnumerator CheckBoidDistances()
    {

        while (true)
        {


            if (listOfBoids != null)
            {
                for (int it = 0; it < listOfBoids.Length; it++)
                {
                    List<Boid> boidList = listOfBoids[it];
                    if (boidList != null)
                    {
                        for (int i = 0; i < boidList.Count; i++)
                        {
                            if (i == 0) continue;
                            Boid boid = boidList[i];
                            float distance = Vector3.Distance(player.position, boid.transform.position);

                            if (distance > distanceThreshold && !IsInView(boid.transform.position))
                            {
                                destroyedBoids[it]++;
                                Destroy(boid.gameObject);
                                boidList.RemoveAt(i);

                                destroyedCount++;
                                activeCount--;
                            }
                        }
                    }
                }
                UnityEngine.Debug.Log($"Nombre de boids détruits : {destroyedCount}, Nombre de boids actifs : {activeCount}");
            }
            yield return new WaitForSeconds(checkInterval);
        }
    }






    IEnumerator SpawnNewBoids()
    {
        while (true)
        {
            float spawnRate = UnityEngine.Random.Range(0.2f, 0.5f);
            UnityEngine.Debug.Log($"prochain spawnBoid : {spawnInterval}, Spawn rate : {spawnRate}, NbBoidSpawn : {destroyedCount * spawnRate}");

            for (int it = 0; it < destroyedBoids.Length; it++)
            {

                if (destroyedBoids[it] != null)
                {

                    for (int i = 0; i < destroyedBoids[it] * spawnRate; i++)
                    {
                        //if (activeCount > maxActiveBoid) break;
                        Vector3 pos;
                        do
                        {
                            pos = GetRandomSpawnPosition();
                        } while (IsInView(pos));

                        destroyedBoids[it]--;
                        destroyedCount--;
                        initiateBoid(pos, it);
                    }
                }
            }
            spawnInterval = UnityEngine.Random.Range(5, 15);
            //maxActiveBoid += UnityEngine.Random.Range(-50, 50);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    bool IsInView(Vector3 worldPosition)
    {
        Vector3 viewportPoint = playerCamera.WorldToViewportPoint(worldPosition);
        return viewportPoint.x >= -frustumMargin && viewportPoint.x <= 1 + frustumMargin &&
               viewportPoint.y >= -frustumMargin && viewportPoint.y <= 1 + frustumMargin &&
               viewportPoint.z > 0;
    }



    void Update()
    {
        if (listOfBoids != null)
        {
            CPU_BOID[][] cpuBoid = new CPU_BOID[listOfBoids.Length][];
            ComputeBuffer[] computeBuffer = new ComputeBuffer[listOfBoids.Length];

            for (int it = 0; it < listOfBoids.Length; it++)
            {
                if (listOfBoids[it] != null)
                {
                    int sizeListOfBoids = listOfBoids[it].Count;
                    cpuBoid[it] = new CPU_BOID[sizeListOfBoids];

                    int stride = Marshal.SizeOf(typeof(CPU_BOID));
                    computeBuffer[it] = new ComputeBuffer(sizeListOfBoids, stride);

                    int threadGroups = Mathf.CeilToInt(sizeListOfBoids / 64.0f);

                    for (int i = 0; i < listOfBoids[it].Count; i++)
                    {
                        Boid boid = listOfBoids[it][i];
                        cpuBoid[it][i].pos = boid.transform.position;
                        cpuBoid[it][i].dir = boid.transform.forward;
                        cpuBoid[it][i].vel = boid.velocity;
                    }

                    computeBuffer[it].SetData(cpuBoid[it]);
                    computeShader.SetBuffer(0, "boids", computeBuffer[it]);
                    computeShader.SetInt("sizeListOfBoids", sizeListOfBoids);
                    computeShader.SetFloat("percRay", boidParam[it].percRay);
                    computeShader.SetFloat("avoidRay", boidParam[it].avoidRay);
                    computeShader.SetFloat("maxSpeed", boidParam[it].maxSpeed);
                    computeShader.SetFloat("maxSteerForce", boidParam[it].maxSteerForce);

                    computeShader.Dispatch(0, threadGroups, 1, 1);

                    computeBuffer[it].GetData(cpuBoid[it]);
                    computeBuffer[it].Dispose();

                    for (int i = 0; i < listOfBoids[it].Count; i++)
                    {
                        Boid boid = listOfBoids[it][i];
                        boid.nbTeammates = cpuBoid[it][i].nbTeammates;
                        boid.alignmentForce = cpuBoid[it][i].alignmentForce;
                        boid.cohesionForce = cpuBoid[it][i].cohesionForce;
                        boid.seperationForce = cpuBoid[it][i].seperationForce;

                        boid.new_Boid();
                    }
                }
            }
        }
    }


    void initiateBoid(Vector3 pos, int it)
    {
        Boid boid = Instantiate(prefabs[it], pos, Quaternion.LookRotation(UnityEngine.Random.insideUnitSphere), transform);
        listOfBoids[it].Add(boid);
        boid.Init(boidParam[it]);
        activeCount++;
    }


    Vector3 GetRandomSpawnPosition()
    {
        Vector3 playerPos = new Vector3(player.position.x, -spawnRay, player.position.z);
        return playerPos + UnityEngine.Random.insideUnitSphere * spawnRay;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct CPU_BOID
    {
        public int nbTeammates;

        public Vector3 pos;
        public Vector3 dir;
        public Vector3 vel;

        public Vector3 groupBoss;
        public Vector3 groupMiddle;
        public Vector3 avoid;

        public Vector3 alignmentForce;
        public Vector3 cohesionForce;
        public Vector3 seperationForce;
    }
}
