using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using Assets.Scripts.GridMapMaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData.ShapeVisualData;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker
{
    public class SpriteLayer : MonoBehaviour
    {
        public GridShape LayerGridShape { get; private set; }

        private Dictionary<Vector2Int, GameObject> SpawnedSprites
                              = new Dictionary<Vector2Int, GameObject>();

        private string layerName;

        public void Initialize(string name, GridShape gridShape)
        {
            this.layerName = name;
            this.LayerGridShape = gridShape;
            gameObject.name = name;

        }

        private GameObject InstatiateSprite(Sprite sprite)
        {
            GameObject spriteObject = new GameObject();
            spriteObject.AddComponent<SpriteRenderer>().sprite = sprite;

            return spriteObject;
        }

        public void InsertSprite(Vector2Int position, Sprite sprite)
        {
            if (SpawnedSprites.ContainsKey(position))
            {
                return;
            }

            GameObject spriteObject = InstatiateSprite(sprite);

            spriteObject.transform.position = LayerGridShape.GetTesselatedPosition(position);

            SpawnedSprites.Add(position, spriteObject);
        }

        public void RemoveSprite(Vector2Int position)
        {
            if (!SpawnedSprites.ContainsKey(position))
            {
                return;
            }

            GameObject spriteObject = SpawnedSprites[position];
            Destroy(spriteObject);

            SpawnedSprites.Remove(position);
        }


        public void Clear()
        {
            foreach (var sprite in SpawnedSprites)
            {
                DestroyImmediate(sprite.Value);
            }

            SpawnedSprites.Clear();
        }
    }
}
