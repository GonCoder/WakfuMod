using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;

namespace WakfuMod.Content.Projectiles
{
    public class FecaGlyphProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Fireball; // Placeholder texture, invisible via Draw

        public override void SetDefaults()
        {
            Projectile.width = 192; // 12 tiles * 16 pixels (Default)
            Projectile.height = 192;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600; // 10 seconds
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            
            // Damage handling: Hit once per second per enemy
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60; // 60 ticks = 1 second
            
            Projectile.alpha = 255; // Invisible main sprite
        }
        
        public override void SetStaticDefaults()
        {
            // DisplayName handled in hjson
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (player.dead || !player.active)
            {
                Projectile.Kill();
                return;
            }

            // Empowerment Logic Check (First tick or change)
            bool isEmpowered = Projectile.ai[1] == 1;
            
            // Resize if empowered (run continuously to ensure hitbox stays correct if reset, though mostly once is enough)
            if (isEmpowered && Projectile.width != 240)
            {
                // Resize to 15x15 (240x240)
                // We need to keep the Center the same when resizing, usually.
                Vector2 center = Projectile.Center;
                Projectile.width = 240;
                Projectile.height = 240;
                Projectile.Center = center; 
            }

            // Update visual timer
            Projectile.ai[0]++;

            // "Traza unas runas ... en cada cubo"
            // Spawn dust to visualize the grid
            // Grid size: 12x12 or 15x15.
            int gridSize = isEmpowered ? 15 : 12;
            int width = gridSize * 16;
            
            // Intensity control: Don't spawn 144 dusts every frame.
            // Spawn random dusts within the grid cells to show activity.
            
            int dustCount = isEmpowered ? 8 : 5; // More dust if empowered

            for (int i = 0; i < dustCount; i++) // Spawn random runes per frame
            {
                int x = Main.rand.Next(gridSize);
                int y = Main.rand.Next(gridSize);
                
                Vector2 cellCenter = Projectile.position + new Vector2(x * 16 + 8, y * 16 + 8);
                
                int dustType = isEmpowered ? DustID.CursedTorch : DustID.Torch; // Green/Cursed fire for empowered? Or just MORE fire? 
                // "pon más efectos de fuego ... debuff fuego infernal" -> Maybe Inferno (Blue/Yellow) or Cursed (Green). Let's use Torch + SolarFlare for empowered.
                
                if (isEmpowered && Main.rand.NextBool(3)) dustType = DustID.SolarFlare;

                Dust d = Dust.NewDustPerfect(cellCenter, dustType, Vector2.Zero, 150, default, 1.5f);
                d.noGravity = true;
                d.velocity = Vector2.Zero;
                d.fadeIn = 0.5f;
            }

            // Outline logic (Optional, helps define the area)
            if (Projectile.ai[0] % 10 == 0)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    // Top and Bottom rows
                    Vector2 top = Projectile.position + new Vector2(x * 16 + 8, 8);
                    Vector2 bot = Projectile.position + new Vector2(x * 16 + 8, width - 8);
                    
                    int dustType = DustID.OrangeTorch;
                    if (isEmpowered) dustType = DustID.SolarFlare;

                    Dust d1 = Dust.NewDustPerfect(top, dustType, Vector2.Zero, 150, default, 1.2f);
                    d1.noGravity = true;
                    Dust d2 = Dust.NewDustPerfect(bot, dustType, Vector2.Zero, 150, default, 1.2f);
                    d2.noGravity = true;
                }
                for (int y = 0; y < gridSize; y++)
                {
                    // Left and Right cols
                    Vector2 left = Projectile.position + new Vector2(8, y * 16 + 8);
                    Vector2 right = Projectile.position + new Vector2(width - 8, y * 16 + 8);
                    
                    int dustType = DustID.OrangeTorch;
                    if (isEmpowered) dustType = DustID.SolarFlare;

                    Dust d3 = Dust.NewDustPerfect(left, dustType, Vector2.Zero, 150, default, 1.2f);
                    d3.noGravity = true;
                    Dust d4 = Dust.NewDustPerfect(right, dustType, Vector2.Zero, 150, default, 1.2f);
                    d4.noGravity = true;
                }
            }

            // Light
            Lighting.AddLight(Projectile.Center, isEmpowered ? 0.8f : 0.6f, 0.3f, 0.1f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
             if (Projectile.ai[1] == 1) // Empowered
             {
                 target.AddBuff(BuffID.CursedInferno, 180); // 3 seconds of Hellfire (Cursed Inferno is good match)
                 // Or BuffID.OnFire3 (Hellfire) for Terraria 1.4.4+
                 target.AddBuff(BuffID.OnFire3, 180);
             }
        }
    }
}
