using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;


namespace WakfuMod.Content.Projectiles
{
    public class PortalHomingGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public override void AI(Projectile projectile)
        {  // Excluir proyectiles del Cénit (ID 758)
            if (projectile.type == ModContent.ProjectileType<TymadorBomb>() || projectile.type >= 755 && projectile.type <= 763 ||
             projectile.type == ModContent.ProjectileType<Jalabola>())
            {
                return;
            }
            // Aplicar homing solo a proyectiles amigables que hayan pasado por un portal,
            // y mientras el timer sea mayor que cero.
            if (projectile.friendly && projectile.localAI[1] > 0f)
            {
                NPC target = FindClosestEnemy(projectile.Center, 700f);
                if (target != null)
                {
                    Vector2 desiredDirection = Vector2.Normalize(target.Center - projectile.Center);
                    float speed = projectile.velocity.Length();
                    // Usa un factor mayor para notar el efecto
                    projectile.velocity = Vector2.Lerp(projectile.velocity, desiredDirection * speed, 1f);
                }
                // Disminuye el timer para que, eventualmente, se detenga el homing.
                projectile.localAI[1] -= 1f; // Esto aplica homing durante 30 ticks si inicias en 30.
                projectile.netUpdate = true;
            }
        }

        private static NPC FindClosestEnemy(Vector2 pos, float maxRange)
        {
            NPC closest = null;
            float closestDist = maxRange;
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && npc.CanBeChasedBy(null))
                {
                    float dist = Vector2.Distance(pos, npc.Center);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closest = npc;
                    }
                }
            }
            return closest;
        }
    }
}
