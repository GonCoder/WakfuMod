// using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Graphics;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using Terraria;
// using Terraria.ModLoader;
// using WakfuMod.Content.Projectiles; // Asegúrate que la ruta es correcta

// namespace WakfuMod.ModSystems
// {
//     public class TymadorCableDrawer : ModSystem
//     {
//         private Texture2D cableTexture;

//         public override void Load()
//         {
//             if (!Main.dedServ)
//             {
//                 // Carga inmediata para asegurar que esté disponible en el primer dibujado
//                 cableTexture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/TymadorCable", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
//             }
//         }

//         public override void Unload()
//         {
//             cableTexture = null;
//         }

//         public override void PostDrawTiles()
//         {
//             if (cableTexture == null || Main.gameMenu) return; // No dibujar si no hay textura o estamos en el menú

//             // Usar la lista del manager es más fiable si está actualizada
//             // Filtrar por bombas del jugador local para evitar dibujar cables de otros en MP (a menos que quieras)
//             var playerActiveBombs = TymadorBombManager.ActiveBombs
//                                         .Where(p => p != null && p.active && p.owner == Main.myPlayer)
//                                         .OrderBy(p => p.ai[1]) // Ordenar por el ID de secuencia
//                                         .ToList();

//             // Necesitamos al menos 2 bombas para dibujar un cable
//             if (playerActiveBombs.Count < 2) return;

//             Main.spriteBatch.Begin(
//                 SpriteSortMode.Deferred,
//                 BlendState.AlphaBlend,
//                 SamplerState.LinearWrap, // CAMBIADO a LinearWrap o PointWrap si prefieres tiling exacto sin suavizado
//                 DepthStencilState.None,
//                 RasterizerState.CullNone,
//                 null,
//                 Main.GameViewMatrix.TransformationMatrix
//             );

//             // Dibujar cables entre bombas consecutivas (máximo 2 cables para 3 bombas)
//             // Iteramos hasta Count - 1 para tener pares (0,1), (1,2)
//             // El límite '&& i < 2' asegura que solo se dibujen para las 3 primeras bombas si hay más
//             for (int i = 0; i < playerActiveBombs.Count - 1 && i < 2; i++)
//             {
//                 DrawCableBetween(playerActiveBombs[i], playerActiveBombs[i + 1]);
//             }

//             Main.spriteBatch.End();
//         }

//         // Método revisado para dibujar un cable segmentado/tileado
//         private void DrawCableBetween(Projectile bombA, Projectile bombB)
//         {
//             Vector2 startPoint = bombA.Center;
//             Vector2 endPoint = bombB.Center;
//             Vector2 direction = endPoint - startPoint;
//             float totalDistance = direction.Length();
//             float rotation = direction.ToRotation();

//             // Asumimos que la textura del cable es HORIZONTAL
//             // Si es vertical, intercambia Width y Height en los cálculos
//             float segmentLength = cableTexture.Width; // Longitud de un segmento de cable
//             float segmentHeight = cableTexture.Height; // Alto del cable

//             // Evitar dibujar si la distancia es muy pequeña o el segmento no tiene tamaño
//             if (totalDistance < 1f || segmentLength <= 0) return;

//             Vector2 normalizedDirection = Vector2.Normalize(direction);

//             // Origen del dibujado: Centro-Izquierda de la textura para rotar correctamente
//             Vector2 origin = new Vector2(0, segmentHeight / 2f);

//             // Dibujar segmentos completos
//             float currentDistance = 0;
//             while (currentDistance + segmentLength <= totalDistance)
//             {
//                 // Calcular posición del inicio del segmento actual (en coordenadas del mundo)
//                 Vector2 segmentPosition = startPoint + normalizedDirection * currentDistance;

//                 Main.spriteBatch.Draw(
//                     cableTexture,
//                     segmentPosition - Main.screenPosition, // Convertir a coordenadas de pantalla
//                     null,                   // Dibujar textura completa
//                     Color.White * 0.85f,     // Color (ligeramente transparente)
//                     rotation,               // Rotación del cable
//                     origin,                 // Origen para la rotación
//                     1f,                     // Escala normal (sin estirar)
//                     SpriteEffects.None,
//                     0f
//                 );
//                 currentDistance += segmentLength;
//             }

//             // Dibujar el último segmento parcial si es necesario
//             float remainingDistance = totalDistance - currentDistance;
//             if (remainingDistance > 0.1f) // Un pequeño umbral para evitar dibujar fragmentos minúsculos
//             {
//                 // Calcular posición del inicio del último segmento
//                 Vector2 segmentPosition = startPoint + normalizedDirection * currentDistance;

//                 // Crear un rectángulo fuente para dibujar solo la parte necesaria de la textura
//                 Rectangle sourceRect = new Rectangle(0, 0, (int)remainingDistance, (int)segmentHeight);

//                 Main.spriteBatch.Draw(
//                     cableTexture,
//                     segmentPosition - Main.screenPosition, // Convertir a coordenadas de pantalla
//                     sourceRect,             // <<< Usar el rectángulo fuente
//                     Color.White * 0.85f,
//                     rotation,
//                     origin,
//                     1f,                     // Escala normal
//                     SpriteEffects.None,
//                     0f
//                 );
//             }
//         }
//     }
// }
// Arriba es para una imagen estática
// AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA

// ModSystems/TymadorCableDrawer.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using WakfuMod.Content.Projectiles; // Asegúrate que la ruta es correcta

namespace WakfuMod.ModSystems
{
    public class TymadorCableDrawer : ModSystem
    {
        private Texture2D cableTexture;

        // --- NUEVO: Variables de Animación ---
        private static int currentFrame = 0;       // El frame actual (0 a 4)
        private static int frameCounter = 0;     // Temporizador para cambiar de frame
        private const int FrameSpeed = 6;        // Ticks que dura cada frame (ajusta velocidad)
        private const int TotalFrames = 5;       // Número total de frames en tu spritesheet
         private const int BottomPadding = 2; // Padding SOLO debajo de cada frame

        public override void Load()
        {
            if (!Main.dedServ)
            {
                cableTexture = ModContent.Request<Texture2D>("WakfuMod/Content/Projectiles/TymadorCable-Sheet", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
        }

        public override void Unload()
        {
            cableTexture = null;
        }

        public override void PostDrawTiles()
        {
            if (cableTexture == null || Main.gameMenu) return;

            // --- NUEVO: Actualizar Animación ---
            // Avanza el contador de frame
            // Actualizar Animación
            frameCounter++;
            if (frameCounter >= FrameSpeed) {
                frameCounter = 0;
                currentFrame = (currentFrame + 1) % TotalFrames;
            }

            var playerActiveBombs = TymadorBombManager.ActiveBombs
                                        .Where(p => p != null && p.active && p.owner == Main.myPlayer)
                                        .OrderBy(p => p.ai[1])
                                        .ToList();

            if (playerActiveBombs.Count < 2) return;

          // --- USA PointClamp ---
            Main.spriteBatch.Begin( SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix );

            // Dibujar cables
            for (int i = 0; i < playerActiveBombs.Count - 1 && i < 2; i++)
            {
                // Pasa la textura y el frame actual a la función de dibujo
                DrawCableBetween(playerActiveBombs[i], playerActiveBombs[i + 1], cableTexture, currentFrame);
            }

            Main.spriteBatch.End();
        }

        
       // --- MÉTODO REVISADO CON OFFSET EN SOURCE RECT ---
        private void DrawCableBetween(Projectile bombA, Projectile bombB, Texture2D texture, int frameIndex)
        {
            Vector2 startPoint = bombA.Center;
            Vector2 endPoint = bombB.Center;
            Vector2 directionVector = endPoint - startPoint;
            float totalDistance = directionVector.Length();
            float rotation = directionVector.ToRotation();

            // --- Cálculo de Dimensiones (Vertical con Padding Inferior) ---
            int totalSlotHeight = texture.Height / TotalFrames;
            int frameContentHeight = totalSlotHeight - BottomPadding; // Altura visible
            int frameWidth = texture.Width;

            if (totalDistance < 1f || frameContentHeight <= 0 || frameWidth <= 0) return;

            // --- Rectángulo Fuente BASE para el Frame VISIBLE (AJUSTADO) ---
            // Calcula la Y inicial del SLOT (incluye padding superior si hubiera, pero dijiste que no hay)
            int startYOfSlot = frameIndex * totalSlotHeight;

            // --- AJUSTE CLAVE: Asegurar que empezamos DENTRO del frame visible ---
            // Si hay un pequeño error de redondeo o el sampler es quisquilloso,
            // empezar exactamente en startYOfSlot PUEDE tomar píxeles del padding inferior del frame ANTERIOR.
            // Empezamos 1 píxel más abajo para estar seguros DENTRO del frame actual.
            // Esto asume que NO hay padding superior. Si lo hubiera, sería startYOfSlot + PaddingSuperior.
            int frameContentY_StartPixel = startYOfSlot; // Asume que el frame empieza en Y=0 del slot

            // Clamp para evitar salirse de la textura (importante)
             frameContentY_StartPixel = Math.Clamp(frameContentY_StartPixel, 0, texture.Height - frameContentHeight);

            // El rectángulo fuente ahora usa esta Y inicial y la altura del contenido
            Rectangle baseVisibleSourceRect = new Rectangle(
                0,                      // X = 0
                frameContentY_StartPixel, // Y inicial del contenido visible
                frameWidth,             // Ancho completo
                frameContentHeight      // Alto del contenido visible
            );

            // --- Origen, Color, Flip (Igual que antes) ---
            Vector2 origin = new Vector2(0, frameContentHeight / 2f); // Centro-Izquierda del contenido
            Color drawColor = Color.White * 0.85f;
            SpriteEffects effects = SpriteEffects.None;
            float wrappedRotation = MathHelper.WrapAngle(rotation);
            if (wrappedRotation > MathHelper.PiOver2 || wrappedRotation < -MathHelper.PiOver2) {
                effects = SpriteEffects.FlipHorizontally;
                rotation += MathHelper.Pi;
                origin = new Vector2(frameWidth, frameContentHeight / 2f); // Origen Centro-Derecha
            }

            // --- Dibujado Segmentado ---
            Vector2 normalizedDirection = Vector2.Normalize(directionVector);
            float segmentLength = frameWidth;
            float currentDistanceDrawn = 0f;
            float scale = 0.6f;

            while (currentDistanceDrawn < totalDistance)
            {
                 Vector2 segmentDrawPosition = startPoint + normalizedDirection * currentDistanceDrawn;
                 float remainingTotalDistance = totalDistance - currentDistanceDrawn;
                 int widthToDrawFromTexture = (int)Math.Min(segmentLength, remainingTotalDistance);

                 if (widthToDrawFromTexture <= 0) break;

                 // Crea el rectángulo fuente recortando el ANCHO
                 Rectangle currentSourceRect = new Rectangle(
                     baseVisibleSourceRect.X, // X del frame visible (0)
                     baseVisibleSourceRect.Y, // Y del frame visible (calculada arriba)
                     widthToDrawFromTexture,  // Ancho a dibujar
                     baseVisibleSourceRect.Height // Alto completo del contenido visible
                 );

                 Vector2 screenPos = segmentDrawPosition - Main.screenPosition;

                 Main.spriteBatch.Draw(
                     texture,
                     screenPos,
                     currentSourceRect,
                     drawColor,
                     rotation,
                     origin,
                     scale,
                     effects,
                     0f
                 );

                 currentDistanceDrawn += widthToDrawFromTexture;
                 if (segmentLength <= 0) break;
            }
        } // Fin DrawCableBetween
    } // Fin Clase
}
