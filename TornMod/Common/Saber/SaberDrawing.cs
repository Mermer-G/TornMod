using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TornMod.Common.SaberCombat;

public static class SaberDrawing
{
    // Mirror the left-facing pose instead of rotating the right-facing sword upside down.
    public static bool FacesLeft(float angle) => MathF.Cos(angle) < 0f;
    public static SpriteEffects Effects(float angle) => FacesLeft(angle) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
    public static float Rotation(float angle) => angle + (FacesLeft(angle) ? MathHelper.Pi * 0.75f : MathHelper.PiOver4);
    public static Vector2 Grip(Texture2D texture, float angle) =>
        new(texture.Width * (FacesLeft(angle) ? 0.8f : 0.2f), texture.Height * 0.84f);
    // Mirror the arc horizontally on the left, keeping the same vertical curvature.
    public static float ArcRotation(float angle) => FacesLeft(angle) ? angle - MathHelper.Pi : angle;
    public static SpriteEffects ArcEffects(float angle) => FacesLeft(angle) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
}
