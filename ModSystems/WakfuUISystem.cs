using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;
using WakfuMod.jugador;
using System.Collections.Generic;
using Terraria.Audio;

namespace WakfuMod.ModSystems
{
    public class WakfuUISystem : ModSystem
    {
        internal BalanceToggleUI balanceToggleUI;
        internal UserInterface balanceToggleInterface;

        // --- NUEVO: UI de Cooldown Aniripsa ---
        internal AniripsaCooldownUI aniripsaCooldownUI;
        internal UserInterface aniripsaCooldownInterface;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                // Balance UI
                balanceToggleUI = new BalanceToggleUI();
                balanceToggleUI.Activate();
                balanceToggleInterface = new UserInterface();
                balanceToggleInterface.SetState(balanceToggleUI);
                
                // Aniripsa UI
                aniripsaCooldownUI = new AniripsaCooldownUI();
                aniripsaCooldownUI.Activate();
                aniripsaCooldownInterface = new UserInterface();
                aniripsaCooldownInterface.SetState(aniripsaCooldownUI);
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.playerInventory)
            {
                // Balance UI logic (keeps existing logic)
                if (balanceToggleInterface != null)
                {
                    Player player = Main.LocalPlayer;
                    if (player != null && player.active)
                    {
                        WakfuPlayer wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
                        if (!(wakfuPlayer.claseElegida == WakfuClase.Xelor || 
                              wakfuPlayer.claseElegida == WakfuClase.Hipermago || 
                              wakfuPlayer.claseElegida == WakfuClase.Uginak))
                        {
                            balanceToggleInterface.Update(gameTime);
                        }
                    }
                }
            }

            // Aniripsa UI Logic (Always update if active, not just in inventory? Usually HUD is always visible)
            // User requested HUD bar, implies always visible when playing.
            if (aniripsaCooldownInterface != null)
            {
                 Player player = Main.LocalPlayer;
                 if (player != null && player.active)
                 {
                     WakfuPlayer wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
                     if (wakfuPlayer.claseElegida == WakfuClase.Aniripsa)
                     {
                         aniripsaCooldownInterface.Update(gameTime);
                     }
                 }
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Inventory"));
            if (inventoryIndex != -1)
            {
                // Balance Toggle Layer
                layers.Insert(inventoryIndex + 1, new LegacyGameInterfaceLayer(
                    "WakfuMod: Balance Toggle",
                    delegate
                    {
                        if (Main.playerInventory)
                        {
                            Player player = Main.LocalPlayer;
                            if (player != null && player.active)
                            {
                                WakfuPlayer wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
                                bool hideUI = wakfuPlayer.claseElegida == WakfuClase.Xelor || 
                                              wakfuPlayer.claseElegida == WakfuClase.Hipermago || 
                                              wakfuPlayer.claseElegida == WakfuClase.Uginak ||
                                              wakfuPlayer.claseElegida == WakfuClase.Aniripsa;

                                if (!hideUI)
                                {
                                    balanceToggleInterface.Draw(Main.spriteBatch, new GameTime());
                                }
                            }
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
            
            // Resource Bars Layer (Generic layer for HUDs)
            int resourceBarIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Resource Bars"));
            if (resourceBarIndex != -1) 
            {
                 layers.Insert(resourceBarIndex + 1, new LegacyGameInterfaceLayer(
                    "WakfuMod: Aniripsa Cooldown",
                    delegate
                    {
                        Player player = Main.LocalPlayer;
                        if (player != null && player.active && !player.dead)
                        {
                            WakfuPlayer wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
                            if (wakfuPlayer.claseElegida == WakfuClase.Aniripsa)
                            {
                                aniripsaCooldownInterface.Draw(Main.spriteBatch, new GameTime());
                            }
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
            }
        }
    }

    public class BalanceToggleUI : UIState
    {
        public UIPanel toggleButton;
        public UIText toggleText;

        // Variables para arrastrar
        private bool dragging = false;
        private Vector2 offset;
        private bool isLeftDrag = false;
        private Vector2 dragStartPos;

        public override void OnInitialize()
        {
            toggleButton = new UIPanel();
            toggleButton.SetPadding(0);
            toggleButton.Width.Set(30, 0f);
            toggleButton.Height.Set(30, 0f);
            toggleButton.BackgroundColor = Color.Red;
            toggleButton.BorderColor = Color.Black;
            
            // Posicionamiento inicial (se ajustará en Update para seguir la basura)
            toggleButton.Left.Set(370, 0f);
            toggleButton.Top.Set(300, 0f);

            // --- Eventos de Arrastre y Clic ---
            // Reemplazamos OnLeftClick simple por lógica de arrastre/clic
            toggleButton.OnLeftMouseDown += (evt, element) => {
                offset = new Vector2(evt.MousePosition.X - toggleButton.Left.Pixels, evt.MousePosition.Y - toggleButton.Top.Pixels);
                dragStartPos = evt.MousePosition;
                dragging = true;
                isLeftDrag = true;
            };
            
            toggleButton.OnLeftMouseUp += (evt, element) => {
                dragging = false;
                // Si soltamos el clic izquierdo y no nos hemos movido mucho, es un clic normal -> Toggle
                if (isLeftDrag && Vector2.Distance(evt.MousePosition, dragStartPos) < 5f)
                {
                    ToggleButton_OnClick(evt, element);
                }
                isLeftDrag = false;
            };
            
            // Arrastre con clic derecho
            toggleButton.OnRightMouseDown += (evt, element) => {
                offset = new Vector2(evt.MousePosition.X - toggleButton.Left.Pixels, evt.MousePosition.Y - toggleButton.Top.Pixels);
                dragging = true;
                isLeftDrag = false;
            };
            
            toggleButton.OnRightMouseUp += (evt, element) => {
                dragging = false;
            };

            Append(toggleButton);

            // --- Texto encima del botón ---
            toggleText = new UIText("");
            toggleText.HAlign = 0.5f; // Centrar horizontalmente respecto al botón
            toggleText.Top.Set(-25f, 0f); // Posicionar encima
            toggleButton.Append(toggleText);
        }

        private void ToggleButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            Player player = Main.LocalPlayer;
            WakfuPlayer wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
            wakfuPlayer.ToggleBalanceMode();
            SoundEngine.PlaySound(Terraria.ID.SoundID.MenuTick);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Lógica de arrastre
            if (dragging)
            {
                toggleButton.Left.Set(Main.MouseScreen.X - offset.X, 0f);
                toggleButton.Top.Set(Main.MouseScreen.Y - offset.Y, 0f);
                Recalculate();
            }

            // Actualizar color y texto según el estado
            Player player = Main.LocalPlayer;
            WakfuPlayer wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
            
            // --- NUEVO: Check para deshabilitar visualmente ---
            if (wakfuPlayer.claseElegida == WakfuClase.Xelor || wakfuPlayer.claseElegida == WakfuClase.Hipermago)
            {
                 toggleButton.BackgroundColor = Color.Gray;
                 toggleText.SetText("Disabled");
                 return; 
            }
            
            if (wakfuPlayer.BalanceMode)
            {
                toggleButton.BackgroundColor = Color.Green;
                toggleText.SetText("Vanilla dmg");
            }
            else
            {
                toggleButton.BackgroundColor = Color.Red;
                toggleText.SetText("Weapons deal maxHP% dmg");
            }
        }
    }
    
    public class AniripsaCooldownUI : UIState
    {
        public UIPanel barContainer;
        public UIPanel barFill;
        public UIText barText;

        // Variables de arrastre
        private bool dragging = false;
        private Vector2 offset;

        public override void OnInitialize()
        {
            // Container
            barContainer = new UIPanel();
            barContainer.SetPadding(0);
            barContainer.Width.Set(150, 0f);
            barContainer.Height.Set(24, 0f);
            barContainer.BackgroundColor = new Color(30, 30, 30, 200);
            barContainer.BorderColor = Color.Black;
            
            // Position (center bottomish)
            barContainer.Left.Set(Main.screenWidth / 2f - 75f, 0f); 
            barContainer.Top.Set(Main.screenHeight - 120f, 0f);
            
            // --- Drag Logic ---
            barContainer.OnLeftMouseDown += (evt, element) => {
                // Solo permitir arrastrar si el inventario está abierto
                if (Main.playerInventory)
                {
                    offset = new Vector2(evt.MousePosition.X - barContainer.Left.Pixels, evt.MousePosition.Y - barContainer.Top.Pixels);
                    dragging = true;
                }
            };

            barContainer.OnLeftMouseUp += (evt, element) => {
                dragging = false;
            };
            
            Append(barContainer);
            
            // Fill
            barFill = new UIPanel();
            barFill.SetPadding(0);
            barFill.Width.Set(0, 0f); // Dynamic
            barFill.Height.Set(24, 0f);
            barFill.BackgroundColor = new Color(50, 255, 50, 200); // Green
            barFill.BorderColor = Color.Transparent;
            barContainer.Append(barFill);
            
            // Text
            barText = new UIText("Explosion Ready");
            barText.HAlign = 0.5f;
            barText.VAlign = 0.5f;
            barContainer.Append(barText);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            // Drag Update
            if (dragging)
            {
                if (Main.playerInventory)
                {
                     barContainer.Left.Set(Main.MouseScreen.X - offset.X, 0f);
                     barContainer.Top.Set(Main.MouseScreen.Y - offset.Y, 0f);
                     Recalculate();
                }
                else
                {
                    dragging = false; // Stop dragging if inventory closes
                }
            }
            
            Player player = Main.LocalPlayer;
            WakfuPlayer wakfuPlayer = player.GetModPlayer<WakfuPlayer>();
            
            // Max Cooldown = 600 ticks
            int maxCD = 600;
            int currentCD = wakfuPlayer.aniripsaAbility2Cooldown;
            
            float progress = 1f - ((float)currentCD / maxCD);
            if (progress < 0f) progress = 0f;
            if (progress > 1f) progress = 1f;
            
            // Update Fill Width
            barFill.Width.Set(150 * progress, 0f);
            
            // Update Text
            if (currentCD <= 0)
            {
                barText.SetText("Explosion READY");
                barFill.BackgroundColor = new Color(50, 255, 50, 200); // Bright Green
            }
            else
            {
                float secondsLeft = currentCD / 60f;
                //barText.SetText($"Explosion: {secondsLeft:F1}s"); // Commented out to reduce diff changes or keep same?
                // Wait, replacement content must be complete.
                // Keeping original text logic
                barText.SetText($"Explosion: {secondsLeft:F1}s");
                barFill.BackgroundColor = new Color(200, 50, 50, 200); // Reddish while cooling down?
            }
             
            // Recalculate called by drag logic. 
        }
    }

}
