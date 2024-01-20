using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class CarController : MonoBehaviour
{
    public TextMeshProUGUI[] variablesTextValue;

    private CNN network;
    public Vector3 startPostion, startRotation;

    [Range(-1.0f, 1.0f)]
    public float a, t;

    public float elapsedTime = 0.0f;

    [Header("Fitness")]
    public float overallFitness;
    public float distanceMultiplier = 1.4f;
    public float averageSpeedMultiplier = 0.2f;
    public float distanceFromObstacleMultiplier = 0.1f;

    [Header("CNN Settings")]
    public int LAYERS = 1;
    public int NEURONS = 10;

    private Vector3 lastPosition;
    private float totalDistanceTraveled;
    private float averageSpeed;

    // Distance value of each raycast sensor between obstacle and vehicule in multiple directions
    private float[] distancesFromObstacle =
    {
        0.0f,
        0.0f,
        0.0f
    };

    private void Awake()
    {
        startPostion = transform.position;
        startRotation = transform.eulerAngles;
        network = new CNN();

        network.Initialize(LAYERS, NEURONS);
    }

    private void ComputeFitness()
    {
        totalDistanceTraveled += Vector3.Distance(transform.position, lastPosition);
        averageSpeed = totalDistanceTraveled / elapsedTime;

        overallFitness = (totalDistanceTraveled * distanceMultiplier) +
                        (averageSpeed * averageSpeedMultiplier) +
                        (distancesFromObstacle.Average() * distanceFromObstacleMultiplier);

        // Reset if not good enough
        if(elapsedTime > 20.0f && overallFitness < 40)
        {
            Death();
        }

        if(overallFitness >= 10000)
        {
            Death();
        }

    }

    private void FixedUpdate()
    {
        DetectObstacle();
        lastPosition = transform.position;

        (a, t) = network.Run(distancesFromObstacle);

        MoveCar(a, t);

        elapsedTime += Time.deltaTime;

        ComputeFitness();
        UpdateText();

    }

    private void UpdateText()
    {

        variablesTextValue[0].text = "Current Generation: " + GameObject.FindObjectOfType<GenericAlgorithm>().currentGeneration.ToString();
        variablesTextValue[1].text = "Current Genome: " + GameObject.FindObjectOfType<GenericAlgorithm>().currentGenome.ToString();
        variablesTextValue[2].text = "Elapsed Time: " + elapsedTime.ToString();
        variablesTextValue[3].text = "Fitness: " + overallFitness.ToString();
        variablesTextValue[4].text = "Turn Rate: " + t.ToString();
        variablesTextValue[5].text = "Acceleration: " + a.ToString();



    }


    private void DetectObstacle()
    {
        Vector3[] directions =
        {
            transform.forward + transform.right, // right
            transform.forward, // forward
            transform.forward - transform.right // left
        };

        const float rayLength = 500.0f;

        int i = 0;
        foreach(Vector3 direction in directions)
        {
            Ray r = new Ray(transform.position, direction);
            RaycastHit hit;

            if (Physics.Raycast(r, out hit, maxDistance: rayLength))
            {
                float value = hit.distance / (direction.magnitude * rayLength); // we normalize the distance for the sigmoid function between 0 & 1
                distancesFromObstacle[i] = value; // we store that distance in the array

                print("value : " + value);

            }

            i++;
        }

    }

    private void OnDrawGizmos()
    {

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;

            Vector3[] directions =
            {
                transform.forward + transform.right, // right
                transform.forward, // forward
                transform.forward - transform.right // left
            };

            foreach (Vector3 direction in directions)
            {
                Gizmos.DrawRay(transform.position, direction * 100); // Adjust the length multiplier as needed
            }
        }

    }


    private void MoveCar(float accelerationRate, float turnRate)
    {
        Vector3 inp = Vector3.Lerp(Vector3.zero, new Vector3(0, 0, accelerationRate * 150.0f), Time.fixedDeltaTime);

        transform.position += transform.TransformDirection(inp);

        transform.eulerAngles += new Vector3(0.0f, turnRate * 90.0f * Time.fixedDeltaTime, 0.0f);
    }

    private void Death()
    {
        GameObject.FindObjectOfType<GenericAlgorithm>().Death(overallFitness, network);
    }

    public void Reset(CNN n)
    {
        network = n;

        elapsedTime = 0.0f;
        totalDistanceTraveled = 0.0f;
        averageSpeed = 0.0f;
        overallFitness = 0.0f;

        lastPosition = startPostion;
        transform.position = startPostion;
        transform.eulerAngles = startRotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Death();
    }
}
