using MonoMod.Cil;
using RainMeadow;
using RWCustom;
using System;
using UnityEngine;

namespace StoryMenagerie.Creatures
{
    class DropBugController : RainMeadow.CreatureController
    {
        private bool forceMove;
        private readonly DropBug dropbug;

        internal static void ApplyHooks()
        {
            On.DropBug.Update += (On.DropBug.orig_Update orig, DropBug self, bool eu) =>
            {
                if (creatureControllers.TryGetValue(self, out var p))
                {
                    p.Update(eu);
                    var old = self.AI.bug.abstractCreature.controlled;
                    self.AI.bug.abstractCreature.controlled = true;//глючное
                    orig(self, eu);
                    self.AI.bug.abstractCreature.controlled = old;
                }
                else
                {
                    orig(self, eu);
                }
            };
            On.DropBug.Act += (On.DropBug.orig_Act orig, DropBug self) =>
            {
                if (creatureControllers.TryGetValue(self, out var p))
                {
                    p.ConsciousUpdate();
                    var old = self.AI.bug.abstractCreature.controlled;
                    self.AI.bug.abstractCreature.controlled = true;//глючное
                    orig(self);
                    self.AI.bug.abstractCreature.controlled = old;
                }
                else
                {
                    orig(self);
                }
            };
            On.DropBugAI.Update += DropBugAI_Update;
            On.DropBugGraphics.RefreshColor += (On.DropBugGraphics.orig_RefreshColor orig, DropBugGraphics self, float timeStacker, RoomCamera.SpriteLeaser sLeaser) =>
            {
                orig(self, timeStacker, sLeaser);
                if (RainMeadow.CreatureController.creatureControllers.TryGetValue(self.bug, out var cc) && cc.isStory(out var scc))
                {
                    for (int i = 0; i < sLeaser.sprites.Length; i++)
                    {
                        sLeaser.sprites[i].color = scc.storyCustomization.eyeColor;
                    }
                    for (int j = 0; j < (sLeaser.sprites[self.ShineMeshSprite] as TriangleMesh).verticeColors.Length; j++)
                    {
                        (sLeaser.sprites[self.ShineMeshSprite] as TriangleMesh).verticeColors[j] = scc.storyCustomization.bodyColor;
                    }   
                }
            };
        }

        public override WorldCoordinate CurrentPathfindingPosition
        {
            get
            {
                if (!forceMove && Custom.DistLess(creature.coord, dropbug.AI.pathFinder.destination, 3))
                {
                    return dropbug.AI.pathFinder.destination;
                }
                return base.CurrentPathfindingPosition;
            }
        }

        private static void DropBugAI_Update(On.DropBugAI.orig_Update orig, DropBugAI self)
        {
            if (creatureControllers.TryGetValue(self.creature.realizedCreature, out var p))
            {
                var old = self.bug.abstractCreature.controlled;
                self.bug.abstractCreature.controlled = true;//глючное
                p.AIUpdate(self);
                orig(self);
                self.bug.abstractCreature.controlled = old;
            }
            else
            {
                orig(self);
            }
        }

        internal void ModifyBodyColor(RainMeadow.MeadowAvatarData self, ref Color ogColor)
        {
            if (self.skinData.baseColor.HasValue)
            {
                ogColor = self.skinData.baseColor.Value;
            }
            if (self.effectiveTintAmount > 0f)
            {
                var hslTint = RainMeadow.Extensions.ToHSL(self.tint);
                var hslOgColor = RainMeadow.Extensions.ToHSL(ogColor);
                ogColor = Color.Lerp(HSLColor.Lerp(hslOgColor, hslTint, self.effectiveTintAmount).rgb, Color.Lerp(ogColor, self.tint, self.effectiveTintAmount), 0.5f); // lerp in average of hsl and rgb, neither is good on its own
            }
        }

        public DropBugController(DropBug creature, RainMeadow.OnlineCreature oc, int playerNumber, CreatureCustomization customization) : base(creature, oc, playerNumber, new ExpandedAvatarData(customization))
        {
            this.dropbug = creature;
            //var c1 = this.dropbug.effectColor;
            //this.ModifyBodyColor(customization, ref c1);
        }

        public override void LookImpl(Vector2 pos)
        {
            //dropbug.AI.reactTarget = Custom.MakeWorldCoordinate(new IntVector2((int)(pos.x / 20f), (int)(pos.y / 20f)), this.dropbug.room.abstractRoom.index);
        }

        public override void Moving(float magnitude)
        {
            dropbug.AI.behavior = DropBugAI.Behavior.Hunt;
            forceMove = true;
        }

        public override void Resting()
        {
            dropbug.AI.behavior = DropBugAI.Behavior.Idle;
            forceMove = false;
        }

        public override void OnCall()
        {
            //truly
        }

        public override void PointImpl(Vector2 dir)
        {
            //uh
        }
    }
}
