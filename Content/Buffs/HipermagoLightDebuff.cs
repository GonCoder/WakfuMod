using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace WakfuMod.Content.Buffs
{
    public class HipermagoLightDebuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // Este es un debuff negativo
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // Marcar que el NPC tiene el debuff de luz (armadura anulada)
            npc.GetGlobalNPC<HipermagoLightDebuffNPC>().hasLightDebuff = true;
        }
    }

    public class HipermagoLightDebuffNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool hasLightDebuff = false;
        private int _animFrame = 0;
        private int _animTimer = 0;
        private const int FRAMES_COUNT = 3;
        private const int TICKS_PER_FRAME = 8;

        public override void ResetEffects(NPC npc)
        {
            hasLightDebuff = false;
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            // Si tiene el debuff de luz, ignora toda la defensa
            if (hasLightDebuff)
            {
                modifiers.ScalingArmorPenetration += 1f; // 100% armor penetration
            }
        }

        public override void PostAI(NPC npc)
        {
            if (hasLightDebuff)
            {
                // Actualizar animación
                _animTimer++;
                if (_animTimer >= TICKS_PER_FRAME)
                {
                    _animTimer = 0;
                    _animFrame = (_animFrame + 1) % FRAMES_COUNT;
                }

                // Partículas de luz ocasionales
                if (Main.rand.NextBool(10))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height,
                        DustID.GoldFlame, 0f, -1f, 150, default, 0.8f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!hasLightDebuff) return;

            // Dibujar efecto de luz sobre el NPC
            var texture = ModContent.Request<Texture2D>("WakfuMod/Content/Buffs/HipermagoLightEffect").Value;
            
            int frameHeight = texture.Height / FRAMES_COUNT;
            Rectangle sourceRect = new Rectangle(0, _animFrame * frameHeight, texture.Width, frameHeight);

            // Escalar al tamaño del NPC
            float scaleX = (float)npc.width / texture.Width;
            float scaleY = (float)npc.height / frameHeight;
            Vector2 scale = new Vector2(scaleX, scaleY);

            // Posición centrada en el NPC
            Vector2 drawPos = npc.Center - screenPos;
            Vector2 origin = new Vector2(texture.Width / 2f, frameHeight / 2f);

            // Dibujar con transparencia
            Color lightColor = Color.White * 0.6f;
            spriteBatch.Draw(texture, drawPos, sourceRect, lightColor, 0f, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
