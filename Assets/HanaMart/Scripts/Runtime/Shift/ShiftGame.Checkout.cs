using System.Collections;
using System.Linq;
using HanaMart.Interaction;
using HanaMart.Store;
using UnityEngine;

namespace HanaMart.Shift
{
    public sealed partial class ShiftGame
    {
        Material bagMaterial, receiptMaterial;
        Mesh bagMesh;

        void FillCounter(Guest guest)
        {
            var types = guest.Profile.Items;
            if (types == null) return;
            for (int slot = 0; slot < types.Length; ++slot)
            {
                var entry = products ? products.Find(types[slot]) : null;
                var position = new Vector3(1.75f, 1.02f, 3.65f + slot * .14f);
                var root = new GameObject(ProductName(types[slot]));
                root.transform.position = ThreeCoords.ToUnity(position);
                root.transform.localRotation = Quaternion.Euler(0, -RandomValue() * .6f * Mathf.Rad2Deg, 0);
                if (entry != null && entry.prefabs.Length > 0)
                {
                    var item = Instantiate(entry.prefabs[RandomIndex(entry.prefabs.Length)], root.transform);
                    var renderers = item.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        var bounds = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; ++i) bounds.Encapsulate(renderers[i].bounds);
                        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                        float scale = .15f / Mathf.Max(size, .001f);
                        Vector3 center = root.transform.InverseTransformPoint(bounds.center);
                        item.transform.localScale *= scale;
                        item.transform.localPosition -= center * scale;
                        item.transform.localPosition += Vector3.up * (bounds.extents.y * scale);
                    }
                }
                guest.Items.Add(root);
            }
        }

        string ProductName(string type) => products?.Find(type)?.displayName ?? (type == "cig" ? "Thuốc lá" : type);

        public void ServeAtCounter()
        {
            if (!CanServe || Dialogue != null || Dead || Ended) return;
            var guest = counterCustomer;
            if (TryStoryCheckout(guest)) return;
            if (guest.Kind == "restroom")
            {
                ShowDialog(guest.Profile.Name, "Xin lỗi... cho tôi mượn nhà vệ sinh trong kho một chút được không? Gấp lắm.",
                    SaleChoice(guest, "Vâng, đi qua rèm nhựa sau quầy ạ.", CheckoutChoice.AllowStorage),
                    SaleChoice(guest, "Xin lỗi, cửa hàng không có nhà vệ sinh cho khách.", CheckoutChoice.DenyStorage));
                return;
            }
            if (guest.Profile.Heat && !guest.Heated)
            {
                if (carry.Kind == "hot") { guest.Heated = true; carry.SetCarry(null); }
                else if (!guest.HeatAsked)
                {
                    guest.HeatAsked = true;
                    Speak(guest, guest.Profile.Line);
                    ShowDialog(guest.Profile.Name, guest.Profile.Line, new Choice("Vâng, quý khách đợi chút ạ.", () =>
                    {
                        if (guest.Items.Count > 0) guest.Items[0].SetActive(false);
                        carry.FoodType = guest.Profile.Items[0];
                        carry.SetCarry("heat");
                        SceneAction?.Invoke("heatOrdered");
                    }));
                    return;
                }
                else { Say("Khách đang đợi món nóng (lò vi sóng ở quầy bên phải).", 3); return; }
            }
            StartCoroutine(Scan(guest));
        }

        IEnumerator Scan(Guest guest)
        {
            scanning = true;
            int count = guest.Items.Count;
            for (int index = 0; index < count; ++index)
            {
                var item = guest.Items[index];
                yield return MovePurchase(item, new Vector3(1.63f, 1.1f, 4.58f), .2f, .06f);
                PlayClip("beep");
                SceneAction?.Invoke("scanFlash");
                yield return MovePurchase(item, new Vector3(1.6f + index % 2 * .05f, 1.03f, 4.98f + index % 3 * .04f), .22f, .05f);
                yield return new WaitForSeconds(.08f);
                if (guest != counterCustomer || Dead) { scanning = false; yield break; }
            }
            PrintReceipt(guest);
            scanning = false;
            ShowPayment(guest);
        }

        static IEnumerator MovePurchase(GameObject item, Vector3 there, float duration, float height)
        {
            if (!item || !item.activeSelf) { yield return new WaitForSeconds(duration); yield break; }
            var from = item.transform.position;
            var to = ThreeCoords.ToUnity(there);
            for (float elapsed = 0; elapsed < duration && item; elapsed += Time.deltaTime)
            {
                item.transform.position = Placement.Arc(from, to, elapsed / duration, height);
                yield return null;
            }
            if (item) item.transform.position = to;
        }

        void PrintReceipt(Guest guest)
        {
            EnsureCheckoutMaterials();
            var bag = new GameObject("Túi hàng", typeof(MeshRenderer), typeof(MeshFilter));
            bag.GetComponent<MeshFilter>().sharedMesh = bagMesh;
            bag.GetComponent<MeshRenderer>().sharedMaterial = bagMaterial;
            bag.transform.position = ThreeCoords.ToUnity(1.62f, 1.13f, 5);
            guest.Items.Add(bag);

            var check = GameObject.CreatePrimitive(PrimitiveType.Quad);
            check.name = "Hóa đơn";
            Destroy(check.GetComponent<Collider>());
            check.GetComponent<MeshRenderer>().sharedMaterial = receiptMaterial;
            check.transform.SetPositionAndRotation(ThreeCoords.ToUnity(1.6f, 1.16f, 4.08f), Quaternion.Euler(-.5f * Mathf.Rad2Deg, -90, 0));
            check.transform.localScale = new Vector3(.06f, .001f, 1);
            guest.Items.Add(check);
            PlayClip("printer");
            StartCoroutine(Print(check.transform));
        }

        void EnsureCheckoutMaterials()
        {
            if (bagMaterial) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            bagMaterial = new Material(lit) { color = new Color(244 / 255f, 246 / 255f, 248 / 255f, .82f), renderQueue = 3000 };
            bagMaterial.SetFloat("_Surface", 1);
            bagMaterial.SetFloat("_SrcBlend", 5);
            bagMaterial.SetFloat("_DstBlend", 10);
            bagMaterial.SetFloat("_ZWrite", 0);
            bagMaterial.SetFloat("_Cull", 0);
            bagMaterial.SetFloat("_Smoothness", .55f);
            bagMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            receiptMaterial = new Material(lit) { color = new Color32(242, 239, 230, 255) };
            receiptMaterial.SetFloat("_Cull", 0);
            receiptMaterial.SetFloat("_Smoothness", .2f);
            const int sides = 14;
            var vertices = new Vector3[sides * 2];
            var triangles = new int[sides * 6];
            for (int side = 0; side < sides; ++side)
            {
                float angle = side * (2 * Mathf.PI / sides);
                var spoke = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                int a = side * 2;
                int b = (a + 2) % vertices.Length;
                vertices[a] = spoke * .1f - Vector3.up * .11f;
                vertices[a + 1] = spoke * .13f + Vector3.up * .11f;
                int index = side * 6;
                triangles[index] = a;
                triangles[index + 1] = a + 1;
                triangles[index + 2] = b;
                triangles[index + 3] = a + 1;
                triangles[index + 4] = b + 1;
                triangles[index + 5] = b;
            }
            bagMesh = new Mesh { name = "HanaMart shopping bag" };
            bagMesh.vertices = vertices;
            bagMesh.triangles = triangles;
            bagMesh.RecalculateNormals();
        }

        static IEnumerator Print(Transform check)
        {
            var top = check.position;
            for (float elapsed = 0; elapsed < .9f && check; elapsed += Time.deltaTime)
            {
                float height = Mathf.Lerp(.002f, .2f, elapsed / .9f);
                check.localScale = new Vector3(.06f, height, 1);
                check.position = top - check.up * height * .5f;
                yield return null;
            }
            if (check)
            {
                check.localScale = new Vector3(.06f, .2f, 1);
                check.position = top - check.up * .1f;
            }
        }

        void ShowPayment(Guest guest)
        {
            string amount = PriceText(CheckoutRules.Total(guest.Profile.Items));
            string productsList = string.Join(", ", guest.Profile.Items.Select(ProductName));
            if (productsList.Length > 26) productsList = productsList.Substring(0, 26);
            PosDisplay = $"HANA POS\n{productsList}\n{amount}";
            var charge = SaleChoice(guest, "Tính tiền: " + amount, CheckoutChoice.Charge);
            var broken = SaleChoice(guest, "Xin lỗi, máy tính tiền bị hỏng rồi ạ.", CheckoutChoice.RegisterBroken);
            var sendHome = SaleChoice(guest, "Muộn rồi, em về nhà đi.", CheckoutChoice.SendHome);
            string name = guest.Profile.Name;
            switch (guest.Kind)
            {
                case "raincoatBad":
                    ShowDialog(name, "(Người áo mưa vàng đặt cơm cuộn và đồ tươi lên quầy. Không nói gì. Nước mưa nhỏ giọt dù ngoài trời...)", charge, broken);
                    break;
                case "raincoatOk":
                    ShowDialog(name, "(Người áo mưa vàng chỉ mua hai ly mì. Áo mưa khô cong.)", charge);
                    break;
                case "raincoatChild":
                    ShowDialog(name, "(Một đứa trẻ mặc áo mưa vàng, ướt sũng, đặt một ly mì lên quầy.) ...Mẹ bảo con mua mì. Chỉ mì thôi.", charge, sendHome, broken);
                    break;
                case "noShadow":
                    ShowDialog(name, "(Một vị khách bình thường. Đèn trên đầu họ chập chờn. Họ đứng im chờ, không chớp mắt.)", charge, broken);
                    break;
                case "childMom":
                    ShowDialog(name, "Anh bán cho em hộp bánh mandu này nha. Mẹ em đứng ngoài kia kìa. (Em chỉ ra cửa kính.)", charge, sendHome);
                    break;
                case "childParent":
                    ShowDialog(name, "(Một em bé đặt snack và bánh mandu lên quầy. Mẹ em đứng ngay sau, gật đầu với bạn.)", charge, sendHome);
                    break;
                case "loneChild":
                    ShowDialog(name, "Anh ơi bán cho em cái này đi. Mẹ em... mẹ em đang đợi ở ngoài.", charge, sendHome);
                    break;
                default:
                    ShowDialog(name, guest.Profile.Line ?? "Cảm ơn.", SaleChoice(guest, $"Tính tiền {amount} · Cảm ơn quý khách!", CheckoutChoice.Charge));
                    break;
            }
        }

        Choice SaleChoice(Guest guest, string label, CheckoutChoice choice) => new(label, () => ApplyCheckout(guest, choice));

        void ApplyCheckout(Guest guest, CheckoutChoice choice)
        {
            if (guest != counterCustomer || !guest.Actor.alive) return;
            var (effect, rule) = CheckoutRules.Decide(guest.Kind, choice);
            if (effect == CheckoutEffect.Die)
            {
                Die(rule, rule switch
                {
                    3 => "Bạn đã bán cho người áo mưa vàng thứ không phải mì ly.",
                    15 => "Vị khách đó không có bóng dưới chân. Bạn đã bán hàng cho thứ không phải người.",
                    _ => "Đứa trẻ đi một mình. Thứ đứng ngoài cửa kính không phải mẹ nó — và giờ nó biết bạn đã tin.",
                });
                return;
            }
            guest.Paid = true;
            if (effect == CheckoutEffect.Sale)
            {
                ++TotalSold;
                if (guest.Kind != "raincoatOk") PlayClip("cash");
                if (guest.Kind == "raincoatChild") Speak(guest, "Cảm ơn anh. Anh đọc nội quy kỹ lắm.");
                if (guest.Kind == "childParent") Say("Em bé: Cảm ơn chú!");
                if (guest.Heated) SceneAction?.Invoke("heatSold");
                return;
            }
            if (effect == CheckoutEffect.Strike)
            {
                if (guest.Kind == "restroom")
                {
                    guest.IntoStorage = true;
                    Run.Progress.Flags["strangerIn"] = 1;
                    Strike(9, "Bạn đã cho người khác vào kho. Không ai được vào kho, kể cả khách.");
                }
                else if (guest.Kind == "loneChild")
                {
                    Strike(4, "Bạn đã bán đồ cho trẻ em đi một mình sau 01:00.");
                    Speak(guest, "Hì hì. Cảm ơn anh. Mẹ sẽ vui lắm.");
                }
                else
                {
                    if (choice == CheckoutChoice.SendHome)
                    {
                        guest.Actor.Motion.revealTarget = .7f;
                        guest.Actor.Motion.headSpin = Mathf.PI;
                        Later(2.5f, () => { if (guest.Actor) { guest.Actor.Motion.revealTarget = 0; guest.Actor.Motion.headSpin = 0; } });
                    }
                    Strike(3, choice == CheckoutChoice.SendHome
                        ? "Điều 3 có số nhỏ hơn điều 4: khách áo mưa vàng mua mì ly PHẢI được phục vụ."
                        : "Em chỉ mua mì ly — bạn không được từ chối khách áo mưa vàng.");
                }
                return;
            }
            if (effect == CheckoutEffect.Complain)
            {
                Complain();
                Say("Người mẹ: Con tôi đi cùng tôi mà? Cửa hàng gì kỳ vậy.");
            }
            if (guest.Kind == "raincoatBad")
            {
                Speak(guest, "...Hỏng à.");
                guest.Actor.Motion.revealTarget = .6f;
                Later(3, () => { if (guest.Actor) guest.Actor.Motion.revealTarget = .15f; });
            }
            if (guest.Kind == "noShadow") { guest.Actor.Motion.revealTarget = .5f; Speak(guest, "...Lần sau."); }
            if (guest.Kind == "loneChild") Speak(guest, "...Vâng.");
            if (guest.Kind == "restroom") Speak(guest, "Vậy à... thôi được.");
            if (guest.Kind == "childMom")
            {
                Speak(guest, "...Mẹ sẽ giận anh đấy.");
                var mother = guest.Companion;
                if (mother != null && mother.Actor)
                {
                    mother.Actor.Motion.revealTarget = 1;
                    Later(2.5f, () => RemoveGuest(mother));
                }
            }
        }

        void ReleaseCheckoutMaterials()
        {
            if (bagMesh) Destroy(bagMesh);
            if (bagMaterial) Destroy(bagMaterial);
            if (receiptMaterial) Destroy(receiptMaterial);
        }
    }
}
