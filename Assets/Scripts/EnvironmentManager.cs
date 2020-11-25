using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.MLAgents;

public class EnvironmentManager : MonoBehaviour
{
    #region Fields and Properties

    GameObject _rockPrefab;
    GameObject _targetPrefab;
    GameObject _componentPrefab;

    GameObject[] _obstacles = new GameObject[1];
    GameObject[] _assembly = new GameObject[1];
    GameObject[] _stack = new GameObject[1];

    Bounds _boundary;

    public GameObject CurrentTarget { get; private set; }
    public GameObject CurrentComponent { get; private set; }

    public Vector3 BoundarySize => _boundary.size;

    #endregion

    #region Unity methods

    private void Start()
    {
        // Reads the prefabs from Resources
        _rockPrefab = Resources.Load<GameObject>("Prefabs/rock");
        _targetPrefab = Resources.Load<GameObject>("Prefabs/Target");
        _componentPrefab = Resources.Load<GameObject>("Prefabs/Component");

        // Get the boudnary of the environment
        _boundary = transform.Find("Platform").GetComponent<BoxCollider>().bounds;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Method for creating rock obstacles in the environment
    /// </summary>
    /// <param name="amt"> Amount of obstacles</param>
    public void GenerateObstacles(int amt)
    {
        _obstacles = new GameObject[amt];
        for (int i = 0; i < amt; i++)
        {
            Quaternion newRotation = Quaternion.Euler(0,Random.Range(0f, 360f),0);
            
            float s = Random.Range(1f, 2f);
            Vector3 newScale = Vector3.one * s;

            Vector3 newPosition = new Vector3(
                Random.Range((-_boundary.size.x / 2) + s, (_boundary.size.x / 2) - s),
                0,
                Random.Range((-_boundary.size.z / 2) + s, (_boundary.size.z / 2) - s)
                );

            _obstacles[i] = Instantiate(_rockPrefab, transform);
            _obstacles[i].transform.localPosition = newPosition;
            _obstacles[i].transform.localRotation = newRotation;
            _obstacles[i].transform.localScale = newScale;
        }
    }

    /// <summary>
    /// Resets the environment during training, at the end of the episode
    /// </summary>
    public void ResetEnvironment()
    {
        // Destroy all elements of the assembly
        foreach (var target in _assembly) if (target != null) Destroy(target);

        // Destroy all elements of the stack
        foreach (var component in _stack) if (component != null) Destroy(component);
    }

    /// <summary>
    /// Generate a pair of elements to be built, one representing the component
    /// and one representing the position it should occupy on the assembly
    /// </summary>
    public void GenerateAssemblyPair()
    {
        // Initiate arrays with one element
        _assembly = new GameObject[1];
        _stack = new GameObject[1];

        // Instantiate the prefabs
        _assembly[0] = Instantiate(_targetPrefab, transform);
        _stack[0] = Instantiate(_componentPrefab, transform);

        // Randomize the orientation of the target
        int xRotation = Random.Range(0, 4);
        int zRotation = Random.Range(0, 4);
        int yRotation = Random.Range(0, 4);
        Vector3 orientation = new Vector3(xRotation * 90, yRotation * 90, zRotation * 90);
        
        _assembly[0].transform.localEulerAngles = orientation;
        
        // Assign the y coordinate value of the target based on its orientation
        float minY = _assembly[0].transform.Find("Box").GetComponent<Renderer>().bounds.min.y;
        float y = - minY;
        
        // Randomize the position of the target in X and Z
        float x = Random.Range((-_boundary.size.x / 2) + 1, (_boundary.size.x / 2) - 1);
        float z = Random.Range((-_boundary.size.z / 2) + 1, (_boundary.size.z / 2) - 1);
        Vector3 assemblyPosition = new Vector3(x, y, z);
       
        _assembly[0].transform.localPosition = assemblyPosition;

        // Randomize the position of the stack 
        float xx = Random.Range((-_boundary.size.x / 2) + 1, (_boundary.size.x / 2) - 1);
        float zz = Random.Range((-_boundary.size.z / 2) + 1, (_boundary.size.z / 2) - 1);
        Vector3 stackPosition = new Vector3(xx, 0.125f, zz);

        // Assure the stack is further than 10 units away from the target
        while (Vector3.Distance(stackPosition, assemblyPosition) < 10)
        {
            xx = Random.Range((-_boundary.size.x / 2) + 1, (_boundary.size.x / 2) - 1);
            zz = Random.Range((-_boundary.size.z / 2) + 1, (_boundary.size.z / 2) - 1);
            stackPosition = new Vector3(xx, 0.125f, zz);
        }

        _stack[0].transform.localPosition = stackPosition;

        // Assign the created elements as current target and resource
        CurrentTarget = _assembly[0];
        CurrentComponent = _stack[0];
    }

    /// <summary>
    /// Finds a valid position for the agent to be placed
    /// </summary>
    /// <returns>The position as a <see cref="Vector3"/></returns>
    public Vector3 FindValidPosition()
    {
        // Randomize the position
        float x = Random.Range((-_boundary.size.x / 2) + 1, (_boundary.size.x / 2) - 1);
        float z = Random.Range((-_boundary.size.z / 2) + 1, (_boundary.size.z / 2) - 1);
        Vector3 position = new Vector3(x, 0.5f, z);

        // Assure it is at least 2 units away from target and stack
        while (_assembly.Any(o => Vector3.Distance(position, o.transform.position) < 2) ||
            _stack.Any(o => Vector3.Distance(position, o.transform.position) < 2))
        {
            x = Random.Range((-_boundary.size.x / 2) + 1, (_boundary.size.x / 2) - 1);
            z = Random.Range((-_boundary.size.z / 2) + 1, (_boundary.size.z / 2) - 1);
            position = new Vector3(x, 0.5f, z);
        }

        return position;
    }

    /// <summary>
    /// Normalizes a position in the space of the environment
    /// </summary>
    /// <param name="position">The <see cref="Vector3"/> to be normalized</param>
    /// <returns>The normalized <see cref="Vector3"/></returns>
    public Vector3 NormalizedPosition(Vector3 position)
    {
        var maxX = _boundary.size.x / 2;
        var minX = -maxX;

        var maxZ = _boundary.size.z / 2;
        var minZ = -maxZ;

        var normalizedX = (position.x - minX) / (maxX - minX);
        var normalizedZ = (position.z - minZ) / (maxZ - minZ);

        return new Vector3(normalizedX, 0, normalizedZ);
    }

    #endregion
}
