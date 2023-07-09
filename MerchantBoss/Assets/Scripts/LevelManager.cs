using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    public Room currentRoom;

    public Text floorText;
    public GameObject treasurePrefab;
    public List<Entity> entities = new List<Entity>();

    void Start()
    {
        instance = this;
        if (currentRoom != null) floorText.text = currentRoom.level.ToString();
    }

    public void RemoveEntity(Entity entity)
    {
        entities.Remove(entity);
        if (entities.Count <= 0)
        {
            if (!currentRoom.bossRoom && currentRoom.waves > 0) currentRoom.Spawn(1);
            else
            {
                currentRoom.thankYouText.SetActive(true);
                currentRoom.dungeonEntrance.Open();
                Instantiate(treasurePrefab, Vector3.up * 4, Quaternion.identity);
            }
        }
    }
}
