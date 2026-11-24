using HanaMart.Render;
using NUnit.Framework;
using UnityEngine;

namespace HanaMart.Tests
{
    public class AgxTests
    {
        // Số tham chiếu tạo bằng tools/three-export/agx_ref.js (công thức AgX của three.js r160, exposure 1.02)
        static readonly object[] Cases =
        {
            new object[] { new Vector3(0, 0, 0), new Vector3(0f, 0f, 0f) },
            new object[] { new Vector3(0.18f, 0.18f, 0.18f), new Vector3(0.217949f, 0.217902f, 0.217899f) },
            new object[] { new Vector3(1, 1, 1), new Vector3(0.594903f, 0.594809f, 0.594775f) },
            new object[] { new Vector3(4, 2, 1), new Vector3(0.886005f, 0.74889f, 0.637419f) },
            new object[] { new Vector3(0.05f, 0.1f, 0.3f), new Vector3(0.073643f, 0.154119f, 0.337925f) },
            new object[] { new Vector3(16, 16, 16), new Vector3(0.996978f, 0.996879f, 0.996779f) },
        };

        [TestCaseSource(nameof(Cases))]
        public void AgxMatchesThreeReference(Vector3 input, Vector3 expected)
        {
            var o = Agx.Tonemap(input, 1.02f);
            Assert.AreEqual(expected.x, o.x, 0.002f); Assert.AreEqual(expected.y, o.y, 0.002f); Assert.AreEqual(expected.z, o.z, 0.002f);
        }
    }
}
