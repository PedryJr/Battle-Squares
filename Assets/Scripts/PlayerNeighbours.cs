using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerNeighbours : MonoBehaviour
{

    List<PlayerBehaviour> neighbours;

    private void Awake()
    {
        neighbours = new List<PlayerBehaviour>();
    }

    public void AddNeighbour(PlayerBehaviour player)
    {
        if (!neighbours.Contains(player)) neighbours.Add(player);
    }

    public void OnDestroy()
    {
        foreach (var neighbour in neighbours)
        {
            try
            {
                if (neighbour && neighbour.gameObject) Destroy(neighbour.gameObject);
            }
            catch (Exception e) 
            {
                Debug.LogException(e);
            }
        }
        neighbours.Clear();
    }

}
