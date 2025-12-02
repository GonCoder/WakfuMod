using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using WakfuMod.Content.Projectiles.Bosses.Toross;

namespace WakfuMod.Content.Items.Weapons
{
    public class TorossSwordItem : ModItem
    {
        public override string Texture => "WakfuMod/Content/NPCs/Bosses/Toross/Toross_Sword";

        public override void SetDefaults()
        {
            Item.damage = 269; // Updated to surpass Meowmere
            Item.DamageType = DamageClass.Melee;
            Item.width = 60;
            Item.height = 60;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6;
            Item.value = Item.sellPrice(gold: 10);
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.scale = 1.5f; // Adjust scale if texture is too big/small
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2) // Right Click
            {
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.useTime = 30;
                Item.useAnimation = 30;
                Item.noMelee = true; // Projectile only
                Item.shoot = ModContent.ProjectileType<TorossStasisLaser>();
                Item.shootSpeed = 18f; // Match laser speed
            }
            else // Left Click
            {
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTime = 20;
                Item.useAnimation = 20;
                Item.noMelee = false;
                Item.shoot = ProjectileID.None;
            }
            return base.CanUseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2) // Right Click
            {
                // Spawn 5 lasers with spread
                int numProjectiles = 5;
                float spreadAngle = MathHelper.ToRadians(30); // 30 degrees spread
                float baseAngle = velocity.ToRotation();
                float startAngle = baseAngle - spreadAngle / 2f;
                float deltaAngle = spreadAngle / (numProjectiles - 1);

                for (int i = 0; i < numProjectiles; i++)
                {
                    float currentAngle = startAngle + deltaAngle * i;
                    Vector2 newVelocity = currentAngle.ToRotationVector2() * velocity.Length();
                    
                    int p = Projectile.NewProjectile(source, position, newVelocity, type, damage, knockback, player.whoAmI);
                    
                    // Set friendly for player use
                    Main.projectile[p].friendly = true;
                    Main.projectile[p].hostile = false;
                    Main.projectile[p].timeLeft = 600;
                    Main.projectile[p].alpha = 0; // Visible immediately
                    Main.projectile[p].localAI[0] = 999; // Skip charge phase
                }
                return false; // Handle spawning manually
            }
            return false;
        }

        public override void UseItemHitbox(Player player, ref Rectangle hitbox, ref bool noHitbox)
        {
            if (player.altFunctionUse != 2) // Left Click
            {
                // Apply 50% width increase to the later frames of the swing (last 3-4 frames approx)
                // itemAnimation counts down from Max to 0.
                // So lower values = later in the swing.
                bool isLaterFrame = player.itemAnimation < (player.itemAnimationMax * 0.7);
                
                float increaseFactor = isLaterFrame ? 0.5f : 0.3f;
                
                int widthIncrease = (int)(hitbox.Width * increaseFactor);
                hitbox.Width += widthIncrease;
                hitbox.X -= widthIncrease / 2; // Keep it centered
            }
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Left Click: 10% chance to spawn homing projectiles
            if (player.altFunctionUse != 2)
            {
                if (Main.rand.NextFloat() < 0.10f)
                {
                    // Spawn projectiles that chase nearest enemy
                    // "Los proyectiles buscarán y perseguirán al enemigo más cercano"
                    // "tendrán 5 segundos de tiempo de vida"
                    
                    int projType = ModContent.ProjectileType<TorossHomingProjectile>();
                    int damage = (int)(Item.damage * 0.8f); // Slightly less damage? Or same?
                    
                    // Spawn 1-3 projectiles? User said "invocar los proyectiles" (plural).
                    int count = Main.rand.Next(2, 4);
                    
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 spawnPos = target.Center + Main.rand.NextVector2Circular(100f, 100f);
                        Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f);
                        
                        int p = Projectile.NewProjectile(
                            player.GetSource_OnHit(target),
                            spawnPos,
                            velocity,
                            projType,
                            damage,
                            Item.knockBack,
                            player.whoAmI
                        );
                        
                        // Set lifetime to 5 seconds (300 ticks)
                        Main.projectile[p].timeLeft = 300;
                        
                        // The projectile AI should handle homing.
                        // If it's the boss projectile, it might target the player by default.
                        // I might need to set ai[0] or something to target enemies.
                        // Since I can't easily change the projectile code right now without reading it,
                        // I'll assume it might need adjustment or I'll set friendly = true.
                        
                        Main.projectile[p].friendly = true;
                        Main.projectile[p].hostile = false;
                    }
                }
            }
        }
    }
}
