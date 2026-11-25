using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        sealed class Shelf
        {
            public GameObject Tag;
            public StoreMarker[] Products;
            public bool Full;
        }

        sealed class Flight
        {
            public GameObject Item, ShelfItem;
            public Vector3 Start, End, Scale;
            public Quaternion Rotation;
            public float Age;
        }

        readonly Dictionary<string, Shelf> shelves = new();
        StoreMarker[] shelfProducts = System.Array.Empty<StoreMarker>();
        Material cardboard;

        void BindStock(StoreMarker[] sceneObjects)
        {
            shelfProducts = sceneObjects.Where(marker => marker.kind == StoreMarker.Kind.Product).ToArray();
            var renderers = doors ? doors.GetComponentsInChildren<Renderer>(true) : FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            BindShelf("shelfRamen", new Vector3(4.138f, .47f, 3.7f));
            BindShelf("shelfMilk", new Vector3(9.338f, .86f, 3.7f));

            void BindShelf(string key, Vector3 tagPosition)
            {
                var target = ThreeCoords.ToUnity(tagPosition);
                var tag = renderers.FirstOrDefault(renderer => (renderer.transform.position - target).sqrMagnitude < .000025f);
                shelves.Add(key, new Shelf
                {
                    Tag = tag ? tag.gameObject : null,
                    Products = sceneObjects.Where(marker => marker.kind == StoreMarker.Kind.Product && marker.key.EndsWith(":" + key, System.StringComparison.Ordinal))
                        .OrderBy(marker => marker.transform.position.z).ThenBy(marker => marker.transform.position.y).ToArray(),
                });
            }
        }

        void Stock(string key, bool full)
        {
            var shelf = shelves[key];
            shelf.Full = full;
            if (shelf.Tag) shelf.Tag.SetActive(!full);
            foreach (var item in shelf.Products) item.gameObject.SetActive(full);
        }

        string RestockLabel(string key, string source, string product, string held)
        {
            if (restocking || shelves[key].Full) return null;
            return held == source ? $"Xếp {product} lên chỗ trống" : $"Hết {product} — cần thùng hàng trong kho";
        }

        void BeginRestock(string key)
        {
            bool ramen = key == "shelfRamen";
            if (restocking || shelves[key].Full || carry.Kind != (ramen ? "boxRamen" : "boxMilk")) return;
            carry.SetCarry(null);
            restocking = true;
            StartCoroutine(Refill(key, shelves[key], ramen));
        }

        IEnumerator Refill(string key, Shelf shelf, bool ramen)
        {
            var box = PropGroup("Thùng bổ sung hàng", new Vector3((ramen ? 4.6f : 9.8f) - .75f, 0, 3.7f));
            box.transform.rotation = Quaternion.Euler(0, -90, 0);
            cardboard ??= Lit(0xa98758, .15f);
            Block(box.transform, "Thân thùng", new Vector3(0, .175f, 0), new Vector3(.5f, .35f, .45f), cardboard);
            var flaps = new Transform[2];
            for (int i = 0; i < flaps.Length; ++i)
            {
                float side = i == 0 ? -1 : 1;
                var pivot = new GameObject("Nắp thùng").transform;
                pivot.SetParent(box.transform, false);
                pivot.localPosition = new Vector3(0, .35f, side * .225f);
                Block(pivot, "Bìa", new Vector3(0, 0, -side * .1125f), new Vector3(.5f, .01f, .225f), cardboard);
                flaps[i] = pivot;
            }
            if (shelf.Tag)
            {
                var tag = Instantiate(shelf.Tag, box.transform.position, shelf.Tag.transform.rotation);
                tag.transform.position = shelf.Tag.transform.position;
                tag.SetActive(true);
                dynamics.Add(tag);
                var body = tag.AddComponent<HanaMart.Interaction.ItemPhysicsBody>();
                body.Body = new HanaMart.Interaction.ItemBody
                {
                    position = ThreeCoords.ToUnity(tag.transform.position), velocity = new Vector3((RandomValue() - .5f) * .4f, .6f, 0),
                    spin = new Vector3(3, 1, 2), halfHeight = .003f, bounce = .1f,
                };
                Later(6, () => RemoveProp(tag));
                shelf.Tag.SetActive(false);
            }
            PlayClip("door");
            Say("Mở thùng, xếp hàng lên kệ...", 2);
            float time = 0;
            int launched = 0, landed = 0;
            var flies = new List<Flight>();
            var source = box.transform.position + Vector3.up * .36f;
            while (landed < shelf.Products.Length)
            {
                float dt = Time.deltaTime;
                time += dt;
                float angle = Mathf.Min(1, time / .35f) * 2.2f * Mathf.Rad2Deg;
                flaps[0].localRotation = Quaternion.Euler(-angle, 0, 0);
                flaps[1].localRotation = Quaternion.Euler(angle, 0, 0);
                while (launched < shelf.Products.Length && time > .4f + launched * .05f)
                {
                    var original = shelf.Products[launched++].gameObject;
                    var start = source + new Vector3((RandomValue() - .5f) * .2f, 0, (RandomValue() - .5f) * .2f);
                    var item = Instantiate(original, start, Quaternion.Euler(RandomValue() * 114 - 57, RandomValue() * 172, RandomValue() * 114 - 57));
                    item.SetActive(true);
                    dynamics.Add(item);
                    flies.Add(new Flight { Item = item, ShelfItem = original, Start = start, End = original.transform.position,
                        Scale = item.transform.localScale, Rotation = item.transform.rotation });
                    if (launched % 3 == 0) PlayClip("shelf");
                }
                for (int j = flies.Count - 1; j >= 0; --j)
                {
                    var flight = flies[j];
                    flight.Age += dt / .34f;
                    float k = Mathf.Clamp01(flight.Age);
                    float arc = .25f + Vector3.Distance(flight.Start, flight.End) * .25f;
                    flight.Item.transform.position = Vector3.Lerp(flight.Start, flight.End, 1 - Mathf.Pow(1 - k, 2.4f)) +
                        Vector3.up * (Mathf.Sin(k * Mathf.PI) * arc);
                    flight.Item.transform.rotation = Quaternion.Slerp(flight.Rotation, flight.ShelfItem.transform.rotation, Mathf.Min(1, k * 1.25f));
                    float height = 1 - Mathf.Sin(Mathf.Clamp01((flight.Age - 1) / .25f) * Mathf.PI) * .14f;
                    flight.Item.transform.localScale = Vector3.Scale(flight.Scale, new Vector3(2 - height, height, 2 - height));
                    if (flight.Age < 1.25f) continue;
                    flight.ShelfItem.SetActive(true);
                    RemoveProp(flight.Item);
                    flies.RemoveAt(j);
                    landed++;
                }
                yield return null;
            }
            for (float until = .4f; until > 0; until -= Time.deltaTime)
            {
                box.transform.localScale = new Vector3(1, Mathf.Max(.05f, box.transform.localScale.y - Time.deltaTime * 3), 1);
                yield return null;
            }
            RemoveProp(box);
            Stock(key, true);
            restocking = false;
            Run.Progress.Flags[ramen ? "ramenDone" : "milkDone"] = 1;
            CompleteTask(ramen ? "ramen" : "milk");
            Say("Đã xếp xong. Thùng rỗng gấp lại mang đi.", 2);
        }

        static Transform Block(Transform parent, string name, Vector3 position, Vector3 dimensions, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            var collider = block.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = dimensions;
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block.transform;
        }

        GameObject CreateProp(string type, Vector3 position, float maximumSize)
        {
            var entry = products ? products.Find(type) : null;
            if (entry == null || entry.prefabs.Length == 0) return null;
            var prop = Instantiate(entry.prefabs[RandomIndex(entry.prefabs.Length)]);
            var renders = prop.GetComponentsInChildren<Renderer>();
            if (renders.Length > 0)
            {
                var bounds = renders[0].bounds;
                for (int i = 1; i < renders.Length; ++i) bounds.Encapsulate(renders[i].bounds);
                prop.transform.localScale *= maximumSize / Mathf.Max(.001f, Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z));
            }
            prop.name = ProductName(type);
            prop.transform.position = ThreeCoords.ToUnity(position);
            return prop;
        }
    }
}
