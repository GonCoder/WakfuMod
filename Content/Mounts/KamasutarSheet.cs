using Terraria;
using Terraria.ModLoader;
using WakfuMod.Content.Buffs; // Asegúrate que el namespace es correcto para tu buff
using Terraria.ID; // Para Main.netMode, NetmodeID.Server, DustID
using Microsoft.Xna.Framework.Graphics; // Para Texture2D
using System.Linq;
using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria.DataStructures;
using ReLogic.Content; // Para Enumerable.Repeat

namespace WakfuMod.Content.Mounts
{
    public class KamasutarSheet : ModMount
    {
        // --- Propiedad para la Textura Principal ---
        // public override string Texture => "WakfuMod/Assets/Mounts/KamasutarSheet"; // Ruta a TU textura, SIN .png

        public override void SetStaticDefaults()
        {
            MountData.buff = ModContent.BuffType<KamasutarMountBuff>();
            MountData.heightBoost = 16;
            MountData.fallDamage = 0.2f;
            MountData.runSpeed = 3f;
            MountData.dashSpeed = MountData.runSpeed;
            MountData.flightTimeMax = 0;
            MountData.fatigueMax = 0;
            MountData.jumpHeight = 12;
            MountData.acceleration = 0.8f;
            MountData.jumpSpeed = 6.2f;
            MountData.blockExtraJumps = false;
            MountData.constantJump = true;
            MountData.totalFrames = 10; // 0-9

            MountData.playerYOffsets = new int[10] { /* TUS 10 OFFSETS AQUÍ */
                18, 18, 20, 21, 22, 23, 22, 21, 20, 19
            };

            MountData.xOffset = 0;
            MountData.bodyFrame = 3;
            MountData.yOffset = 6;
            MountData.playerHeadOffset = 0;

            MountData.standingFrameStart = 0;
            MountData.standingFrameCount = 2;
            MountData.standingFrameDelay = 12;
            MountData.idleFrameStart = 0;
            MountData.idleFrameCount = 2;
            MountData.idleFrameDelay = 14;
            MountData.idleFrameLoop = true;
            MountData.runningFrameStart = 2;
            MountData.runningFrameCount = 8;
            MountData.runningFrameDelay = 8;
            MountData.inAirFrameStart = 2;
            MountData.inAirFrameCount = 8;
            MountData.inAirFrameDelay = MountData.runningFrameDelay;
            MountData.swimFrameStart = 2;
            MountData.swimFrameCount = 8;
            MountData.swimFrameDelay = MountData.runningFrameDelay;
            MountData.spawnDust = DustID.Sand;

            if (Main.netMode != NetmodeID.Server)
            {
                MountData.textureWidth = MountData.backTexture.Width();
                MountData.textureHeight = MountData.backTexture.Height();
            }
        }




        public override void UpdateEffects(Player player)
        {
            player.statDefense += 20; // --- AÑADIDA DEFENSA --- (Ajusta el valor)

            // Ejemplo de polvo al correr (adaptado del ExampleMount)
            if (Math.Abs(player.velocity.X) > MountData.runSpeed * 0.5f)
            {
                Rectangle rect = player.getRect();
                // Desplazar el polvo a los pies de la montura
                Vector2 dustPos = player.BottomLeft + new Vector2(Main.rand.NextFloat(player.width), -Main.rand.NextFloat(4, 8));
                if (player.direction == -1) dustPos.X -= 6;


                if (Main.rand.NextBool(4)) // No generar polvo en cada frame
                    Dust.NewDustPerfect(dustPos, MountData.spawnDust, new Vector2(player.velocity.X * 0.2f, Main.rand.NextFloat(-1, -0.5f)), 100, default, 1.2f);
            }
        }

        //     // --- MÉTODO DRAW PARA FORZAR EL DIBUJADO DE LA TEXTURA PRINCIPAL ---
        //     public override bool Draw(List<DrawData> playerDrawData, int drawType, Player drawPlayer,
        //                     ref Texture2D texture, ref Texture2D glowTexture, ref Vector2 drawPosition,
        //                     ref Rectangle frame, ref Color drawColor, ref Color glowColor,
        //                     ref float rotation, ref SpriteEffects spriteEffects, ref Vector2 drawOrigin,
        //                     ref float drawScale, float shadow)
        //     {
        //         // El sistema vanilla DEBERÍA haberte pasado tu textura principal en el parámetro 'texture'
        //         // y el 'frame' (sourceRectangle) correcto para el frame de animación actual de la montura.
        //         // 'drawPosition' es donde se va a dibujar el JUGADOR.

        //         // Si 'texture' es null o es una dummy texture, la carga automática falló.
        //         if (texture == null || texture.Name.Contains("Dummy"))
        //         {
        //             // Intenta cargarla explícitamente como fallback
        //             Asset<Texture2D> explicitAsset = ModContent.Request<Texture2D>(this.Texture);
        //             if (explicitAsset.IsLoaded)
        //             {
        //                 texture = explicitAsset.Value;
        //             }
        //             else
        //             {
        //                 Mod.Logger.Warn($"Draw: Main mount texture for {Name} is STILL null or dummy, and explicit load failed. Cannot draw.");
        //                 return true; // No podemos dibujar, dejar que el sistema lo intente (aunque probablemente falle)
        //             }
        //         }

        //         // Si llegamos aquí, 'texture' debería ser tu spritesheet de montura.
        //         // 'frame' debería ser el sourceRectangle correcto para el frame actual de la montura.
        //         // 'drawPosition' es el punto de anclaje para el DIBUJADO DEL JUGADOR.

        //         // Origen de la montura: Centro X, Base Y del FRAME de la montura.
        //         // Usamos MountData.textureWidth y MountData.textureHeight porque deberían estar correctamente
        //         // calculados en SetStaticDefaults para representar las dimensiones de UN SOLO frame.
        //         Vector2 mountOrigin = new Vector2(MountData.textureWidth / 2f, MountData.textureHeight);

        //         // Posición de dibujado de la montura:
        //         // Queremos que el 'mountOrigin' (pies/base de la montura) se alinee con
        //         // la posición donde se dibuja el jugador ('drawPosition'), pero ajustado para que
        //         // la montura quede debajo del jugador.
        //         // 'drawPosition' ya incluye los playerYOffsets.
        //         // Si nuestro origen es la base de la montura, y drawPosition es donde deberían estar los pies del jugador,
        //         // entonces están casi alineados. Se pueden hacer pequeños ajustes.
        //         Vector2 finalDrawPosition = drawPosition;
        //         // finalDrawPosition.Y += algunos_pixeles; // Si necesitas bajar más la montura
        //         // finalDrawPosition.X += algunos_pixeles; // Si necesitas moverla horizontalmente

        //         spriteEffects = drawPlayer.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        //         playerDrawData.Add(new DrawData(
        //             texture,           // La textura principal de la montura
        //             finalDrawPosition,
        //             frame,             // El sourceRectangle del frame actual de la montura, pasado por el juego
        //             drawColor,
        //             rotation,          // La rotación calculada por el juego
        //             mountOrigin,       // Nuestro origen para el sprite de la montura
        //             drawScale,         // La escala calculada por el juego
        //             spriteEffects,
        //             0f
        //         ));

        //         // Mod.Logger.Info($"Added DrawData for {Name}. Type: {drawType}, Tex: {texture.Name}, Pos: {finalDrawPosition}, Frame: {frame}, Origin: {mountOrigin}");

        //         return false; // Hemos dibujado la textura principal de la montura, no dejar que Terraria lo intente.
        //     }

    }
}