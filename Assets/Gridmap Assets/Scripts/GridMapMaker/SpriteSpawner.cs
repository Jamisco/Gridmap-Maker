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
using UnityEngine.UIElements;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker
{
    /// <summary>
    /// A simple helper class to spawn sprites
    /// </summary>
    public class SpriteSpawner : MonoBehaviour
    {
        public GridShape LayerGridShape { get; private set; }

        private string layerName;

        public void Initialize(string name, GridShape gridShape)
        {
            this.layerName = name;
            this.LayerGridShape = gridShape;
            gameObject.name = name;
        }
        private GameObject InstantiateSprite(Sprite sprite)
        {
            GameObject spriteObject = new GameObject();
            spriteObject.AddComponent<SpriteRenderer>().sprite = sprite;
            
            return spriteObject;
        }

        public void InsertSprite(Vector2Int position, Sprite sprite)
        {
            GameObject spriteObject = InstantiateSprite(sprite);

            spriteObject.transform.position = LayerGridShape.GetTesselatedPosition(position);

            spriteObject.transform.parent = transform;

            spriteObject.name = sprite.name;
        }
    }
}
