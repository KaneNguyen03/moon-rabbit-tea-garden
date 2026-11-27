using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static VuonTra.EditorTools.RabbitRigSpec;

namespace VuonTra.EditorTools
{
    public static class RabbitViewFactory
    {
        [Serializable]
        sealed class ArmPivot
        {
            public string name;
            public float[] shoulder;
            public float[] paw;
        }

        [Serializable]
        sealed class Pivots
        {
            public float[] feet;
            public float[] hips;
            public float[] neck;
            public float[] crown;
            public float[] top;
            public bool lop;
            public ArmPivot[] arms;
        }

        const float GripLengthPx = 24f;

        static Vector2 V(float[] xy) => new Vector2(xy[0], xy[1]);

        public static RigView Build(RabbitKind kind, RigViewKind viewKind)
        {
            var pivots = JsonUtility.FromJson<Pivots>(File.ReadAllText(kind.Pivots(viewKind)));
            bool front = viewKind == RigViewKind.Front;
            string prefix = front ? "Front" : "Side";
            var bones = new List<RigBone>
            {
                new RigBone("Hips", null, V(pivots.feet), V(pivots.hips)),
                new RigBone("Spine", "Hips", V(pivots.hips), V(pivots.neck)),
                new RigBone("Neck", "Spine", V(pivots.neck), V(pivots.crown)),
            };
            bool ears = !kind.Lop;
            if (ears) bones.Add(new RigBone("EarRoot", "Neck", V(pivots.crown), V(pivots.top)));

            var parts = new List<RigPart>
            {
                new RigPart(prefix + "Body", front ? 2 : 1, "Spine", "Hips", pivots.hips[1] - 20f, pivots.hips[1] + 30f),
                new RigPart(prefix + "Head", 3, "Neck", upperBone: ears ? "EarRoot" : null, upperFrom: pivots.crown[1]),
            };
            var hands = new List<RigHand>();
            foreach (ArmPivot arm in pivots.arms)
            {
                string side = arm.name == "ArmL" ? "L" : "R";
                Vector2 paw = V(arm.paw);
                bones.Add(new RigBone("Shoulder" + side, "Spine", V(arm.shoulder), paw));
                bones.Add(new RigBone("Grip" + side, "Shoulder" + side, paw, paw + new Vector2(0f, GripLengthPx)));
                int armOrder = front ? (side == "L" ? 5 : 7) : 5;
                parts.Add(new RigPart(prefix + arm.name, armOrder, "Shoulder" + side));
                hands.Add(new RigHand(side, "Grip" + side, armOrder - 1));
            }

            return new RigView
            {
                Kind = viewKind,
                Name = ViewObject(viewKind),
                Key = ViewKey(viewKind),
                Psb = kind.Psb(viewKind),
                Bones = bones.ToArray(),
                Parts = parts.ToArray(),
                Hands = hands.ToArray(),
            };
        }
    }
}
