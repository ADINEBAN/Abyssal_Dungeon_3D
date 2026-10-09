using UnityEngine;

namespace AbyssalDungeon.Dungeon
{
    public class DungeonRoomGenerator : MonoBehaviour
    {
        [Header("Room Prefabs & Materials")]
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material emissiveMaterial;
        
        [Header("Generation Settings")]
        [SerializeField] private float wallThickness = 1f;
        [SerializeField] private float roomHeight = 5f;

        public void GenerateRoom(RoomType type, Vector3 position, Vector3 size)
        {
            GameObject roomRoot = new GameObject($"Room_{type}");
            roomRoot.transform.position = position;

            if (type == RoomType.Boss)
            {
                GenerateBossArena(roomRoot.transform, size.x);
            }
            else
            {
                GenerateRectangularRoom(roomRoot.transform, size);
            }

            PlaceTorches(roomRoot.transform, size);
            
            if (type == RoomType.Puzzle)
            {
                PlaceCrystals(roomRoot.transform);
            }
            
            PlaceRandomProps(roomRoot.transform);
        }

        private void GenerateRectangularRoom(Transform parent, Vector3 size)
        {
            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.parent = parent;
            floor.transform.localPosition = new Vector3(0, -wallThickness / 2f, 0);
            floor.transform.localScale = new Vector3(size.x, wallThickness, size.z);
            if (floorMaterial != null) floor.GetComponent<Renderer>().material = floorMaterial;

            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.parent = parent;
            ceiling.transform.localPosition = new Vector3(0, roomHeight + wallThickness / 2f, 0);
            ceiling.transform.localScale = new Vector3(size.x, wallThickness, size.z);
            if (wallMaterial != null) ceiling.GetComponent<Renderer>().material = wallMaterial;

            // Walls (North, South, East, West)
            CreateWall(parent, "Wall_North", new Vector3(0, roomHeight / 2f, size.z / 2f), new Vector3(size.x, roomHeight, wallThickness));
            CreateWall(parent, "Wall_South", new Vector3(0, roomHeight / 2f, -size.z / 2f), new Vector3(size.x, roomHeight, wallThickness));
            CreateWall(parent, "Wall_East", new Vector3(size.x / 2f, roomHeight / 2f, 0), new Vector3(wallThickness, roomHeight, size.z));
            CreateWall(parent, "Wall_West", new Vector3(-size.x / 2f, roomHeight / 2f, 0), new Vector3(wallThickness, roomHeight, size.z));
        }

        private void CreateWall(Transform parent, string name, Vector3 localPos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.parent = parent;
            wall.transform.localPosition = localPos;
            wall.transform.localScale = scale;
            if (wallMaterial != null) wall.GetComponent<Renderer>().material = wallMaterial;
        }

        private void GenerateBossArena(Transform parent, float radius)
        {
            // A simple circular floor approximation using a flat cylinder
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Boss_Floor";
            floor.transform.parent = parent;
            floor.transform.localPosition = new Vector3(0, -wallThickness / 2f, 0);
            floor.transform.localScale = new Vector3(radius, wallThickness / 2f, radius);
            if (floorMaterial != null) floor.GetComponent<Renderer>().material = floorMaterial;

            // Minimal walls for the arena
            int wallSegments = 12;
            for (int i = 0; i < wallSegments; i++)
            {
                float angle = i * Mathf.PI * 2 / wallSegments;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * radius / 2f, roomHeight / 2f, Mathf.Sin(angle) * radius / 2f);
                CreateWall(parent, $"Boss_Wall_{i}", pos, new Vector3(radius / 3f, roomHeight, wallThickness));
                parent.GetChild(parent.childCount - 1).localRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0);
            }
        }

        private void PlaceTorches(Transform parent, Vector3 roomSize)
        {
            // Corner torches
            Vector3[] corners = {
                new Vector3(roomSize.x / 2f - 1f, 2f, roomSize.z / 2f - 1f),
                new Vector3(-roomSize.x / 2f + 1f, 2f, -roomSize.z / 2f + 1f)
            };

            foreach (var pos in corners)
            {
                GameObject torch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                torch.transform.parent = parent;
                torch.transform.localPosition = pos;
                torch.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
                
                Light light = torch.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = Color.yellow;
                light.range = 10f;
            }
        }

        private void PlaceCrystals(Transform parent)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crystal.transform.parent = parent;
                crystal.transform.localPosition = new Vector3((i - 1) * 3f, 1f, 0);
                crystal.transform.localScale = Vector3.one * 0.8f;
                if (emissiveMaterial != null) crystal.GetComponent<Renderer>().material = emissiveMaterial;
                
                // Add puzzle interactable components here in real implementation
            }
        }

        private void PlaceRandomProps(Transform parent)
        {
            // Generate some random crates or barrels
            for (int i = 0; i < 3; i++)
            {
                GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prop.transform.parent = parent;
                prop.transform.localPosition = new Vector3(Random.Range(-2f, 2f), 0.5f, Random.Range(-2f, 2f));
                prop.transform.localScale = Vector3.one * Random.Range(0.5f, 1.2f);
            }
        }
    }
}
